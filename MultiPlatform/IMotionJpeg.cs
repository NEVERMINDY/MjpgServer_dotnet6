using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MultiPlatform
{
    internal interface IMotionJpeg
    {
        //Read images to byte array to send directly
        public byte[] ReadToBuffer();

        //Read images to Mat array for processing
        public Mat ReadToMat();

        //to control sending
        public bool WhetherImageLeft();

    }
}
