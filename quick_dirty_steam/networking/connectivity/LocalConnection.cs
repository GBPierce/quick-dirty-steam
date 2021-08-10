using Steamworks;

namespace QuickDirtySteam.Networking.Connectivity 
{
    internal class LocalConnection : Connection
    {
        public event StatusChangedEventHandler Accepted;
        public event StatusChangedEventHandler Rejected; 


        public LocalConnection(SteamNetworkingIdentity remoteIdentity, HSteamNetConnection connectionHandle) : base(remoteIdentity, connectionHandle)
        {   
        }

        protected override void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t callback)
        {
            if (IsAffectedByCallback(callback)) 
                switch (callback.m_info.m_eState)
                {
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                        if (callback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting || callback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute)
                        {
                            Console.LogDebug($"LocalConnection: Connection Request has been Rejected");
                            Rejected?.Invoke(this);
                        }
                        else if (callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer)
                        {
                            Console.LogDebug($"LocalConnection: Connection has been Closed by the Remote Host");
                            OnClosedByRemoteHost();
                        }
                        else
                        {
                            Console.LogDebug($"LocalConnection: Connection has been Closed by the Local Host");
                            OnClosedByLocalHost();
                        }
                        
                        callbackSteamNetConnectionStatusChanged.Dispose();
                        break;
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                        Console.LogDebug("LocalConnection: Connection Request has been Accepted");
                        Accepted?.Invoke(this);
                        break;
                }
        }

        protected override bool IsAffectedByCallback(SteamNetConnectionStatusChangedCallback_t callback) 
            => connectionHandle != HSteamNetConnection.Invalid && connectionHandle == callback.m_hConn;
    }
}