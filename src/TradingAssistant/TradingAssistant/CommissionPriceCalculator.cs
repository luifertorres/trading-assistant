namespace TradingAssistant
{
    public static class CommissionPriceCalculator
    {
        public static decimal CalculateStopLossPriceBeforeFees(decimal entryPrice, decimal quantity, decimal roi, int leverage)
        {
            var sign = decimal.Sign(quantity);
            var notional = entryPrice * Math.Abs(quantity);
            var margin = notional / leverage;
            var moneyToLose = margin * roi / 100;
            var stopLossPrice = (notional - (sign * moneyToLose)) / Math.Abs(quantity);
            var entryCommissionCost = notional * 0.05m / 100;
            var entryCommissionPriceDistance = entryCommissionCost / quantity;
            var stopLossPriceBeforeEntryCommission = stopLossPrice + entryCommissionPriceDistance;
            var lossBeforeFees = 1 - sign * 0.05m / 100;
            var stopLossPriceBeforeTotalFees = stopLossPriceBeforeEntryCommission / lossBeforeFees;

            return stopLossPriceBeforeTotalFees;
        }

        public static decimal CalculateTakeProfitPriceBeforeFees(decimal entryPrice, decimal quantity, decimal roi, int leverage)
        {
            var sign = decimal.Sign(quantity);
            var notional = entryPrice * Math.Abs(quantity);
            var margin = notional / leverage;
            var profit = margin * roi / 100;
            var takeProfitPrice = (notional + (sign * profit)) / Math.Abs(quantity);
            var entryCommissionCost = notional * 0.05m / 100;
            var entryCommissionPriceDistance = entryCommissionCost / quantity;
            var takeProfitPriceBeforeEntryCommission = takeProfitPrice + entryCommissionPriceDistance;
            var profitBeforeExitCommission = 1 - (sign * 0.05m / 100);
            var takeProfitPriceBeforeTotalFees = takeProfitPriceBeforeEntryCommission / profitBeforeExitCommission;

            return takeProfitPriceBeforeTotalFees;
        }
    }
}
