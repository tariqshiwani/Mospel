using Mospel.MqttPackets;

namespace Mospel.MqttBroker
{
    public class MqttPublishInterceptor
    {

        public MqttPublishPacket Request { get; set; }
        public bool Allowed { get; set; } = true;
        public object ClientInfo { get; set; }

        public MqttPublishInterceptor(object clientInfo, MqttPublishPacket request)
        {
            Request = request;
            ClientInfo = clientInfo;
        }
    }
}