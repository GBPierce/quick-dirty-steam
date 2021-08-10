using System.Text;
using Steamworks;

namespace QuickDirtySteam.Matchmaking 
{   
    internal class Chat 
    {
        Callback<LobbyChatMsg_t> callbackLobbyChatMsg;

        public const string KickMessage = "KICK_MEMBER"; 

        public delegate void ChatMessageReceivedEventHandler(CSteamID senderID, string message);
        public event ChatMessageReceivedEventHandler OnChatMessageReceived;

        CSteamID lobbyID;


        public Chat(CSteamID lobbyID)
        {
            callbackLobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);
            this.lobbyID = lobbyID;
        }

        ~Chat() 
            => callbackLobbyChatMsg.Dispose();

        public void SendMessage(string message) 
        {  
            Console.LogDebug($"Chat: Sending Message with Content: '{message}'");

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            if (!SteamMatchmaking.SendLobbyChatMsg(lobbyID, buffer, buffer.Length))
                Console.LogWarning($"Chat: Failed to Send Chat Message");
        }

        void OnLobbyChatMsg(LobbyChatMsg_t callback) 
        {
            byte[] messageBuffer = new byte[4096];
            int length = SteamMatchmaking.GetLobbyChatEntry(lobbyID, (int)callback.m_iChatID, out _, messageBuffer, messageBuffer.Length, out _);
            
            string message = Encoding.UTF8.GetString(messageBuffer, 0, length);
            Console.LogDebug($"Chat: Received Message from Member: '{SteamFriends.GetFriendPersonaName(new CSteamID(callback.m_ulSteamIDUser))}' with Content: '{message}'");

            OnChatMessageReceived?.Invoke(new CSteamID(callback.m_ulSteamIDUser), message);
        }
    }
}