namespace Mospel.MqttPackets
{
    public class MqttPubRelPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubRel";
        }
    }
}
