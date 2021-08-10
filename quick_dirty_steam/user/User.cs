using Steamworks;
using QuickDirtySteam.Matchmaking;

namespace QuickDirtySteam.User
{
    public class User
    {
        Callback<PersonaStateChange_t> callbackPersonaStateChange;
        Callback<AvatarImageLoaded_t> callbackAvatarLoaded;

        /// <summary>
        /// Can be Used to Retrieve the Username of a Known User
        /// Note: A Known User is either a Friend a Lobby Member etc.
        /// </summary>
        public static string GetKnownUserName(ulong userID) => SteamFriends.GetFriendPersonaName(new CSteamID(userID));

        public delegate void UserUpdatedEventHandler(User user);

        /// <summary>
        /// Emitted when the User's Information has been Updated
        /// </summary>
        public event UserUpdatedEventHandler InformationLoaded;
        /// <summary>
        /// Emitted when the User's Avatar has been Updated
        /// </summary>
        public event UserUpdatedEventHandler AvatarLoaded;

        /// <summary>
        /// True when the Provided SteamID was Valid
        /// </summary>
        public bool IsValid { get => userID.IsValid(); }

        /// <summary>
        /// The User's SteamID
        /// </summary>
        public ulong ID { get => userID.m_SteamID; }

        /// <summary>
        /// <para>The Name of the User</para>
        /// <para>Note: This has to Explicitly be Requested by Calling <see cref="RequestName"/> after Creating a User Object and Verifying its' Validity</para>
        /// </summary>
        public string Name { get => name; }

        /// <summary>
        /// <para>The (<see cref="Avatar.Size"/> by <see cref="Avatar.Size"/>) large Avatar of the User</para>
        /// <para>Note: This has to Explicitly be Requested by Calling <see cref="RequestAvatar"/> after Requesting the User's Information</para>
        /// </summary>
        public Avatar Avatar { get; private set; }

        CSteamID userID;
        string name;

        bool isInformationLoaded;
        bool isAvatarRequested;


        /*********************************************************************/
        /* Local User Creation ***********************************************/
        /*********************************************************************/
        /// <summary>
        /// A User Object describing the Local User
        /// </summary>
        public class LocalUser : User
        {
            public LocalUser() : base(SteamUser.GetSteamID().m_SteamID)
            {
                InformationLoaded += OnLocalInformationLoaded;
                AvatarLoaded += OnLocalAvatarLoaded;
                
                RequestName();
            }

            void OnLocalInformationLoaded(User local) 
            { 
                local.RequestAvatar();
                local.InformationLoaded -= OnLocalInformationLoaded; 
            }

            void OnLocalAvatarLoaded(User local) 
            {
                Console.LogDebug($"User: Created Static Local User Object");             
                local.AvatarLoaded -= OnLocalInformationLoaded; 
            }
        }

        // NOTE: This only works because the Callbacks are Handled the Same Frame the Static Constructor is Called 
        public static User Local = new LocalUser(); 


        public User(ulong userID) 
        {   
            callbackPersonaStateChange = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
            callbackAvatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);

            this.userID = new CSteamID(userID);

            if (!IsValid)
                Console.LogWarning($"User: Failed to Create an Instance of User for ID: '{userID}' because the ID is Invalid");
        }

        ~User() 
        {
            callbackPersonaStateChange.Dispose();
            callbackAvatarLoaded.Dispose();
        }


        /*********************************************************************/
        /* Information Aquisition ********************************************/
        /*********************************************************************/
        /// <summary>
        /// <para>Starts an Attempt to Retrieve a User's Information</para>
        /// <para>Invokes <see cref="InformationLoaded"/> after the Request has been Processed</para>
        /// </summary>
        public void RequestName()
        {
            if (IsValid)
                if (!isInformationLoaded)
                {
                    if (Lobby.IsValid && !Lobby.Members.Contains(userID.m_SteamID) && SteamFriends.RequestUserInformation(this.userID, true))
                        Console.LogDebug($"User: Requesting all Information about User with ID: '{userID}'");
                    else
                    {
                        Console.LogDebug($"User: All Information about the User '{SteamFriends.GetFriendPersonaName(this.userID)}' is already Available");
                        
                        GetInformation();
                        isInformationLoaded = true;
                        InformationLoaded?.Invoke(this);
                    }
                }    
            else
                Console.LogWarning($"User: Tried to Request the Information of an Invalid User");
        }

        void OnPersonaStateChange(PersonaStateChange_t callback) 
        {
            if (callback.m_ulSteamID == userID.m_SteamID && callback.m_nChangeFlags == EPersonaChange.k_EPersonaChangeNameFirstSet)
            {
                Console.LogDebug($"User: Received Information Update about User: '{SteamFriends.GetFriendPersonaName(userID)}'");
                
                GetInformation();                
                isInformationLoaded = true;
                InformationLoaded?.Invoke(this);
            }
        }

        void GetInformation() 
            => name = SteamFriends.GetFriendPersonaName(userID);


        /*********************************************************************/
        /* Avatar Aquisition *************************************************/
        /*********************************************************************/
        /// <summary>
        /// <para>Starts an Attempt to Retrieve the User's Avatar</para>
        /// <para>Note: This must Only be Called after the <see cref="InformationLoaded"/> Event has been Received</para>
        /// <para>Emits <see cref="AvatarLoaded"/> after the Request has been Processed</para>
        /// </summary>
        public void RequestAvatar() 
        {
            if (isInformationLoaded)
                if (!isAvatarRequested)
                {
                    int handle = SteamFriends.GetLargeFriendAvatar(userID);

                    if (handle != -1)
                    {
                        Console.LogDebug($"User: Avatar for User: '{SteamFriends.GetFriendPersonaName(userID)}' is Already Available");
                        CreateAvatar(handle);
                        AvatarLoaded?.Invoke(this);
                    }
                    else
                        Console.LogDebug($"User: Requesting Avatar for User: '{SteamFriends.GetFriendPersonaName(userID)}'");

                    isAvatarRequested = true;
                }
            else
                Console.LogWarning($"User: Tried to Request the Avatar of a User before Requesting its' Information");
        }

        void OnAvatarImageLoaded(AvatarImageLoaded_t callback) 
        {
            if (callback.m_steamID == userID)
            {
                Console.LogDebug($"User: Received Avatar Update with Handle ID: '{callback.m_iImage}' for User: '{SteamFriends.GetFriendPersonaName(userID)}'");
                
                CreateAvatar(callback.m_iImage);
                AvatarLoaded?.Invoke(this);
            }
        }        

        void CreateAvatar(int handle) 
        {
            if (handle == 0)
            {
                Console.LogDebug($"User: Avatar for User: '{SteamFriends.GetFriendPersonaName(userID)}' is not Set, Creating Default");
                Avatar = Avatar.CreateDefaultAvatar();
            }
            else
            {
                Console.LogDebug($"User: Creating Avatar for User: '{SteamFriends.GetFriendPersonaName(userID)}'");
                Avatar = new Avatar(handle);
            }
        }
    }
}