using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace MultiPlatform
{
    internal class JpegFromMemory:IMotionJpeg
    {
        #region properties

        #region static properties
        public static string? ImagesPath { get; private set; }

        public static FileInfo[]? ImagesInfo { get; private set; }

        public static int ImageCount { get; private set; }
        #endregion

        public static int index  { get;set; }

        #endregion

        #region Methods

        //construct function
        //initialize: ImagesPath,ImagesInfo,ImageCount according to ./images
        //initialize: index = 0
        public JpegFromMemory()
        {
            ImagesPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName + @"\images\";
            DirectoryInfo dir = new DirectoryInfo(ImagesPath);
            ImagesInfo = dir.GetFiles("*.jpg");
            Array.Sort(ImagesInfo, new FileNameSort());
            ImageCount = ImagesInfo.Length;
        }

        #region implement interface

        public Stream ReadToStream()
        {
            if (ImagesPath != null && ImagesInfo != null && ImageCount != 0)
            {
                FileStream fs = new FileStream(ImagesInfo[index++].FullName, FileMode.Open);
                return fs;
            }
            return FileStream.Null;
        }

        public byte[] ReadToBuffer()
        {
            if (ImagesPath != null && ImagesInfo != null && ImageCount != 0)
            {
                bool ifObtained = true;
                while(ifObtained)
                {
                    try
                    {
                        //FileStream fs = new FileStream(ImagesInfo[index++].FullName, FileMode.Open);
                        FileStream fs = new FileStream(ImagesInfo[index].FullName, FileMode.Open);
                        BinaryReader br = new BinaryReader(fs);
                        byte[] ImageBuffer = br.ReadBytes((int)fs.Length);
                        //Console.WriteLine("image name:{0}", ImagesInfo[index-1].Name);
                        ifObtained = false;
                        //Console.WriteLine("index:" + index.ToString());
                        return ImageBuffer;
                    }
                    catch
                    {

                    }
                }
            }
            return null;
        }

        public Mat ReadToMat()
        {
            if (ImagesPath != null && ImagesInfo != null && ImageCount != 0)
            {
                bool ifObtained = true;
                while(ifObtained)
                {
                    try
                    {
                        //Mat mat = Cv2.ImRead(ImagesInfo[index++].FullName);
                        Mat mat = Cv2.ImRead(ImagesInfo[index].FullName);
                        ifObtained = false;
                        return mat;
                    }
                    catch
                    {

                    }
                }
                
            }
            return null;
        }

        public bool WhetherImageLeft()
        {
            return index < ImageCount ? true : false;
        }

        #endregion

        #endregion

    }
}
