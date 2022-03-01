using System;

namespace MjpgServerDotnet6
{
    public class ImageInfoReceived : EventArgs
    {
        public long fileSize;

        public string fileName;

        public int width;

        public int height;

        public byte[] data;
    }
}
