using Binance.Net.Enums;
using FASTER.core;

namespace TradingAssistant
{
    internal class CandleIdSerializer : BinaryObjectSerializer<CandleId>
    {
        public override void Serialize(ref CandleId candleId)
        {
            writer.Write(candleId.Symbol);
            writer.Write((int)candleId.TimeFrame);
            writer.Write(candleId.OpenTime.ToBinary());
        }

        public override void Deserialize(out CandleId candleId)
        {
            var symbol = reader.ReadString();
            var timeFrame = (KlineInterval)reader.ReadInt32();
            var openTime = DateTime.FromBinary(reader.ReadInt64());

            candleId = new CandleId(symbol, timeFrame, openTime);
        }
    }
}
