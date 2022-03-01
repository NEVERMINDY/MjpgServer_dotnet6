using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace MjpgServerDotnet6
{
    //singleton
    internal class GetResponsor:IRequestResponsor
    {

        #region Properties
        
        public static string ResponseHead = "HTTP/1.1 200 OK" + "\r\n" +
            "Content-Type: text/html;charset=utf-8" + "\r\n" +
            "Connection: keep-alive" + "\r\n" +
            "Server: Alan-chen" + "\r\n" +
            "X-Powered-By: Hexo" + "\r\n\r\n";

        #endregion

        #region Implement Method
        
        /// <summary>
        /// 回复GET请求,完成后关闭Socket
        /// </summary>
        /// <param name="message">报文信息</param>
        /// <param name="FromClient"> 用于与浏览器通信的Socket</param>
        public void Respond(string message, Socket FromClient)
        {
            string FirstLine = message.Split("\r\n", StringSplitOptions.None)[0];
            string GetURL = FirstLine.Replace("GET", "").Replace("HTTP/1.1", "").Trim();
            string CurrentPath = Directory.GetCurrentDirectory();
            string path = (CurrentPath + GetURL).Replace("/", @"\");

            if (File.Exists(path))
            {
                lock (path)
                {
                    FileStream fs = new FileStream(path, FileMode.Open);
                    byte[] SendBuffer = new byte[ResponseHead.Length + fs.Length];
                    Array.Copy(Encoding.Default.GetBytes(ResponseHead), 0, SendBuffer, 0, ResponseHead.Length);
                    fs.Read(SendBuffer, ResponseHead.Length, SendBuffer.Length - ResponseHead.Length);
                    fs.Close();
                    FromClient.Send(SendBuffer);
                }
                FromClient.Close();
            }else if (Directory.Exists(path))
            {
                if (GetURL == @"/")
                {
                    lock(path+ "/html/index.html")
                    {
                        FileStream fs = new FileStream(path+"/html/index.html", FileMode.Open);
                        byte[] SendBuffer = new byte[ResponseHead.Length + fs.Length];
                        Array.Copy(Encoding.Default.GetBytes(ResponseHead), 0, SendBuffer, 0, ResponseHead.Length);
                        fs.Read(SendBuffer, ResponseHead.Length, SendBuffer.Length - ResponseHead.Length);
                        FromClient.Send(SendBuffer);
                    }
                    FromClient.Close();
                }
                else
                {
                    FromClient.Close();
                    throw new Exception("GET INVALID Directory!");
                }
            }
            else
            {
                FromClient.Close();
                throw new Exception("Can't tell the Requested File!");
            }
            
        } 
        
        #endregion
    }
}
