using Mospel.MqttPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mospel.MqttBroker
{
    public class MqttPublishQueueItem
    {
        public ushort Identifier { get; set; }
        public MqttPublishPacket Packet { get; set; }
        public bool Sent { get; set; }
    }
}
