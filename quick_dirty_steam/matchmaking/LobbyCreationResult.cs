namespace QuickDirtySteam.Matchmaking 
{
    public enum LobbyCreationResult
    {
        Success = 1,
        ServerError = 2,
        NoConnection = 3,
        AccessDenied = 15,
        ServerTimeout = 16,
        LimitExceeded = 25,
    }
}