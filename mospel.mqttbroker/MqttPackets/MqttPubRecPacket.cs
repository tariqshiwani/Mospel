namespace Mospel.MqttPackets
{
    public class MqttPubRecPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubRec";
        }
    }
}
