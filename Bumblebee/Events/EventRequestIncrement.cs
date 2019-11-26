using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public struct EventRequestIncrementArgs
    {

        public EventRequestIncrementArgs(HttpRequest request, int code, long time, Servers.ServerAgent server)
        {
            Request = request;
            Code = code;
            Time = time;
            Server = server;
        }

        public HttpRequest Request { get; set; }

        public int Code { get; set; }

        public long Time { get; set; }

        public Servers.ServerAgent Server { get; set; }
    }
}
