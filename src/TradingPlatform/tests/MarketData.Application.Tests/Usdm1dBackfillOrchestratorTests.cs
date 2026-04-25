using System.Text.Json;
using FluentAssertions;
using MarketData.Application;
using Microsoft.Extensions.Logging.Abstractions;
using TradingPlatform.Kernel;

namespace MarketData.Application.Tests;

public sealed class Usdm1dBackfillOrchestratorTests
{
    private static readonly JsonSerializerOptions CheckpointJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public async Task RunAsync_WhenOneSymbolFails_ContinuesAndRecordsFailure()
    {
        var failingSymbol = "FAILUSDT";
        var healthySymbol = "BTCUSDT";
        var exchange = new FakeExchange([failingSymbol, healthySymbol]);
        exchange.OnGetDailyKlinesPageAsync = symbol =>
            symbol == failingSymbol
                ? Task.FromException<IReadOnlyList<OhlcBar>>(new InvalidOperationException("synthetic failure"))
                : Task.FromResult<IReadOnlyList<OhlcBar>>([Bar(0)]);
        var writer = new RecordingWriter();
        var checkpointStore = new InMemoryCheckpointStore();
        var orchestrator = CreateOrchestrator(writer, exchange, checkpointStore);

        await orchestrator.RunAsync(Options());

        writer.WrittenSymbols.Should().ContainSingle().Which.Should().Be(healthySymbol);
        var checkpoint = checkpointStore.SavedDocuments.Last();
        var failedEntry = checkpoint.Symbols.Single(e => e.Symbol == failingSymbol);
        failedEntry.Complete.Should().BeFalse();
        failedEntry.LastErrorMessage.Should().Be("synthetic failure");
        failedEntry.LastErrorAtUtc.Should().NotBeNull();
        checkpoint.Symbols.Single(e => e.Symbol == healthySymbol).Complete.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WhenPreviouslyFailingSymbolCompletes_ClearsErrorFields()
    {
        var symbol = "BTCUSDT";
        var checkpoint = new BackfillCheckpointDocumentV1
        {
            RunId = Guid.NewGuid(),
            MarketDatabasePath = Path.GetFullPath(Options().MarketDatabasePath),
            Symbols =
            [
                new BackfillCheckpointSymbolEntryV1
                {
                    Symbol = symbol,
                    LastErrorMessage = "previous failure",
                    LastErrorAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
                }
            ]
        };
        var exchange = new FakeExchange([symbol])
        {
            OnGetDailyKlinesPageAsync = _ => Task.FromResult<IReadOnlyList<OhlcBar>>([Bar(0)])
        };
        var checkpointStore = new InMemoryCheckpointStore(checkpoint);
        var orchestrator = CreateOrchestrator(new RecordingWriter(), exchange, checkpointStore);

        await orchestrator.RunAsync(Options());

        var entry = checkpointStore.SavedDocuments.Last().Symbols.Single();
        entry.Complete.Should().BeTrue();
        entry.LastErrorMessage.Should().BeNull();
        entry.LastErrorAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_WhenCanceled_PropagatesAndDoesNotRecordFailure()
    {
        var symbol = "BTCUSDT";
        var checkpoint = new BackfillCheckpointDocumentV1
        {
            RunId = Guid.NewGuid(),
            MarketDatabasePath = Path.GetFullPath(Options().MarketDatabasePath),
            Symbols = [new BackfillCheckpointSymbolEntryV1 { Symbol = symbol }]
        };
        var exchange = new FakeExchange([symbol])
        {
            OnGetDailyKlinesPageAsync = _ => Task.FromException<IReadOnlyList<OhlcBar>>(new OperationCanceledException())
        };
        var checkpointStore = new InMemoryCheckpointStore(checkpoint);
        var orchestrator = CreateOrchestrator(new RecordingWriter(), exchange, checkpointStore);

        await FluentActions
            .Awaiting(() => orchestrator.RunAsync(Options()))
            .Should()
            .ThrowAsync<OperationCanceledException>();

        var entry = checkpoint.Symbols.Single();
        entry.Complete.Should().BeFalse();
        entry.LastErrorMessage.Should().BeNull();
        entry.LastErrorAtUtc.Should().BeNull();
    }

    [Fact]
    public void BackfillCheckpointSymbolEntryV1_DeserializesOldCheckpointWithNullErrorFields()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "runId": "11111111-1111-1111-1111-111111111111",
              "updatedAtUtc": "2026-04-17T00:00:00+00:00",
              "marketDatabasePath": "C:\\data\\market.sqlite",
              "symbols": [
                {
                  "symbol": "BTCUSDT",
                  "complete": false,
                  "lastWrittenOpenTimeMs": 1713312000000
                }
              ]
            }
            """;

        var checkpoint = JsonSerializer.Deserialize<BackfillCheckpointDocumentV1>(json, CheckpointJsonOptions);

        checkpoint.Should().NotBeNull();
        var entry = checkpoint!.Symbols.Single();
        entry.LastErrorMessage.Should().BeNull();
        entry.LastErrorAtUtc.Should().BeNull();
    }

    [Fact]
    public void BackfillCheckpointSymbolEntryV1_OmitsNullErrorFields()
    {
        var checkpoint = new BackfillCheckpointDocumentV1
        {
            RunId = Guid.NewGuid(),
            MarketDatabasePath = "market.sqlite",
            Symbols = [new BackfillCheckpointSymbolEntryV1 { Symbol = "BTCUSDT" }]
        };

        var json = JsonSerializer.Serialize(checkpoint, CheckpointJsonOptions);

        json.Should().NotContain("lastErrorMessage");
        json.Should().NotContain("lastErrorAtUtc");
    }

    private static Usdm1dBackfillOrchestrator CreateOrchestrator(
        ICandleSeriesWriter writer,
        IUsdM1dBackfillExchange exchange,
        IBackfillCheckpointStore checkpointStore) =>
        new(writer, exchange, checkpointStore, NullLogger<Usdm1dBackfillOrchestrator>.Instance);

    private static Usdm1dBackfillRunOptions Options() =>
        new(
            Path.Combine(Path.GetTempPath(), "market.sqlite"),
            Path.Combine(Path.GetTempPath(), "trading-platform-test-data"),
            null,
            false);

    private static OhlcBar Bar(long openTimeMs)
    {
        var openTime = DateTimeOffset.FromUnixTimeMilliseconds(openTimeMs);
        return new OhlcBar(openTime, openTime.AddDays(1), 1m, 2m, 0.5m, 1.5m, 100m);
    }

    private sealed class FakeExchange(IReadOnlyList<string> symbols) : IUsdM1dBackfillExchange
    {
        public Func<string, Task<IReadOnlyList<OhlcBar>>> OnGetDailyKlinesPageAsync { get; set; } =
            _ => Task.FromResult<IReadOnlyList<OhlcBar>>([]);

        public Task<IReadOnlyList<string>> GetActiveUsdtPerpetualSymbolsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(symbols);

        public Task<IReadOnlyList<OhlcBar>> GetDailyKlinesPageAsync(
            string symbol,
            DateTimeOffset startTimeInclusive,
            DateTimeOffset endTimeInclusive,
            CancellationToken cancellationToken = default) =>
            OnGetDailyKlinesPageAsync(symbol);

        public Task WriteExchangeInfoSnapshotAsync(string dataRoot, Guid runId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RecordingWriter : ICandleSeriesWriter
    {
        public List<string> WrittenSymbols { get; } = [];

        public Task UpsertAsync(SeriesDescriptor series, IReadOnlyList<OhlcBar> bars, CancellationToken cancellationToken = default)
        {
            WrittenSymbols.Add(series.Symbol);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCheckpointStore(BackfillCheckpointDocumentV1? initial = null) : IBackfillCheckpointStore
    {
        public List<BackfillCheckpointDocumentV1> SavedDocuments { get; } = [];

        public Task<BackfillCheckpointDocumentV1?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(initial);

        public Task SaveAsync(BackfillCheckpointDocumentV1 document, CancellationToken cancellationToken = default)
        {
            SavedDocuments.Add(Clone(document));
            return Task.CompletedTask;
        }

        private static BackfillCheckpointDocumentV1 Clone(BackfillCheckpointDocumentV1 document)
        {
            var json = JsonSerializer.Serialize(document, CheckpointJsonOptions);
            return JsonSerializer.Deserialize<BackfillCheckpointDocumentV1>(json, CheckpointJsonOptions)!;
        }
    }
}
