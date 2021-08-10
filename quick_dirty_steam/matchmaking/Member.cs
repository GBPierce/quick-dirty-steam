using System.Collections.Generic;
using Steamworks;

namespace QuickDirtySteam.Matchmaking 
{
    internal class Member
    {
        Callback<LobbyDataUpdate_t> callbackLobbyDataUpdate;

        const int MaxKeyLength = 255;
        const int MaxValueLength = 8192;

        public static List<string> KeyFilter { get; set; }

        public delegate void MetaDataPairUpdatedEventHandler(Member member, string key, string value);
        public event MetaDataPairUpdatedEventHandler MetaDataPairUpdated;

        public CSteamID ID { get; private set; }
        public ulong ID64 { get => ID.m_SteamID; }
        public bool IsValid { get => ID.IsValid(); }

        CSteamID lobbyID;
        Dictionary<string, string> metaDataPairs = new Dictionary<string, string>();


        public static void SetMetaDataPair(CSteamID lobbyID, string key, string value) 
        {
            if (KeyFilter != null && KeyFilter.Contains(key))
                if (key.Length > 0 && key.Length < MaxKeyLength && value.Length > 0 && value.Length < MaxValueLength)
                {
                    Console.LogDebug($"Member: Setting Key: '{key}' to Value: '{value}'");
                    SteamMatchmaking.SetLobbyMemberData(lobbyID, key, value);
                }
                else
                    Console.LogWarning($"Member: Failed to Set a Meta Data Pair because of either the Key being Empty, Too Long or the Value being Too Long");
            else
                Console.LogWarning($"Member: Tried to Set a Value for Key '{key}' which is not Present in the Key Filter");
        }

        public Member(CSteamID memberID, CSteamID lobbyID) 
        {
            if (memberID.IsValid())
            {
                ID = memberID;
                this.lobbyID = lobbyID;
                callbackLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
            }
            else
            {
                Console.LogWarning($"Member: Tried to Create an Instance of Member with the Invalid ID: '{memberID.m_SteamID}'");
                return;
            }
        }

        ~Member() 
            => callbackLobbyDataUpdate.Dispose();

        public string GetMetaDataValue(string key) 
        {
            if (ID.IsValid())
                if (metaDataPairs.ContainsKey(key))
                    return metaDataPairs[key];
                else
                    Console.LogWarning($"Member: No Key: '{key}' was Set for Member: '{SteamFriends.GetFriendPersonaName(ID)}'");
            else
                Console.LogWarning($"Member: Tried to Get a Meta Data Value from an Invalid Member Instance");

            return null;
        }

        Dictionary<string, string> GetMetaDataPairs() 
        {
            var metaDataPairs = new Dictionary<string, string>();

            if (KeyFilter != null)
                foreach (string key in KeyFilter)
                    metaDataPairs.Add(key, SteamMatchmaking.GetLobbyMemberData(lobbyID, ID, key));
            
            return metaDataPairs;
        }

        void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
        {
            // If the Callback is of our Concern
            if (ID64 == callback.m_ulSteamIDMember)
            {
                // Acquire Collection of Updated Meta Data Pairs
                var newMetaDataPairs = GetMetaDataPairs();

                // Compare each Pair of the new Collection against the Old Collections Entries
                foreach (var pair in newMetaDataPairs)
                {
                    // If a new Key has been Added
                    if (!metaDataPairs.ContainsKey(pair.Key))
                        MetaDataPairUpdated?.Invoke(this, pair.Key, pair.Value);   
                    else 
                        // If a Keys assigned Value was Altered 
                        if (metaDataPairs[pair.Key] != newMetaDataPairs[pair.Key])
                            MetaDataPairUpdated?.Invoke(this, pair.Key, pair.Value);
                }

                // Set new Collection as Current
                metaDataPairs = newMetaDataPairs;
            }
        }
    }
}
