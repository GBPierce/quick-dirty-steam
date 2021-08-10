using Godot;
using Steamworks;

namespace QuickDirtySteam 
{
    public static class SteamManager
    {
        public static bool IsInitialized { get; private set; }

        public static void Initialize() 
        {
            InitializeSteamworks();
            
            if (IsInitialized)
                InitializeRelayNetworkAccess();
        }

        public static void RunCallbacks()
        {
            if (SteamAPI.IsSteamRunning() && IsInitialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        public static void Shutdown() 
        {
            if (SteamAPI.IsSteamRunning() && IsInitialized) 
            {
                SteamAPI.Shutdown();
            }
        }

        static void InitializeSteamworks() 
        {
            if (SteamAPI.Init()) 
            {
                IsInitialized = true;
            }
            else
            {
                IsInitialized = false;
            }
        }

        static void InitializeRelayNetworkAccess() 
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
        }
    }
}
