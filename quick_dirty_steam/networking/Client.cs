using System.Collections.Generic;
using Steamworks;
using QuickDirtySteam.Matchmaking;
using QuickDirtySteam.Networking.Connectivity;

namespace QuickDirtySteam.Networking
{
    public static class Client
    {
        public delegate void ConnectionStatusChangedEventHandler();

        /// <summary>
        /// Emitted when a Connection Request has been Accepted by the Remote Host
        /// </summary>
        public static ConnectionStatusChangedEventHandler OnConnectionRequestAccepted;
        
        /// <summary>
        /// Emitted when a Connection Request has been Rejected by the Remote Host
        /// </summary>
        public static ConnectionStatusChangedEventHandler OnConnectionRequestRejected;
        
        /// <summary>
        /// Emitted when a Connection has been Closed either due to Local Problems or Willingly by Calling <see cref="Client.Disconnect"/>
        /// </summary>
        public static ConnectionStatusChangedEventHandler OnConnectionClosedByClient;

        /// <summary>
        /// Emitted when a Connection has been Closed by the Server either due to Loss of Connection or Willingly
        /// </summary>
        public static ConnectionStatusChangedEventHandler OnConnectionClosedByServer;

        /// <summary>
        /// True when the Client is Connected to a Server
        /// </summary>
        public static bool IsConnected { get; private set; }

        static LocalConnection Connection; 


        static Client() 
        {
            Lobby.Left += OnLobbyLeft;
            Lobby.Kicked += OnLobbyLeft;
        }


        /*********************************************************************/
        /* Connection State Changers *****************************************/
        /*********************************************************************/
        /// <summary>
        /// <para>Starts an Attempt to Connect to the Server of Whomever is Currently set to be <see cref="Lobby.OwnerID"/></para>
        /// <para>This can only Work if the Following Conditions are met:</para> 
        /// <list type="bullet">
        ///     <item>A Lobby has been Entered</item>
        ///     <item>A Server has been Started by the Lobby Owner</item>
        ///     <item>No Connection to Another Server is Currently Active</item>
        /// </list>
        /// <para>Emits <see cref="Client.OnConnectionRequestAccepted"/> if The Connection Request was Accepted</para>
        /// <para>Emits <see cref="Client.OnConnectionRequestRejected"/> if The Connection Request was Declined or Timed out</para>
        /// </summary>
        public static bool Connect() 
        {
            if (Lobby.IsValid)       
                if (!IsConnected && Connection == null) 
                {
                    var connection = SendConnectionRequest();

                    if (connection != null)
                        Initialize(connection);
                }
                else
                    Console.LogWarning($"Client: Tried to connect to a new Server before Leaving the Current One");
            else
                Console.LogWarning($"Client: Tried to Connect to a Server before Entering a Lobby");

            return false;
        }

        /// <summary>
        /// <para>Closes an Active Connection to the Server</para>
        /// <para>
        ///     Note: It is Seen as an Error not to Send a 'Goodbye Message' to the Server prior to Calling this. 
        ///           Both Parties should Know about the Disconnect of the Other and Close their Side of the Connection
        /// </para>
        /// </summary>
        public static void Disconnect() 
        {
            if (IsConnected)
            {
                Console.LogInfo($"Client: Disconnected");
                OnLocalConnectionClosedByLocalHost(null);
            }
            else
                Console.LogWarning($"Client: Tried to Disconnect without Establishing a valid Connection first");
        }

        static LocalConnection SendConnectionRequest()
        {
            var remoteIndentity = new SteamNetworkingIdentity();
            remoteIndentity.SetSteamID64(Lobby.OwnerID);

            var connectionHandle = SteamNetworkingSockets.ConnectP2P(ref remoteIndentity, 0, 0, null);

            if (connectionHandle != HSteamNetConnection.Invalid) 
            {
                Console.LogDebug($"Client: Sending a Connetion Request on Connection with ID: '{connectionHandle.m_HSteamNetConnection}'");
                
                var connection = new LocalConnection(remoteIndentity, connectionHandle);
                return connection;
            }

            Console.LogWarning($"Client: Failed to Create a Valid Connection");
            return null;
        }

        // Catches the Case where the Lobby was Left Prior to Closing the Connection
        static void OnLobbyLeft() 
        {
            if (IsConnected)
            {
                Console.LogDebug($"Client: The Lobby has been left, disconnecting");
                Disconnect();
            }
        }


        /*********************************************************************/
        /* Connection State Change Handlers **********************************/
        /*********************************************************************/
        static void OnLocalConnectionAccepted(Connection connection) 
        {
            Console.LogInfo($"Client: Connection Request was Accepted");

            IsConnected = true;
            OnConnectionRequestAccepted?.Invoke();
        }

        static void OnLocalConnectionRejected(Connection connection)
        {
            Console.LogInfo($"Client: Connection Request was Rejected");
            
            ConnectionCloser.CloseDeadConnection(Connection);
            Invalidate();
            OnConnectionRequestRejected?.Invoke();
        }

        static void OnLocalConnectionClosedByLocalHost(Connection connection) 
        {
            ConnectionCloser.CloseOpenConnection(Connection);
            Invalidate();
            OnConnectionClosedByClient?.Invoke();
        }

        static void OnLocalConnectionClosedByRemoteHost(Connection connection)
        {
            ConnectionCloser.CloseDeadConnection(Connection);
            Invalidate();
            OnConnectionClosedByServer?.Invoke();
        }


        /*********************************************************************/
        /* Messaging *********************************************************/
        /*********************************************************************/
        /// <summary>
        /// Sends a Message to The Server
        /// </summary>
        public static void SendMessage(byte[] buffer) 
        {
            if (IsConnected)
                MessageSender.Send(Connection.ConnectionHandle, buffer);
            else
                Console.LogWarning($"Client: Tried to send a Message over an invalid Connection");
        }

        /// <summary>
        /// <para>Bundles up all Received Messages and Returns them for further Processing</para>
        /// <para>This Should Ideally be Called once a Frame</para>
        /// </summary>
        /// <returns>
        ///     <para><c>A List of byte[]</c> which can be Empty if No Messages were Sent</para>
        /// </returns>
        public static List<byte[]> PollMessages() 
        {
            if (IsConnected)
                return MessageReceiver.Receive(Connection.ConnectionHandle);
            else
            {
                Console.LogWarning($"Tried to Poll Messages before Establishing a Connection to a Server");
                return null;
            }
        }


        /*********************************************************************/
        /* Helper Methods ****************************************************/
        /*********************************************************************/
        static void Initialize(LocalConnection connection) 
        {
            Connection = connection;

            Connection.Accepted += OnLocalConnectionAccepted;
            Connection.Rejected += OnLocalConnectionRejected;
            Connection.ClosedByLocalHost += OnLocalConnectionClosedByLocalHost;
            Connection.ClosedByRemoteHost += OnLocalConnectionClosedByRemoteHost;
        }

        static void Invalidate() 
        {
            Connection.Accepted -= OnLocalConnectionAccepted;
            Connection.Rejected -= OnLocalConnectionRejected;
            Connection.ClosedByLocalHost -= OnLocalConnectionClosedByLocalHost;
            Connection.ClosedByRemoteHost -= OnLocalConnectionClosedByRemoteHost;

            Connection = null;

            IsConnected = false;
        }
    }
}