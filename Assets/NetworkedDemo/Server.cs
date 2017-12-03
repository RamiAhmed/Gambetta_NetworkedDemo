namespace NetworkedDemo
{
    using System;
    using Lidgren.Network;
    using UnityEngine;

    public class Server
    {
        public readonly Entity[] entities = new Entity[Constants.MaxPlayers];
        public readonly int[] last_processed_input = new int[Constants.MaxPlayers];

        public readonly EntityState[] world_state = new EntityState[Constants.MaxPlayers];

        // :: Aditions
        private NetServer _network;

        private NetIncomingMessage _msg;
        private ulong _nextUpdate;

        internal int averageRoundTripTimeMS
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

        internal void Initialize(int port)
        {
            var config = new NetPeerConfiguration(Constants.AppId);
            config.Port = port;
            config.MaximumConnections = Constants.MaxPlayers;

            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.StatusChanged);
            config.EnableMessageType(NetIncomingMessageType.Data);

            config.EnableMessageType(NetIncomingMessageType.DebugMessage);
            //config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
            config.EnableMessageType(NetIncomingMessageType.WarningMessage);
            config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
            config.EnableMessageType(NetIncomingMessageType.Error);

            _network = new NetServer(config);
            _network.Start();
        }

        public int GetFreeEntityID()
        {
            for (var i = 0; i < Constants.MaxPlayers; i++)
            {
                if (this.entities[i] == null)
                {
                    return i;
                }
            }

            throw new IndexOutOfRangeException("Too many entity IDs requested!");
        }

        public void CreateEntity(int entity_id)
        {
            // Create a new Entity for this Client.
            var entity = new Entity();
            entity.entity_id = entity_id;

            this.entities[entity_id] = entity;

            // Set the initial state of the Entity (e.g. spawn point)
            entity.position = Constants.SpawnPoints[entity_id];
        }

        // :: Addition
        public void Tick()
        {
            var now = new Date();
            if (now < _nextUpdate)
            {
                return;
            }

            _nextUpdate = now + Constants.ServerUpdateRate;
            Update();
        }

        private void Update()
        {
            ProcessInputs();
            SendWorldState();
            // Server has no world to render, it just simulates (we render with Gizmos)
        }

        private void ProcessInputs()
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
                        if (type == MessageType.Connected)
                        {
                            var entity_id = GetFreeEntityID();
                            CreateEntity(entity_id);

                            var msgOut = _network.CreateMessage(4);
                            msgOut.Write((byte)MessageType.Connected);
                            msgOut.Write(entity_id);
                            _network.SendMessage(msgOut, _msg.SenderConnection, Constants.DeliveryMethod);
                        }
                        else if (type == MessageType.Input)
                        {
                            OnData(Input.Read(_msg));
                        }

                        break;
                    }

                    case NetIncomingMessageType.ConnectionApproval:
                    {
                        OnConnectionApproval(_msg);
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

        internal void OnData(Input input)
        {
            var id = input.entity_id;
            var entity = this.entities[id];
            if (entity == null)
            {
                return;
            }

            if (!ValidateInput(input))
            {
                return;
            }

            entity.ApplyInput(input);
            this.last_processed_input[id] = input.input_sequence_number;
        }

        private bool ValidateInput(Input input)
        {
            // TODO: validation
            return true;
        }

        internal void OnConnectionApproval(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;

            var secret = msg.ReadString();
            if (secret == Constants.HailSecret)
            {
                sender.Approve();
                Debug.Log("NetworkServer OnConnectionApproval, sender (" + msg.SenderEndPoint + ") approved");
            }
            else
            {
                sender.Deny();
                Debug.Log("NetworkServer OnConnectionApproval, sender (" + msg.SenderEndPoint + ") denied");
            }
        }

        internal void OnEvent(NetIncomingMessage msg)
        {
            Debug.Log("NetworkServer OnEvent, msg type == " + msg.MessageType);
        }

        internal virtual void OnDebug(NetIncomingMessage msg)
        {
            Debug.Log(msg.ReadString());
        }

        internal virtual void OnWarning(NetIncomingMessage msg)
        {
            Debug.LogWarning(msg.ReadString());
        }

        internal virtual void OnError(NetIncomingMessage msg)
        {
            Debug.LogError(msg.ReadString());
        }

        private void OnDisconnected(NetIncomingMessage msg)
        {
            // TODO:
            Debug.Log("Disconnection: " + msg.SenderConnection + " @ " + msg.SenderEndPoint);
        }

        private void SendWorldState()
        {
            Array.Clear(world_state, 0, Constants.MaxPlayers);

            for (var i = 0; i < Constants.MaxPlayers; i++)
            {
                var entity = this.entities[i];
                if (entity == null)
                {
                    continue;
                }

                world_state[entity.entity_id] = new EntityState()
                {
                    entity_id = entity.entity_id,
                    position = entity.position,
                    last_processed_input = this.last_processed_input[i]
                };
            }

            var msg = _network.CreateMessage(16);
            msg.Write((byte)MessageType.WorldState);

            for (var i = 0; i < Constants.MaxPlayers; i++)
            {
                var exists = this.entities[i] != null;
                msg.Write(exists);

                if (exists)
                {
                    world_state[i].Write(msg);
                }
            }

            _network.SendToAll(msg, Constants.DeliveryMethod);
        }

        internal virtual void TearDown()
        {
            _network.Shutdown(string.Empty);
        }

        internal void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            for (var i = 0; i < Constants.MaxPlayers; i++)
            {
                var entity = this.entities[i];
                if (entity == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(entity.position, 0.5f);
            }
        }
    }
}