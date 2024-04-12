using Binance.Net.Enums;
using MediatR;

namespace TradingAssistant
{
    public class TradeHandler : IRequestHandler<TradeRequest, bool>
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _factory;
        private readonly BinanceService _binance;

        public TradeHandler(IConfiguration configuration, IServiceScopeFactory factory, BinanceService binance)
        {
            _configuration = configuration;
            _factory = factory;
            _binance = binance;
        }

        public async Task<bool> Handle(TradeRequest trade, CancellationToken cancellationToken)
        {
            using (var database = _factory.CreateScope().ServiceProvider.GetRequiredService<TradingContext>())
            {
                if (database.OpenPositions.Any(p => p.Symbol == trade.Symbol))
                {
                    return false;
                }

                if (database.OpenPositions.Any(p => !p.HasStopLossInBreakEven))
                {
                    return false;
                }
            }

            if (!_binance.TryGetLeverage(trade.Symbol, out var leverage))
            {
                return false;
            }

            var account = await _binance.TryGetAccountInformationAsync(cancellationToken);

            if (account is null)
            {
                return false;
            }

            var accountMarginPercentage = _configuration.GetValue<decimal>("Binance:RiskManagement:AccountMarginPercentage");
            var accountPercentageForEntry = accountMarginPercentage * leverage;
            var notional = account.AvailableBalance * accountPercentageForEntry / 100;
            var quantity = notional / trade.EntryPrice;

            return await _binance.TryPlaceEntryOrderAsync(trade.Symbol,
                trade.Side,
                FuturesOrderType.Market,
                quantity,
                trade.EntryPrice,
                cancellationToken);
        }
    }
}
