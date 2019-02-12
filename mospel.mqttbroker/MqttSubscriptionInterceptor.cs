using Mospel.MqttPackets;

namespace Mospel.MqttBroker
{
    public class MqttSubscriptionInterceptor
    {
        public MqttSubscribePacket Request { get; set; }
        public bool Allowed { get; set; } = true;
        public object ClientInfo { get; set; }

        public MqttSubscriptionInterceptor(object clientInfo, MqttSubscribePacket request)
        {
            Request = request;
            ClientInfo = clientInfo;
        }

    }
}