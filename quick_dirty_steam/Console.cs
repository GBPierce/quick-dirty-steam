namespace QuickDirtySteam 
{
    public static class Console 
    {
        public delegate void OnInfoEventHandler(string message);
        public delegate void OnErrorEventHandler(string message); 
        public delegate void OnWarningEventHandler(string message);
        public delegate void OnDebugEventHandler(string message);

        public static event OnErrorEventHandler OnError;
        public static event OnWarningEventHandler OnWarning;
        public static event OnInfoEventHandler OnInfo;
        public static event OnDebugEventHandler OnDebug;

        public static void LogInfo(string message) => OnInfo?.Invoke(message);
        public static void LogError(string message) => OnError?.Invoke(message);
        public static void LogWarning(string message) => OnWarning?.Invoke(message);
        public static void LogDebug(string message) => OnDebug?.Invoke(message);
    }
}