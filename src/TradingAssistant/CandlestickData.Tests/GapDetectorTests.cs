using CandlestickData.Domain;
using FluentAssertions;
using Xunit;

namespace CandlestickData.Tests;

public class GapDetectorTests
{
    [Fact]
    public void DetectGaps_WithConsecutiveCandles_ReturnsEmpty()
    {
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var candles = new List<CandlestickRecord>
        {
            CreateCandle("BTCUSDT", TimeFrame.OneMinute, baseTime),
            CreateCandle("BTCUSDT", TimeFrame.OneMinute, baseTime.AddMinutes(1))
        };

        var gaps = GapDetector.DetectGaps(candles, TimeFrame.OneMinute);

        gaps.Should().BeEmpty();
    }

    [Fact]
    public void DetectGaps_WithMissingCandle_ReturnsGap()
    {
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var candles = new List<CandlestickRecord>
        {
            CreateCandle("BTCUSDT", TimeFrame.OneMinute, baseTime),
            CreateCandle("BTCUSDT", TimeFrame.OneMinute, baseTime.AddMinutes(2))
        };

        var gaps = GapDetector.DetectGaps(candles, TimeFrame.OneMinute);

        gaps.Should().HaveCount(1);
        gaps[0].MissingCandleCount.Should().Be(1);
        gaps[0].FromOpenTime.Should().Be(baseTime);
        gaps[0].ToOpenTime.Should().Be(baseTime.AddMinutes(2));
    }

    [Fact]
    public void Classify_WithoutMaintenanceWindows_ReturnsAtypical()
    {
        var gap = new DetectedGap(
            new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 1, 12, 5, 0, DateTimeKind.Utc),
            5);

        var classification = GapDetector.Classify(gap);

        classification.Should().Be(GapClassification.Atypical);
    }

    [Fact]
    public void Classify_WithMatchingMaintenanceWindow_ReturnsNormal()
    {
        var gapDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var gap = new DetectedGap(gapDate, gapDate.AddMinutes(5), 5);
        var maintenanceWindows = new HashSet<DateTime> { gapDate.Date };

        var classification = GapDetector.Classify(gap, maintenanceWindows);

        classification.Should().Be(GapClassification.Normal);
    }

    private static CandlestickRecord CreateCandle(string symbol, TimeFrame tf, DateTime openTime)
    {
        return new CandlestickRecord
        {
            Symbol = symbol,
            TimeFrame = tf,
            OpenTime = openTime,
            CloseTime = openTime.AddMinutes(1),
            OpenPrice = 50000,
            HighPrice = 50100,
            LowPrice = 49900,
            ClosePrice = 50050,
            Volume = 100
        };
    }
}
