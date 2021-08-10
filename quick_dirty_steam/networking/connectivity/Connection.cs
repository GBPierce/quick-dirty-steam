using System;
using Steamworks;

namespace QuickDirtySteam.Networking.Connectivity 
{
    internal class Connection : IEquatable<Connection>
    {
        protected Callback<SteamNetConnectionStatusChangedCallback_t> callbackSteamNetConnectionStatusChanged;

        public delegate void StatusChangedEventHandler(Connection connection);
        public event StatusChangedEventHandler ClosedByLocalHost;
        public event StatusChangedEventHandler ClosedByRemoteHost;

        public CSteamID ID { get => remoteIdentity.GetSteamID(); }
        public ulong ID64 { get => remoteIdentity.GetSteamID64(); } 
        public HSteamNetConnection ConnectionHandle{ get => new HSteamNetConnection(connectionHandle.m_HSteamNetConnection); }
        
        protected SteamNetworkingIdentity remoteIdentity;
        protected HSteamNetConnection connectionHandle;


        public Connection(SteamNetworkingIdentity remoteIdentity, HSteamNetConnection connectionHandle)
        {
            this.remoteIdentity = remoteIdentity;
            this.connectionHandle = connectionHandle;

            callbackSteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);
        }

        ~Connection()
            => callbackSteamNetConnectionStatusChanged.Dispose();

        public bool Equals(Connection other)
            => other != null && ID64 == other.ID64;

        protected void OnClosedByLocalHost()
            => ClosedByLocalHost?.Invoke(this);

        protected void OnClosedByRemoteHost()    
            => ClosedByRemoteHost?.Invoke(this);

        protected virtual void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t callback) { }
        protected virtual bool IsAffectedByCallback(SteamNetConnectionStatusChangedCallback_t callback) { return false; }
    }
}