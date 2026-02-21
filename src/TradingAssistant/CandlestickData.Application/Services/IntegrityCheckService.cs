using CandlestickData.Application.Interfaces;
using CandlestickData.Domain;
using Microsoft.Extensions.Logging;

namespace CandlestickData.Application.Services;

public sealed class IntegrityCheckService(
    ICandlestickRepository candlestickRepository,
    ISymbolIntegrityRepository integrityRepository,
    ILogger<IntegrityCheckService> logger)
{
    public async Task CheckAndUpdateIntegrityAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var candles = await candlestickRepository.GetCandlesAsync(
            [symbol], timeFrame, from, to, cancellationToken);

        var gaps = GapDetector.DetectGaps(candles, timeFrame);
        var integrity = await integrityRepository.GetAsync(symbol, timeFrame, cancellationToken)
            ?? SymbolIntegrity.CreateEligible(symbol, timeFrame, DateTime.UtcNow);

        var hasAtypicalGap = false;
        DetectedGap? firstAtypicalGap = null;

        foreach (var gap in gaps)
        {
            var classification = GapDetector.Classify(gap);

            logger.LogInformation(
                "Gap detected for {Symbol}/{TimeFrame}: {From} to {To} ({MissingCount} candles) - {Classification}",
                symbol, timeFrame.ToShortString(), gap.FromOpenTime, gap.ToOpenTime,
                gap.MissingCandleCount, classification);

            if (classification == GapClassification.Atypical)
            {
                hasAtypicalGap = true;
                firstAtypicalGap ??= gap;
            }
        }

        var now = DateTime.UtcNow;

        if (hasAtypicalGap && firstAtypicalGap is not null)
        {
            integrity.MarkCompromised(
                IntegrityReason.AtypicalGap,
                firstAtypicalGap.FromOpenTime,
                firstAtypicalGap.ToOpenTime,
                now);
        }
        else if (candles.Count > 0)
        {
            integrity.MarkEligible(candles[^1].OpenTime, now);
        }

        await integrityRepository.SaveAsync(integrity, cancellationToken);
    }

    public async Task VerifyAndRestoreEligibilityAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime gapFrom,
        DateTime gapTo,
        CancellationToken cancellationToken = default)
    {
        var missingRanges = await candlestickRepository.FindMissingRangesAsync(
            symbol, timeFrame, gapFrom, gapTo, cancellationToken);

        var integrity = await integrityRepository.GetAsync(symbol, timeFrame, cancellationToken);
        if (integrity is null)
            return;

        var now = DateTime.UtcNow;

        if (missingRanges.Count == 0)
        {
            integrity.MarkEligible(gapTo, now);
            logger.LogInformation("Integrity restored to Eligible for {Symbol}/{TimeFrame}", symbol, timeFrame.ToShortString());
        }
        else
        {
            integrity.MarkRecovering(now);
            logger.LogInformation("Integrity still Recovering for {Symbol}/{TimeFrame}, {Count} ranges missing",
                symbol, timeFrame.ToShortString(), missingRanges.Count);
        }

        await integrityRepository.SaveAsync(integrity, cancellationToken);
    }
}
