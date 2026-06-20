namespace TradingAssistant
{
    [Serializable]
    public class BinanceServiceNotConfiguredException : Exception
    {
        public BinanceServiceNotConfiguredException()
        {
        }

        public BinanceServiceNotConfiguredException(string? message) : base(message)
        {
        }

        public BinanceServiceNotConfiguredException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}


