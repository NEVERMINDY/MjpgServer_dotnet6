using System;
using System.Net;

namespace MjpgServerDotnet6
{
    interface ITCPReader
    {

        event EventHandler<ImageInfoReceived> OnReceiveImage;

        event EventHandler<string> OnReceiveMessage;

        bool Connect(IPEndPoint iPEndPoint);

        bool Disconnect();        
                       
        void GetImage();
       
    }
}
