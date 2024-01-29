using Binance.Net.Interfaces;

namespace TradingAssistant
{
    public class CandleClosedProvider : IObservable<CircularTimeSeries<string, IBinanceKline>>
    {
        private readonly List<IObserver<CircularTimeSeries<string, IBinanceKline>>> _observers = [];

        public IDisposable Subscribe(IObserver<CircularTimeSeries<string, IBinanceKline>> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber(_observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<CircularTimeSeries<string, IBinanceKline>>> _observers;
            private readonly IObserver<CircularTimeSeries<string, IBinanceKline>> _observer;

            public Unsubscriber(List<IObserver<CircularTimeSeries<string, IBinanceKline>>> observers,
                IObserver<CircularTimeSeries<string, IBinanceKline>> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                {
                    _observers.Remove(_observer);
                }
            }
        }

        public void Update(CircularTimeSeries<string, IBinanceKline> candlestick)
        {
            foreach (var observer in _observers)
            {
                observer.OnNext(candlestick);
            }
        }

        public void EndTransmission()
        {
            foreach (var observer in _observers.ToArray())
            {
                if (_observers.Contains(observer))
                {
                    observer.OnCompleted();
                }
            }

            _observers.Clear();
        }
    }
}
