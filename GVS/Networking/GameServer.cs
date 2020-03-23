using GVS.Networking.Players;
using Lidgren.Network;
using System.Collections.Generic;

namespace GVS.Networking
{
    public class GameServer : NetPeerBase
    {
        public NetPeerStatus Status
        {
            get
            {
                return server.Status;
            }
        }
        public bool IsRunning { get; private set; }
        /// <summary>
        /// The password that clients need to give before connecting.
        /// If null, no password is required. This can be changed once the server is
        /// running, or beforehand.
        /// </summary>
        public string Password { get; set; } = null;

        private List<Player> connectedPlayers;
        private Dictionary<uint, Player> idToPlayer;
        private Dictionary<long, HumanPlayer> remoteIDToPlayer;
        private NetServer server;
        private uint nextID;

        public GameServer(int port, int maxPlayers) : base(MakeConfig(port, maxPlayers))
        {
            server = new NetServer(base.Config);
            base.peer = server;
            base.tag = "Server";

            connectedPlayers = new List<Player>();
            idToPlayer = new Dictionary<uint, Player>();
            remoteIDToPlayer = new Dictionary<long, HumanPlayer>();

            // Add status change listener.
            base.OnStatusChange += (conn, status, msg) =>
            {
                HumanPlayer player = GetPlayer(conn);
                switch (status)
                {
                    case NetConnectionStatus.Connected:
                        // No need to do anything here, since the player should already be set up.
                        break;
                    case NetConnectionStatus.Disconnected:
                        // Player has left the game, bye!
                        // Remove the player associated with them.
                        if(player == null)
                        {
                            Error($"Remote client has disconnected, but a player object is not associated with them...");
                            break;
                        }

                        string text = msg.PeekString();
                        Log($"Client has disconnected: {text}");

                        RemovePlayer(player);
                        break;
                }
            };

            // Add handlers.
            base.SetBaseHandler(NetIncomingMessageType.ConnectionApproval, (msg) =>
            {
                // TODO add check for number of players currently in the game.

                // Password (or empty string).
                string password = msg.ReadString();
                if(this.Password != null && password.Trim() != this.Password.Trim())
                {
                    msg.SenderConnection.Deny("Incorrect password");
                    return;
                }

                // Player name.
                string name = msg.ReadString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    msg.SenderConnection.Deny("Invalid name");
                    return;
                }

                // Create player object for them.
                HumanPlayer p = new HumanPlayer(name);
                p.ConnectionToClient = msg.SenderConnection;

                // Add player to the game. (gives them an Id and stuff)
                AddPlayer(p);

                // Accept the connection, everything looks good!
                msg.SenderConnection.Approve(); // TODO send game info here.
            });
        }

        private static NetPeerConfiguration MakeConfig(int port, int maxPlayers)
        {
            var config = new NetPeerConfiguration(Net.APP_ID);
            config.Port = port;
            config.MaximumConnections = maxPlayers;
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            return config;
        }

        private void AddPlayer(Player p)
        {
            if (p == null)
                return;

            // Set unique id.
            p.ID = nextID;
            nextID++;

            connectedPlayers.Add(p);
            idToPlayer.Add(p.ID, p);
            if(p is HumanPlayer human)
            {
                remoteIDToPlayer.Add(human.RemoteUniqueIdentifier, human);
            }

            Log($"Player has joined: {p}");
        }

        private void RemovePlayer(uint id)
        {
            RemovePlayer(idToPlayer[id]);
        }

        private void RemovePlayer(Player p)
        {
            if (p == null)
                return;

            connectedPlayers.Remove(p);
            idToPlayer.Remove(p.ID);
            if(p is HumanPlayer human)
            {
                remoteIDToPlayer.Remove(human.RemoteUniqueIdentifier);
            }

            Log($"Player has left: {p}");
        }

        public Player GetPlayer(uint id)
        {
            if (idToPlayer.ContainsKey(id))
                return idToPlayer[id];
            else
                return null;
        }

        public HumanPlayer GetPlayer(NetIncomingMessage msg)
        {
            if (msg == null)
                return null;
            return GetPlayer(msg.SenderConnection);
        }

        public HumanPlayer GetPlayer(NetConnection connection)
        {
            if (connection == null)
                return null;
            long id = connection.RemoteUniqueIdentifier;
            return GetPlayer(id);
        }

        public HumanPlayer GetPlayer(long remoteID)
        {
            if (remoteIDToPlayer.ContainsKey(remoteID))
                return remoteIDToPlayer[remoteID];
            else
                return null;
        }

        public void Start()
        {
            if(IsRunning)
            {
                Warn("Server is already running.");
                return;
            }

            // Reset some values.
            nextID = 0;
            connectedPlayers.Clear();
            idToPlayer.Clear();
            remoteIDToPlayer.Clear();
            
            Trace($"Starting server on port {Config.Port}...");
            server.Start();
            IsRunning = true;
        }

        public void Update()
        {
            base.ProcessMessages();
        }

        public void Shutdown(string message)
        {
            if (!IsRunning)
            {
                Warn("Server is not running.");
                return;
            }

            Trace($"Shutting down server: {message}");
            server.Shutdown(message);
            IsRunning = false;

            connectedPlayers.Clear();
            idToPlayer.Clear();
            remoteIDToPlayer.Clear();
        }

        public override void Dispose()
        {
            if (IsRunning)
                Shutdown("Server has closed (dsp)");
            server = null;

            if(idToPlayer != null)
            {
                idToPlayer.Clear();
                idToPlayer = null;
            }
            if(connectedPlayers != null)
            {
                connectedPlayers.Clear();
                connectedPlayers = null;
            }
            if(remoteIDToPlayer != null)
            {
                remoteIDToPlayer.Clear();
                remoteIDToPlayer = null;
            }

            base.Dispose();
        }
    }
}
