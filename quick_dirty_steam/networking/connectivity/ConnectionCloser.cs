using Steamworks;

namespace QuickDirtySteam.Networking.Connectivity 
{
    internal static class ConnectionCloser
    {
        // TODO: Remove the Methods accepting Connections and Replace them with HSteamNetConnection
        public static void CloseOpenConnection(Connection connection) 
        {
            if (SteamNetworkingSockets.CloseConnection(connection.ConnectionHandle, 0, string.Empty, true))
                Console.LogDebug($"ConnectionCloser: Closed Existing Connection to User: '{SteamFriends.GetFriendPersonaName(connection.ID)}' on Connection with ID: '{connection.ConnectionHandle}'");
            else
                Console.LogError($"ConnectionCloser: Failed to Close Existing Connection to User: '{SteamFriends.GetFriendPersonaName(connection.ID)}' on Connection with ID: '{connection.ConnectionHandle}'");
        }

        public static void CloseOpenConnection(HSteamNetConnection connectionHandle) 
        {
            SteamNetworkingSockets.GetConnectionUserData(connectionHandle);
            if (SteamNetworkingSockets.CloseConnection(connectionHandle, 0, string.Empty, true))
            {}    //Console.LogDebug($"ConnectionCloser: Closed Existing Connection to User: '{SteamFriends.GetFriendPersonaName(connection.ID)}' on Connection with ID: '{connection.ConnectionHandle}'");
            else{}
                //Console.LogError($"ConnectionCloser: Failed to Close Existing Connection to User: '{SteamFriends.GetFriendPersonaName(connection.ID)}' on Connection with ID: '{connection.ConnectionHandle}'");
        }

        public static void CloseDeadConnection(Connection connection)
        {
            if (SteamNetworkingSockets.CloseConnection(connection.ConnectionHandle, 0, string.Empty, false))
                Console.LogDebug($"ConnectionCloser: Closed Dead Connection to User: '{SteamFriends.GetFriendPersonaName(connection.ID)}' on Connection with ID: '{connection.ConnectionHandle}'");
            else
                Console.LogError($"ConnectionCloser: Failed to Close Dead Connection to User: '{SteamFriends.GetFriendPersonaName(connection.ID)}' on Connection with ID: '{connection.ConnectionHandle}'");
        }
    }
}