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

    private static readonly InstrumentId FailingInstrumentId = new(1);
    private static readonly InstrumentId HealthyInstrumentId = new(2);

    [Fact]
    public async Task RunAsync_WhenOneInstrumentFails_ContinuesAndRecordsFailure()
    {
        var failingSymbol = "FAILUSDT";
        var healthySymbol = "BTCUSDT";
        var exchange = new FakeExchange(
        [
            Listing(failingSymbol, FailingInstrumentId),
            Listing(healthySymbol, HealthyInstrumentId)
        ]);
        exchange.OnGetDailyKlinesPageAsync = handle =>
            BrokerFetchHandleUnwrapForTests.Symbol(handle) == failingSymbol
                ? Task.FromException<IReadOnlyList<OhlcBar>>(new InvalidOperationException("synthetic failure"))
                : Task.FromResult<IReadOnlyList<OhlcBar>>([Bar(0)]);
        var writer = new RecordingWriter();
        var registry = new FakeRegistry(
        [
            Instrument(FailingInstrumentId, failingSymbol),
            Instrument(HealthyInstrumentId, healthySymbol)
        ]);
        var checkpointStore = new InMemoryCheckpointStore();
        var orchestrator = CreateOrchestrator(writer, exchange, registry, checkpointStore);

        await orchestrator.RunAsync(Options());

        writer.WrittenInstrumentIds.Should().ContainSingle().Which.Should().Be(HealthyInstrumentId);
        var checkpoint = checkpointStore.SavedDocuments.Last();
        var failedEntry = checkpoint.Instruments.Single(e => e.InstrumentId == FailingInstrumentId.Value);
        failedEntry.Complete.Should().BeFalse();
        failedEntry.LastErrorMessage.Should().Be("synthetic failure");
        failedEntry.LastErrorAtUtc.Should().NotBeNull();
        checkpoint.Instruments.Single(e => e.InstrumentId == HealthyInstrumentId.Value).Complete.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WhenPreviouslyFailingInstrumentCompletes_ClearsErrorFields()
    {
        var symbol = "BTCUSDT";
        var instrumentId = HealthyInstrumentId;
        var checkpoint = new BackfillCheckpointDocumentV2
        {
            RunId = Guid.NewGuid(),
            MarketDatabasePath = Path.GetFullPath(Options().MarketDatabasePath),
            Instruments =
            [
                new BackfillCheckpointInstrumentEntryV2
                {
                    InstrumentId = instrumentId.Value,
                    LastErrorMessage = "previous failure",
                    LastErrorAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
                }
            ]
        };
        var exchange = new FakeExchange([Listing(symbol, instrumentId)])
        {
            OnGetDailyKlinesPageAsync = _ => Task.FromResult<IReadOnlyList<OhlcBar>>([Bar(0)])
        };
        var registry = new FakeRegistry([Instrument(instrumentId, symbol)]);
        var checkpointStore = new InMemoryCheckpointStore(checkpoint);
        var orchestrator = CreateOrchestrator(new RecordingWriter(), exchange, registry, checkpointStore);

        await orchestrator.RunAsync(Options());

        var entry = checkpointStore.SavedDocuments.Last().Instruments.Single();
        entry.Complete.Should().BeTrue();
        entry.LastErrorMessage.Should().BeNull();
        entry.LastErrorAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_WhenCanceled_PropagatesAndDoesNotRecordFailure()
    {
        var symbol = "BTCUSDT";
        var instrumentId = HealthyInstrumentId;
        var checkpoint = new BackfillCheckpointDocumentV2
        {
            RunId = Guid.NewGuid(),
            MarketDatabasePath = Path.GetFullPath(Options().MarketDatabasePath),
            Instruments = [new BackfillCheckpointInstrumentEntryV2 { InstrumentId = instrumentId.Value }]
        };
        var exchange = new FakeExchange([Listing(symbol, instrumentId)])
        {
            OnGetDailyKlinesPageAsync = _ => Task.FromException<IReadOnlyList<OhlcBar>>(new OperationCanceledException())
        };
        var registry = new FakeRegistry([Instrument(instrumentId, symbol)]);
        var checkpointStore = new InMemoryCheckpointStore(checkpoint);
        var orchestrator = CreateOrchestrator(new RecordingWriter(), exchange, registry, checkpointStore);

        await FluentActions
            .Awaiting(() => orchestrator.RunAsync(Options()))
            .Should()
            .ThrowAsync<OperationCanceledException>();

        var entry = checkpoint.Instruments.Single();
        entry.Complete.Should().BeFalse();
        entry.LastErrorMessage.Should().BeNull();
        entry.LastErrorAtUtc.Should().BeNull();
    }

    [Fact]
    public void BackfillCheckpointJsonStore_IgnoresV1Checkpoint()
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

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(1);
    }

    [Fact]
    public void BackfillCheckpointInstrumentEntryV2_DeserializesCheckpointWithNullErrorFields()
    {
        const string json = """
            {
              "schemaVersion": 2,
              "runId": "11111111-1111-1111-1111-111111111111",
              "updatedAtUtc": "2026-04-17T00:00:00+00:00",
              "marketDatabasePath": "C:\\data\\market.sqlite",
              "instruments": [
                {
                  "instrumentId": 42,
                  "complete": false,
                  "lastWrittenOpenTimeMs": 1713312000000
                }
              ]
            }
            """;

        var checkpoint = JsonSerializer.Deserialize<BackfillCheckpointDocumentV2>(json, CheckpointJsonOptions);

        checkpoint.Should().NotBeNull();
        var entry = checkpoint!.Instruments.Single();
        entry.InstrumentId.Should().Be(42);
        entry.LastErrorMessage.Should().BeNull();
        entry.LastErrorAtUtc.Should().BeNull();
    }

    [Fact]
    public void BackfillCheckpointInstrumentEntryV2_OmitsNullErrorFields()
    {
        var checkpoint = new BackfillCheckpointDocumentV2
        {
            RunId = Guid.NewGuid(),
            MarketDatabasePath = "market.sqlite",
            Instruments = [new BackfillCheckpointInstrumentEntryV2 { InstrumentId = 42 }]
        };

        var json = JsonSerializer.Serialize(checkpoint, CheckpointJsonOptions);

        json.Should().NotContain("lastErrorMessage");
        json.Should().NotContain("lastErrorAtUtc");
    }

    private static UsdmBackfillOrchestrator CreateOrchestrator(
        ICandleSeriesWriter writer,
        IUsdM1dBackfillExchange exchange,
        IInstrumentRegistry registry,
        IBackfillCheckpointStore checkpointStore) =>
        new(writer, exchange, registry, checkpointStore, NullLogger<UsdmBackfillOrchestrator>.Instance);

    private static UsdmBackfillRunOptions Options() =>
        new(
            Path.Combine(Path.GetTempPath(), "market.sqlite"),
            Path.Combine(Path.GetTempPath(), "trading-platform-test-data"),
            null,
            false,
            TimeFrameCode.Day1);

    private static OhlcBar Bar(long openTimeMs)
    {
        var openTime = DateTimeOffset.FromUnixTimeMilliseconds(openTimeMs);
        return new OhlcBar(openTime, openTime.AddDays(1), 1m, 2m, 0.5m, 1.5m, 100m);
    }

    private static UsdMInstrumentListing Listing(string symbol, InstrumentId id) =>
        new(
            new InstrumentUpsert(
                "binance", "usdm", "perpetual", symbol,
                symbol[..^4], "USDT", symbol, 2, 3, "[]", "TRADING", DateTimeOffset.UtcNow),
            new BrokerFetchHandle(symbol));

    private static Domain.Instrument Instrument(InstrumentId id, string symbol) =>
        new()
        {
            Id = id,
            Venue = "binance",
            Market = "usdm",
            ContractType = "perpetual",
            ExchangeSymbol = symbol
        };

    private sealed class FakeExchange(IReadOnlyList<UsdMInstrumentListing> listings) : IUsdM1dBackfillExchange
    {
        public Func<BrokerFetchHandle, Task<IReadOnlyList<OhlcBar>>> OnGetDailyKlinesPageAsync { get; set; } =
            _ => Task.FromResult<IReadOnlyList<OhlcBar>>([]);

        public Task<IReadOnlyList<UsdMInstrumentListing>> ListUsdtPerpetualInstrumentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(listings);

        public Task<IReadOnlyList<OhlcBar>> GetDailyKlinesPageAsync(
            BrokerFetchHandle handle,
            DateTimeOffset startTimeInclusive,
            DateTimeOffset endTimeInclusive,
            CancellationToken cancellationToken = default) =>
            OnGetDailyKlinesPageAsync(handle);

        public Task<IReadOnlyList<OhlcBar>> GetKlinesPageAsync(
            BrokerFetchHandle handle,
            TimeFrameCode timeFrame,
            DateTimeOffset startTimeInclusive,
            DateTimeOffset endTimeInclusive,
            CancellationToken cancellationToken = default) =>
            OnGetDailyKlinesPageAsync(handle);

        public Task WriteExchangeInfoSnapshotAsync(
            string dataRoot,
            Guid runId,
            IReadOnlyDictionary<string, InstrumentId> instrumentIdsByExchangeSymbol,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeRegistry(IReadOnlyList<Domain.Instrument> instruments) : IInstrumentRegistry
    {
        public Task<InstrumentId> UpsertAsync(InstrumentUpsert upsert, CancellationToken cancellationToken = default)
        {
            var match = instruments.FirstOrDefault(i =>
                string.Equals(i.ExchangeSymbol, upsert.ExchangeSymbol, StringComparison.Ordinal));
            return Task.FromResult(match?.Id ?? new InstrumentId(instruments.Count + 1));
        }

        public Task<Domain.Instrument?> GetByIdAsync(InstrumentId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(instruments.FirstOrDefault(i => i.Id == id));

        public Task<Domain.Instrument?> GetByExchangeSymbolAsync(
            string venue,
            string market,
            string contractType,
            string exchangeSymbol,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(instruments.FirstOrDefault(i =>
                string.Equals(i.ExchangeSymbol, exchangeSymbol, StringComparison.Ordinal)));

        public Task<IReadOnlyList<Domain.Instrument>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Domain.Instrument>>(instruments);
    }

    private sealed class RecordingWriter : ICandleSeriesWriter
    {
        public List<InstrumentId> WrittenInstrumentIds { get; } = [];

        public Task UpsertAsync(SeriesDescriptor series, IReadOnlyList<OhlcBar> bars, CancellationToken cancellationToken = default)
        {
            WrittenInstrumentIds.Add(series.Instrument);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCheckpointStore(BackfillCheckpointDocumentV2? initial = null) : IBackfillCheckpointStore
    {
        public List<BackfillCheckpointDocumentV2> SavedDocuments { get; } = [];

        public Task<BackfillCheckpointDocumentV2?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(initial);

        public Task SaveAsync(BackfillCheckpointDocumentV2 document, CancellationToken cancellationToken = default)
        {
            SavedDocuments.Add(Clone(document));
            return Task.CompletedTask;
        }

        private static BackfillCheckpointDocumentV2 Clone(BackfillCheckpointDocumentV2 document)
        {
            var json = JsonSerializer.Serialize(document, CheckpointJsonOptions);
            return JsonSerializer.Deserialize<BackfillCheckpointDocumentV2>(json, CheckpointJsonOptions)!;
        }
    }

    private static class BrokerFetchHandleUnwrapForTests
    {
        public static string Symbol(BrokerFetchHandle handle) => (string)handle.Token;
    }
}
