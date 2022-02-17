using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MultiPlatform
{
    internal class ProcessSingleThread:IGraphicProcess
    {
        #region private methods

        //calculate the raw image's histogram
        //return:: mat of histogram
        protected Mat CalculateHist(Mat Src)
        {
            //create paramters
            Mat[] mats = Cv2.Split(Src);
            Mat[] mats0 = new Mat[] { mats[0] };//b
            Mat[] mats1 = new Mat[] { mats[1] };//g
            Mat[] mats2 = new Mat[] { mats[2] };//r

            Mat[] hist = new Mat[] { new Mat(), new Mat(), new Mat() };

            int[] channels = new int[] { 0 };

            int[] histsize = new int[] { 256 };

            Rangef[] range = new Rangef[1]; range[0] = new Rangef(0, 255);

            Mat mask = new Mat();

            //calculate the histogram
            Cv2.CalcHist(mats0, channels, mask, hist[0], 1, histsize, range, true, false);
            Cv2.CalcHist(mats1, channels, mask, hist[1], 1, histsize, range, true, false);
            Cv2.CalcHist(mats2, channels, mask, hist[2], 1, histsize, range, true, false);

            //normalize
            Cv2.Normalize(hist[0], hist[0], 0, 255, NormTypes.MinMax);
            Cv2.Normalize(hist[1], hist[1], 0, 255, NormTypes.MinMax);
            Cv2.Normalize(hist[2], hist[2], 0, 255, NormTypes.MinMax);

            //create dest image
            Mat histresult = new Mat(256, 256, MatType.CV_8UC3, Scalar.White);

            //draw lines
            for (int i = 0; i < 256; i++)
            {
                int len_b = (int)hist[0].Get<float>(i);
                int len_g = (int)hist[1].Get<float>(i);
                int len_r = (int)hist[2].Get<float>(i);


                int prelen_b = (int)hist[0].Get<float>(i - 1);
                int prelen_g = (int)hist[1].Get<float>(i - 1);
                int prelen_r = (int)hist[2].Get<float>(i - 1);
                //point_r[i]=(i,256-len_b)
                //point_r[i-1]=(i-1,256)
                if (i > 0)
                {
                    Cv2.Line(histresult, i, 256 - len_b, i - 1, 256 - prelen_b, Scalar.Blue);
                    Cv2.Line(histresult, i, 256 - len_g, i - 1, 256 - prelen_g, Scalar.Green);
                    Cv2.Line(histresult, i, 256 - len_r, i - 1, 256 - prelen_r, Scalar.Red);
                }
            }
            return histresult;
        }

        public Mat CalcHistGray(Mat srcimg)
        {
            Mat hist = new Mat();
            Cv2.CalcHist(new Mat[] { srcimg }, new int[] { 0 }, new Mat(), hist, 1, new int[] { 256 }, new Rangef[] { new Rangef(0, 255) });
            Cv2.Normalize(hist, hist);
            Mat result = new Mat(new OpenCvSharp.Size(256, 256), MatType.CV_8UC3, Scalar.White);
            for (int i = 0; i < 256; i++)
            {
                float v = hist.Get<float>(i);
                int len = (int)(v * 256);
                if (len != 0)
                {
                    Cv2.Line(result, i, 256, i, 256 - len, Scalar.Black);
                }
            }
            return result;
        }

        public Mat AdjustBrightness(Mat src, int value, ref ManualResetEvent handle)
        {

            for (int x = 0; x < src.Height; x++)
            {
                for (int y = 0; y < src.Width; y++)
                {
                    SearchPixelBrightness(ref src.At<Vec3b>(x, y), value);
                    //AdjustPixelBrightness(ref src.At<Vec3b>(x, y), value);
                }
            }
            handle.Set();
            return src;
        }

        public Mat AdjustContrast(Mat src, int value, ManualResetEvent handle)
        {
            for (int x = 0; x < src.Height; x++)
            {
                for (int y = 0; y < src.Width; y++)
                {
                    SearchPixelContrast(ref src.At<Vec3b>(x, y), value);
                }
            }
            handle.Set();
            return src;
        }

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

        protected Vec3b SearchPixelBrightness(ref Vec3b srcpixel,int beta)
        {
            for (int color = 0;color<3; color++)
            {
                srcpixel[color] = ProcessParameters.BrightTable[srcpixel[color]];
                //srcpixel[color] = ProcessParameters.BrightTable.ContainsKey(srcpixel[color]) ? 
                //    (byte)ProcessParameters.BrightTable[srcpixel[color]] : 
                //    AdjustPixelBrightness(ref srcpixel, beta)[color];
            }
            return srcpixel;
        }

        protected Vec3b SearchPixelContrast(ref Vec3b srcpixel,int contrast)
        {
            for(int color = 0; color < 3; color++)
            {
                srcpixel[color] = ProcessParameters.ContrastTable[srcpixel[color]];
            }
            return srcpixel;
        }

        #endregion


        #region implement methods
        public Mat DrawString(Mat src,string text)
        {
            Cv2.PutText(src, text, new Point(100, 100), HersheyFonts.HersheySimplex, 0.5, Scalar.Red);
            return src;
        }

        public Mat AddHist(Mat srcimg)
        {
            //Mat hist = CalculateHist(srcimg);
            Mat hist = CalcHistGray(srcimg);
            Mat resizedhist = ResizeByValue(hist, srcimg.Height / 3, srcimg.Width / 3);

            int pos_x = srcimg.Width - resizedhist.Width;
            int pos_y = srcimg.Height - resizedhist.Height;
            //create interesed rectangle
            Rect roi = new Rect(pos_x, pos_y, resizedhist.Width, resizedhist.Height);
            //claim the interested position
            if (pos_x + resizedhist.Width <= srcimg.Width || pos_y + resizedhist.Height <= srcimg.Height) 
            {
                Mat roimat = new Mat(srcimg, roi);
                //copy hist to the position claimed
                resizedhist.CopyTo(roimat);
                return srcimg;
            }
            return srcimg;
        }

        public Mat ResizeByRate(Mat src, int WidthPercent, int HeightPercent)
        {
            float widthrate = (float)WidthPercent / 100;
            float heightrate = (float)HeightPercent / 100;
            Mat ResizeMat = new Mat(src.Height * HeightPercent/100, src.Width * WidthPercent/100, MatType.CV_8UC3);
            Cv2.Resize(src, ResizeMat, new OpenCvSharp.Size(), heightrate, widthrate, InterpolationFlags.Cubic);
            return ResizeMat;
        }

        public Mat ResizeByValue(Mat src, int WidthValue, int HeightValue)
        {
            Mat ResizeMat = new Mat(HeightValue, WidthValue, MatType.CV_8UC3);
            Cv2.Resize(src, ResizeMat, new OpenCvSharp.Size(WidthValue, HeightValue), 0, 0, InterpolationFlags.Cubic);
            return ResizeMat;
        }

        public Mat AdjustBrightness(Mat src,int value)
        {
            for(int x = 0; x < src.Height; x++)
            {
                for(int y = 0; y < src.Width; y++)
                {
                    SearchPixelBrightness(ref src.At<Vec3b>(x, y), value);
                }
            }
            return src;
        }

        public Mat AdjustContrast(Mat src,int value)
        {
            for(int x = 0; x < src.Height; x++)
            {
                for(int y=0; y< src.Width; y++)
                {
                    SearchPixelContrast(ref src.At<Vec3b>(x,y), value);
                }
            }
            return src;
        }

        #endregion

    }
}
