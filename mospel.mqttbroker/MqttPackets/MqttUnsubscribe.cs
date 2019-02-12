using System.Collections.Generic;

namespace Mospel.MqttPackets
{
    public class MqttUnsubscribePacket : MqttBasePacket
    {
        public ushort? PacketIdentifier { get; set; }

        public IList<string> TopicFilters { get; set; } = new List<string>();

        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters);
            return "Unsubscribe: [PacketIdentifier=" + PacketIdentifier + "] [TopicFilters=" + topicFiltersText + "]";
        }
    }
}
