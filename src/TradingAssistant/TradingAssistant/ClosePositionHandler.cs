using MediatR;

namespace TradingAssistant;

public class ClosePositionHandler : IRequestHandler<ClosePositionRequest, bool>
{
    private readonly ILogger<ClosePositionHandler> _logger;
    private readonly BinanceService _binance;

    public ClosePositionHandler(ILogger<ClosePositionHandler> logger, BinanceService binance)
    {
        _logger = logger;
        _binance = binance;
    }

    public async Task<bool> Handle(ClosePositionRequest request, CancellationToken cancellationToken)
    {
        var position = await _binance.TryGetPositionInformationAsync(request.Symbol, cancellationToken);

        if (position is null || position.Quantity == 0)
        {
            _logger.LogDebug("No open position detected for {Symbol}", request.Symbol);
            return false;
        }

        var closed = await _binance.TryClosePositionAtMarketAsync(request.Symbol,
            position.Quantity,
            cancellationToken);

        if (closed)
        {
            _logger.LogInformation("{Symbol} position closed due to indicator condition", request.Symbol);
        }

        return closed;
    }
}


