using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MultiPlatform
{
    internal interface IGraphicProcess
    {
        public Mat DrawString(Mat src, string text);
        public Mat AddHist(Mat src);

        public Mat ResizeByRate(Mat src, int WidthPercent, int HeightPercent);

        public Mat ResizeByValue(Mat src, int WidthValue, int HeightValue);

        public Mat AdjustBrightness(Mat src, int BrightnessValue);

        public Mat AdjustContrast(Mat src, int ContrastValue);
    }
}
