using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace GVS.Networking
{
    public abstract class NetPeerBase : IDisposable
    {
        public static bool LogStatusChange = false;

        /// <summary>
        /// The configuration that was used to set up this end point.
        /// Changes values on this will do nothing, it is intended for read-only purposes.
        /// </summary>
        public NetPeerConfiguration Config { get; private set; }

        public Action<NetConnection, NetConnectionStatus, NetIncomingMessage> OnStatusChange;

        protected NetPeer peer;
        protected string tag;
        private Dictionary<NetIncomingMessageType, Action<NetIncomingMessage>> baseHandlers;

        protected NetPeerBase(NetPeerConfiguration config)
        {
            this.Config = config;
            this.baseHandlers = new Dictionary<NetIncomingMessageType, Action<NetIncomingMessage>>();

            // Add some default handlers
            SetBaseHandler(NetIncomingMessageType.DebugMessage, (msg) =>
            {
                Log(msg.ReadString());
            });
            SetBaseHandler(NetIncomingMessageType.WarningMessage, (msg) =>
            {
                Warn(msg.ReadString());
            });
            SetBaseHandler(NetIncomingMessageType.ErrorMessage, (msg) =>
            {
                Error(msg.ReadString());
            });
            SetBaseHandler(NetIncomingMessageType.StatusChanged, (msg) =>
            {
                var status = (NetConnectionStatus) msg.ReadByte();
                if(LogStatusChange)
                    Trace($"Status: {msg.SenderEndPoint}, {status}");
                OnStatusChange?.Invoke(msg.SenderConnection, status, msg);
            });
        }

        public void SetBaseHandler(NetIncomingMessageType type, Action<NetIncomingMessage> action)
        {
            if (!baseHandlers.ContainsKey(type))
                baseHandlers.Add(type, action);
            else
                baseHandlers[type] = action;
        }

        protected void ProcessMessages()
        {
            if (peer == null)
                return;
            if (peer.Status == NetPeerStatus.NotRunning)
                return;

            NetIncomingMessage msg;
            while ((msg = peer.ReadMessage()) != null)
            {
                NetIncomingMessageType type = msg.MessageType;

                // This is not the fastest way to do this...
                if (baseHandlers.ContainsKey(type))
                {
                    baseHandlers[type].Invoke(msg);
                }

                peer.Recycle(msg);
            }
        }

        protected void Trace(string msg)
        {
            Debug.Trace($"[{tag}]: {msg}");
        }

        protected void Log(string msg)
        {
            Debug.Log($"[{tag}]: {msg}");
        }

        protected void Warn(string msg)
        {
            Debug.Warn($"[{tag}]: {msg}");
        }

        protected void Error(string msg)
        {
            Debug.Error($"[{tag}]: {msg}");
        }

        public virtual void Dispose()
        {
            Config = null;
            if(baseHandlers != null)
            {
                baseHandlers.Clear();
                baseHandlers = null;
            }
        }
    }
}
