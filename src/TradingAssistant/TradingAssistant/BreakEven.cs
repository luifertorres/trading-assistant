namespace TradingAssistant
{
    public class BreakEven(decimal entryPrice, decimal quantity, decimal minRoi, int leverage)
    {
        private readonly decimal _entryPrice = entryPrice;
        private readonly decimal _quantity = quantity;
        private readonly decimal _minRoi = minRoi;
        private readonly int _leverage = leverage;

        public bool TryGetPrice(decimal currentPrice, out decimal stopPrice)
        {
            var sign = decimal.Sign(_quantity);
            var pnlPercentage = sign * ((currentPrice / _entryPrice) - 1) * 100;
            var roi = pnlPercentage * _leverage;

            if (roi < _minRoi)
            {
                stopPrice = 0;

                return false;
            }

            stopPrice = TakeProfitPrice.Calculate(_entryPrice, _quantity, roi: 0, leverage: _leverage, includeFees: true);

            return true;
        }
    }
}
