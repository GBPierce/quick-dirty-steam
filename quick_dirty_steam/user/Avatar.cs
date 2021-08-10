using Steamworks;

namespace QuickDirtySteam.User
{
    public class Avatar 
    {
        /// <summary>
        /// The Dimensions of the Avatar in Both X and Y Directions
        /// </summary>
        public const uint Size = 184;
        public static Avatar CreateDefaultAvatar() => new Avatar() { Handle = -1, imageBuffer = AvatarDefaultImageBuffer };
        
        static byte[] AvatarDefaultImageBuffer { get; set; }

        static Avatar()
            => AvatarDefaultImageBuffer = new byte[Size * Size * 4];

        public int Handle { get; private set; }
        
        /// <summary>
        /// The Pixel Data of the Image in RGBA Format
        /// </summary>
        public byte[] ImageBuffer { get => (byte[])imageBuffer.Clone(); }

        byte[] imageBuffer;

        public Avatar(int handle) 
        {
            Handle = handle;

            if (!SteamUtils.GetImageSize(handle, out _, out _)) 
                Console.LogWarning("Avatar: Failed to Retrieve the Image Size because of an Invalid Handle");

            imageBuffer = new byte[Size * Size * 4];
            
            if (!SteamUtils.GetImageRGBA(handle, imageBuffer, imageBuffer.Length))
                Console.LogWarning($"Avatar: Failed to Retrieve the Image Buffer");
        }

        private Avatar() { }
    }
}
