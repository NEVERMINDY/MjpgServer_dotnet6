using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MultiPlatform
{
    internal class JpegFromTCP:IMotionJpeg
    {
        public byte[] ReadToBuffer()
        {
            byte[] buffer = null;
            return buffer;
        }

        public Mat ReadToMat()
        {
            Mat matframe = new Mat();
            return matframe;
        }

        public bool WhetherImageLeft()
        {
            return true;
        }
    }
}
