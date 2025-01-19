using Binance.Net.Enums;
using FASTER.core;

namespace TradingAssistant
{
    internal class CandleIdSerializer : BinaryObjectSerializer<CandleId>
    {
        public override void Serialize(ref CandleId obj)
        {
            writer.Write(obj.Symbol);
            writer.Write((int)obj.TimeFrame);
            writer.Write(obj.OpenTime.ToBinary());
        }

        public override void Deserialize(out CandleId obj)
        {
            var symbol = reader.ReadString();
            var timeFrame = (KlineInterval)reader.ReadInt32();
            var openTime = DateTime.FromBinary(reader.ReadInt64());
            obj = new CandleId(symbol, timeFrame, openTime);
        }
    }
}
