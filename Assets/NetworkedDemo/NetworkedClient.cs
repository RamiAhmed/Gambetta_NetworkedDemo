namespace NetworkedDemo
{
    using Lidgren.Network;
    using UnityEngine;

    public sealed class NetworkedClient : ClientBase
    {
        private NetClient _network;
        private NetIncomingMessage _msg;

        private EntityState[] _world_state = new EntityState[Constants.MaxPlayers];

        public override int averageRoundTripTimeMS
        {
            get
            {
                if (_network.ConnectionsCount == 0)
                {
                    return 0;
                }

                return Mathf.RoundToInt(_network.Connections[0].AverageRoundtripTime * 1000f);
            }
        }

        internal void Initialize(string serverIp, int serverPort)
        {
            var config = new NetPeerConfiguration(Constants.AppId);
            config.MaximumConnections = Constants.MaxPlayers;

            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);
            config.EnableMessageType(NetIncomingMessageType.Data);

            config.EnableMessageType(NetIncomingMessageType.DebugMessage);
            //config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
            config.EnableMessageType(NetIncomingMessageType.WarningMessage);
            config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
            config.EnableMessageType(NetIncomingMessageType.Error);

            _network = new NetClient(config);
            _network.Start();

            var hailMsg = _network.CreateMessage(Constants.HailSecret);
            _network.Connect(serverIp, serverPort, hailMsg);
        }

        protected override void Send(Input input)
        {
            var msgOut = _network.CreateMessage(16);
            input.Write(msgOut);
            _network.SendMessage(msgOut, Constants.DeliveryMethod);
        }

        protected override void ProcessServerMessages()
        {
            while ((_msg = _network.ReadMessage()) != null)
            {
                switch (_msg.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                    {
                        OnStatusChanged(_msg, (NetConnectionStatus)_msg.ReadByte());
                        break;
                    }

                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                    {
                        OnDebug(_msg);
                        break;
                    }

                    case NetIncomingMessageType.WarningMessage:
                    {
                        OnWarning(_msg);
                        break;
                    }

                    case NetIncomingMessageType.Error:
                    case NetIncomingMessageType.ErrorMessage:
                    {
                        OnError(_msg);
                        break;
                    }

                    case NetIncomingMessageType.Data:
                    {
                        var type = (MessageType)_msg.ReadByte();
                        if (type == MessageType.WorldState)
                        {
                            System.Array.Clear(_world_state, 0, Constants.MaxPlayers);

                            for (var i = 0; i < Constants.MaxPlayers; i++)
                            {
                                var exists = _msg.ReadBoolean();
                                if (exists)
                                {
                                    _world_state[i] = EntityState.Read(_msg);
                                }
                            }

                            OnData(_world_state);
                        }
                        else if (type == MessageType.Connected)
                        {
                            var entity_id = _msg.ReadInt32();
                            Create(entity_id);
                        }

                        break;
                    }

                    default:
                    {
                        OnEvent(_msg);
                        break;
                    }
                }

                _network.Recycle(_msg);
            }
        }

        internal void OnStatusChanged(NetIncomingMessage msg, NetConnectionStatus status)
        {
            switch (status)
            {
                case NetConnectionStatus.Connected:
                {
                    Debug.Log("Connection: " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
                    OnConnected(msg);
                    break;
                }

                case NetConnectionStatus.Disconnected:
                {
                    OnDisconnected(msg);
                    break;
                }

                default:
                {
                    Debug.Log("Status Changed to: " + status + " from " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
                    break;
                }
            }
        }

        internal void OnEvent(NetIncomingMessage msg)
        {
            Debug.Log("NetworkServer OnEvent, msg type == " + msg.MessageType);
        }

        internal void OnDebug(NetIncomingMessage msg)
        {
            Debug.Log(msg.ReadString());
        }

        internal void OnWarning(NetIncomingMessage msg)
        {
            Debug.LogWarning(msg.ReadString());
        }

        internal void OnError(NetIncomingMessage msg)
        {
            Debug.LogError(msg.ReadString());
        }

        private void OnConnected(NetIncomingMessage msg)
        {
            Debug.Log("OnConnected: " + msg.SenderConnection + " @ " + msg.SenderEndPoint);

            var msgOut = _network.CreateMessage();
            msgOut.Write((byte)MessageType.Connected);
            _network.SendMessage(msgOut, Constants.DeliveryMethod);
        }

        private void OnDisconnected(NetIncomingMessage msg)
        {
            Debug.Log("Disconnection: " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
        }

        internal override void TearDown()
        {
            base.TearDown();
            _network.Shutdown(string.Empty);
        }
    }
}