using System;
using Steamworks;

namespace QuickDirtySteam.Matchmaking 
{
    internal class LobbyCreator 
    {
        CallResult<LobbyCreated_t> callResultLobbyCreated;

        public delegate void CreatedEventHandler(LobbyCreationResult result, CSteamID lobbyID);
        public event CreatedEventHandler Created;

        public bool IsAwaitingResult { get; private set; }


        public LobbyCreator() 
            => callResultLobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);

        public void Create(uint memberLimit) 
        {
            if (!IsAwaitingResult) 
            {
                Console.LogDebug($"LobbyCreator: Trying to Create a new Lobby");
                
                callResultLobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeInvisible, (int)memberLimit));
                IsAwaitingResult = true;
            }
            else
                Console.LogWarning($"LobbyCreator: Tried to Create a Lobby while Waiting for a Creation Result");
        }

        void OnLobbyCreated(LobbyCreated_t result, bool IOFailure) 
        {
            if (result.m_eResult == EResult.k_EResultOK && !IOFailure)
            {
                Console.LogInfo($"LobbyCreator: Successfully Created a new Lobby with ID: '{result.m_ulSteamIDLobby}'");
                Created?.Invoke(LobbyCreationResult.Success, new CSteamID(result.m_ulSteamIDLobby));
            }
            else
            {
                var creationResult = (LobbyCreationResult)result.m_eResult; 
                Console.LogWarning($"LobbyCreator: Failed to Create a new Lobby for the following Reason: '{Enum.GetName(typeof(LobbyCreationResult), creationResult)}'");
                Created?.Invoke(creationResult, CSteamID.Nil);
            }

            IsAwaitingResult = false;
        }
    }
}