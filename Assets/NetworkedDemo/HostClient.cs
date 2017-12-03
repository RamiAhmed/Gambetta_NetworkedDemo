namespace NetworkedDemo
{
    public class HostClient : ClientBase
    {
        private Server _server;

        public override int averageRoundTripTimeMS
        {
            get
            {
                if (_server == null)
                {
                    return 0;
                }
                
                return _server.averageRoundTripTimeMS;
            }
        }

        internal void Initialize(Server server, int entity_id)
        {
            _server = server;
            Create(entity_id);
        }

        protected override void ProcessServerMessages()
        {
            // get world state and call OnData
            OnData(_server.world_state);
        }

        protected override void Send(Input input)
        {
            // inform server of input
            _server.OnData(input);
        }

        internal override void TearDown()
        {
            base.TearDown();
            _server = null;
        }
    }
}