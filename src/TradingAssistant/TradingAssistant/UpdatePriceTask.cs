namespace TradingAssistant
{
    public class UpdatePriceTask
    {
        private readonly Func<decimal, CancellationToken, Task> _action;
        private decimal _price;
        private readonly Task _task;
        private CancellationTokenSource? _cancellationTokenSource;

        public UpdatePriceTask(decimal entryPrice, Func<decimal, CancellationToken, Task> action)
        {
            _price = entryPrice;
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _cancellationTokenSource = new CancellationTokenSource();
            _task = Task.Factory.StartNew(RunActionAsync, TaskCreationOptions.LongRunning);
        }

        private async Task RunActionAsync()
        {
            while (_cancellationTokenSource is not null)
            {
                try
                {
                    if (_cancellationTokenSource?.Token.IsCancellationRequested ?? default)
                    {
                        break;
                    }

                    await _action(_price, _cancellationTokenSource?.Token ?? default);
                    await Task.Delay(1_000);
                }
                catch
                {
                }
            }
        }

        public void UpdatePrice(decimal price)
        {
            _price = price;
        }

        public void Stop()
        {
            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _task.Wait();
                _task.Dispose();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}
