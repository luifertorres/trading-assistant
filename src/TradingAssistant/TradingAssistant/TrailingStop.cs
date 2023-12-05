namespace TradingAssistant
{
    public class TrailingStop(decimal entryPrice, decimal quantity, int leverage)
    {
        private const decimal Step = 100m;
        private readonly decimal _entryPrice = entryPrice;
        private readonly decimal _quantity = quantity;
        private readonly int _leverage = leverage;
        private int _multiplier;

        public bool TryAdvance(decimal currentPrice, out decimal? stopPrice)
        {
            stopPrice = default;

            var sign = decimal.Sign(_quantity);
            var pnlPercentage = sign * ((currentPrice / _entryPrice) - 1) * 100;
            var roi = pnlPercentage * _leverage;

            if (roi < Step)
            {
                return false;
            }

            var remainder = roi % Step;
            var multiplier = (int)((roi - remainder) / Step);

            if (multiplier <= _multiplier)
            {
                return false;
            }

            var target = Step * (multiplier - 1);
            var newStopPrice = _entryPrice * (1 + (sign * target / _leverage / 100));

            stopPrice = newStopPrice;
            _multiplier = multiplier;

            return true;
        }
    }
}
