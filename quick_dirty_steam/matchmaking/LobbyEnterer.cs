using Steamworks;

namespace QuickDirtySteam.Matchmaking 
{
    internal class LobbyEnterer 
    {
        CallResult<LobbyEnter_t> callResultLobbyEnter;

        public delegate void EnteredEventHandler(bool success, CSteamID lobbyID);
        public event EnteredEventHandler Entered;

        public bool IsAwaitingResult { get; private set; }


        public LobbyEnterer() 
        {
            callResultLobbyEnter = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
        }

        public void Enter(CSteamID id) 
        {
            if (!IsAwaitingResult) 
            {
                callResultLobbyEnter.Set(SteamMatchmaking.JoinLobby(id));
                IsAwaitingResult = true;
            }
            else
                Console.LogWarning($"LobbyEnterer: Tried to Enter a Lobby while Waiting for a Creation Result");
        }

        void OnLobbyEnter(LobbyEnter_t result, bool IOFailure) 
        {
            if ((EChatRoomEnterResponse)result.m_EChatRoomEnterResponse == EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess && !IOFailure)
            {
                Console.LogDebug($"LobbyEnterer: Successfully Entered a Lobby with ID: '{result.m_ulSteamIDLobby}'");
                Entered?.Invoke(true, new CSteamID(result.m_ulSteamIDLobby));
            }
            else
            {
                Console.LogDebug($"LobbyEnterer: Failed to Enter a Lobby");
                Entered?.Invoke(false, CSteamID.Nil);
            }

            IsAwaitingResult = false;
        }
    }
}