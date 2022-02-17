using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace MultiPlatform
{
    
    public class Client : IClient
    {
        public event EventHandler<Mjpg> ImageEvent; // Receive image

        public event EventHandler<string> MessageEvent; // Receive message

        private static Socket clientSocket;

        private static Thread ReceiveThread;

        public bool Connect(IPEndPoint iPEndPoint)
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(iPEndPoint);
                ReceiveThread = new Thread(() =>
                {
                    while (true)
                    {
                        Mjpg image = Receive(clientSocket);                        
                    }
                });
                ReceiveThread.Start();               
                
                return true;
            }
            catch
            {
                return false;
            }            
        }        

        public bool Disconnect()
        {
            if (clientSocket != null)
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
                ReceiveThread.Suspend();
                return true;
            }
            return false;
        }

        private void Method(object sender, EventArgs e) // Receive image event process program
        {
            Mjpg image = e as Mjpg;
            string str = sender + "   " + Convert.ToString(image.fileName);
            MessageEvent?.Invoke(this, str);
        }

        public void GetImage() // Client ask server for images
        {
            clientSocket.Send(Encoding.UTF8.GetBytes("GetImage"));
        } 

        private Mjpg Receive(Socket clientSocket)
        {
            while (true)
            {
                try
                {
                    Mjpg image = new Mjpg();
                    int headLength = 521;
                    byte[] buffer = new byte[1024];
                    int length = clientSocket.Receive(buffer);
                    // Connection dropped
                    if (length <= 0)
                    {
                        string dropLog = DateTime.Now.ToString("yyyy - mm - dd HH:mm:ss:fff") + " Connection dropped.";
                        MessageEvent?.Invoke(this, dropLog);
                        return null;
                    }
                    // Receive a buffer of data
                    while (length < buffer.Length)
                    {
                        int rest = buffer.Length - length;
                        int len = clientSocket.Receive(buffer, length, rest, SocketFlags.None);
                        length += len;
                    }
                    if (Encoding.UTF8.GetString(buffer, 0, 1) == "A") // Receive file
                    {
                        if (Encoding.UTF8.GetString(buffer).TrimEnd('\0') == "A" + "End")
                        {
                            string endLog = DateTime.Now.ToString("yyyy - mm - dd HH:mm:ss:fff") + " Transmission finished.";
                            MessageEvent?.Invoke(this, endLog);
                            return null; // Transmission finished
                        }
                        else
                        {
                            int offset = 0;
                            int count = 1;
                            long fileSize = 0;
                            for (int i = 1; i <= 8; i++)
                            {
                                fileSize += buffer[i] << (8 * (i - 1));
                            }

                            image.fileSize = fileSize;
                            image.fileName = Encoding.UTF8.GetString(buffer, 9, 512).TrimEnd('\0');
                            image.data = new byte[image.fileSize];

                            Array.Copy(buffer, headLength, image.data, offset, buffer.Length - headLength);
                            fileSize -= buffer.Length - headLength;
                            offset += buffer.Length - headLength;
                            int pieces = (int)Math.Ceiling((decimal)(image.fileSize - (buffer.Length - headLength)) / (buffer.Length - 1)) + 1;
                            while (count < pieces - 1)
                            {
                                length = clientSocket.Receive(buffer);
                                while (length < buffer.Length)
                                {
                                    int rest = buffer.Length - length;
                                    int len = clientSocket.Receive(buffer, length, rest, SocketFlags.None);
                                    length += len;
                                }
                                count++;
                                Array.Copy(buffer, 1, image.data, offset, buffer.Length - 1);
                                offset += buffer.Length - 1;
                                fileSize -= buffer.Length - 1;
                            }
                            length = clientSocket.Receive(buffer);
                            while (length < buffer.Length)
                            {
                                int rest = buffer.Length - length;
                                int len = clientSocket.Receive(buffer, length, rest, SocketFlags.None);
                                length += len;
                            }
                            count++;
                            Array.Copy(buffer, 1, image.data, offset, fileSize);

                            // Save image 
                            FileStream fileStream = Save(image);

                            // Obtain with and height
                            Image image1 = Image.FromStream(fileStream);
                            image.width = image1.Width;
                            image.height = image1.Height;

                            ImageEvent?.Invoke(this, image);

                            fileStream.Close();

                            string receiveImageLog = DateTime.Now.ToString("yyyy - mm - dd HH:mm:ss:fff") + " Receive file:" + image.fileName;
                            
                            MessageEvent?.Invoke(this, receiveImageLog);

                            return image;
                        }
                    }
                    else if (Encoding.UTF8.GetString(buffer, 0, 1) == "B") // Receive message
                    {
                        string message = Encoding.UTF8.GetString(buffer, 1, length - 1).TrimEnd('\0');
                        string messageLog = DateTime.Now.ToString("yyyy - mm - dd HH:mm:ss:fff") + " Receive " + clientSocket.RemoteEndPoint + " message：" + message;
                        
                        MessageEvent?.Invoke(this, messageLog);
                        
                        return null;
                    }
            }
                catch (Exception exp)
            {
                MessageEvent?.Invoke(this, exp.ToString());
                return null;
            }
        }
        }

        private FileStream Save(Mjpg image)
        {
            string path = Directory.GetCurrentDirectory() + "//" + image.fileName;
            long fileSize = image.fileSize;
            int offset = 0;
            int segment = 1024 * 1024;
            FileStream fileStream;
            fileStream = new FileStream(path, FileMode.Create);
            while (fileSize > segment)
            {
                fileStream.Write(image.data, offset, segment);
                offset += segment;
                fileSize -= segment;
            }
            fileStream.Write(image.data, offset, (int)fileSize);
            string str = DateTime.Now.ToString("yyyy - mm - dd HH: mm: ss: fff") + " Successfully save file:" + image.fileName;
            MessageEvent?.Invoke(this, str);
            return fileStream;
        }
    }
}
