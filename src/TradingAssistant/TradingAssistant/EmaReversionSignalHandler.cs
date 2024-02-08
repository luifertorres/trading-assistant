using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    public class EmaReversionSignalHandler : INotificationHandler<EmaReversionSignal>
    {
        private const string BtcUsdt = "BTCUSDT";
        private readonly IConfiguration _configuration;
        private readonly BinanceService _binanceService;

        public EmaReversionSignalHandler(IConfiguration configuration, BinanceService binanceService)
        {
            _configuration = configuration;
            _binanceService = binanceService;
        }

        public async Task Handle(EmaReversionSignal signal, CancellationToken cancellationToken)
        {
            await Task.Delay(Random.Shared.Next(maxValue: 1000), cancellationToken);

            var positions = await _binanceService.TryGetPositionsAsync(cancellationToken);

            if (positions.Any(p => p.Symbol == signal.Symbol))
            {
                return;
            }

            if (positions.Any(p => p.Quantity.AsOrderSide() == signal.Side))
            {
                return;
            }

            if (!_binanceService.TryGetLeverage(signal.Symbol, out var leverage))
            {
                return;
            }

            var account = await _binanceService.TryGetAccountInformationAsync(cancellationToken);

            if (account is null)
            {
                return;
            }

            var accountMarginPercentage = _configuration.GetValue<decimal>("Binance:RiskManagement:AccountMarginPercentage");
            var accountPercentageForEntry = accountMarginPercentage * leverage;
            var notional = account.AvailableBalance * accountPercentageForEntry / 100;
            var quantity = notional / signal.EntryPrice;

            await _binanceService.TryPlaceEntryOrderAsync(signal.Symbol,
                signal.Side,
                FuturesOrderType.Market,
                quantity,
                signal.EntryPrice,
                cancellationToken);
        }
    }
}
