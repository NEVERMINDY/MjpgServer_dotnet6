using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MultiPlatform
{
    internal class ProcessMultiThread:ProcessSingleThread,IGraphicProcess
    {
        static int PiecesNumber = int.Parse(ConfigurationManager.AppSettings["GraphicProcessThreadNum"]);

        public ProcessMultiThread(int PiecesNumbervalue)
        {
            PiecesNumber = PiecesNumbervalue > 2 ? PiecesNumber : 2;
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

        #region implement methods

        //adjust brightness
        //1.Cut Mat
        //2.Create handle of threads
        //3.Create threads
        //4.every thread handles one pieces,setting handle after done
        //5.Wait for all threads done
        public Mat AdjustBrightnessMul(Mat src,int value)
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
                    AdjustBrightness(mats[index], ProcessParameters.BrightnessValue, ref _brHandles[index]);
                });
                _brThreads[i].Start();
                i++;
            }
            WaitHandle.WaitAll(_brHandles);
            return src;
        }

        //same as AdjustBrightnessMul
        public Mat AdjustContrastMul(Mat src,int value)
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
