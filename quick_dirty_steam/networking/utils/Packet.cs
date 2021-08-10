using System;

namespace QuickDirtySteam.Networking.Utils
{
    [Serializable]
    public class Packet 
    {
        public uint PacketID { get; private set; }
        public ulong SenderID { get; private set; }

        protected Packet(uint packetID, ulong senderID)
        {
            PacketID = packetID;
            SenderID = senderID;
        }
    } 
}
