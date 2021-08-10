using System.Collections.Generic;
using Steamworks;

namespace QuickDirtySteam.Matchmaking
{
    internal class LobbyMetaData 
    {
        Callback<LobbyDataUpdate_t> callbackLobbyDataUpdate;

        public delegate void MetaDataPairUpdatedEventHandler(string key, string value);
        public event MetaDataPairUpdatedEventHandler OnMetaDataPairUpdated;

        const int MaxKeyLength = 255;
        const int MaxValueLength = 8192;

        CSteamID lobbyID;
        Dictionary<string, string> metaDataPairs = new Dictionary<string, string>();

        public LobbyMetaData(CSteamID lobbyID) 
        {
            Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate); 
            this.lobbyID = lobbyID; 
        }

        ~LobbyMetaData()
            => callbackLobbyDataUpdate.Dispose();

        public string GetMetaDataValue(string key) 
        {
            if (metaDataPairs.ContainsKey(key))
                return metaDataPairs[key];
            else
                Console.LogWarning($"LobbyMetaData: No Key: '{key}' was Set");
            
            return null;
        }

        public void SetMetaDataPair(string key, string value)
        {
            if (key.Length > 0 && key.Length < MaxKeyLength && value.Length > 0 && value.Length < MaxValueLength)
            {
                Console.LogDebug($"LobbyMetaData: Setting Key: '{key}' to Value: '{value}'");
                
                if (!SteamMatchmaking.SetLobbyData(lobbyID, key, value))
                    Console.LogWarning($"LobbyMetaData: Failed to Set Meta Data Pair");
            }
            else
                Console.LogWarning($"LobbyMetaData: Failed to Set a Meta Data Pair because of either the Key being Empty, Too Long or the Value being Too Long");
        }

        void OnLobbyDataUpdate(LobbyDataUpdate_t callback) 
        {
            // If the Callback is of our Concern
            if (lobbyID.m_SteamID == callback.m_ulSteamIDMember)
                if (callback.m_bSuccess != 0)
                {
                    // Acquire Collection of Updated Meta Data Pairs
                    var newMetaDataPairs = GetMetaDataPairs();
                    
                    // Compare each Pair of the new Collection against the Old Collections Entries
                    foreach (var pair in newMetaDataPairs) 
                    {
                        // If a new Key has been Added
                        if (!metaDataPairs.ContainsKey(pair.Key)) 
                            OnMetaDataPairUpdated?.Invoke(pair.Key, pair.Value);
                        else
                            // If a Keys assigned Value was Altered 
                            if (metaDataPairs[pair.Key] != newMetaDataPairs[pair.Key])
                                OnMetaDataPairUpdated?.Invoke(pair.Key, pair.Value);
                    }

                    // Set new Collection as Current
                    metaDataPairs = newMetaDataPairs;
                }
                else
                    Console.LogWarning($"LobbyData: Failed to Update Lobby Meta Data Pairs");
        }        

        Dictionary<string, string> GetMetaDataPairs()
        {
            var metaData = new Dictionary<string, string>();

            int dataCount = SteamMatchmaking.GetLobbyDataCount(lobbyID);

            string key;
            string value;

            for (int i = 0; i < dataCount; ++i) 
            {
                SteamMatchmaking.GetLobbyDataByIndex(lobbyID, i, out key, MaxKeyLength, out value, MaxValueLength);
                metaData.Add(key, value);
            }

            return metaData;
        }
    }
}