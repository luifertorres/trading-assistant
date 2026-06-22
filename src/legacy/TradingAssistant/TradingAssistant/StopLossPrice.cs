namespace TradingAssistant
{
    internal static class StopLossPrice
    {
        public static decimal Calculate(decimal entryPrice,
            decimal quantity,
            decimal roi,
            int leverage,
            bool includeFees = false)
        {
            return Calculate(entryPrice, quantity, roi / leverage, includeFees);
        }

        public static decimal Calculate(decimal entryPrice,
            decimal quantity,
            decimal offset,
            bool includeFees = false)
        {
            var sign = decimal.Sign(quantity);
            var notional = entryPrice * Math.Abs(quantity);
            var loss = notional * offset / 100;
            var stopLossPrice = (notional - (sign * loss)) / Math.Abs(quantity);
            var entryCommissionCost = notional * 0.05m / 100;
            var entryCommissionPriceDistance = entryCommissionCost / quantity;
            var stopLossPriceBeforeEntryCommission = stopLossPrice + entryCommissionPriceDistance;
            var lossBeforeFees = 1 - sign * 0.05m / 100;
            var stopLossPriceBeforeTotalFees = stopLossPriceBeforeEntryCommission / lossBeforeFees;

            return includeFees ? stopLossPriceBeforeTotalFees : stopLossPrice;
        }
    }
}
