using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Diagnostics;
using OpenCvSharp;
namespace MultiPlatform
{ 
    internal class Program
    {
        static void Main(string[] args)
        {

/*            ProcessParameters.initBrightTable(50);
            Stopwatch sw = new Stopwatch();

            Mat[] mat = new Mat[10];
            mat[0] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-001.jpeg");
            mat[1] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-002.jpeg");
            mat[2] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-003.jpeg");
            mat[3] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-004.jpeg");
            mat[4] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-005.jpeg");
            mat[5] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-006.jpeg");
            mat[6] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-007.jpeg");
            mat[7] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-008.jpeg");
            mat[8] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-009.jpeg");
            mat[9] = Cv2.ImRead(@"C:\program_Alanchen\C#imageprocessing\MjpgServer_dotnet6\MultiPlatform\MultiPlatform\bin\Debug\net6.0\image-010.jpeg");


            ProcessSingleThread single = new ProcessSingleThread();
            ProcessMultiThread multi1 = new ProcessMultiThread(3);

            sw.Start();
            for (int i = 0; i < mat.Length; i++)
            {
                multi1.AdjustBrightnessMul(mat[i], 50);
            }
            sw.Stop();
            Console.WriteLine("multi threads take:" + sw.ElapsedMilliseconds + "ms");

            sw.Reset();
            sw.Start();
            for (int i = 0; i < mat.Length; i++)
            {
                single.AdjustBrightness(mat[i], 50);
            }
            sw.Stop();
            Console.WriteLine("single thread take:" + sw.ElapsedMilliseconds + "ms");*/
            MjpgServer MyServer = new MjpgServer();
            MyServer.StartServer();
        }
    }
}
// See https://aka.ms/new-console-template for more information

