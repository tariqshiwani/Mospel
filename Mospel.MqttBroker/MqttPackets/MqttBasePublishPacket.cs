namespace Mospel.MqttPackets
{
    public class MqttBasePublishPacket : MqttBasePacket
    {
        public ushort? PacketIdentifier { get; set; }
    }
}
