using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mospel.MqttBroker
{
    public class ReceivedMqttPacket
    {
        public ReceivedMqttPacket(byte fixedHeader, MqttPacketBodyReader body)
        {
            FixedHeader = fixedHeader;
            Body = body;
        }

        public byte FixedHeader { get; }

        public MqttPacketBodyReader Body { get; }
    }
}
