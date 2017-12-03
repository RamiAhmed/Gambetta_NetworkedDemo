namespace NetworkedDemo
{
    using Lidgren.Network;
    using UnityEngine;

    public struct EntityState
    {
        public int entity_id;
        public Vector2 position;
        public int last_processed_input;

        public void Write(NetOutgoingMessage msgOut)
        {
            msgOut.Write(entity_id);
            msgOut.Write(position);
            msgOut.Write(last_processed_input);
        }

        public static EntityState Read(NetIncomingMessage msgIn)
        {
            return new EntityState()
            {
                entity_id = msgIn.ReadInt32(),
                position = msgIn.ReadVector2(),
                last_processed_input = msgIn.ReadInt32()
            };
        }
    }
}