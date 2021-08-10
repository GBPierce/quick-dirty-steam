using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;

namespace QuickDirtySteam.Networking 
{
    internal static class MessageReceiver 
    {
        public static List<byte[]> Receive(HSteamNetConnection connection) 
        {
            List<byte[]> buffers = new List<byte[]>();

            IntPtr[] receiveBuffers = new IntPtr[16];

            int messageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(connection, receiveBuffers, receiveBuffers.Length);

            SteamNetworkingMessage_t netMessage = new SteamNetworkingMessage_t();

            for (int i = 0; i < messageCount; ++i)
            {
                try
                {
                    netMessage = Marshal.PtrToStructure<SteamNetworkingMessage_t>(receiveBuffers[i]);
                    
                    byte[] buffer = new byte[netMessage.m_cbSize];
                    Marshal.Copy(netMessage.m_pData, buffer, 0, buffer.Length);

                    buffers.Add(buffer);
                }
                finally
                {
                    //netMessage.Release(); // TODO: Figure out why this Crashes absolutely Everything
                    Marshal.DestroyStructure<SteamNetworkingMessage_t>(receiveBuffers[i]);
                }
            }
            
            return buffers;
        } 
    } 
}