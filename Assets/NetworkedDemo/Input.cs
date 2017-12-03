namespace NetworkedDemo
{
    using Lidgren.Network;
    using UnityEngine;

    public struct Input
    {
        public Vector2 move_input;
        public int input_sequence_number;
        public int entity_id;

        public void Write(NetOutgoingMessage msgOut)
        {
            msgOut.Write((byte)MessageType.Input);
            msgOut.Write(move_input);
            msgOut.Write(input_sequence_number);
            msgOut.Write(entity_id);
        }

        public static Input Read(NetIncomingMessage msgIn)
        {
            return new Input()
            {
                move_input = msgIn.ReadVector2(),
                input_sequence_number = msgIn.ReadInt32(),
                entity_id = msgIn.ReadInt32()
            };
        }
    }
}