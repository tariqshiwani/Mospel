using Fleck;
using Mospel.MqttBroker;
using Mospel.MqttPackets;
using Mospel.MqttSerializer;
using Mospel.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mospel.MqttBroker
{
    public class MqttClientSession
    {
        private IWebSocketConnection Socket { get; set; }
        private MqttPacketSerializer Serializer { get; set; } = new MqttPacketSerializer();
        private IList<TopicFilter> Subscription { get; set; } = new List<TopicFilter>();
        private Dictionary<ushort, MqttBasePacket> PublishQueue { get; set; } = new Dictionary<ushort, MqttBasePacket>();
        private bool connected = false;
        private DateTime disconnectedAt = DateTime.Now;
        private DateTime LastSentAt = DateTime.Now;
        
        
        private int KeepAlive = 60;

        internal object ClientInfo { get; set; }

        internal MqttPublishPacket WillMessage;
        internal bool CanDispose = false;
        internal string ClientId { get; set; }

        internal MqttClientSession(IWebSocketConnection socket, MqttConnectPacket packet)
        {
            connected = true;
            ClientId = packet.ClientId;
            Socket = socket;
            Serializer.ProtocolVersion = packet.ProtocolVersion;
            KeepAlive = packet.KeepAlivePeriod;
            WillMessage = packet.WillMessage;
        }

        internal bool IsConnected
        {
            get
            {
                return connected;
            }
        }

        internal int CleanupTime { get; set; } = 10;

        internal void Subscribe(MqttSubscribePacket request)
        {
            MqttSubAckPacket subResPacket = new MqttSubAckPacket();
            subResPacket.PacketIdentifier = request.PacketIdentifier;
            lock (Subscription)
            {
                foreach (TopicFilter tf in request.TopicFilters)
                {
                    Subscription = Subscription.Where(s => s.Topic != tf.Topic).ToList();
                    Subscription.Add(tf);
                    subResPacket.SubscribeReturnCodes.Add((MqttSubscribeReturnCode)tf.QualityOfServiceLevel);
                }
            }

            Send(subResPacket);
        }

        internal void UnSubscribe(MqttUnsubscribePacket request)
        {
            MqttUnsubAckPacket unSubResPacket = new MqttUnsubAckPacket();
            unSubResPacket.PacketIdentifier = request.PacketIdentifier;
            lock (Subscription)
            {
                foreach (string tf in request.TopicFilters)
                {
                    for (int i = Subscription.Count-1; i >= 0 ; i--)
                    {
                        if (Subscription[i].Topic == tf)
                        {
                            Subscription.RemoveAt(i);
                        }
                    }
                }
            }
            Send(unSubResPacket);
        }

        internal MqttQualityOfServiceLevel? HasSubscribed(string topic, MqttQualityOfServiceLevel QosLevel)
        {
            lock (Subscription)
            {
                foreach (TopicFilter tf in Subscription)
                {
                    if (MqttTopicFilterComparer.IsMatch(tf.Topic, topic))
                    {
                        return tf.QualityOfServiceLevel;
                    }
                }
            }
            return null;
        }

        internal void Cleanup()
        {
            if (!connected && disconnectedAt < DateTime.Now.AddMinutes((CleanupTime * -1)))
            {
                CanDispose = true;
            }
        }

        internal void Close()
        {
            connected = false;
            disconnectedAt = DateTime.Now;
            Socket.Close();
        }

        internal void Disconnect()
        {
            WillMessage = null;
            Close();
        }

        internal void Dispose()
        {
            Socket = null;
            Serializer = null;
            Subscription = null;
            PublishQueue = null;
        }

        internal void SendSubAck(MqttSubAckPacket packet)
        {
            Send(packet);
        }

        internal void SendPingRes()
        {
            Send(new MqttPingRespPacket());
        }

        internal void SendPublish(MqttPublishPacket packet)
        {
            MqttQualityOfServiceLevel? SubQos = HasSubscribed(packet.Topic, packet.QualityOfServiceLevel);
            if (SubQos != null)
            {
                if (packet.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce || packet.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
                {
                    if(packet.QualityOfServiceLevel > SubQos)
                    {
                        if (SubQos == MqttQualityOfServiceLevel.AtMostOnce)
                            packet.QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
                        else
                            packet.QualityOfServiceLevel -= 1;
                    }
                    lock (PublishQueue)
                    {
                        packet.PacketIdentifier = GenerateIdentifier();
                        PublishQueue.Add((ushort)packet.PacketIdentifier, packet);
                    }
                }
                Send(packet);
            }
        }

        internal void RemoveFromQueue(ushort idenfier)
        {
            //if (PublishQueue[idenfier].QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            //{
            lock (PublishQueue)
            {
                PublishQueue.Remove(idenfier);
            }
            //}
        }

        internal void SendConnAck(MqttConnectReturnCode returnCode)
        {
            MqttConnAckPacket response = new MqttConnAckPacket();
            response.ConnectReturnCode = returnCode;
            Send(response);
        }

        internal void SendPubAck(ushort? identifier)
        {
            MqttPubAckPacket pubResPacket = new MqttPubAckPacket();
            pubResPacket.PacketIdentifier = identifier;
            Send(pubResPacket);
        }

        internal bool MatchSocket(IWebSocketConnection socket)
        {
            return (socket == Socket);
        }

        internal void SendPubRel(MqttPubRecPacket request)
        {
            MqttPubRelPacket res = new MqttPubRelPacket();
            res.PacketIdentifier = request.PacketIdentifier;
            lock (PublishQueue)
            {
                PublishQueue[(ushort)request.PacketIdentifier] = res;
            }
            Send(res);
        }

        internal void SendPubComp(MqttPubRelPacket request)
        {
            MqttPubCompPacket res = new MqttPubCompPacket();
            res.PacketIdentifier = request.PacketIdentifier;
            Send(res);
        }

        internal void SendPubRec(MqttPublishPacket request)
        {
            MqttPubRecPacket res = new MqttPubRecPacket();
            res.PacketIdentifier = request.PacketIdentifier;
            Send(res);
        }

        internal void UpdateConnection(IWebSocketConnection socket, MqttConnectPacket packet)
        {
            ResetTimer();
            WillMessage = packet.WillMessage;
            connected = true;
            Socket = socket;
            KeepAlive = packet.KeepAlivePeriod;
        }

        internal void SendQueue()
        {
            lock (PublishQueue)
            {
                foreach(MqttBasePacket packet in PublishQueue.Values)
                {
                    Send(packet,true);
                }
            }
        }

        internal void ResetTimer()
        {
            LastSentAt = DateTime.Now;
        }

        internal bool TimedOut()
        {
            if (KeepAlive > 0)
            {
                if (LastSentAt < DateTime.Now.AddSeconds((KeepAlive * 1.5) * -1))
                {
                    return true;
                }
            }
            return false;
        }

        private void Send(MqttBasePacket packet, bool dup = false)
        {
            try
            {
                if (packet.GetType() == typeof(MqttPublishPacket))
                {
                    MqttPublishPacket p = ((MqttPublishPacket)packet);
                    p.Dup = dup;
                    packet = p;
                }
                Socket.Send(Serializer.Serialize(packet).ToArray());
                ResetTimer();
            }catch(Exception ex)
            {
                Close();
            }
        }

        private ushort GenerateIdentifier()
        {
            ushort id = 1;
            lock (PublishQueue)
            {
                Random rnd = new Random();
                do
                {
                    id = (ushort)rnd.Next(1, ushort.MaxValue);
                }
                while (PublishQueue.ContainsKey(id));
            }
            return id;
        }
    }
}
