using Binance.Net.Enums;
using FASTER.core;

namespace TradingAssistant
{
    internal class CandleSerializer : BinaryObjectSerializer<Candle>
    {
        public override void Serialize(ref Candle obj)
        {
            writer.Write(obj.Symbol);
            writer.Write((int)obj.Interval);
            writer.Write(obj.OpenTime.ToBinary());
            writer.Write(obj.CloseTime.ToBinary());
            writer.Write(obj.OpenPrice);
            writer.Write(obj.HighPrice);
            writer.Write(obj.LowPrice);
            writer.Write(obj.ClosePrice);
        }

        public override void Deserialize(out Candle obj)
        {
            obj = new Candle
            {
                Symbol = reader.ReadString(),
                Interval = (KlineInterval)reader.ReadInt32(),
                OpenTime = DateTime.FromBinary(reader.ReadInt64()),
                CloseTime = DateTime.FromBinary(reader.ReadInt64()),
                OpenPrice = reader.ReadDecimal(),
                HighPrice = reader.ReadDecimal(),
                LowPrice = reader.ReadDecimal(),
                ClosePrice = reader.ReadDecimal()
            };
        }
    }
}
