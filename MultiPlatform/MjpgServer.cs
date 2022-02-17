using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Configuration;
using System.Net.WebSockets;
using System.Net.Http;
using OpenCvSharp;

namespace MultiPlatform
{
    internal class MjpgServer
    {
        #region properties

        private static List<Socket>? _websocketList = new List<Socket>();

        private Mjpg? _thismjpg;

        static bool _isRunning = false;

        protected static string ResponseHead = "HTTP/1.1 200 OK" + "\r\n" +
            "Content-Type: text/html;charset=utf-8" + "\r\n" +
            "Connection: keep-alive" + "\r\n" +
            "Server: Alan-chen" + "\r\n" +
            "X-Powered-By: Hexo" + "\r\n\r\n";

        protected static string webSocketShakingResponse =
            @"HTTP/1.1 101 Switching Protocals" + "\r\n" +
            @"Connection: Upgrade" + "\r\n" +
            @"Upgrade: websocket" + "\r\n" +
            @"Sec-WebSocket-Accept: AcceptKeywords" + "\r\n" +
            @"WebSocket-Origin: null" + "\r\n" +
            @"Sec-WebSocket-Location: ws://10.132.60.231:2022/" + "\r\n\r\n";

        protected static string RFC6456 = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static string SingleImageResponseHead = "HTTP/1.1 200 OK" +
            "\r\n" +
            "Content-Type:image/jpeg;charset=utf-8" +
            "\r\n" +
            "Content-length:" + "LengthValue" +
            "\r\n";

        private static string MjpgResponseHead = "HTTP/1.1 200 OK" + "\r\n" +
            "Content-Type: multipart/x-mixed-replace; boundary=Alanchen" + "\r\n";

        private static string MjpgImageHead = "Content-Type: image/jpeg" + "\r\n" +
            "Content-length: " + "ContentLengthValue" + "\r\n\r\n";

        private static string MjpgImageTail = "\n" + "--Alanchen" + "\n\r\n";

        //这个标志位供browser控制服务器是否发送
        private bool IfSend = false;

        //这个标志位供内部函数控制是否需要调用sendtoall
        private bool _IfAlreadySending;

        #endregion

        #region Methods

        //return :: IPV4 address
        protected IPAddress GetServerIp()
        {
            var host = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var ip in host)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return IPAddress.None;
        }

        //Initialize Server
        public void StartServer()
        {
            if (_isRunning)
            {
                Console.WriteLine("Server is already running!Don't Open again");
                return;
            }

            //1.create a ServerSocket object
            //2.bind ServerSocket to IP:2022
            //3.ServerSocket begin to listen
            
            Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(new IPEndPoint(GetServerIp(), int.Parse(ConfigurationManager.AppSettings["Port"])));
            ServerSocket.Listen(5);
            _isRunning = true;
            Console.WriteLine("Server is Running");
            ThreadPool.SetMaxThreads(5, 5);
            while (_isRunning)
            {
                Socket FromClient = ServerSocket.Accept();
                ThreadPool.QueueUserWorkItem(TReceive, FromClient);
            }

        }

        //Receive thread
        protected void TReceive(object fromclient)
        {
            Socket FromClient = (Socket)fromclient;
            byte[] ReceiveBuffer = new byte[1024 * 1024];
            int ReceiveLength = -1;
            try
            {
                ReceiveLength = FromClient.Receive(ReceiveBuffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (ReceiveLength == 0)
            {
                Console.WriteLine("Connection Break");
                FromClient.Close();
            }
            else//ReceiveLength!=0
            {
                //0.HTTP GET
                //1.HTTP POST
                //2.TCP Message
                //Tell Kind by the first row of message
                string PostMsgString = Encoding.Default.GetString(ReceiveBuffer, 0, ReceiveLength);
                switch (DistinguishMsg(PostMsgString))
                {
                    case 0:
                        Console.WriteLine("\nA GET Request Accepted,Jumping to Process Function:");
                        ProcessGetRequest(PostMsgString, FromClient);
                        break;
                    case 1:
                        Console.WriteLine("\nA POST Request Accepted,Jumping to Process Function:");
                        ProcessPostRequest(PostMsgString, FromClient);
                        break;
                    case 2:
                        Console.WriteLine("\nFirst part of a TCP Message Accepted,Jumping to Process Function:"); break;
                    case 3:
                        Console.WriteLine("\nA WebSocket Shaking Request,Jumping to Process Function:");
                        answerShaking(PostMsgString, FromClient);
                        break;
                    case -1:
                        Console.WriteLine("\nOther parts of a TCP Message Accepted,Jumping to Process Function:"); break;
                    default:
                        Console.WriteLine("\nUnreconized Request");
                        Console.WriteLine("\n" + PostMsgString);
                        break;
                }

            }

        }

        //Analyze Files requested and Send back
        //1,extract first line of Get Request
        //2.extract url requested
        //3.Determine whether the request is a file or a directory By URL
        //4.file => send back
        //5.directory => "/" ? send index.html  , else : error
        /// <summary>
        /// 处理GET请求(找得到get文件就发回，找不到则输出错误信息)
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="FromClient"></param>
        protected void ProcessGetRequest(string Message, Socket FromClient)
        {
            string FirstLine = Message.Split(new string[] { "\r\n" }, StringSplitOptions.None)[0];
            Console.WriteLine("REQUEST:" + FirstLine);
            //Console.WriteLine("Message:" + "\r\n" + Message);
            string GetURL = FirstLine.Replace("GET", "").Replace("HTTP/1.1", "").Trim();

            string CurrentPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName;
            string path = CurrentPath + GetURL;
            path = path.Replace("/", @"\");
            //Console.WriteLine("Path:"+path);
            if (File.Exists(path))
            {
                //1.Get File name
                //2.Open as stream
                //3.Write stream into buffer
                //4.Send buffer
                //5.Close Socket
                //path = path.Replace("")
                bool ifObtained = true;
                while (ifObtained)
                {
                    try
                    {
                        FileStream fileStream = new FileStream(path, FileMode.Open);
                        byte[] SendBuffer = new byte[ResponseHead.Length + fileStream.Length];
                        Array.Copy(Encoding.Default.GetBytes(ResponseHead), 0, SendBuffer, 0, ResponseHead.Length);
                        fileStream.Read(SendBuffer, ResponseHead.Length, SendBuffer.Length - ResponseHead.Length);
                        fileStream.Close();
                        FromClient.Send(SendBuffer);
                        ifObtained = false;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                FromClient.Close();
            }
            else if (Directory.Exists(path))
            {
                //send index.html
                if (GetURL == "/")
                {
                    bool ifObtained = true;
                    while (ifObtained)
                    {
                        try
                        {
                            FileStream streamhttpcontent = new FileStream(CurrentPath + "/html/index.html", FileMode.Open);
                            byte[] SendBuffer = new byte[ResponseHead.Length + streamhttpcontent.Length];
                            Array.Copy(Encoding.Default.GetBytes(ResponseHead), 0, SendBuffer, 0, ResponseHead.Length);
                            streamhttpcontent.Read(SendBuffer, ResponseHead.Length, SendBuffer.Length - ResponseHead.Length);
                            FromClient.Send(SendBuffer);
                            FromClient.Close();
                            ifObtained = false;
                        }
                        catch (Exception e)
                        {

                        }
                    }

                }
                else//send 404 error
                {
                    Console.WriteLine("Cannot find such a file/directory");
                }
            }
            else//send 404 error
            {
                Console.WriteLine("Cannot find such a file/directory");
            }
        }

        /*
         **** WebSocket shaking Request is like: ****
        GET / HTTP/1.1
        Host: 10.132.60.231:2022
        Connection: Upgrade
        Pragma: no-cache
        Cache-Control: no-cache
        User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36
        Upgrade: websocket
        Origin: http://10.132.60.231:2022
        Sec-WebSocket-Version: 13
        Accept-Encoding: gzip, deflate
        Accept-Language: zh-CN,zh;q=0.9
        Sec-WebSocket-Key: VQZETlQtnquO3BJM08nOOQ==
        Sec-WebSocket-Extensions: permessage-deflate; client_max_window_bits
        */

        /*
         **** response should be like: ****
        HTTP/1.1 101 Switching Protocals
        Connection: Upgrade
        Upgrade: websocket
        Sec-WebSocket-Accept: fFBooB7FAkLlXgRSz0BT3v4hq5s
        Sec-WebSocket-Origin: null
        Sec-WebSocket-Location: ws://10.132.60.231:2022/
        */

        //1.Send shaking messages back according to Sec-WebSocket-Key
        //2.read images from TCP/Memory
        //3.Send images back
        /// <summary>
        /// 答复Websocket升级请求
        /// </summary>
        /// <param name="Message">升级请求的报文内容</param>
        /// <param name="FromClient">收到请求的Socket</param>
        protected void answerShaking(string Message, Socket FromClient)
        {
            IfSend = true;
            //Console.WriteLine("Message:\r\n" + Message);
            string[] msg = Message.Split("\r\n");
            string webSocketKey = msg[msg.Length - 4].Split(":")[1].Trim() + MjpgServer.RFC6456;
            string webSocketKey_SHA1 = CalculateSHA1(webSocketKey);
            string answer = webSocketShakingResponse.Replace("AcceptKeywords", webSocketKey_SHA1);
            FromClient.Send(Encoding.Default.GetBytes(answer));
            Console.WriteLine("Shaking Success!\n");


            //sendWebSocketFrame(FromClient);
            _websocketList.Add(FromClient);
            if (_IfAlreadySending == false)
            {
                string? ReadJpegFrom = ConfigurationManager.AppSettings["ReadJpegFrom"];
                if (ReadJpegFrom!=null&&ReadJpegFrom=="Memory")
                {
                    _IfAlreadySending = true;
                    JpegFromMemory temptjpeg = new JpegFromMemory();
                    while (temptjpeg.WhetherImageLeft())
                    {
                        SendToAll(compositeWebSocketFrame(), _websocketList);
                    }
                    _IfAlreadySending = false;
                    JpegFromMemory.index = 0;
                }
                else if(ReadJpegFrom!=null&&ReadJpegFrom=="TCP")
                {
                    IClient socketAsClient = new Client();
                    socketAsClient.Connect(IPEndPoint.Parse(ConfigurationManager.AppSettings["RemoteServerIPEndPoint"]));
                    socketAsClient.ImageEvent += new EventHandler<Mjpg>(WhenReceiveImage);
                    socketAsClient.GetImage();
                }
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

        //return 0:Http Get
        //return 1:Http Post
        //return 2:First Part of TCP msg
        //return 3:Shaking Hands of WebSocket
        //return 4:Waving Hands of WebSocket
        //return -1 Other Parts of TCP msg
        /// <summary>
        /// 识别报文的请求方式(POST/GET/WebSocket升级)
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        protected int DistinguishMsg(string Message)
        {
            Console.WriteLine("\r\n\r\n\r\n" + Message+"\r\n\r\n\r\n");
            Regex RegexPost = new Regex(@"POST");
            Regex RegexGet = new Regex(@"GET");
            Regex RegexWebSocket = new Regex(@"Connection: Upgrade");
            Regex RegexTCPFirstPart = new Regex(@"TCPMESSAGE");

            if (RegexGet.IsMatch(Message))
            {
                if (RegexWebSocket.IsMatch(Message))
                {
                    return 3;
                }
                //Console.WriteLine(Message);
                //HTTP GET REQUEST
                return 0;
            }
            else if (RegexPost.IsMatch(Message))
            {
                //Console.WriteLine(Message);
                //HTTP POST REQUEST
                return 1;
            }
            else if (RegexTCPFirstPart.IsMatch(Message))
            {
                //TCP Message of first part
                return 2;
            }
            else
            {
                //TCP Messaga other parts
                return -1;
            }
        }

        //PAUSE         :at pause 
        //CONTINUE      :continue to send images
        //STOP          :stop sending images
        //CHANGEPARAMETERS
        //              :change parameters of Processing Images
        //RESET         :reset the image index
        //INITIALIZE    :initialize processing parameters and index of images
        /// <summary>
        /// 使用RecognizePostRequest分析client请求，并转到相应的处理程序
        /// </summary>
        /// <param name="Message">报文</param>
        /// <param name="FromClient">收到报文的Socket</param>
        private void ProcessPostRequest(string Message, Socket FromClient)
        {
            switch (RecognizePostRequest(Message))
            {
                //START本是为了发送MJPG流，但是目前没有搞定，前端展示用的是websocket，
                //websocket发送图片的具体实现在answerShaking函数，因此目前POST的功能不包含START   
                case "STOP":
                    StreamStop(FromClient);
                    break;
                case "PAUSE":
                    //IfSend = false;
                    StreamPause();
                    break;
                case "CONTINUE":
                    IfSend = true;
                    break;
                case "CHANGEPARAMETERS":
                    ChangeParameters(Message.Split("&"));
                    break;
                case "RESET":
                    ResetIndex();
                    break;
            }
        }

        //1.extract postmessage from the last line of message
        //2.Option is the first parameter
        //3.return option as string
        /// <summary>
        /// 根据Post报文信息，提取client的请求
        /// </summary>
        /// <param name="Message">Post报文</param>
        /// <returns>Post请求</returns>
        protected string RecognizePostRequest(string Message)
        {
            //Console.WriteLine(Message);
            string[] PostMessageArray = Message.Split("\r\n", StringSplitOptions.None);
            string PostMessage = PostMessageArray[PostMessageArray.Length - 1];
            string OptionStr = PostMessage.Split("&", StringSplitOptions.None)[0];
            return OptionStr;
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
            IfSend = false;
        }

        /// <summary>
        /// 重置图片索引(JpegFromMemory.index)
        /// </summary>
        private void ResetIndex()
        {
            JpegFromMemory.index = 0;
        }

        /// <summary>
        /// 根据图片索引(JpegFromMemory.index),从Memory读取图片,进行相应处理，得到Websocket的帧内容
        /// </summary>
        /// <returns>Websocket帧内容的byte[]</returns>
        private byte[] compositeWebSocketFrame()
        {
            byte[] emptybuffer = null;
            if (IfSend)
            {
                JpegFromMemory jpegFrame = new JpegFromMemory();
                if (ProcessParameters.WhetherResize || ProcessParameters.WhetherBrightness || ProcessParameters.WhetherContrast || ProcessParameters.WhetherDrawString)
                {
                    ProcessSingleThread pst = new ProcessSingleThread();
                    if (jpegFrame.WhetherImageLeft())
                    {
                        if (IfSend == true)
                        {
                            Mat MatToProcess = jpegFrame.ReadToMat();
                            JpegFromMemory.index++;
                            //Mat clone = MatToProcess.Clone();
                            Mat clone = pst.ResizeByRate(MatToProcess, 50, 50);
                            //Mat result = pst.ResizeByRate(GraphicProcess(ref clone), 200, 200);
                            Mat result = GraphicProcess(ref clone);
                            return TurnMatToArray(pst.ResizeByRate(result, 200, 200));
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
                        //Console.WriteLine("index:{0},count:{1}", i++, JpegFromMemory.ImageCount);
                        if (IfSend == true)
                        {
                            byte[] frame = jpegFrame.ReadToBuffer();
                            JpegFromMemory.index++;
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

        #endregion

        /// <summary>
        /// 把jpg图像传输给所有保存在Socket集合中的Socket对象
        /// </summary>
        /// <param name="buffer">要发送的图片的byte[]</param>
        /// <param name="socketlist"></param>
        private void SendToAll(byte[] buffer, List<Socket> socketlist)
        {
            if (socketlist.Count != 0)
            {
                for(int index=0;index< socketlist.Count; index++)
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
        /// 处理图像，
        /// </summary>
        /// <param name="clone">待处理的图像的副本</param>
        /// <returns>处理完的图像</returns>
        private Mat GraphicProcess(ref Mat clone)
        {

            if (ProcessParameters.WhetherBrightness)
            {
                ProcessMultiThread multiThread = new ProcessMultiThread(int.Parse(ConfigurationManager.AppSettings["GraphicProcessThreadNum"]));
                multiThread.AdjustBrightnessMul(clone, ProcessParameters.BrightnessValue);
            }
            if (ProcessParameters.WhetherContrast)
            {
                ProcessMultiThread multiThread = new ProcessMultiThread(int.Parse(ConfigurationManager.AppSettings["GraphicProcessThreadNum"]));
                multiThread.AdjustContrastMul(clone, ProcessParameters.ContrastValue);
            }
            if (ProcessParameters.WhetherResize)
            {
                IGraphicProcess resizeSingleThread = new ProcessSingleThread();
                resizeSingleThread.ResizeByRate(clone, ProcessParameters.WidthPercent, ProcessParameters.HeightPercent);
            }
            if (ProcessParameters.WhetherDrawString)
            {
                IGraphicProcess addstring = new ProcessSingleThread();
                addstring.DrawString(clone, ProcessParameters.StringToDraw);
            }
            IGraphicProcess singleThread = new ProcessSingleThread();
            singleThread.AddHist(clone);
            return clone;
        }

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

        #region Delegate Method

        /// <summary>
        /// delegate,在从服务器收到graphic时响应
        /// </summary>
        /// <param name="sender">事件发起者</param>
        /// <param name="mjpg">Mjpg类对象(保存了从TCP服务器收到的突破)</param>
        private void WhenReceiveImage(object sender,Mjpg mjpg)
        {
            if (ProcessParameters.WhetherResize || ProcessParameters.WhetherBrightness || ProcessParameters.WhetherContrast)
            {
                ProcessSingleThread singlet = new ProcessSingleThread();
                Mat toProcess = Cv2.ImDecode(mjpg.data, ImreadModes.Color);
                Mat clone = singlet.ResizeByRate(toProcess, 50, 50);
                if (_websocketList.Count != 0 && IfSend)
                {
                    try
                    {
                        Mat result = singlet.ResizeByRate(GraphicProcess(ref clone), 200, 200);
                        SendToAll(TurnMatToArray(result), _websocketList);
                    }
                    catch(Exception ex)
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
                if (_websocketList.Count != 0 && IfSend)
                {
                    try
                    {
                        SendToAll(content, _websocketList);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("No processed JpegFromTCP Sending error:{0}", ex.Message);
                    }
                }
            }
        }
        #endregion
    }
}
