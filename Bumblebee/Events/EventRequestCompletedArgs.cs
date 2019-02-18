using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventRequestCompletedArgs : EventRequestArgs
    {
        public EventRequestCompletedArgs(HttpRequest request, HttpResponse response, Gateway gateway, int code,
            Servers.ServerAgent server, long useTime)
            : base(request, response, gateway)
        {
            Code = code;
            Server = server;
            Time = useTime;
        }

        public long Time { get; set; }

        public Servers.ServerAgent Server { get; private set; }

        public int Code { get; private set; }
    }
}
