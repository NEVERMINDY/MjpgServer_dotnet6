using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MjpgServerDotnet6
{
    internal class ResponsorFactory
    {
        public IRequestResponsor getResponsor(string message)
        {
            Regex RegGet = new Regex(@"GET");
            Regex RegPost = new Regex(@"POST");
            Regex RegWebSocket = new Regex(@"Connection: Upgrade");

            if (RegGet.IsMatch(message))
            {
                if (RegWebSocket.IsMatch(message))
                {
                    return new ShakingResponsor();
                }
                else
                {
                    return new GetResponsor();
                }
            }
            else if (RegPost.IsMatch(message))
            {
                return new PostResponsor();
            }
            else
            {
                throw new Exception(message);
            }
        }
    }
}
