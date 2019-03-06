using Fleck;
using Mospel.MqttPackets;
using Mospel.MqttSerializer;
using Mospel.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Mospel.MqttBroker
{
    public class MqttServer
    {
        private Thread t;
        private bool isRunning = false;
        private string Host = "0.0.0.0";
        private int Port = 8181;
        private Dictionary<string, MqttClientSession> allConnections = new Dictionary<string, MqttClientSession>();
        private Dictionary<string, MqttBasePacket> RetainMessages = new Dictionary<string, MqttBasePacket>();
        private WebSocketServer server;
        private LogHelper errorLog;
        private LogHelper activityLog;
        private LogHelper webSocketLog;

        public MqttServerOptions ServerOptions { get; set; }

        public MqttServer(string host, int port)
        {
            Host = host;
            Port = port;
            errorLog = new LogHelper(Path.Combine(AppContext.BaseDirectory, "Logs", "Mospel", "Error"), "Errors.txt");
            activityLog = new LogHelper(Path.Combine(AppContext.BaseDirectory, "Logs", "Mospel", "Activity"), "Activity.txt");
            webSocketLog = new LogHelper(Path.Combine(AppContext.BaseDirectory, "Logs", "Mospel", "WebSocket"), "Log.txt");
        }

        public void Start(X509Certificate2 cert = null)
        {
            FleckLog.Level = LogLevel.Error;

            FleckLog.LogAction = (level, message, ex) => {
                webSocketLog.Add(message + " " + (ex != null ? ex.Message : ""));
            };

            isRunning = true;
            t = new Thread(Process);
            t.IsBackground = false;
            t.Start();

            activityLog.Add("Starting WebSocketServer");
            if(cert != null)
            {
                server = new WebSocketServer("wss://" + Host + ":" + Port.ToString());
                server.Certificate = cert;
            }
            else
            {
                server = new WebSocketServer("ws://" + Host + ":" + Port.ToString());
            }
            server.RestartAfterListenError = true;
            List<string> SupportedProtocols = new List<string>();
            SupportedProtocols.Add("mqtt");
            SupportedProtocols.Add("mqttv3.1");
            server.SupportedSubProtocols = SupportedProtocols;

            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    activityLog.Add("New Connection request");
                };
                socket.OnClose = () =>
                {
                    activityLog.Add("Connection closed", socket.ConnectionInfo.ClientIpAddress);
                    MqttClientSession connection = Find(socket);
                    if (connection != null)
                    {
                        if (connection.WillMessage != null && isRunning == true)
                        {
                            activityLog.Add("Sending will message from ", "ClientId:" + connection.ClientId, connection.ClientInfo.ToString());
                            PublishToAll(connection.WillMessage);
                        }

                        var disConInterceptor = new MqttDisconnectionInterceptor(connection, connection.ClientInfo);
                        ServerOptions.DisconnectionInterceptor?.Invoke(disConInterceptor);
                    }
                };
                //socket.OnMessage = message =>
                //{
                //};
                socket.OnBinary = message =>
                {
                    MqttClientSession connection = Find(socket);
                    try
                    {
                        MqttBasePacket packet = MqttPacketSerializer.Deserialize(message);
                        switch (packet)
                        {
                            case MqttConnectPacket connectPacket:
                                connection = HandleConnectPacket(connectPacket, socket, connection);
                                break;
                            case MqttDisconnectPacket disconnectPacket:
                                HandleDisconnectPacket(disconnectPacket, connection);
                                break;
                            case MqttPingReqPacket pingReqPacket:
                                HandlePingReqPacket(pingReqPacket, connection);
                                break;
                            case MqttPublishPacket publishPacket:
                                HandlePublishPacket(publishPacket, connection);
                                break;
                            case MqttPubAckPacket pubAckPacket:
                                HandlePubAckPacket(pubAckPacket, connection);
                                break;
                            case MqttPubRecPacket pubRecPacket:
                                HandlePubRecPacket(pubRecPacket, connection);
                                break;
                            case MqttPubRelPacket pubRelPacket:
                                HandlePubRelPacket(pubRelPacket, connection);
                                break;
                            case MqttPubCompPacket pubCompPacket:
                                HandlePubCompPacket(pubCompPacket, connection);
                                break;
                            case MqttSubscribePacket subscribePacket:
                                HandleSubscribePacket(subscribePacket, connection);
                                break;
                            case MqttUnsubscribePacket unsubscribePacket:
                                HandleUnsubscribePacket(unsubscribePacket, connection);
                                break;
                        }
                        connection.ResetTimer();
                    }
                    catch (MqttProtocolViolationException pvex)
                    {
                        connection.Close(); // violatin of protocol so disconnect
                    }
                };
            });
            activityLog.Add("Started");
        }

        private MqttClientSession Find(IWebSocketConnection socket)
        {
            lock (allConnections)
            {
                foreach (MqttClientSession con in allConnections.Values)
                {
                    if (con.MatchSocket(socket))
                    {
                        return con;
                    }
                }
            }
            return null;
        }

        public void Stop()
        {
            server.ListenerSocket.Close(); // stop new connections
            isRunning = false;
        }

        private void Process()
        {
            while (isRunning) // cleanup process for all the timed out and long disconnected connections
            {
                try
                {
                    for (int i = allConnections.Count - 1; i >= 0; i--)
                    {
                        var cid = allConnections.Keys.ToArray()[i];
                        if (allConnections[cid].TimedOut() && allConnections[cid].IsConnected) // if there is no activity since keep alive x 1.5
                        {
                            allConnections[cid].Close(); // disconnect
                        }
                        allConnections[cid].Cleanup(); // mark CanDispose if disconnected for long time (CleanupTime)
                        if (allConnections[cid].CanDispose) // see if it can be disposed
                        {
                            allConnections[cid].Dispose(); // dispose and remove
                            lock (allConnections)
                            {
                                allConnections.Remove(cid);
                            }
                        }
                    }
                }catch(Exception ex)
                {
                    Error(ex.Message);
                }
                Thread.Sleep(1000);
            }

            // looks like service has stopped
            foreach (MqttClientSession con in allConnections.Values)
            {
                con.Disconnect(); //disconnect all connections
                con.Dispose();
            }


        }

        private MqttClientSession HandleConnectPacket(MqttConnectPacket packet, IWebSocketConnection socket, MqttClientSession connection)
        {
            activityLog.Add("Connect packet received from ", packet.ClientId);
            var conInterceptor = new MqttConnectionInterceptor(packet);
            ServerOptions.ConnectionInterceptor?.Invoke(conInterceptor);
            if (conInterceptor.ReturnCode == MqttConnectReturnCode.ConnectionAccepted) // if authenticated
            {
                lock (allConnections)
                {
                    if (connection != null) // if connection already exist that means we have received connect packet again
                    {
                        connection.Close(); // violatin of protocol so disconnect
                        return connection;
                    }

                    if (allConnections.ContainsKey(packet.ClientId)) // if clientId already exist that means it was disconnected
                    {
                        connection = allConnections[packet.ClientId]; // so close old connection
                        connection.Close(); // connected from another device with same clientid
                    }

                    if (packet.CleanSession) // if clean session is requested
                    {
                        // always create new session object, regardless if connection exists
                        connection = new MqttClientSession(socket, packet);
                    }
                    else
                    {
                        if (connection != null) // if connection already exist
                        {
                            // just update new socket to existing session
                            connection.UpdateConnection(socket, packet);
                        }
                        else
                        {
                            // create new connection
                            connection = new MqttClientSession(socket, packet);
                        }
                    }
                    connection.ClientInfo = conInterceptor.ClientInfo;
                    allConnections[packet.ClientId] = connection;
                }
                activityLog.Add("connection accepted", packet.ClientId, connection.ClientInfo.ToString());

            }
            else
            {
                activityLog.Add("connection rejected", packet.ClientId, connection.ClientInfo.ToString());
                connection = new MqttClientSession(socket, packet);
            }
            activityLog.Add("sending connack to", packet.ClientId, connection.ClientInfo.ToString());
            connection.SendConnAck(conInterceptor.ReturnCode);
            if (conInterceptor.ReturnCode == MqttConnectReturnCode.ConnectionAccepted)
            {
                activityLog.Add("sending retain message to ", packet.ClientId, connection.ClientInfo.ToString());
                SendRetainMessages(connection);
                activityLog.Add("sending any pending queue", packet.ClientId, connection.ClientInfo.ToString());
                connection.SendQueue();
            }

            return connection;
        }

        private void HandleUnsubscribePacket(MqttUnsubscribePacket request, MqttClientSession connection)
        {
            connection.UnSubscribe(request);
        }

        private void HandleSubscribePacket(MqttSubscribePacket request, MqttClientSession connection)
        {
            var subInterceptor = new MqttSubscriptionInterceptor(connection.ClientInfo, request);
            ServerOptions.SubscriptionInterceptor?.Invoke(subInterceptor);
            if (subInterceptor.Allowed)
            {
                connection.Subscribe(request);
                SendRetainMessages(connection);
            }
            else
            {
                MqttSubAckPacket sub = new MqttSubAckPacket();
                foreach (TopicFilter tf in request.TopicFilters)
                {
                    sub.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                }
                connection.SendSubAck(sub);
            }
        }

        private void HandlePubCompPacket(MqttPubCompPacket request, MqttClientSession connection)
        {
            connection.RemoveFromQueue((ushort)request.PacketIdentifier);
        }

        private void HandlePubRelPacket(MqttPubRelPacket request, MqttClientSession connection)
        {
            connection.SendPubComp(request.PacketIdentifier);
        }

        private void HandlePubRecPacket(MqttPubRecPacket request, MqttClientSession connection)
        {
            connection.SendPubRel(request.PacketIdentifier);
        }

        private void HandlePubAckPacket(MqttPubAckPacket request, MqttClientSession connection)
        {
            connection.RemoveFromQueue((ushort)request.PacketIdentifier);
        }

        private void HandlePublishPacket(MqttPublishPacket request, MqttClientSession connection)
        {
            activityLog.Add("Received publish packet id ", request.PacketIdentifier.ToString(), "from", connection.ClientId, connection.ClientInfo.ToString());
            var pubInterceptor = new MqttPublishInterceptor(connection.ClientInfo, request);
            ServerOptions.PublishInterceptor?.Invoke(pubInterceptor);
            ushort? pId = request.PacketIdentifier;
            pubInterceptor.Request.PacketIdentifier = pId;
            if (pubInterceptor.Allowed)
            {
                if (request.Retain) // if retain is requested
                {
                    lock (RetainMessages)
                    {
                        if (request.Payload.Length > 0) //if payload exists
                        {
                            RetainMessages[request.Topic] = pubInterceptor.Request; // update retain message, use intercepted request
                        }
                        else
                        {
                            RetainMessages.Remove(request.Topic); // delete retain message
                        }
                    }
                }
                //pubInterceptor.Request.Retain = false;
                PublishToAll(pubInterceptor.Request);
            }
            else
            {
                activityLog.Add("publish denied ",request.PacketIdentifier.ToString());
            }
            if (request.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                connection.SendPubAck(pId);
            }
            if (request.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                connection.SendPubRec(pId);
            }
        }

        private void PublishToAll(MqttPublishPacket request)
        {
            activityLog.Add("Publishing packet ", request.PacketIdentifier.ToString());
            request.Retain = false;
            lock (allConnections)
            {
                foreach (MqttClientSession cs in allConnections.Values)
                {
                    cs.SendPublish(request);
                }
            }
        }

        private void HandlePingReqPacket(MqttPingReqPacket request, MqttClientSession connection)
        {
            connection.SendPingRes();
        }

        private void HandleDisconnectPacket(MqttDisconnectPacket request, MqttClientSession connection)
        {
            connection.Disconnect();
            lock (allConnections)
            {
                allConnections.Remove(connection.ClientId);
            }
        }

        private void SendRetainMessages(MqttClientSession connection)
        {
            lock (RetainMessages)
            {
                foreach (KeyValuePair<string, MqttBasePacket> msg in RetainMessages)
                {
                    MqttPublishPacket packet = (MqttPublishPacket)msg.Value;
                    connection.SendPublish(packet);
                }
            }
        }

        private void Error(params object[] args)
        {
            errorLog.Add(args);
        }
        private void Log(params object[] args)
        {
            activityLog.Add(args);
        }
    }

   
}
