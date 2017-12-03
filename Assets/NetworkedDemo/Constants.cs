namespace NetworkedDemo
{
    using UnityEngine;

    public static class Constants
    {
        public const int MaxPlayers = 2;
        public const string HailSecret = "NetworkedDemoHail";
        public const string AppId = "NetworkedDemo";
        public const int ServerUpdateRate = 10;

        public const Lidgren.Network.NetDeliveryMethod DeliveryMethod = Lidgren.Network.NetDeliveryMethod.ReliableOrdered;

        public static readonly Vector2[] SpawnPoints = new Vector2[]
        {
            new Vector2(-3, -3f),
            new Vector2(3f, 3f)
        };
    }
}