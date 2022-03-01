using System.Net.Sockets;


namespace MjpgServerDotnet6
{
    internal interface IRequestResponsor
    {
        public void Respond(string message,Socket FromClient);
    }
}
