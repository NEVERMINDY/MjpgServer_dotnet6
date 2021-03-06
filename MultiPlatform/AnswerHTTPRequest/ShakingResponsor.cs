using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using OpenCvSharp;


namespace MjpgServerDotnet6
{
    internal class ShakingResponsor:IRequestResponsor
    {
        #region static properties
        //这个标志位供内部函数控制是否需要调用sendtoall
        private static List<Socket>? _websocketList = new List<Socket>();

        private static bool _IfAlreadySending = false;

        //这个标志位供browser控制服务器是否发送
        public static bool _IfSend = false;

        private static string RFC6456 = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static readonly string webSocketShakingResponse =
                @"HTTP/1.1 101 Switching Protocals" + "\r\n" +
                @"Connection: Upgrade" + "\r\n" +
                @"Upgrade: websocket" + "\r\n" +
                @"Sec-WebSocket-Accept: AcceptKeywords" + "\r\n" +
                @"WebSocket-Origin: null" + "\r\n" +
                @"Sec-WebSocket-Location: ws://10.132.60.231:2022/" + "\r\n\r\n";

        #endregion

        /// <summary>
        /// 实现接口
        /// </summary>
        /// <param name="message"></param>
        /// <param name="FromClient"></param>
        public void Respond(string message,Socket FromClient)
        {
            if (AnswerShaking(message, FromClient))
            {
                //如果没有已在发送的websocket连接
                if (_IfAlreadySending == false) // if(!MjpgServer._IfAlreadySending)
                {
                    string? ReadJpegFrom = ConfigurationManager.AppSettings["ReadJpegFrom"];
                    if (ReadJpegFrom != null && ReadJpegFrom == "Memory")
                    {
                        _IfAlreadySending = true;
                        MemoryReader MemoryReader = new MemoryReader();
                        while (MemoryReader.WhetherImageLeft())
                        {
                            SendToAll(compositeWebSocketFrame(), _websocketList);
                        }
                        _IfAlreadySending = false;
                        MemoryReader.index = 0;
                    }
                    else if (ReadJpegFrom != null && ReadJpegFrom == "TCP")
                    {
                        ITCPReader TCPReader = new TCPReader();
                        TCPReader.Connect(IPEndPoint.Parse(ConfigurationManager.AppSettings["RemoteServerIPEndPoint"]));
                        TCPReader.OnReceiveImage += new EventHandler<ImageInfoReceived>(WhenReceiveImage);
                        TCPReader.GetImage();
                    }
                }
            }
            else
            {
                Console.WriteLine("Shaking Failed!");
            }
        }

        /// <summary>
        /// 处理WebSocket升级请求中的key,回传答复报文
        /// </summary>
        /// <param name="message">WebSocket升级请求</param>
        /// <param name="FromClient">接收到的Socket对象</param>
        /// <returns>是否握手成功</returns>
        private bool AnswerShaking(string message,Socket FromClient)
        {
            try
            {
                _IfSend = true;
                string[] msg = message.Split("\r\n");
                string webSocketKey = msg[msg.Length - 4].Split(":")[1].Trim() + RFC6456;
                string webSocketKey_SHA1 = CalculateSHA1(webSocketKey);
                string answer = webSocketShakingResponse.Replace("AcceptKeywords", webSocketKey_SHA1);
                FromClient.Send(Encoding.Default.GetBytes(answer));
                _websocketList.Add(FromClient);
                Console.WriteLine("Shaking Success!\n");
                return true;
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 计算WebSocket密钥
        /// </summary>
        /// <param name="webSocketKey">浏览器发来的WebSocket升级请求中提取的key</param>
        /// <returns>计算后的密钥</returns>
        private string CalculateSHA1(string webSocketKey)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] sha1buffer = sha1.ComputeHash(UTF8Encoding.Default.GetBytes(webSocketKey));
            return Convert.ToBase64String(sha1buffer);
        }

        /// <summary>
        /// 把jpg图像传输给所有保存在Socket集合中的Socket对象
        /// </summary>
        /// <param name="buffer">要发送的图片的byte[]</param>
        /// <param name="socketlist"></param>
        private void SendToAll(byte[] buffer, List<Socket> socketlist)
        {
            if (socketlist.Count != 0)
            {
                for (int index = 0; index < socketlist.Count; index++)
                {
                    if (buffer != null)
                    {
                        try
                        {
                            socketlist[index].Send(buffer);
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根据图片索引(MemoryReader.index),从Memory读取图片,进行相应处理，得到Websocket的帧内容
        /// </summary>
        /// <returns>Websocket帧内容的byte[]</returns>
        private byte[] compositeWebSocketFrame()
        {
            byte[] emptybuffer = null;
            if (_IfSend)
            {
                MemoryReader jpegFrame = new MemoryReader();
                if (ProcessParameters.WhetherNeedProcess())
                {
                    ProcessSingleThread pst = new ProcessSingleThread();
                    if (jpegFrame.WhetherImageLeft())
                    {
                        if (_IfSend == true)
                        {
                            Mat MatToProcess = jpegFrame.ReadToMat();
                            MemoryReader.index++;
                            //Mat clone = MatToProcess.Clone();
                            Mat clone = pst.ResizeByRate(MatToProcess, 50, 50);
                            //Mat result = pst.ResizeByRate(GraphicProcess(ref clone), 200, 200);
                            Mat result = GraphicProcess(ref clone);
                            return TurnMatToArray(result);
                            //return TurnMatToArray(pst.ResizeByRate(result, 200, 200));
                        }
                    }
                }
                else
                {
                    if (jpegFrame.WhetherImageLeft())
                    {
                        //1.Read image to buffer
                        //2.calculate header of websocket frame according to image
                        //3.send image to browser
                        //Console.WriteLine("index:{0},count:{1}", i++, MemoryReader.ImageCount);
                        if (_IfSend == true)
                        {
                            byte[] frame = jpegFrame.ReadToBuffer();
                            MemoryReader.index++;
                            byte[] contentHeader = compositeWebSocketHeader(frame);
                            byte[] content = new byte[frame.Length + contentHeader.Length];
                            Array.Copy(contentHeader, 0, content, 0, contentHeader.Length);
                            Array.Copy(frame, 0, content, contentHeader.Length, frame.Length);
                            Thread.Sleep(ProcessParameters.playDelay);
                            return content;
                        }
                    }
                }
                return emptybuffer;
            }
            return emptybuffer;
        }

        //websocket的头部根据其内容大小不同，有不同的长度
        //小于126字节：头部2个字节
        //小于2^16字节，大于126字节：头部4个字节，前2个字节为控制位，后2个字节(16bit)用来记录字节数
        //大于2^16字节：头部10个字节，前2个字节为控制位，后8个字节(64bit)用来记录字节数
        /// <summary>
        /// 根据Websocket帧内容构建头部
        /// </summary>
        /// <param name="jpegFrame">每一帧图像的byte[]</param>
        /// <returns>由该帧图像计算得到的websocket的帧头部</returns>
        private byte[] compositeWebSocketHeader(byte[] jpegFrame)
        {
            byte[] websocketHeader = null;
            if (jpegFrame.Length < 126)
            {
                websocketHeader = new byte[2];
                websocketHeader[0] = 0x82;
                websocketHeader[1] = (byte)jpegFrame.Length;
            }
            else if (jpegFrame.Length <= 0xFFFF)
            {
                websocketHeader = new byte[4];
                websocketHeader[0] = 0x82;
                websocketHeader[1] = 126;
                websocketHeader[2] = (byte)((jpegFrame.Length & 0xFF00) >> 8);
                websocketHeader[3] = (byte)(jpegFrame.Length & 0xFF);
            }
            //length >2^16 bytes
            else
            {
                websocketHeader = new byte[10];
                websocketHeader[0] = 0x82;
                websocketHeader[1] = 127;
                websocketHeader[2] = 0;
                websocketHeader[3] = 0;
                websocketHeader[4] = 0;
                websocketHeader[5] = 0;
                websocketHeader[6] = (byte)((jpegFrame.Length & 0xFF000000) >> 24);
                websocketHeader[7] = (byte)((jpegFrame.Length & 0XFF0000) >> 16);
                websocketHeader[8] = (byte)((jpegFrame.Length & 0xFF00) >> 8);
                websocketHeader[9] = (byte)(jpegFrame.Length & 0xFF);
            }
            //foreach(byte b in websocketHeader)
            //{
            //    Console.Write(Convert.ToInt32(b).ToString()+"\t");
            //}
            //Console.WriteLine("\n\n");
            return websocketHeader;
        }

        /// <summary>
        /// 处理图像，
        /// </summary>
        /// <param name="clone">待处理的图像的副本</param>
        /// <returns>处理完的图像</returns>
        private Mat GraphicProcess(ref Mat clone)
        {
            int Threadnum = int.Parse(ConfigurationManager.AppSettings["GraphicProcessThreadNum"]);

            IGraphicProcess MultiThread = new ProcessMultiThread(Threadnum);
            
            if (ProcessParameters.WhetherBrightness)
            {
                //MultiThread process
                MultiThread.AdjustBrightness(clone, ProcessParameters.BrightnessValue);

                //SingleThread process
                //IGraphicProcess single1 = new ProcessSingleThread();
                //single1.AdjustBrightness(clone, 50);

            }
            if (ProcessParameters.WhetherContrast)
            {
                //MultiThread process
                MultiThread.AdjustContrast(clone, ProcessParameters.ContrastValue);

                //SingleThread process
                //IGraphicProcess single2 = new ProcessSingleThread();
                //single2.AdjustContrast(clone, 50);
            }
            if (ProcessParameters.WhetherDrawString)
            {
                //IGraphicProcess addstring = new ProcessSingleThread();
                MultiThread.DrawString(clone, ProcessParameters.StringToDraw);
            }
            if (ProcessParameters.WhetherResize)
            {
                Mat resizemat = new Mat();
                resizemat = MultiThread.ResizeByRate(clone, ProcessParameters.WidthPercent, ProcessParameters.HeightPercent);
                MultiThread.AddHist(resizemat);
                return resizemat;
            }
            MultiThread.AddHist(clone);
            return clone;
        }

        /// <summary>
        /// Mat格式转为字节数组
        /// </summary>
        /// <param name="processedMat"></param>
        /// <returns></returns>
        private byte[] TurnMatToArray(Mat processedMat)
        {
            string ext = ".jpg";
            byte[] Matbuffer = new byte[processedMat.Width * processedMat.Height * 3];
            Cv2.ImEncode(ext, processedMat, out Matbuffer);
            byte[] MatHeader = compositeWebSocketHeader(Matbuffer);
            byte[] WebSocketFrame = new byte[Matbuffer.Length + MatHeader.Length];
            Array.Copy(MatHeader, 0, WebSocketFrame, 0, MatHeader.Length);
            Array.Copy(Matbuffer, 0, WebSocketFrame, MatHeader.Length, Matbuffer.Length);
            return WebSocketFrame;
        }

        #region StaticMethod

        internal static void Pause()
        {
            _IfSend = false;
        }

        internal static void Continue()
        {
            _IfSend = true;
        }

        #endregion


        #region Delegate Method

        /// <summary>
        /// delegate,在从服务器收到graphic时响应
        /// </summary>
        /// <param name="sender">事件发起者</param>
        /// <param name="mjpg">Mjpg类对象(保存了从TCP服务器收到的突破)</param>
        private void WhenReceiveImage(object sender, ImageInfoReceived mjpg)
        {
            if (ProcessParameters.WhetherNeedProcess())
            {
                ProcessSingleThread singlet = new ProcessSingleThread();
                Mat toProcess = Cv2.ImDecode(mjpg.data, ImreadModes.Color);
                Mat clone = singlet.ResizeByRate(toProcess, 50, 50);
                if (_websocketList.Count != 0 && _IfSend)
                {
                    try
                    {
                        Mat result = singlet.ResizeByRate(GraphicProcess(ref clone), 200, 200);
                        SendToAll(TurnMatToArray(result), _websocketList);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Processed JpegFromTCP error:{0}", ex.Message);
                    }
                }
            }
            else
            {
                byte[] websockethead = compositeWebSocketHeader(mjpg.data);
                byte[] content = new byte[websockethead.Length + mjpg.data.Length];
                Array.Copy(websockethead, 0, content, 0, websockethead.Length);
                Array.Copy(mjpg.data, 0, content, websockethead.Length, mjpg.data.Length);
                if (_websocketList.Count != 0 && _IfSend)
                {
                    try
                    {
                        SendToAll(content, _websocketList);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("No processed JpegFromTCP Sending error:{0}", ex.Message);
                    }
                }
            }
        }

        #endregion

    }
}
