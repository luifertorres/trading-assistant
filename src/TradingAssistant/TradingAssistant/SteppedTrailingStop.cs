namespace TradingAssistant
{
    public class SteppedTrailingStop
    {
        private readonly decimal _entryPrice;
        private readonly decimal _quantity;
        private readonly int _leverage;
        private readonly decimal _stepRoi;
        private readonly decimal _detectionOffsetRoi;
        private decimal _stopPrice;
        private decimal _highestRoi;
        private decimal _stopRoiMultiplier;

        public SteppedTrailingStop(decimal entryPrice,
            decimal quantity,
            int leverage,
            decimal stepRoi = 100,
            decimal detectionOffsetRoi = 50)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepRoi);
            ArgumentOutOfRangeException.ThrowIfNegative(detectionOffsetRoi);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(detectionOffsetRoi, stepRoi);

            _entryPrice = entryPrice;
            _quantity = quantity;
            _leverage = leverage;
            _stepRoi = stepRoi;
            _detectionOffsetRoi = detectionOffsetRoi;
        }

        public bool TryAdvance(decimal currentPrice, out decimal stopPrice)
        {
            stopPrice = _stopPrice;

            var sign = decimal.Sign(_quantity);
            var pnlPercentage = sign * ((currentPrice / _entryPrice) - 1) * 100;
            var roi = pnlPercentage * _leverage;

            if (roi < _detectionOffsetRoi)
            {
                return false;
            }

            if (roi <= _highestRoi)
            {
                return false;
            }

            _highestRoi = roi;

            var remainder = roi % _stepRoi;
            var multiplier = (roi - remainder) / _stepRoi;

            _stopRoiMultiplier = remainder < _detectionOffsetRoi ? multiplier - 1 : multiplier;

            var target = _stepRoi * _stopRoiMultiplier;

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
