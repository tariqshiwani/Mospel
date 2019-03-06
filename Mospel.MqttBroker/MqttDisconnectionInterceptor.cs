
namespace Mospel.MqttBroker
{
    public class MqttDisconnectionInterceptor
    {
        public MqttClientSession ClientSession { get; set; }
        public object ClientInfo { get; set; }

        public MqttDisconnectionInterceptor(MqttClientSession session, object clientInfo)
        {
            ClientSession = session;
            ClientInfo = clientInfo;
        }
    }
}