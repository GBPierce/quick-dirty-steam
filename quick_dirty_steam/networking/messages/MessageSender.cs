using System;
using System.Runtime.InteropServices;
using Steamworks;

namespace QuickDirtySteam.Networking 
{
    internal static class MessageSender 
    {
        static IntPtr sendBuffer = Marshal.AllocHGlobal(65536);

        public static bool Send(HSteamNetConnection connection, byte[] buffer) 
        {
            try
            {
                long messageNumber;
                
                Marshal.Copy(buffer, 0, sendBuffer, buffer.Length);
                EResult result = SteamNetworkingSockets.SendMessageToConnection(connection, sendBuffer, (uint)buffer.Length, 8, out messageNumber);

                switch( result )
                {
                    // TODO: Add error Messages
                    case EResult.k_EResultInvalidParam:
                    case EResult.k_EResultInvalidState:
                    case EResult.k_EResultNoConnection:
                    case EResult.k_EResultIgnored:
                    case EResult.k_EResultLimitExceeded:
                        Console.LogWarning("MessageSender: Failed to send Message");
                        break;
                }
            }           
            catch
            {
                Marshal.FreeHGlobal(sendBuffer);
            }    

            return true;   
        }
    }
}