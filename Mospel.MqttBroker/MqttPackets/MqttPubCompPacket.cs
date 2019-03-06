namespace Mospel.MqttPackets
{
    public class MqttPubCompPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubComp";
        }
    }
}
