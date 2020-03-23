using Lidgren.Network;

namespace GVS.Networking.Players
{
    /// <summary>
    /// A human player is a player, who is human. Duh. As opposed to a bot.
    /// See <see cref="Player"/>. Most of the data in this class is only valid on the server.
    /// </summary>
    public class HumanPlayer : Player
    {
        /// <summary>
        /// Gets the network connection to the remote client. May be null if the client has disconnected.
        /// This has lots of useful information such as client IP address, RTT and other.
        /// </summary>
        public NetConnection ConnectionToClient { get; internal set; }
        /// <summary>
        /// Gets a unique identifier for this player's network connection.
        /// This is NOT the same as <see cref="Player.ID"/>, because only human players have client connections.
        /// This value can be used to find out which Player a message has come from, by using <see cref="GameServer.GetPlayer(long)"/>.
        /// </summary>
        public long RemoteUniqueIdentifier
        {
            get
            {
                return ConnectionToClient.RemoteUniqueIdentifier;
            }
        }

        public HumanPlayer(string name) : base(name)
        {
        }

        public override string ToString()
        {
            return $"[{ID}] Human '{Name}' (from {ConnectionToClient.RemoteEndPoint}, {RemoteUniqueIdentifier})";
        }
    }
}
