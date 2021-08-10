using Steamworks;

namespace QuickDirtySteam.Networking.Connectivity 
{
    internal class RemoteConnection : Connection
    {
        HSteamListenSocket listenSocket;

        public RemoteConnection(SteamNetConnectionStatusChangedCallback_t callback) : base(callback.m_info.m_identityRemote, callback.m_hConn) 
            => listenSocket = callback.m_info.m_hListenSocket;

        protected override void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t callback)
        {
            if (IsAffectedByCallback(callback)) 
                switch (callback.m_info.m_eState)
                {
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                        if (callback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
                            Console.LogWarning($"RemoteConnection: Client: '{SteamFriends.GetFriendPersonaName(ID)}' with ID: '{ID64}' on Connection with ID: '{connectionHandle.m_HSteamNetConnection}' Disconnected");

                        OnClosedByRemoteHost();
                        break;
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                        if (callback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
                            Console.LogWarning($"RemoteConnection: Lost Connection to Client: '{SteamFriends.GetFriendPersonaName(ID)}' with ID: '{ID64}' on Connection with ID: '{connectionHandle.m_HSteamNetConnection}' due to a Local Problem");
                        else if (callback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting || callback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute)
                            Console.LogWarning($"RemoteConnection: Connection to Client: '{SteamFriends.GetFriendPersonaName(ID)}' with ID: '{ID64}' on Connection with ID: '{connectionHandle.m_HSteamNetConnection}' has been Closed because Nobody Answered its' Connection Request");
                        
                        OnClosedByLocalHost();
                        break;
                }
        }

        protected override bool IsAffectedByCallback(SteamNetConnectionStatusChangedCallback_t callback) 
            => listenSocket != HSteamListenSocket.Invalid && listenSocket == callback.m_info.m_hListenSocket && 
               connectionHandle != HSteamNetConnection.Invalid && connectionHandle == callback.m_hConn;
    }
}