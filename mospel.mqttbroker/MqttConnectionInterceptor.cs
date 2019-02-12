using Mospel.MqttPackets;
using Mospel.Protocol;

namespace Mospel.MqttBroker
{
    public class MqttConnectionInterceptor
    {
        public MqttConnectPacket Request { get; set; }

        public MqttConnectReturnCode ReturnCode { get; set; }

        public object ClientInfo { get; set; } = null;

        public MqttConnectionInterceptor(MqttConnectPacket request)
        {
            Request = request;

        }

        
    }
}