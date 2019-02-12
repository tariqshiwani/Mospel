using Mospel.MqttPackets;
using Mospel.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mospel
{
    public class MqttClientSubscriptionManager
    {
        private Dictionary<string, MqttQualityOfServiceLevel> subscription = new Dictionary<string, MqttQualityOfServiceLevel>();
        private string ClientId = "";
        public MqttClientSubscriptionManager(string clientId)
        {
            ClientId = clientId;
        }

        public MqttClientSubscribeResult Subscribe(MqttSubscribePacket subscribePacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var result = new MqttClientSubscribeResult
            {
                ResponsePacket = new MqttSubAckPacket
                {
                    PacketIdentifier = subscribePacket.PacketIdentifier
                },

                CloseConnection = false
            };

            foreach (var topicFilter in subscribePacket.TopicFilters)
            {
                var interceptorContext = InterceptSubscribe(topicFilter);
                if (!interceptorContext.AcceptSubscription)
                {
                    result.ResponsePacket.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                }
                else
                {
                    result.ResponsePacket.SubscribeReturnCodes.Add(ConvertToMaximumQoS(topicFilter.QualityOfServiceLevel));
                }

                if (interceptorContext.CloseConnection)
                {
                    result.CloseConnection = true;
                }

                if (interceptorContext.AcceptSubscription)
                {
                    lock (_subscriptions)
                    {
                        _subscriptions[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                    }

                    _eventDispatcher.OnClientSubscribedTopic(_clientId, topicFilter);
                }
            }

            return result;
        }

    }
}
