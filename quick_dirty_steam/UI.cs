using Steamworks;
using QuickDirtySteam.Matchmaking;

namespace QuickDirtySteam 
{
    public static class UI 
    {
        /// <summary>
        /// Opens an Invite Dialog Pointing to <see cref="Lobby.ID"/>
        /// </summary>
        public static void ActivateInviteDialog() 
        {
            if (Lobby.IsValid)
                SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(Lobby.ID));
            else
                Console.LogWarning($"UI: Tried to Open the Invite Dialog before Entering a Lobby");
        }

        /// <summary>
        /// Opens the Web Browser at the Provided URL
        /// </summary>
        public static void ActivateWebBrowser(string url)
            => SteamFriends.ActivateGameOverlayToWebPage(url);
    }
}