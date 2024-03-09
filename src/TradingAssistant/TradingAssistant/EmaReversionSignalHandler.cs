using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    public class EmaReversionSignalHandler : INotificationHandler<EmaReversionSignal>
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _factory;
        private readonly BinanceService _binance;

        public EmaReversionSignalHandler(IConfiguration configuration, IServiceScopeFactory factory, BinanceService binance)
        {
            _configuration = configuration;
            _factory = factory;
            _binance = binance;
        }

        public async Task Handle(EmaReversionSignal signal, CancellationToken cancellationToken)
        {
            using var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>();

            if (database.OpenPositions.Any(p => p.Symbol == signal.Symbol))
            {
                return;
            }

            if (database.OpenPositions.Any(p => !p.HasStopLossInBreakEven))
            {
                return;
            }

            if (!_binance.TryGetLeverage(signal.Symbol, out var leverage))
            {
                return;
            }

            var account = await _binance.TryGetAccountInformationAsync(cancellationToken);

            if (account is null)
            {
                return;
            }

            var accountMarginPercentage = _configuration.GetValue<decimal>("Binance:RiskManagement:AccountMarginPercentage");
            var accountPercentageForEntry = accountMarginPercentage * leverage;
            var notional = account.AvailableBalance * accountPercentageForEntry / 100;
            var quantity = notional / signal.EntryPrice;

            await _binance.TryPlaceEntryOrderAsync(signal.Symbol,
                signal.Side,
                FuturesOrderType.Market,
                quantity,
                signal.EntryPrice,
                cancellationToken);
        }
    }
}
