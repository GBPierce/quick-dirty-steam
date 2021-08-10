using Steamworks;
using System.Collections.Generic;

namespace QuickDirtySteam.Matchmaking 
{
    internal class MetaDataCrawler
    {
        const int MaxLobbyKeyLength = 255;
        const int MaxLobbyValueLength = 8192;

        CSteamID lobbyID;


        public MetaDataCrawler(CSteamID lobbyID) 
            => this.lobbyID = lobbyID;

        public Dictionary<string, string> GetLobbyMetaData()
        {
            var metaData = new Dictionary<string, string>();

            int dataCount = SteamMatchmaking.GetLobbyDataCount(lobbyID);

            string key;
            string value;

            for (int i = 0; i < dataCount; ++i) 
            {
                SteamMatchmaking.GetLobbyDataByIndex(lobbyID, i, out key, MaxLobbyKeyLength, out value, MaxLobbyValueLength);
                metaData.Add(key, value);
            }

            return metaData;
        }

        public Dictionary<string, string> GetMemberMetaData(CSteamID memberID, List<string> keyFilter) 
        {
            var memberMetaData = new Dictionary<string, string>();

            foreach (string key in keyFilter)
                memberMetaData.Add(key, SteamMatchmaking.GetLobbyMemberData(lobbyID, memberID, key));
            
            return memberMetaData;
        }
    }
}