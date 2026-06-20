using MarketData.Domain;
using TradingPlatform.Kernel;

namespace MarketData.Application;

public interface IInstrumentRegistry
{
    Task<InstrumentId> UpsertAsync(InstrumentUpsert upsert, CancellationToken cancellationToken = default);

    Task<Instrument?> GetByIdAsync(InstrumentId id, CancellationToken cancellationToken = default);

    Task<Instrument?> GetByExchangeSymbolAsync(
        string venue,
        string market,
        string contractType,
        string exchangeSymbol,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Instrument>> ListAllAsync(CancellationToken cancellationToken = default);
}
