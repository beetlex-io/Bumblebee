using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee
{
    public class BadGateway : InnerErrorResult
    {
        public BadGateway(string errormsg) : base("502", "Bad Gateway", new Exception(errormsg), false)
        {

        }

        public BadGateway(Exception error) : base("502", "Bad Gateway", error, false)
        {

        }

        public const int CLUSTER_SERVER_UNAVAILABLE = 10000;

        public const int URL_NODE_SERVER_UNAVAILABLE = 12000;

        public const int URL_FILTER_ERROR = 13000;

        public const int SERVER_NET_ERROR = 21001;

        public const int SERVER_AGENT_PROCESS_ERROR = 22001;

        public const int SERVER_MAX_OF_CONNECTIONS = 23001;


    }
}
