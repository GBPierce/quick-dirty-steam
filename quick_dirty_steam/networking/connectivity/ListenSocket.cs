using System.Collections.Generic;
using Steamworks; 

namespace QuickDirtySteam.Networking.Connectivity
{
    static class ListenSocket 
    {
        static Callback<SteamNetConnectionStatusChangedCallback_t> callbackSteamNetConnectionStatusChanged;

        public delegate void ConnectionStatusChangedEventHandler(ulong remoteID);
        public static event ConnectionStatusChangedEventHandler ConnectionRequested;
        public static event ConnectionStatusChangedEventHandler ConnectionAccepted;
        public static event ConnectionStatusChangedEventHandler ConnectionRejected;
        public static event ConnectionStatusChangedEventHandler ConnectionClosedByLocalHost;
        public static event ConnectionStatusChangedEventHandler ConnectionClosedByRemoteHost;

        public static bool IsOpen { get => ListenSocketHandle != HSteamListenSocket.Invalid; }

        static HSteamListenSocket ListenSocketHandle = HSteamListenSocket.Invalid;

        static List<Connectivity.RemoteConnection> OpenConnections = new List<Connectivity.RemoteConnection>();
        static List<Connectivity.RemoteConnection> PendingConnections = new List<Connectivity.RemoteConnection>();


        static ListenSocket() 
            => callbackSteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);


        /*********************************************************************/
        /* Listen Socket State Changers **************************************/
        /*********************************************************************/
        public static bool Open() 
        {
            if (ListenSocketHandle == HSteamListenSocket.Invalid)
            {
                ListenSocketHandle = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);

                if (ListenSocketHandle != HSteamListenSocket.Invalid)
                {
                    Console.LogDebug($"Listen Socket: Successfully Created a Listen Socket with ID: '{ListenSocketHandle.m_HSteamListenSocket}'");
                    return true;
                }
                else
                    Console.LogError($"Listen Socket: Failed to Create a Listen Socket");
            }
            else
                Console.LogWarning($"Listen Socket: Tried to Create a Listen Socket prior to Closing the Existing one");
            
            return false;
        }

        public static void Close() 
        {
            if (ListenSocketHandle != HSteamListenSocket.Invalid) 
            {
                Console.LogDebug($"Listen Socket: Closing the Listen Socket");

                foreach (var connection in OpenConnections)
                {
                    UnsubscribeFromConnectionEvents(connection);
                    Connectivity.ConnectionCloser.CloseOpenConnection(connection.ConnectionHandle);
                    ConnectionClosedByLocalHost?.Invoke(connection.ID64);
                }
                foreach (var connection in PendingConnections)
                {
                    UnsubscribeFromConnectionEvents(connection);
                    Connectivity.ConnectionCloser.CloseOpenConnection(connection.ConnectionHandle);
                    ConnectionClosedByLocalHost?.Invoke(connection.ID64);
                }

                OpenConnections.Clear();
                PendingConnections.Clear();

                if (SteamNetworkingSockets.CloseListenSocket(ListenSocketHandle))
                    Console.LogDebug($"Listen Socekt: Successfully Closed the Listen Socket");
                else
                    Console.LogError($"Listen Socket: Failed to Close the Listen Socket");

                ListenSocketHandle = HSteamListenSocket.Invalid;
            }
            else
                Console.LogWarning($"ListenSocket: Tried to Close a Listen Socket before Creating One");
        }


        /*********************************************************************/
        /* Connection State Changers *****************************************/
        /*********************************************************************/
        public static void AcceptRequest(ulong remoteID) 
        {
            if (IsOpen)
            {
                var connection = PendingConnections.Find(con => con.ID64 == remoteID);

                if (connection != null)
                {
                    var result = SteamNetworkingSockets.AcceptConnection(connection.ConnectionHandle);

                    switch (result) 
                    {
                        case EResult.k_EResultOK:
                            Console.LogInfo($"ListenSocket: Successfully Accepted Connection to Remote Host: '{SteamFriends.GetFriendPersonaName(connection.ID)}'");
                            
                            OpenConnections.Add(connection);
                            ConnectionAccepted?.Invoke(remoteID);
                            break;
                        default:
                            Console.LogWarning($"ListenSocket: Failed to Accept Connection to Remote Host: '{SteamFriends.GetFriendPersonaName(connection.ID)}'");
                            
                            Connectivity.ConnectionCloser.CloseOpenConnection(connection);
                            ConnectionClosedByLocalHost?.Invoke(remoteID);
                            break;
                    }
                    
                    PendingConnections.Remove(connection);
                }
                else
                    Console.LogWarning($"ListenSocket: Failed to Accept Connection to Remote Host with ID: '{remoteID}' because the ID is Invalid");
            }
            else
                Console.LogWarning($"ListenSocket: Tried to Accept a Connection Request before Opening a Listen Socket");
        }

        public static void RejectRequest(ulong remoteID) 
        {
            if (IsOpen)
            {
                var connection = GetPendingConnection(remoteID);
                
                if (connection != null)
                {
                    Console.LogInfo($"ListenSocket: Rejected Connection to Remote Host: '{SteamFriends.GetFriendPersonaName(connection.ID)}'");
                    
                    Connectivity.ConnectionCloser.CloseDeadConnection(connection);
                    PendingConnections.Remove(connection);
                    ConnectionRejected(remoteID);
                }
                else
                    Console.LogWarning($"ListenSocket: Failed to Reject Connection Remote Host with ID: '{remoteID}' because the ID is Invalid");
            }
            else
                Console.LogWarning($"ListenSocket: Tried to Reject a Connection Request before Opening a Listen Socket");
        }

        public static void CloseConnection(ulong remoteID) 
        {
            if (IsOpen)
            {
                var connection = GetOpenConnection(remoteID);

                if (connection != null)
                {
                    Console.LogInfo($"Listen Socket: Closing the Connection to User: {SteamFriends.GetFriendPersonaName(connection.ID)}");
                    
                    Connectivity.ConnectionCloser.CloseOpenConnection(connection);
                    OpenConnections.Remove(connection);
                    ConnectionClosedByLocalHost?.Invoke(remoteID);
                }
                else
                    Console.LogWarning($"ListenSocket: Failed to Close Connection to Remote Host with ID: '{remoteID}' because the ID is Invalid");
            }
            else
                Console.LogWarning($"ListenSocket: Tried to Close a Connection before Opening a Listen Socket");
        }


        /*********************************************************************/
        /* Connection State Change Handlers **********************************/
        /*********************************************************************/
        static void OnConnectionClosedByLocalHost(Connectivity.Connection connection) 
        {
            var remoteConnection = (Connectivity.RemoteConnection)connection;

            if (!PendingConnections.Remove(remoteConnection))
                OpenConnections.Remove(remoteConnection);

            Connectivity.ConnectionCloser.CloseDeadConnection(remoteConnection);
            UnsubscribeFromConnectionEvents(remoteConnection);
            ConnectionClosedByLocalHost(remoteConnection.ID64);
        }
        
        static void OnConnectionClosedByRemoteHost(Connectivity.Connection connection) 
        { 
            var remoteConnection = (Connectivity.RemoteConnection)connection;

            if (!PendingConnections.Remove(remoteConnection))
                OpenConnections.Remove(remoteConnection);
            
            Connectivity.ConnectionCloser.CloseDeadConnection(remoteConnection);
            UnsubscribeFromConnectionEvents(remoteConnection);
            ConnectionClosedByRemoteHost(remoteConnection.ID64);
        }


        /*********************************************************************/
        /* Messaging *********************************************************/
        /*********************************************************************/
        public static void SendMessage(ulong remoteID, byte[] buffer)
        {
            if (IsOpen)
            {
                var connection = GetOpenConnection(remoteID);

                if (connection != null)
                {
                    Console.LogDebug($"ListenSocket: Sending Message to Remote Host '{SteamFriends.GetFriendPersonaName(connection.ID)}'");
                    MessageSender.Send(connection.ConnectionHandle, buffer);
                }
                else
                    Console.LogWarning($"ListenSocket: Failed to Send Message to Remote Host with ID: '{remoteID}' because the ID is Invalid");
            }
            else
                Console.LogWarning($"ListenSocket: Tried to Send a Message before Opening the Listen Socket");
        }

        public static List<byte[]> PollMessages(ulong remoteID)
        {
            if (IsOpen)
            {
                var connection = GetOpenConnection(remoteID);

                if (connection != null)
                    return MessageReceiver.Receive(connection.ConnectionHandle);
                else
                    Console.LogWarning($"ListenSocket: failed to Poll Messages from Remote Host with ID: '{remoteID}' because the ID is Invalid");
            }
            else
                Console.LogWarning("ListenSocket: Tried to Poll Messages before Opening a Listen Socket");
            
            return null;
        }


        /*********************************************************************/
        /* Connection Creation ***********************************************/
        /*********************************************************************/
        static void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t callback)
        {
            CSteamID ID = callback.m_info.m_identityRemote.GetSteamID();
            ulong ID64 = ID.m_SteamID;

            if (IsAffectedByCallback(callback))
            {
                if (callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
                {
                    Connectivity.RemoteConnection connection;

                    connection = OpenConnections.Find(con => con.ID64 == ID64);
                    if (connection != null) 
                    {
                        Console.LogWarning($"Listen Socket: Already Connected Client: '{SteamFriends.GetFriendPersonaName(ID)}' with ID: '{connection.ID}' on Connection with ID: '{connection.ConnectionHandle}' sent another Connection Request");

                        Connectivity.ConnectionCloser.CloseOpenConnection(callback.m_hConn);
                        return;
                    }

                    connection = PendingConnections.Find(con => con.ID64 == ID64);
                    if (connection != null)
                    {
                        Console.LogWarning($"Listen Socket: A Client awaiting a Connection Request Response: '{SteamFriends.GetFriendPersonaName(connection.ID)}' with ID: '{connection.ID}' on Connection with ID: '{connection.ConnectionHandle}' sent another Connection Request");
                        
                        Connectivity.ConnectionCloser.CloseOpenConnection(callback.m_hConn);
                        return;
                    }

                    
                    Console.LogDebug($"ListenSocket: Received a Connection Request from Remote Host: '{SteamFriends.GetFriendPersonaName(ID)}'");
                    
                    connection = new Connectivity.RemoteConnection(callback);
                    SubscribeToConnectionEvents(connection);
                    PendingConnections.Add(connection);
                    ConnectionRequested?.Invoke(ID64);        
                }
            }   
        }

        static bool IsAffectedByCallback(SteamNetConnectionStatusChangedCallback_t callback)
            => ListenSocketHandle != HSteamListenSocket.Invalid && ListenSocketHandle == callback.m_info.m_hListenSocket;


        /*********************************************************************/
        /* Helper Methods ****************************************************/
        /*********************************************************************/
        static void SubscribeToConnectionEvents(Connectivity.RemoteConnection connection) 
        {
            connection.ClosedByLocalHost += OnConnectionClosedByLocalHost;
            connection.ClosedByRemoteHost += OnConnectionClosedByRemoteHost;
        }

        static void UnsubscribeFromConnectionEvents(Connectivity.RemoteConnection connection)
        {
            connection.ClosedByLocalHost -= OnConnectionClosedByLocalHost;
            connection.ClosedByRemoteHost -= OnConnectionClosedByRemoteHost;
        }

        static Connectivity.RemoteConnection GetOpenConnection(ulong remoteID) 
            => OpenConnections.Find(connection => connection.ID64 == remoteID);
            
        static Connectivity.RemoteConnection GetPendingConnection(ulong remoteID)
            => PendingConnections.Find(connection => connection.ID64 == remoteID);
    }
}