using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;


namespace MjpgServerDotnet6
{
    internal class ProcessSinglePixel
    {
        [Obsolete]
        protected Vec3b AdjustPixelBrightness(ref Vec3b srcpixel, int beta)
        {
            for (int color = 0; color < 3; color++)
            {
                float pixelvalue = 1.0f * srcpixel[color] + beta;
                pixelvalue = pixelvalue > 255 ? 255 : pixelvalue;
                pixelvalue = pixelvalue < 0 ? 0 : pixelvalue;
                srcpixel[color] = (byte)pixelvalue;
            }
            return srcpixel;
        }

        [Obsolete]
        protected Vec3b AdjustPixelContrast(ref Vec3b pixel, int contrast)
        {
            for (int color = 0; color < 3; color++)
            {
                float pixelvalue = pixel[color] + (pixel[color] - 127) * contrast / 255;
                pixelvalue = pixelvalue > 255 ? 255 : pixelvalue;
                pixelvalue = pixelvalue < 0 ? 0 : pixelvalue;
                pixel[color] = (byte)pixelvalue;
            }
            return pixel;
        }

        protected Vec3b SearchPixelBrightness(ref Vec3b srcpixel, int beta)
        {
            srcpixel[0] = ProcessParameters.BrightTable[srcpixel[0]];
            srcpixel[1] = ProcessParameters.BrightTable[srcpixel[1]];
            srcpixel[2] = ProcessParameters.BrightTable[srcpixel[2]];
            return srcpixel;
        }

        protected Vec3b SearchPixelContrast(ref Vec3b srcpixel, int contrast)
        {
            srcpixel[0] = ProcessParameters.ContrastTable[srcpixel[0]];
            srcpixel[1] = ProcessParameters.ContrastTable[srcpixel[1]];
            srcpixel[2] = ProcessParameters.ContrastTable[srcpixel[2]];
            return srcpixel;
        }


    }
}
