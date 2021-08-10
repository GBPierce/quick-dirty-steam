using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace QuickDirtySteam.Networking.Utils
{
    public static class PacketCoverter
    {
        static BinaryFormatter Formatter = new BinaryFormatter();


        /// <summary>
        /// Attempts to Convert a <see cref="Packet"/> to bytes
        /// </summary>
        /// <returns><c>byte[]</c> or <c>null</c></returns> 
        public static byte[] ToBytes(Packet packet) 
        {
            var stream = new MemoryStream();
            
            try
            {
                Formatter.Serialize(stream, packet);
                return stream.ToArray();
            }
            catch (Exception exception) 
            {
                Console.LogWarning($"PacketConverter: An Exception was Thrown in the Process of Serializing a Packet: '{exception.Message}'");
            }

            return null;
        }

        /// <summary>
        /// Attempts to Convert a <c>byte[]</c> to a <see cref="Packet"/>
        /// </summary>
        /// <returns><see cref="Packet"/> or <c>null</c></returns> 
        public static Packet ToPacket(byte[] buffer) 
        {
            try
            {
                var packet = (Packet)Formatter.Deserialize(new MemoryStream(buffer));
                return packet;
            }
            catch (Exception exception) 
            {
                Console.LogWarning($"PacketConverter: An Exception was Thrown in the Process of Deserializing a Packet: '{exception.Message}'");
            }

            return null;
        }
    } 
}
