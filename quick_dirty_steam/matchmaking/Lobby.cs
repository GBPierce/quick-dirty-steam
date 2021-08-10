using System.Collections.Generic;
using Steamworks;

namespace QuickDirtySteam.Matchmaking
{
    public static class Lobby
    {
        static Callback<GameLobbyJoinRequested_t> callbackGameLobbyJoinRequested;
        static Callback<LobbyChatUpdate_t> callbackLobbyChatUpdate;
        static Callback<LobbyGameCreated_t> callbackLobbyGameCreated;

        public delegate void CreatedEventHandler(LobbyCreationResult result);
        /// <summary>
        /// Emitted when a Lobby Creation Request has been Responded to
        /// </summary>
        public static event CreatedEventHandler Created;
        public delegate void EnteredEventHandler(bool success);
        /// <summary>
        /// Emitted when a Lobby Enter Request has been Responded to
        /// </summary>
        public static event EnteredEventHandler Entered; 
        public delegate void LeftEventHandler();
        /// <summary>
        /// Emitted when the Lobby was Left i.e. by Calling <see cref="Leave"/>
        /// </summary>
        public static event LeftEventHandler Left;
        /// <summary>
        /// Emitted when the Local Member was Forcefully Removed from the Lobby through <see cref="KickMember"/>
        /// </summary> 
        public static event LeftEventHandler Kicked;

        public delegate void ChatMessageReceivedEventHandler(ulong memberID, string message);
        /// <summary>
        /// Emitted when a Chat Message has been received
        /// </summary>
        public static event ChatMessageReceivedEventHandler ChatMessageReceived;

        public delegate void MemberMetaDataPairChangedEventHandler(ulong memberID, string key, string value);
        public static event MemberMetaDataPairChangedEventHandler MemberMetaDataPairChanged;

        public delegate void MetaDataPairChangedEventHandler(string key, string value);
        public static event MetaDataPairChangedEventHandler MetaDataPairChanged;

        public delegate void MemberStatusChangedEventHandler(ulong memberID);
        public static event MemberStatusChangedEventHandler MemberEntered;
        public static event MemberStatusChangedEventHandler MemberLeft;
        public static event MemberStatusChangedEventHandler OwnerLeft;

        public delegate void ServerActivityChangedEventHandler(bool active);
        /// <summary>
        /// <para>Emitted when the Lobby Owner has Started or Stopped a Server</para>
        /// <para>Call <see cref="IsServerActive"/> to See if a Server is Active</para>
        /// </summary>
        public static event ServerActivityChangedEventHandler OnServerActivityChanged;

        /// <summary>
        /// <para>Indicates whether or not the Local User is Currently in a Lobby</para>
        /// </summary>
        public static bool IsValid { get => SteamID.IsValid(); }

        public static ulong ID 
        { 
            get
            {
                if (IsValid)
                    return SteamID.m_SteamID;
                
                Console.LogWarning($"Lobby: Field 'ID' was Accessed before Entering a Lobby");
                return 0;
            }
        }

        public static ulong OwnerID 
        { 
            get 
            {
                if (IsValid)
                    return OwnerSteamID.m_SteamID;
                
                Console.LogWarning($"Lobby: Field 'OwnerID' was Accessed before Entering a Lobby");
                return 0;
            } 
        }
        
        public static bool IsLocalOwner 
        { 
            get 
            {
                if (IsValid)
                    return OwnerID == SteamUser.GetSteamID().m_SteamID;
                
                Console.LogWarning($"Lobby: Field 'IsLocalOwner' was Accessed before Entering a Lobby");
                return false;
            }
        }

        /// <summary>
        /// Indicates whether or not the Lobby Owner has Started a Server
        /// </summary>
        public static bool IsServerActive 
        { 
            get 
            {
                if (IsValid)
                    return SteamMatchmaking.GetLobbyGameServer(SteamID, out _, out _, out _);
                
                Console.LogWarning($"Lobby: Field 'IsServerActive' was Accessed before Entering a Lobby");
                return false;
            } 
        }

        /// <summary>
        /// A List of IDs of All Lobby Members
        /// </summary>
        public static List<ulong> Members 
        { 
            get 
            {
                if (IsValid)
                    return MemberList.ConvertAll<ulong>((Member member) => member.ID64);

                Console.LogWarning($"Lobby: Field 'Members' was Accessed before Entering a Lobby");
                return null;
            }
        }

        public static int MemberLimit 
        { 
            get 
            {
                if (IsValid)
                    return SteamMatchmaking.GetLobbyMemberLimit(SteamID);
                
                Console.LogWarning($"Lobby: Field 'MemberLimit' was Accessed before Entering a Lobby");
                return -1;
            } 
        }
        
        /// <summary> 
        /// <para>If it is Intended that Members can Set Member Specific Meta Data, a List with Permitted Keys is Required</para>
        /// <para>Only Updates Made to Keys Specified in this List will be Notified about in <see cref="MemberMetaDataPairChanged"/></para>
        /// </summary>
        public static List<string> MemberMetaDataKeyFilter { get => new List<string>(Member.KeyFilter); set => Member.KeyFilter = value; }

        static CSteamID SteamID;
        static CSteamID OwnerSteamID { get => SteamMatchmaking.GetLobbyOwner(SteamID); }

        static List<Member> MemberList = new List<Member>();

        static QuickDirtySteam.Matchmaking.LobbyCreator Creator = new QuickDirtySteam.Matchmaking.LobbyCreator();
        static LobbyEnterer Enterer = new LobbyEnterer();
        static LobbyMetaData MetaData;
        static Chat Chat;


        static Lobby()
        {
            callbackGameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            callbackLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            callbackLobbyGameCreated = Callback<LobbyGameCreated_t>.Create(OnLobbyGameCreated);

            Creator.Created += OnCreatorCreated;
            Enterer.Entered += OnEntererEntered;
        }


        /*********************************************************************/
        /* State Changers ****************************************************/
        /*********************************************************************/
        /// <summary>
        /// <para>Starts an Attempt to Create a Lobby with the Specified Member Limit</para>
        /// <para>This can only Work if the Following Conditions are met:</para> 
        /// <list type="bullet">
        ///     <item>The User is Not Currently In a Lobby</item>
        ///     <item>No Attempts were Made to Create or Enter another Lobby before Calling this</item>
        /// </list>
        /// <para>Emits <see cref="Created"/> when the Request has been Processed</para>
        /// </summary>
        /// <param name="memberLimit">Is not Allowed to be Smaller than 1 and Larger than 200 and will be Clamped</param>
        public static void Create(uint memberLimit)
        {
            if (!IsValid)
                if (!IsCreatingOrEnteringLobby())
                    Creator.Create(memberLimit < 1 ? 1 : memberLimit > 200 ? 200 : memberLimit );
                else
                    Console.LogWarning($"Lobby: Tried to Create a Lobby while either the Lobby Creation or the Entering Process is still Awaiting a Result");
            else
                Console.LogWarning($"Lobby: Tried to Create a Lobby before Leaving the Currently Entered One");
        }

        /// <summary>
        /// <para>Starts an Attempt to Enter a Lobby with the Specified ID</para>
        /// <para>This can only Work if the Following Conditions are met:</para> 
        /// <list type="bullet">
        ///     <item>The User is Not Currently In a Lobby</item>
        ///     <item>No Attempts were Made to Create or Enter another Lobby before Calling this</item>
        /// </list>
        /// <para>Emits <see cref="Entered"/> when the Request has been Processed</para>
        /// </summary>
        public static void Enter(ulong lobbyID) 
        {
            if (!IsValid)
                if (!IsCreatingOrEnteringLobby())
                    Enterer.Enter(new CSteamID(lobbyID));
                else
                    Console.LogWarning($"Lobby: Tried to Enter a Lobby while either the Lobby Creation or the Entering Process is still Awaiting a Result");
            else
                Console.LogWarning($"Lobby: Tried to Enter a Lobby before Leaving the Currently Entered One");
        }

        /// <summary>
        /// Forcefully Removes the Member with the Specified ID
        /// </summary>
        public static void KickMember(ulong memberID) 
        {
            if (IsValid)
                if (IsLocalOwner)
                    if (Members.Contains(memberID))
                        Chat.SendMessage($"{Chat.KickMessage}_{memberID.ToString()}");
                    else
                        Console.LogWarning($"Lobby: Failed to Kick Member with ID: '{memberID}' because the ID is Invalid");
                else
                    Console.LogWarning($"Lobby: Tried to Kick member with ID: '{memberID}' without Possessing Lobby Ownership");
            else
                Console.LogWarning($"Lobby: Tried to Kick Member with ID: '{memberID}' before Entering a Lobby");
        }

        /// <summary>
        /// Leaves the Currently Entered Lobby
        /// </summary>
        public static void Leave()
        {
            if (IsValid)
            {
                Console.LogInfo($"Lobby: Left the Lobby with ID: '{ID}'");
                SteamMatchmaking.LeaveLobby(SteamID);
                Invalidate();
                Left?.Invoke();
            }
            else
                Console.LogWarning($"Lobby: Tried to Leave a Lobby before Entering One");
        }

        static void Kick()
        {
            Console.LogInfo($"Lobby: Local User was Kicked from the Lobby with ID: '{ID}'");
            SteamMatchmaking.LeaveLobby(SteamID);
            Invalidate();
            Kicked?.Invoke();
        }

        static void OnCreatorCreated(LobbyCreationResult result, CSteamID lobbyID)
        {
            if (result == LobbyCreationResult.Success) 
                Initialize(lobbyID);

            Created?.Invoke(result);
        } 

        static void OnEntererEntered(bool success, CSteamID lobbyID)
        {
            if (success)
                Initialize(lobbyID);
            
            Entered?.Invoke(success);
        }

        // Handles the Event where a User gets Invited to a Lobby through Steam
        static void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback) 
        {
            if (!IsValid)
                Enterer.Enter(callback.m_steamIDLobby);
            else
            {
                Console.LogWarning($"Lobby: Tried to Enter Lobby with ID: '{callback.m_steamIDLobby.m_SteamID}' before Leaving the Currently Entered Lobby");
                Entered?.Invoke(false);
            }
        }


        /*********************************************************************/
        /* Visibility Changers ***********************************************/
        /*********************************************************************/
        /// <summary>
        /// Opens up the Lobby to the Public
        /// </summary>
        public static void Open() 
        {
            if (IsValid)
                if(IsLocalOwner)
                    SteamMatchmaking.SetLobbyType(SteamID, ELobbyType.k_ELobbyTypePublic);
                else
                    Console.LogWarning($"Lobby: Tried to Change the Lobby's Type without Possessing Ownership");
            else
                Console.LogWarning($"Lobby: Tried to Change the Lobby's Type before Entering One");
        }

        /// <summary>
        /// <para>Makes the Lobby Invisible to the Public</para>
        /// <para>This is the Default State of a Lobby after its' Creation</para>
        /// <para>Call <see cref="Open"/> after Creating a Lobby to Make it Visible</para> 
        /// </summary>
        public static void Close() 
        {
            if (IsValid)
                if (IsLocalOwner) 
                    SteamMatchmaking.SetLobbyType(SteamID, ELobbyType.k_ELobbyTypeInvisible);
                else
                    Console.LogWarning($"Lobby: Tried to Change the Lobby's Type without Possessing Ownership");
            else
                Console.LogWarning($"Lobby: Tried to Change the Lobby's Type before Entering One");
        }


        /*********************************************************************/
        /* Chat Messaging ****************************************************/
        /*********************************************************************/
        /// <summary>Sends a UTF8 Chat Message to all Lobby Members</summary>
        public static void SendChatMessage(string message)
        {
            if (IsValid)
                Chat.SendMessage(message);
            else
                Console.LogWarning($"Lobby: Tried to Send a Chat Message before Entering a Lobby");
        }

        static void OnLobbyChatMessageReceived(CSteamID memberID, string message) 
        {
            if (memberID == OwnerSteamID && message.Contains(Chat.KickMessage))
            {
                if (message.Contains(SteamUser.GetSteamID().m_SteamID.ToString()))
                   Kick();
            }
            else
                ChatMessageReceived?.Invoke(memberID.m_SteamID, message);
        }


        /*********************************************************************/
        /* Meta Data Pair Getters ********************************************/
        /*********************************************************************/
        public static string GetMetaDataValue(string key)
        {
            if (IsValid)
                return MetaData.GetMetaDataValue(key);
            
            Console.LogWarning($"Lobby: Tried to Get a Meta Data Value before Entering a Lobby");
            return null;
        }

        public static string GetMemberMetaDataValue(ulong memberID, string key) 
        {
            if (IsValid)
            {
                var member = MemberList.Find(m => m.ID64 == memberID);

                if (member != null)
                    return member.GetMetaDataValue(key);
                else
                    Console.LogWarning($"Lobby: Failed to Get a Member Meta Data Value for ID: '{memberID}' because it is Invalid");
            }
            else
                Console.LogWarning($"Lobby: Tried to Get a Member Meta Data Value before Entering a Lobby");

            return null;
        }


        /*********************************************************************/
        /* Meta Data Pair Setters ********************************************/
        /*********************************************************************/
        public static void SetMetaDataPair(string key, string value)
        {
            if (IsValid)
                if (IsLocalOwner)
                    MetaData.SetMetaDataPair(key, value);
                else
                    Console.LogWarning($"Lobby: Tried to Set a Meta Data Pair without Possessing the Lobby Ownership");
            else
                Console.LogWarning($"Lobby: Tried to Set a Meta Data Pair before Entering a Lobby");
        }

        public static void SetMemberMetaDataPair(string key, string value)
        {
            if (IsValid)
                Member.SetMetaDataPair(SteamID, key, value);
            else
                Console.LogWarning($"Lobby: Tried to Set a Member Meta Data Pair before Entering a Lobby");
        }


        /*********************************************************************/
        /* Meta Data Pair Update Handlers ************************************/
        /*********************************************************************/
        static void OnLobbyMetaDataPairUpdated(string key, string value) 
            => MetaDataPairChanged?.Invoke(key, value);

        static void OnMemberMetaDataPairUpdated(Member member, string key, string value) 
            => MemberMetaDataPairChanged?.Invoke(member.ID64, key, value);


        /*********************************************************************/
        /* Adding and Removing Members ***************************************/
        /*********************************************************************/
        static void OnLobbyChatUpdate(LobbyChatUpdate_t callback) 
        {
            var memberState = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;
            var memberID = new CSteamID(callback.m_ulSteamIDUserChanged);

            switch (memberState)
            {
                case EChatMemberStateChange.k_EChatMemberStateChangeEntered:
                    Console.LogInfo($"Lobby: Member '{SteamFriends.GetFriendPersonaName(memberID)}' Entered the Lobby");
                    
                    AddMember(memberID);
                    MemberEntered?.Invoke(memberID.m_SteamID);
                    break;
                case EChatMemberStateChange.k_EChatMemberStateChangeLeft:
                case EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
                case EChatMemberStateChange.k_EChatMemberStateChangeKicked:
                case EChatMemberStateChange.k_EChatMemberStateChangeBanned:
                    Console.LogInfo($"Lobby: Member '{SteamFriends.GetFriendPersonaName(memberID)}' Left the Lobby");

                    RemoveMember(memberID);
                    if (memberID.m_SteamID == OwnerID)
                        OwnerLeft?.Invoke(memberID.m_SteamID);
                    else
                        MemberLeft?.Invoke(memberID.m_SteamID);
                    break;
            }
        }

        static void RemoveMember(CSteamID memberID) 
        {
            Console.LogDebug($"Lobby: Removing Member: '{SteamFriends.GetFriendPersonaName(memberID)}'");

            var member = MemberList.Find(m => m.ID64 == memberID.m_SteamID);
            MemberList.Remove(member);
            member.MetaDataPairUpdated -= OnMemberMetaDataPairUpdated;
        }

        static void AddMember(CSteamID memberID) 
        {
            var member = new Member(memberID, SteamID);
            
            if (member.IsValid)
            {
                Console.LogDebug($"Lobby: Adding Member: '{SteamFriends.GetFriendPersonaName(member.ID)}'");
                
                MemberList.Add(member);
                member.MetaDataPairUpdated += OnMemberMetaDataPairUpdated;
            }
            else
                Console.LogWarning($"Lobby: Failed to Add Member");
        }


        /*********************************************************************/
        /* Server Activity ***************************************************/
        /*********************************************************************/
        /// <summary>
        /// <para>Lets all Lobby Members know when a Server has been Started </para>
        /// <para>This can Only be Called by the Lobby Owner who is Also the Only Member allowed to Host a Server</para>
        /// <para>Emits <see cref="OnServerActivityChanged"/> for all Lobby Members</para>
        /// </summary>
        public static void SetServerActive(bool active) 
        {
            if (IsValid)
                if (IsLocalOwner)
                {
                    Console.LogInfo($"Lobby: Set Server Activity to '{active}'");
                    SteamMatchmaking.SetLobbyGameServer(SteamID, 0, 0, active ? OwnerSteamID : CSteamID.Nil);
                }
                else
                    Console.LogWarning($"Lobby: Tried to Change the Server Activity without Possessing Lobby Ownership");
            else
                Console.LogWarning($"Lobby: Tried to Change the Server Activity before Entering a Lobby");
        }

        static void OnLobbyGameCreated(LobbyGameCreated_t callback) 
        {
            if (callback.m_ulSteamIDLobby == ID)
            {
                Console.LogInfo($"Lobby: Received Server Activity Update: Server Active -> '{callback.m_ulSteamIDGameServer != 0}'");
                OnServerActivityChanged?.Invoke(new CSteamID(callback.m_ulSteamIDGameServer).IsValid());
            }
        }


        /*********************************************************************/
        /* Helper Methods ****************************************************/
        /*********************************************************************/
        static void Initialize(CSteamID lobbyID) 
        {
            SteamID = lobbyID;

            for (int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(SteamID); ++i)
                AddMember(SteamMatchmaking.GetLobbyMemberByIndex(SteamID, i));

            Chat = new Chat(SteamID);
            Chat.OnChatMessageReceived += OnLobbyChatMessageReceived;

            MetaData = new LobbyMetaData(SteamID);
            MetaData.OnMetaDataPairUpdated += OnLobbyMetaDataPairUpdated;
        }

        static void Invalidate() 
        {
            SteamID = CSteamID.Nil;

            foreach (var member in MemberList)
                member.MetaDataPairUpdated -= OnMemberMetaDataPairUpdated;

            MemberList.Clear();

            Chat.OnChatMessageReceived -= OnLobbyChatMessageReceived;
            Chat = null;

            MetaData.OnMetaDataPairUpdated -= OnLobbyMetaDataPairUpdated;
            MetaData = null;
        }

        static bool IsCreatingOrEnteringLobby() 
            => Creator.IsAwaitingResult || Enterer.IsAwaitingResult;
    }
}