namespace TradingAssistant
{
    public class TrailingStop(decimal entryPrice, decimal quantity, int leverage)
    {
        private const decimal Step = 100m;
        private const decimal Offset = Step / 2;
        private readonly decimal _entryPrice = entryPrice;
        private readonly decimal _quantity = quantity;
        private readonly int _leverage = leverage;
        private decimal _stopPrice;
        private decimal _highestRoi;
        private decimal _stopRoiMultiplier;

        public bool TryAdvance(decimal currentPrice, out decimal stopPrice)
        {
            stopPrice = _stopPrice;

            var sign = decimal.Sign(_quantity);
            var pnlPercentage = sign * ((currentPrice / _entryPrice) - 1) * 100;
            var roi = pnlPercentage * _leverage;

            if (roi < Offset)
            {
                return false;
            }

            if (roi <= _highestRoi)
            {
                return false;
            }

            _highestRoi = roi;

            var remainder = roi % Step;
            var multiplier = (roi - remainder) / Step;

            _stopRoiMultiplier = remainder < Offset ? multiplier - 1 : multiplier;

            var target = Step * _stopRoiMultiplier;

            stopPrice = _entryPrice * (1 + (sign * target / _leverage / 100));

            if (stopPrice == _stopPrice)
            {
                return false;
            }

            _stopPrice = stopPrice;

            return true;
        }
    }
}
