using System;

namespace MjpgServerDotnet6
{
    public class ImageFromDetector : EventArgs
    {
        public long fileSize;

        public string fileName;

        public int width;

        public int height;

        public byte[] data;
    }
}
