using System;
using Lidgren.Network;

namespace GVS.Networking
{
    public class GameClient : NetPeerBase
    {
        public NetConnectionStatus ConnectionStatus
        {
            get
            {
                return client.ConnectionStatus;
            }
        }
        public Action<NetIncomingMessage> OnConnected;
        public Action<NetIncomingMessage> OnDisconnected;

        private NetClient client;

        public GameClient() : base(MakeConfig())
        {
            client = new NetClient(base.Config);
            base.peer = client;
            base.tag = "Client";

            base.OnStatusChange += (conn, status, msg) =>
            {
                switch (status)
                {
                    case NetConnectionStatus.Disconnected:

                        // Should have a string to read, reason for disconnect.
                        string text = msg.PeekString();
                        Log($"Disconnected. Reason: {text}");

                        OnDisconnected?.Invoke(msg);
                        break;

                    case NetConnectionStatus.Connected:
                        Log("Connected.");
                        OnConnected?.Invoke(msg);
                        break;
                }
            };
        }

        private static NetPeerConfiguration MakeConfig()
        {
            var config = new NetPeerConfiguration(Net.APP_ID);

            return config;
        }

        public bool Connect(string ip, int port)
        {
            if(ConnectionStatus != NetConnectionStatus.Disconnected)
            {
                Error($"Cannot connect now, wrong state: {ConnectionStatus}, expected Disconnected.");
                return false;
            }

            Debug.Trace($"Starting client connect to {ip} on port {port}");

            if(client.Status == NetPeerStatus.NotRunning)
                client.Start();

            client.Connect(ip, port, CreateHailMessage());

            return true;
        }

        private NetOutgoingMessage CreateHailMessage()
        {
            NetOutgoingMessage msg = client.CreateMessage(64);
            /*
             * 0. Server password (or empty string)
             * 1. Player name.
             */
            msg.Write(string.Empty);
            msg.Write($"James #{Rand.Range(0, 1000)}");

            return msg;
        }

        public void Update()
        {
            base.ProcessMessages();
        }

        public void Disconnect()
        {
            if (ConnectionStatus == NetConnectionStatus.Disconnected || ConnectionStatus == NetConnectionStatus.Disconnecting)
            {
                Warn("Already disconnected or disconnecting!");
                return;
            }

            client.Disconnect("Bye");
        }

        public override void Dispose()
        {
            if (ConnectionStatus != NetConnectionStatus.Disconnected)
                Disconnect();
            client = null;
            base.Dispose();
        }
    }
}
