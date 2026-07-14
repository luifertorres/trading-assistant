namespace WebSocketTrading;

public sealed class CandleBuffer
{
    public CandleBuffer(int capacity) => _capacity = capacity;

    public IReadOnlyList<Candle> Candles => _candles;

    public void Add(Candle candle)
    {
        if (_candles.Count == _capacity)
            _candles.RemoveAt(0);

        _candles.Add(candle);
    }

    public void AddRange(IEnumerable<Candle> candles)
    {
        foreach (var candle in candles)
            Add(candle);
    }

    private readonly int _capacity;
    private readonly List<Candle> _candles = [];
}
