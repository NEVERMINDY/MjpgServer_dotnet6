using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MjpgServerDotnet6
{
    internal class ProcessMultiThread: ProcessSinglePixel, IGraphicProcess
    {
        static int PiecesNumber = int.Parse(ConfigurationManager.AppSettings["GraphicProcessThreadNum"]);

        public ProcessMultiThread(int PiecesNumbervalue)
        {
            PiecesNumber = PiecesNumbervalue > 2 ? PiecesNumber : 2;
        }

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

        //把Mat矩阵竖着切，分成x长条以供多线程处理
        //use ROI(region of interested) to claim a Mat
        //1.Calculate width step
        //2.create roi array (posx,posy,widthstep,heightstep)
        //3.Create new Mat array according to Roi
        //4.return Mat array
        private Mat[] Cut(Mat src)
        {
            int widthstep = src.Width / PiecesNumber;
            int posx = 0, posy = 0;
            Rect[] roi = new Rect[PiecesNumber];
            Mat[] MatOfRoi = new Mat[PiecesNumber];
            for(int i = 0; i < PiecesNumber-1; i++)
            {
                roi[i] = new Rect(posx, posy, widthstep, src.Height);
                MatOfRoi[i] = new Mat(src, roi[i]);
                posx += widthstep;
            }
            roi[PiecesNumber - 1] = new Rect(posx, posy, src.Width - posx, src.Height);
            MatOfRoi[PiecesNumber - 1] = new Mat(src, roi[PiecesNumber - 1]);
            return MatOfRoi;
        }

        public Mat AdjustBrightness(Mat src, int value, ManualResetEvent handle)
        {
            Parallel.For(0, src.Height, x =>
            {
                for (int y = 0; y < src.Width; y++)
                {
                    SearchPixelBrightness(ref src.At<Vec3b>(x, y), value);
                }
            });

            handle.Set();
            return src;
        }

        public Mat AdjustContrast(Mat src, int value, ManualResetEvent handle)
        {
            Parallel.For(0, src.Height, x =>
            {
                for (int y = 0; y < src.Width; y++)
                {
                    SearchPixelContrast(ref src.At<Vec3b>(x, y), value);
                }
            });

            handle.Set();
            return src;
        }

        #region implement methods

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
            if (0 < pos_x && 0 < pos_y && pos_x + resizedhist.Width <= srcimg.Width && pos_y + resizedhist.Height <= srcimg.Height)
            {
                Mat roimat = new Mat(srcimg, roi);
                //copy hist to the position claimed
                resizedhist.CopyTo(roimat);
                return srcimg;
            }
            return srcimg;
        }

        public Mat DrawString(Mat src, string text)
        {
            Cv2.PutText(src, text, new Point(100, 100), HersheyFonts.HersheySimplex, 1.0, Scalar.Red);
            return src;
        }

        public Mat ResizeByRate(Mat src, int WidthPercent, int HeightPercent)
        {
            float widthrate = (float)WidthPercent / 100;
            float heightrate = (float)HeightPercent / 100;
            Mat ResizeMat = new Mat(src.Height * HeightPercent / 100, src.Width * WidthPercent / 100, MatType.CV_8UC3);
            Cv2.Resize(src, ResizeMat, new OpenCvSharp.Size(), heightrate, widthrate, InterpolationFlags.Cubic);
            return ResizeMat;
        }

        public Mat ResizeByValue(Mat src, int WidthValue, int HeightValue)
        {
            Mat ResizeMat = new Mat(HeightValue, WidthValue, MatType.CV_8UC3);
            Cv2.Resize(src, ResizeMat, new OpenCvSharp.Size(WidthValue, HeightValue), 0, 0, InterpolationFlags.Cubic);
            return ResizeMat;
        }

        //adjust brightness
        //1.Cut Mat
        //2.Create handle of threads
        //3.Create threads
        //4.every thread handles one pieces,setting handle after done
        //5.Wait for all threads done
        public Mat AdjustBrightness(Mat src,int value)
        {
            Mat[] mats = Cut(src);
            ManualResetEvent[] _brHandles = new ManualResetEvent[PiecesNumber];
            Thread[] _brThreads = new Thread[PiecesNumber];
            int i = 0;
            while (i< PiecesNumber)
            {
                int index = i;
                _brHandles[i] = new ManualResetEvent(false);
                _brThreads[i] = new Thread(() =>
                {
                    AdjustBrightness(mats[index], ProcessParameters.BrightnessValue,_brHandles[index]);
                });
                _brThreads[i].Start();
                i++;
            }
            WaitHandle.WaitAll(_brHandles);
            return src;
        }

        //same as AdjustBrightnessMul
        public Mat AdjustContrast(Mat src,int value)
        {
            Mat[] mats = Cut(src);
            ManualResetEvent[] _conHandles = new ManualResetEvent[PiecesNumber];
            Thread[] _conThreads = new Thread[PiecesNumber];
            for(int i = 0; i < PiecesNumber; i++)
            {
                int index = i;
                _conHandles[i] = new ManualResetEvent(false);
                _conThreads[i] = new Thread(() =>
                {
                    AdjustContrast(mats[index], ProcessParameters.ContrastValue, _conHandles[index]);
                });
                _conThreads[i].Start();
            }
            WaitHandle.WaitAll(_conHandles);
            return src;
        }

        #endregion
    }
}
