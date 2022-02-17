using System;

namespace MultiPlatform
{
    public class Mjpg : EventArgs
    {
        public long fileSize;

        public string fileName;

        public int width;

        public int height;

        public byte[] data;
    }
}
