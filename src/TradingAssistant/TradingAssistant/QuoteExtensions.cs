using Skender.Stock.Indicators;

namespace TradingAssistant
{
    internal static class QuoteExtensions
    {
        internal static IEnumerable<(DateTime date, double value)> UseAmplitude(this IEnumerable<Quote> quotes)
        {
            static (DateTime Date, double) ToAmplitude(Quote quote)
            {
                var amplitude = quote.Close > quote.Open
                    ? (quote.High / quote.Low) - 1
                    : (quote.Low / quote.High) - 1;

                return (quote.Date, (double)amplitude);
            }

            return quotes.Select(ToAmplitude);
        }
    }
}
