namespace Mospel.MqttPackets
{
    public class MqttUnsubAckPacket : MqttBasePacket
    {
        public ushort? PacketIdentifier { get; set; }

        public override string ToString()
        {
            return "UnsubAck: [PacketIdentifier=" + PacketIdentifier + "]";
        }
    }
}
