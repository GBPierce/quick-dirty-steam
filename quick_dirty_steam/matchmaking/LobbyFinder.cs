using System.Collections.Generic;
using Steamworks;

namespace QuickDirtySteam.Matchmaking 
{
    public static class LobbyFinder
    {
        static CallResult<LobbyMatchList_t> callResultLobbyMatchList;
        
        /// <summary>
        /// A Struct Containing all the Important Information about a Requested Lobby
        /// </summary>
        public struct LobbyInfo 
        {
            public ulong ID { get; private set; }
            public ulong OwnerID { get; private set; }
            public int MemberCount { get; private set; }
            public int MemberLimit { get; private set; }
            public Dictionary<string, string> MetaData { get; private set; }

            public LobbyInfo(ulong id, ulong owner, int memberCount, int memberLimit, Dictionary<string, string> metaData) 
            {
                ID = id;
                OwnerID = owner;
                MemberCount = memberCount;
                MemberLimit = memberLimit;
                MetaData = metaData;
            }
        }

        public enum SearchDistance 
        {
            Close = 0,
            Normal = 1,
            Far = 2,
            Global = 3
        }        

        public delegate void SearchCompletedEventHandler(bool success, List<LobbyInfo> lobbyInfoList);
        /// <summary>
        /// Emitted when the LobbyFinder has Completed its' Search
        /// </summary>
        public static event SearchCompletedEventHandler SearchCompleted;

        public static SearchDistance Distance { get; set; }
        public static uint SlotsAvailable { get; set; }
        /// <summary>
        /// Defines after how many Found Lobbies the Search should be Aborted
        /// </summary>
        public static uint MaxResults { get; set; }
        /// <summary>
        /// Defines a Set of Meta Data Pairs that Must Match the Ones Set in the Lobby
        /// </summary>
        public static Dictionary<string, string> StringFilter { get; set; }
        
        static bool IsAwaitingResult;


        static LobbyFinder() 
        {
            callResultLobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);

            Distance = SearchDistance.Far;
            SlotsAvailable = 1;
            MaxResults = 50;
        }

        /// <summary>
        /// <para>Starts an Attempt to Find all Matching Lobbies based on the Search Filters Set</para>
        /// <para>Emits <see cref="SearchCompleted"/> on Completion</para>
        /// </summary>
        public static void Search() 
        {
            if (!IsAwaitingResult) 
            {
                SteamMatchmaking.AddRequestLobbyListDistanceFilter((ELobbyDistanceFilter)Distance);
                SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable((int)SlotsAvailable);
                SteamMatchmaking.AddRequestLobbyListResultCountFilter((int)MaxResults);

                if (StringFilter != null)
                    foreach (var pair in StringFilter) 
                        SteamMatchmaking.AddRequestLobbyListStringFilter(pair.Key, pair.Value, ELobbyComparison.k_ELobbyComparisonEqual);

                callResultLobbyMatchList.Set(SteamMatchmaking.RequestLobbyList());
                IsAwaitingResult = true;
            }
            else
                Console.LogWarning($"LobbyFinder: Tried to Start a Search while Waiting for a Search Result");
        }

        static void OnLobbyMatchList(LobbyMatchList_t result, bool IOFailure) 
        {
            var lobbyInfoList = new List<LobbyInfo>();

            for (int i = 0; i < result.m_nLobbiesMatching; ++i) 
            {
                CSteamID id = SteamMatchmaking.GetLobbyByIndex(i); 
                
                lobbyInfoList.Add(new LobbyInfo(
                    (ulong)id,
                    (ulong)SteamMatchmaking.GetLobbyOwner(id),
                    SteamMatchmaking.GetNumLobbyMembers(id),
                    SteamMatchmaking.GetLobbyMemberLimit(id),
                    new MetaDataCrawler(id).GetLobbyMetaData())); // TODO: Consider making MetaDataCrawlers target ID changeable
            }

            SearchCompleted?.Invoke(true, lobbyInfoList);

            IsAwaitingResult = false;
        }
    }
}