namespace TradingAssistant
{
    public class CandleClosedProvider : IObservable<CandleId>
    {
        private readonly List<IObserver<CandleId>> _observers = [];

        public IDisposable Subscribe(IObserver<CandleId> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber(_observers, observer);
        }

        private class Unsubscriber(List<IObserver<CandleId>> observers, IObserver<CandleId> observer) : IDisposable
        {
            public void Dispose()
            {
                if (observer != null && observers.Contains(observer))
                {
                    observers.Remove(observer);
                }
            }
        }

        public void Update(CandleId candleId)
        {
            foreach (var observer in _observers)
            {
                observer.OnNext(candleId);
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
