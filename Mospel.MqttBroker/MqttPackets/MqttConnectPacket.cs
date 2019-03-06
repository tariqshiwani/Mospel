﻿using Mospel.MqttSerializer;

namespace Mospel.MqttPackets
{
    public class MqttConnectPacket : MqttBasePacket
    {
        public MqttProtocolVersion ProtocolVersion { get; set; }

        public string ClientId { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public ushort KeepAlivePeriod { get; set; }

        public bool CleanSession { get; set; }

        public MqttPublishPacket WillMessage { get; set; }

        public override string ToString()
        {
            return "Connect: [ClientId=" + ClientId + "] [Username=" + Username + "] [Password=" + Password + "] [KeepAlivePeriod=" + KeepAlivePeriod + "] [CleanSession=" + CleanSession + "]";
        }
    }
}
