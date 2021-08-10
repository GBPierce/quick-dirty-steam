using System.Collections.Generic;

namespace QuickDirtySteam.Networking.Utils
{
    /// <summary>
    /// A Simple Server Relaying all Received Messages
    /// </summary> 
    public class RelayServer : Server
    {
        public RelayServer() 
            => MessagesReceived += OnServerMessagesReceived;

        ~RelayServer() 
            => MessagesReceived -= OnServerMessagesReceived;

        // Simply Broadcasts all Received Messages
        void OnServerMessagesReceived(ulong clientID, List<byte[]> messages) 
        {
            foreach (var message in messages)
                BroadcastMessage(message, new List<ulong>() { clientID });
        }
    }
}