namespace TradingAssistant
{
    internal static class TakeProfitPrice
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
            var profit = notional * offset / 100;
            var takeProfitPrice = (notional + (sign * profit)) / Math.Abs(quantity);
            var entryCommissionCost = notional * 0.05m / 100;
            var entryCommissionPriceDistance = entryCommissionCost / quantity;
            var takeProfitPriceBeforeEntryCommission = takeProfitPrice + entryCommissionPriceDistance;
            var profitBeforeExitCommission = 1 - (sign * 0.05m / 100);
            var takeProfitPriceBeforeTotalFees = takeProfitPriceBeforeEntryCommission / profitBeforeExitCommission;

            return includeFees ? takeProfitPriceBeforeTotalFees : takeProfitPrice;
        }
    }
}
