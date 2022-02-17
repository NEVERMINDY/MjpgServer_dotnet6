using System;
using System.Net;

namespace MultiPlatform
{
    interface IClient
    {

        event EventHandler<Mjpg> ImageEvent;

        event EventHandler<string> MessageEvent;

        bool Connect(IPEndPoint iPEndPoint);

        bool Disconnect();        
                       
        void GetImage();
       
    }
}
