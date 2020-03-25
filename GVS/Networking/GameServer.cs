using GVS.Networking.Players;
using GVS.World;
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
        /// <summary>
        /// The number of current uploads that are happening simultaneously.
        /// To limit the number of uploads allowed at once (each upload makes the game and network lag),
        /// you can set <see cref="MaxConcurrentUploads"/>.
        /// </summary>
        public int ActiveWorldUploadCount
        {
            get
            {
                return worldUploads.Count;
            }
        }
        /// <summary>
        /// The maximum number of concurrent world uploads that can happen at once.
        /// This is only really important for large maps that will have more than a few people trying
        /// to connect at once.
        /// Default value is 2.
        /// </summary>
        public int MaxConcurrentUploads { get; set; } = 2;
        /// <summary>
        /// The maximum amount of world chunks that can be sent each frame that an active upload is happening.
        /// So, the total number of chunks that could be uploaded any given frame is <see cref="MaxChunksToUploadPerFrame"/> * <see cref="MaxConcurrentUploads"/>.
        /// Making this value smaller will put less strain on the server and it's network, but will give longer world download times
        /// to clients. For small maps with a small number of players, this will have basically no effect. Only tweak for larger maps
        /// with players entering and leaving more frequently.
        ///
        /// Default value is 8.
        /// </summary>
        public int MaxChunksToUploadPerFrame { get; set; } = 4;

        private List<ActiveWorldUpload> worldUploads;
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

            this.connectedPlayers = new List<Player>();
            this.idToPlayer = new Dictionary<uint, Player>();
            this.remoteIDToPlayer = new Dictionary<long, HumanPlayer>();
            this.worldUploads = new List<ActiveWorldUpload>();

            base.SetHandler(NetMessageType.Req_BasicServerInfo, (id, msg) =>
            {
                // Send a little info about the server and the map...
                NetOutgoingMessage first = CreateBaseMapInfo();
                server.SendMessage(first, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                // The client should send back a response asking for full map info. (see below for response)
            });

            base.SetHandler(NetMessageType.Req_WorldChunks, (id, msg) =>
            {
                // Client has request for map/entity data to be sent.
                // TODO check that the client hasn't already requested this before.

                // Send map chunks.

                // Add a new world upload. TODO also upload entities.
                var upload = new ActiveWorldUpload(msg.SenderConnection, Main.Map.GetNumberOfNetChunks(), MaxChunksToUploadPerFrame);
                worldUploads.Add(upload);

                Trace($"Now starting to send {upload.TotalChunksToSend} chunks of map data...");
            });

            // Add status change listener.
            base.OnStatusChange += (conn, status, msg) =>
            {
                HumanPlayer player = GetPlayer(conn);
                switch (status)
                {
                    case NetConnectionStatus.Connected:
                        // At this point, the HumanPlayer object has already been set up because it's connection was approved.
                        // Nothing to do here for now.
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
                msg.SenderConnection.Approve();
            });
        }

        private NetOutgoingMessage CreateBaseMapInfo()
        {
            var msg = CreateMessage(NetMessageType.Data_BasicServerInfo);

            // Write size of world.
            int width = Main.Map.Width;
            int depth = Main.Map.Depth;
            int height = Main.Map.Height;
            msg.Write(new Point3D(width, depth, height));

            // Write number of world chunks to send.
            msg.Write(Main.Map.GetNumberOfNetChunks());

            return msg;
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
            bool shouldProcess = base.ProcessMessages(out int _);

            if (shouldProcess)
            {
                UpdateUploads();
            }
        }

        private void UpdateUploads()
        {
            int processed = 0;
            for (int i = 0; i < worldUploads.Count; i++)
            {
                var upload = worldUploads[i];

                upload.Tick(this);
                processed++;

                if (upload.IsDone)
                {
                    worldUploads.RemoveAt(i);
                    i--;
                }

                if (processed >= MaxConcurrentUploads)
                    break;
            }
        }

        public void SendMessage(NetConnection conn, NetOutgoingMessage msg, NetDeliveryMethod delivery)
        {
            server.SendMessage(msg, conn, delivery);
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
            if(worldUploads != null)
            {
                worldUploads.Clear();
                worldUploads = null;
            }

            base.Dispose();
        }

        private class ActiveWorldUpload
        {
            public bool IsDone
            {
                get
                {
                    return sentIndex == TotalChunksToSend;
                }
            }

            public readonly int TotalChunksToSend;

            private readonly int maxChunksPerFrame;
            private readonly NetConnection targetClient;
            private int sentIndex;

            public ActiveWorldUpload(NetConnection client, int toSendCount, int maxChunksPerFrame)
            {
                this.targetClient = client;
                this.maxChunksPerFrame = maxChunksPerFrame;
                this.TotalChunksToSend = toSendCount;
            }

            public void Tick(GameServer server)
            {
                for (int i = 0; i < maxChunksPerFrame; i++)
                {
                    if (sentIndex == TotalChunksToSend)
                        break;

                    var msg = Main.Map.NetSerializeAllTiles(server, sentIndex);
                    server.SendMessage(targetClient, msg, NetDeliveryMethod.ReliableUnordered);
                    sentIndex++;
                }
            }
        }
    }
}
