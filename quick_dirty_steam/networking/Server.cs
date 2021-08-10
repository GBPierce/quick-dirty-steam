using System.Collections.Generic;
using QuickDirtySteam.Matchmaking;
using QuickDirtySteam.Networking.Connectivity;

namespace QuickDirtySteam.Networking
{   
    /// <summary>
    /// Override this Class to Implement your own Custom Server Logic
    /// </summary>
    public class Server
    {
        protected delegate void StateUpdatedEventHandler();
        protected delegate void ClientConnectionStatusChangedEventHandler(ulong clientID);
        protected delegate void MessageReceivedEventHandler(ulong clientID, List<byte[]> messages);

        /// <summary>
        /// Emitted after the Server was Started
        /// </summary>
        protected event StateUpdatedEventHandler Started;
        /// <summary>
        /// Emitted after the Server was Updated
        /// </summary>
        protected event StateUpdatedEventHandler Updated;
        /// <summary>
        /// <para>Emitted right before the Server Closes its Listen Socket</para>
        /// <para>Note: This would be the Perfect Time to Send a Final 'Goodbye Message' to all Connected Clients</para>  
        /// </summary>
        protected event StateUpdatedEventHandler Stopped;

        /// <summary>
        /// Emitted when a Client Requested to Enter the Server
        /// </summary>
        protected event ClientConnectionStatusChangedEventHandler ClientConnectionRequested;
        /// <summary>
        /// Emitted when a Clients Connection Request has been Accepted by Calling <see cref="AcceptConnectionReqeust"/>
        /// </summary>
        protected event ClientConnectionStatusChangedEventHandler ClientConnectionAccepted;
        /// <summary>
        /// Emitted when a Clients Connection Request has been Rejected by Calling <see cref="RejectConnectionRequest"/>
        /// </summary>
        protected event ClientConnectionStatusChangedEventHandler ClientConnectionRejected;
        /// <summary>
        /// Emitted when the Server Closed the Connection to a Client i.e. after Calling <see cref="CloseConnection"/>
        /// </summary> 
        protected event ClientConnectionStatusChangedEventHandler ClientConnectionClosedByServer;
        /// <summary>
        /// Emitted when a Client Closed the Connection to the Server 
        /// </summary>
        protected event ClientConnectionStatusChangedEventHandler ClientConnectionClosedByClient;

        /// <summary>
        /// Emitted when a Clients Connection has been Polled for Messages
        /// </summary>
        protected event MessageReceivedEventHandler MessagesReceived;

        public bool IsRunning { get; private set; }
        /// <summary>
        /// A List of all Currently Connected Clients
        /// </summary>
        protected List<ulong> Clients { get => new List<ulong>(clients); }
        List<ulong> clients = new List<ulong>();

        protected Server() { }

        /*********************************************************************/
        /* State Changers ****************************************************/
        /*********************************************************************/
        /// <summary>
        /// <para>Starts an Attempt to Start the Server</para>
        /// <para>This can only Work if the Following Conditions are met:</para> 
        /// <list type="bullet">
        ///     <item>A Lobby has been Entered</item>
        ///     <item>The Local User is the Owner of the Lobby</item>
        ///     <item>No Other Server is Currently Active</item>
        /// </list>
        /// <para>Emits <see cref="Started"/> when the Server Started Successfully</para>
        /// </summary>
        public bool Start() 
        {
            if (Lobby.IsValid)
                if (Lobby.IsLocalOwner)
                    if (!IsRunning) 
                        if (ListenSocket.Open())
                        {
                            SubscribeToListenSocketEvents();
                            IsRunning = true;
                            Started?.Invoke();
                            Console.LogInfo($"Server: Started the Server");
                            return true;
                        }
                        else
                            Console.LogWarning($"Server: Tried to Start the Server but Failed to Create a Listen Socket");
                    else
                        Console.LogWarning($"Server: Tried to Start an already Started Server");
                else
                    Console.LogWarning($"Server: Tried to Start a Server while not being the Lobby Owner");
            else
                Console.LogWarning($"Server: Tried to Start a Server before Creating a Lobby");
            
            return false;
        }

        /// <summary>
        /// <para>Stops the Currently Active Server</para>
        /// <para>Emits see <see cref="Stopped"/></para>
        /// </summary>
        public void Stop() 
        {
            if (IsRunning)
            {
                Stopped?.Invoke();
                IsRunning = false;
                ListenSocket.Close();
                UnsubscribeFromListenSocketEvents();
                Console.LogInfo($"Server: Stopped the Server");
            }
            else
                Console.LogWarning($"Server: Tried to Stop an already Stopped Server");
        }

        // Catches the Case where the Lobby was Left Prior to Stopping the Connection
        void OnLobbyLeft() 
        {
            if (IsRunning)
            {
                Console.LogDebug("Server: Lobby has been left, shutting down the Server");
                Stop();
            }
        }


        /*********************************************************************/
        /* Logic *************************************************************/
        /*********************************************************************/
        /// <summary>
        /// <para>Updates the Server, Forcefully Removes all Clients who are Not in the Users Lobby and Polls all Clients Messages</para>
        /// <para>This should Ideally be Called Once a Frame</para>
        /// <para>Emits <see cref="Updated"/></para>
        /// </summary>
        public void Update() 
        {
            if (IsRunning)
            {
                RemoveNonLobbyClients();
                PollMessages();
                Updated?.Invoke();
            }
            else
                Console.LogWarning($"Server: Tried to Update the Server before Starting it");
        }

        void RemoveNonLobbyClients() 
        {
            var clientsToRemove = new List<ulong>();
            
            foreach (var client in clients)
                if (!IsClientInLobby(client))
                    clientsToRemove.Add(client);

            foreach (var client in clientsToRemove)
            {
                Console.LogWarning($"Server: Closing Connection to Client with ID: '{client}' because the Client is not in the Lobby");
                ListenSocket.CloseConnection(client);
            }
        }


        /*********************************************************************/
        /* Connection State Changers *****************************************/
        /*********************************************************************/
        /// <summary>
        /// Accepts a Connection Request previously Received through <see cref="ClientConnectionRequested"/>
        /// </summary>
        protected void AcceptConnectionReqeust(ulong clientID)
        {
            if (IsRunning)
                ListenSocket.AcceptRequest(clientID);
            else
                Console.LogWarning($"Server: Tried to Accept a Connection Request before Starting the Server");
        }

        /// <summary>
        /// Rejects a Connection Request previously Received through <see cref="ClientConnectionRequested"/>
        /// </summary>
        protected void RejectConnectionRequest(ulong clientID)
        {
            if (IsRunning)
                ListenSocket.RejectRequest(clientID);
            else
                Console.LogWarning($"Server: Tried to Reject a Connection Request before Starting the Server");
        }

        /// <summary>
        /// <para>Forcefully Closes the Connection a Client</para>
        /// <para>
        ///     Note: Always make sure to Send some Kind of 'Goodbye Message' prior to Calling this so it doesn't look like the 
        ///           Connection was Closed due to a Timeout on the Servers End</para>
        /// </summary> 
        protected void CloseConnection(ulong clientID)
        {
            if (IsRunning)
                ListenSocket.CloseConnection(clientID);
            else
                Console.LogWarning($"Server: Tried to Close a Connection before Starting the Server");
        }


        /*********************************************************************/
        /* Listen Socket Event Handlers **************************************/
        /*********************************************************************/
        void OnListenSocketConnectionRequested(ulong clientID) 
            => ClientConnectionRequested?.Invoke(clientID);
        
        void OnListenSocketConnectionAccepted(ulong clientID)
        {
            clients.Add(clientID);
            ClientConnectionAccepted?.Invoke(clientID);
        }
        
        void OnListenSocketConnectionRejected(ulong clientID)
            => ClientConnectionRejected?.Invoke(clientID);

        void OnListenSocketConnectionClosedByLocalHost(ulong clientID)
        {
            clients.Remove(clientID);
            ClientConnectionClosedByServer?.Invoke(clientID);
        }
        
        void OnListenSocketConnectionClosedByRemoteHost(ulong clientID)
        {
            clients.Remove(clientID);
            ClientConnectionClosedByClient?.Invoke(clientID);
        }
        

        /*********************************************************************/
        /* Messaging *********************************************************/
        /*********************************************************************/
        /// <summary>
        /// Sends a Message to a Specific Client
        /// </summary>
        protected void SendMessage(ulong clientID, byte[] buffer)
        {
            if (IsRunning)
                ListenSocket.SendMessage(clientID, buffer);
            else
                Console.LogWarning($"Server: Tried to Send a Message before Starting the Server");
        }

        /// <summary>
        /// Broadcasts a Message to All Connected Clients
        /// </summary>
        /// <param name="execptions">All Clients in this List will Not Receive the Message Broadcast</param>
        protected void BroadcastMessage(byte[] buffer, List<ulong> execptions = null) 
        {
            if (IsRunning)
                foreach (var client in clients)
                    if (!(execptions != null && execptions.Contains(client)))
                        ListenSocket.SendMessage(client, buffer);
            else
                Console.LogWarning("Server: Tried to Broadcast a Message before Starting the Server");
        }

        void PollMessages() 
        {
            if (IsRunning)
                foreach (var client in clients) 
                {
                    var buffers = ListenSocket.PollMessages(client);
                    
                    if (buffers.Count > 0)
                        MessagesReceived?.Invoke(client, buffers);
                }
            else
                Console.LogWarning($"Server: Tried to Poll Messages before Starting the Server");
        }


        /*********************************************************************/
        /* Helper Methods ****************************************************/
        /*********************************************************************/
        void SubscribeToListenSocketEvents() 
        {
            Lobby.Left += OnLobbyLeft;
            Lobby.Kicked += OnLobbyLeft;

            ListenSocket.ConnectionRequested += OnListenSocketConnectionRequested;
            ListenSocket.ConnectionAccepted += OnListenSocketConnectionAccepted;
            ListenSocket.ConnectionRejected += OnListenSocketConnectionRejected;
            ListenSocket.ConnectionClosedByLocalHost += OnListenSocketConnectionClosedByLocalHost;
            ListenSocket.ConnectionClosedByRemoteHost += OnListenSocketConnectionClosedByRemoteHost;
        }

        void UnsubscribeFromListenSocketEvents() 
        {
            Lobby.Left -= OnLobbyLeft;
            Lobby.Kicked -= OnLobbyLeft;

            ListenSocket.ConnectionRequested -= OnListenSocketConnectionRequested;
            ListenSocket.ConnectionAccepted -= OnListenSocketConnectionAccepted;
            ListenSocket.ConnectionRejected -= OnListenSocketConnectionRejected;
            ListenSocket.ConnectionClosedByLocalHost -= OnListenSocketConnectionClosedByLocalHost;
            ListenSocket.ConnectionClosedByRemoteHost -= OnListenSocketConnectionClosedByRemoteHost;
        }

        bool IsClientInLobby(ulong clientID) 
        {
            if (Lobby.Members.Contains(clientID)) 
                return true;
            else
                return false;
        }
    }
}