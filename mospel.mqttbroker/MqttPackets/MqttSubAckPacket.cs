using Mospel.Protocol;
using System.Collections.Generic;
using System.Linq;


namespace Mospel.MqttPackets
{
    public class MqttSubAckPacket : MqttBasePacket
    {
        public ushort? PacketIdentifier { get; set; }

        public IList<MqttSubscribeReturnCode> SubscribeReturnCodes { get; } = new List<MqttSubscribeReturnCode>();

        public override string ToString()
        {
            var subscribeReturnCodesText = string.Join(",", SubscribeReturnCodes.Select(f => f.ToString()));
            return "SubAck: [PacketIdentifier=" + PacketIdentifier + "] [SubscribeReturnCodes=" + subscribeReturnCodesText + "]";
        }
    }
}
