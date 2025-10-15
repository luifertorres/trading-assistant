using Binance.Net.Enums;
using FASTER.core;

namespace TradingAssistant.Infrastructure.Faster
{
    internal class CandleSerializer : BinaryObjectSerializer<Candle>
    {
        public override void Serialize(ref Candle candle)
        {
            writer.Write(candle.Symbol);
            writer.Write((int)candle.Interval);
            writer.Write(candle.OpenTime.ToBinary());
            writer.Write(candle.CloseTime.ToBinary());
            writer.Write((long)(candle.OpenPrice * 100_000_000));
            writer.Write((long)(candle.HighPrice * 100_000_000));
            writer.Write((long)(candle.LowPrice * 100_000_000));
            writer.Write((long)(candle.ClosePrice * 100_000_000));
        }

        public override void Deserialize(out Candle candle)
        {
            candle = new Candle
            {
                Symbol = reader.ReadString(),
                Interval = (KlineInterval)reader.ReadInt32(),
                OpenTime = DateTime.FromBinary(reader.ReadInt64()),
                CloseTime = DateTime.FromBinary(reader.ReadInt64()),
                OpenPrice = reader.ReadInt64() / 100_000_000m,
                HighPrice = reader.ReadInt64() / 100_000_000m,
                LowPrice = reader.ReadInt64() / 100_000_000m,
                ClosePrice = reader.ReadInt64() / 100_000_000m
            };
        }
    }
}
