using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjpgServerDotnet6
{
    internal static class ProcessParameters
    {
        public static bool WhetherResize;
        public static bool WhetherBrightness;
        public static bool WhetherContrast;
        public static bool WhetherDrawString;
        //(1,200)
        public static int WidthPercent;
        //(1,200)
        public static int HeightPercent;
        //(-255,255)
        public static int BrightnessValue;
        //(-255,255)
        public static int ContrastValue;
        //(1,1000);
        public static int playDelay = 20;

        public static string StringToDraw = "";

        public static Dictionary<byte,byte> BrightTable;

        public static Dictionary<byte,byte> ContrastTable;

        public static void initBrightTable(int brightness)
        {
            BrightTable = new Dictionary<byte, byte>();
            for (int srcvalue = 0; srcvalue < 256; srcvalue++)
            {
                int destvalue = srcvalue + brightness;
                destvalue = destvalue > 255 ? 255 : destvalue;
                destvalue = destvalue < 0 ? 0 : destvalue;
                BrightTable.Add((byte)srcvalue, (byte)destvalue);
            }
        }

        public static void initContrastTable(int contrast)
        {
            ContrastTable = new Dictionary<byte, byte>();
            for (int srcvalue = 0; srcvalue < 256; srcvalue++)
            {
                int destvalue = srcvalue + (srcvalue - 127) * contrast / 255;
                destvalue = destvalue > 255 ? 255 : destvalue;
                destvalue = destvalue < 0 ? 0 : destvalue;
                ContrastTable.Add((byte)srcvalue, (byte)destvalue);
            }
        }

        public static void RenewBrightTable(int brightness)
        {
            int destvalue;
            Parallel.For(0, 256, srcvalue =>
            {
                destvalue = srcvalue + brightness;
                destvalue = destvalue > 255 ? 255 : destvalue;
                destvalue = destvalue < 0 ? 0 : destvalue;
                BrightTable[(byte)srcvalue] = (byte)destvalue;
            });
        }

        public static void RenewContrastTable(int contrast)
        {
            int destvalue;
            Parallel.For(0, 256, srcvalue =>
            {
                destvalue = srcvalue + (srcvalue - 127) * contrast / 255;
                destvalue = destvalue > 255 ? 255 : destvalue;
                destvalue = destvalue < 0 ? 0 : destvalue;
                ContrastTable[(byte)srcvalue] = (byte)destvalue;
            });
        }

        public static bool WhetherNeedProcess()
        {
            return (WhetherResize || WhetherDrawString || WhetherBrightness || WhetherContrast);
        }

        static ProcessParameters()
        {
            WhetherResize = false;
            WhetherBrightness = false;
            WhetherContrast = false;
            WhetherDrawString = false;
            WidthPercent = 100;
            HeightPercent = 100;
            BrightnessValue = 0;
            ContrastValue = 0;
            playDelay = 20;
            StringToDraw = "";
            initBrightTable(0);
            initContrastTable(0);
        }

    }
}
