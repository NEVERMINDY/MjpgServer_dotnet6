using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Diagnostics;
using OpenCvSharp;
namespace MjpgServerDotnet6
{ 
    internal class MjpgServerMain
    {
        static void Main(string[] args)
        {
            MjpgServer MyServer = new MjpgServer();
            MyServer.StartServer();
        }
    }
}
// See https://aka.ms/new-console-template for more information

