namespace NetworkedDemo
{
    using System.Collections.Generic;
    using UnityEngine;

    internal sealed class NetworkedWorldCanvas : SingletonMonoBehaviour<NetworkedWorldCanvas>
    {
#pragma warning disable 0649
        public bool isHost;

        public bool writeRTT = true;

        public Transform[] transforms;

        [Header("Server")]
        public int port = 6700;

        public bool drawGizmos;

        private Server _server;

        [Header("Client")]
        public string serverIP = "192.168.0.13";

        public int serverPort = 6700;

        [Space]
        public int update_rate = 50;

        public bool prediction;

        public bool reconciliation;
        public bool interpolation;

        private ClientBase _client;
#pragma warning restore 0649

        private void OnValidate()
        {
            if (_client == null)
            {
                return;
            }

            UpdateParameters();
        }

        private void Awake()
        {
            if (this.isHost)
            {
                _server = new Server();
                _server.Initialize(this.port);

                var entity_id = _server.GetFreeEntityID();
                _server.CreateEntity(entity_id);

                var client = new HostClient();
                client.Initialize(_server, entity_id);

                _client = client;
            }
            else
            {
                var client = new NetworkedClient();
                client.Initialize(this.serverIP, this.serverPort);

                _client = client;
            }

            UpdateParameters();
        }

        private void OnDisable()
        {
            if (_client != null)
            {
                _client.TearDown();
                _client = null;
            }

            if (_server != null)
            {
                _server.TearDown();
                _server = null;
            }
        }

        private void UpdateParameters()
        {
            var reconciliation = this.reconciliation;
            var prediction = this.prediction;

            // Client Side Prediction disabled => disable Server Reconciliation.
            if (_client.client_side_prediction && !prediction)
            {
                reconciliation = false;
            }

            // Server Reconciliation enabled => enable Client Side Prediction.
            if (!_client.server_reconciliation && !prediction)
            {
                prediction = true;
            }

            _client.client_side_prediction = prediction;
            _client.server_reconciliation = reconciliation;
            _client.entity_interpolation = interpolation;
            _client.SetUpdateRate((ulong)this.update_rate);
        }

        private void Update()
        {
            if (_client != null)
            {
                _client.key_left = UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.A);
                _client.key_right = UnityEngine.Input.GetKey(KeyCode.RightArrow) || UnityEngine.Input.GetKey(KeyCode.D);
                _client.key_up = UnityEngine.Input.GetKey(KeyCode.UpArrow) || UnityEngine.Input.GetKey(KeyCode.W);
                _client.key_down = UnityEngine.Input.GetKey(KeyCode.DownArrow) || UnityEngine.Input.GetKey(KeyCode.S);

                _client.Tick();
            }

            if (_server != null)
            {
                _server.Tick();
            }
        }

        // Caller id 0 = server, id 1 = client 1, id 2 = client 2
        public void RenderWorld(List<Entity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == null)
                {
                    Debug.LogWarning("Null entity at: " + i);
                    continue;
                }

                var tr = this.transforms[entity.entity_id];
                tr.position = entity.position;
            }
        }

        private void OnGUI()
        {
            if (this.writeRTT && _client != null)
            {
                GUI.Box(new Rect(5f, 5f, 160f, 40f), string.Concat("RTT (ms): ", _client.averageRoundTripTimeMS));
            }
        }

        private void OnDrawGizmos()
        {
            if (this.drawGizmos && _server != null)
            {
                _server.OnDrawGizmos();
            }
        }
    }
}