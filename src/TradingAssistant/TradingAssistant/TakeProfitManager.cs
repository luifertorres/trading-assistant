﻿using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;

namespace TradingAssistant
{
    public class TakeProfitManager : BackgroundService
    {
        private readonly BinanceService _binanceService;
        private readonly IConfiguration _configuration;

        public TakeProfitManager(IConfiguration configuration, BinanceService binanceService)
        {
            _configuration = configuration;
            _binanceService = binanceService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _binanceService.SubscribeToAccountUpdates(@event => HandleAccountUpdate(@event, stoppingToken));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async void HandleAccountUpdate(DataEvent<BinanceFuturesStreamAccountUpdate> @event, CancellationToken cancellationToken = default)
        {
            foreach (var position in @event.Data.UpdateData.Positions)
            {
                if (position.EntryPrice != 0 && position.Quantity != 0)
                {
                    await UpdateTakeProfitAsync(position, cancellationToken);
                }
                else
                {
                    await _binanceService.CancelAllOrdersAsync(position.Symbol, cancellationToken);
                }
            }
        }

        private async Task UpdateTakeProfitAsync(BinanceFuturesStreamPosition position, CancellationToken cancellationToken = default)
        {
            var roi = _configuration.GetValue<decimal>("Binance:RiskManagement:TakeProfitRoi");
            var isTakeProfitPlaced = await _binanceService.TryPlaceTakeProfitAsync(position.Symbol,
                position.EntryPrice,
                position.Quantity,
                roi,
                cancellationToken);

            if (!isTakeProfitPlaced)
            {
                await _binanceService.TryCancelTakeProfitAsync(position.Symbol, cancellationToken);
                await _binanceService.TryPlaceTakeProfitAsync(position.Symbol,
                    position.EntryPrice,
                    position.Quantity,
                    roi,
                    cancellationToken);
            }
        }
    }
}
