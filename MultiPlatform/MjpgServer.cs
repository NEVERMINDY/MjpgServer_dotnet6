using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Configuration;

namespace MjpgServerDotnet6
{
    internal class MjpgServer
    {
        #region properties

        public static List<Socket>? _websocketList = new List<Socket>();

        static bool _isRunning = false;

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
            ServerSocket.Listen(10);
            _isRunning = true;
            Console.WriteLine("Server is Running");
            ThreadPool.SetMaxThreads(10, 10);
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
                string PostMsgString = Encoding.Default.GetString(ReceiveBuffer, 0, ReceiveLength);

                ResponsorFactory resFactory = new ResponsorFactory();
                IRequestResponsor responsor = resFactory.getResponsor(PostMsgString);
                responsor.Respond(PostMsgString, FromClient);
            }
        }

        #endregion

    }
}
