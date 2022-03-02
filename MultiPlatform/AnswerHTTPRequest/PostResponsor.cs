using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace MjpgServerDotnet6
{
    internal class PostResponsor:IRequestResponsor
    {
        public void Respond(string message,Socket FromClient)
        {
            switch (RecognizePostRequest(message))
            {
                //START本是为了发送MJPG流，但是目前没有搞定，前端展示用的是websocket，
                //websocket发送图片的具体实现在answerShaking函数，因此目前POST的功能不包含START   
                case "STOP":
                    StreamStop(FromClient);
                    break;
                case "PAUSE":
                    StreamPause();
                    break;
                case "CONTINUE":
                    StreamContinue();
                    break;
                case "CHANGEPARAMETERS":
                    ChangeParameters(message.Split("&"));
                    break;
                case "RESET":
                    ResetIndex();
                    break;
            }
        }

        /// <summary>
        /// 停止发送图片(断开连接)
        /// </summary>
        /// <param name="fromclient"></param>
        private void StreamStop(Socket fromclient)
        {
            fromclient.Shutdown(SocketShutdown.Both);
            fromclient.Close();
        }

        /// <summary>
        /// 暂停发送图片
        /// </summary>
        private void StreamPause()
        {
            ShakingResponsor.Pause();
        }

        /// <summary>
        /// 继续发送图片
        /// </summary>
        private void StreamContinue()
        {
            ShakingResponsor.Continue();
        }

        /// <summary>
        /// 根据Post报文信息，提取client的请求
        /// </summary>
        /// <param name="Message">Post报文</param>
        /// <returns>Post请求</returns>
        private string RecognizePostRequest(string Message)
        {
            //Console.WriteLine(Message);
            string[] PostMessageArray = Message.Split("\r\n", StringSplitOptions.None);
            string PostMessage = PostMessageArray[PostMessageArray.Length - 1];
            string OptionStr = PostMessage.Split("&", StringSplitOptions.None)[0];
            return OptionStr;
        }

        //convention: Posted data of CHANGEPARAMETER message should be like:
        //CHANGEPARAMETERS&a&b&c&d&e&f&g
        //a:WhetherResize         bool:false no/true yes
        //b:WhetherBrightness     bool:false no/true yes
        //c:WhetherContrast       bool:false no/true yes
        //d:WidthPercent          int :
        //e:HeightPercent         int :
        //f:brightnessvalue       int :
        //g:contrastvalue         int :
        /// <summary>
        /// 根据收到的PostData更改图像处理的参数
        /// </summary>
        /// <param name="parameterString"></param>
        private void ChangeParameters(string[] parameterString)
        {
            ProcessParameters.WhetherResize = (parameterString[1] == "true") ? true : false;
            ProcessParameters.WhetherBrightness = (parameterString[2] == "true") ? true : false;
            ProcessParameters.WhetherContrast = (parameterString[3] == "true") ? true : false;
            ProcessParameters.WidthPercent = int.Parse(parameterString[4]);
            ProcessParameters.HeightPercent = int.Parse(parameterString[5]);
            ProcessParameters.BrightnessValue = int.Parse(parameterString[6]);
            ProcessParameters.ContrastValue = int.Parse(parameterString[7]);
            ProcessParameters.playDelay = parameterString[8] == "" ? 20 : int.Parse(parameterString[8]);
            ProcessParameters.WhetherDrawString = parameterString[9] == "" ? false : true;
            ProcessParameters.StringToDraw = parameterString[9];

            if (ProcessParameters.WhetherBrightness)
            {
                ProcessParameters.RenewBrightTable(ProcessParameters.BrightnessValue);
            }
            if (ProcessParameters.WhetherContrast)
            {
                ProcessParameters.RenewContrastTable(ProcessParameters.ContrastValue);
            }

            Console.WriteLine("\r\nParameters Changed:\r\n");
            Console.Write("WhetherResize:" + ProcessParameters.WhetherResize.ToString() + "\t");
            Console.Write("WhetherBrightness:" + ProcessParameters.WhetherBrightness.ToString() + "\t");
            Console.Write("WhtherContrast:" + ProcessParameters.WhetherContrast.ToString() + "\t");
            Console.Write("WhetherDrawString:" + ProcessParameters.WhetherDrawString.ToString() + "\n");
            Console.Write("WidthPercent:" + parameterString[4]);
            Console.Write("\tHeightPercent:" + parameterString[5]);
            Console.Write("\tBrighenessValue:" + parameterString[6]);
            Console.Write("\tContrastValue:" + parameterString[7]);
            Console.Write("\nPlayDelay:" + parameterString[8]);
            Console.Write("\tStringToDraw:" + parameterString[9]);
        }

        /// <summary>
        /// 重置图片索引(MemoryReader.index)
        /// </summary>
        private void ResetIndex()
        {
            MemoryReader.index = 0;
        }
    }
}
