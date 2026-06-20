using CandlestickData.Application.Contracts;
using CandlestickData.Application.Interfaces;
using CandlestickData.Application.Services;
using CandlestickData.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CandlestickData.Application.Commands;

public sealed class RemediateGapCommandHandler(
    IExchangeDataSource exchangeDataSource,
    ICandlestickRepository candlestickRepository,
    ISymbolIntegrityRepository integrityRepository,
    IntegrityCheckService integrityCheckService,
    ILogger<RemediateGapCommandHandler> logger)
    : IRequestHandler<RemediateGapCommand, SyncCommandResponse>
{
    public async Task<SyncCommandResponse> Handle(
        RemediateGapCommand request,
        CancellationToken cancellationToken)
    {
        var integrity = await integrityRepository.GetAsync(
            request.Symbol, request.TimeFrame, cancellationToken);

        if (integrity is null)
        {
            return new SyncCommandResponse(Guid.Empty, "NotFound", "No integrity record found.");
        }

        integrity.MarkRecovering(DateTime.UtcNow);
        await integrityRepository.SaveAsync(integrity, cancellationToken);

        logger.LogInformation("Remediating gap for {Symbol}/{TimeFrame} from {From} to {To}",
            request.Symbol, request.TimeFrame.ToShortString(), request.GapFrom, request.GapTo);

        var startTime = request.GapFrom;
        while (startTime < request.GapTo && !cancellationToken.IsCancellationRequested)
        {
            var klines = await exchangeDataSource.GetKlinesAsync(
                request.Symbol, request.TimeFrame, startTime, request.GapTo, 1000, cancellationToken);

            if (klines.Count == 0)
                break;

            var valid = klines.Where(c => c.IsValid()).ToList();
            if (valid.Count > 0)
            {
                await candlestickRepository.UpsertManyAsync(valid, cancellationToken);
                startTime = valid[^1].OpenTime + request.TimeFrame.ToTimeSpan();
            }
            else
            {
                break;
            }
        }

        await integrityCheckService.VerifyAndRestoreEligibilityAsync(
            request.Symbol, request.TimeFrame, request.GapFrom, request.GapTo, cancellationToken);

        return new SyncCommandResponse(Guid.Empty, "Completed", "Gap remediation completed.");
    }
}
