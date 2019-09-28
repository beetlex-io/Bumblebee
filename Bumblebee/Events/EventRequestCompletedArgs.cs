using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventRequestCompletedArgs : EventRequestArgs
    {
        public EventRequestCompletedArgs(Routes.UrlRoute urlRoute, HttpRequest request, HttpResponse response, Gateway gateway, int code,
            Servers.ServerAgent server, long useTime)
            : base(request, response, gateway)
        {
            Code = code;
            Server = server;
            Time = useTime;
            UrlRoute = urlRoute;
        }

        public long Time { get; set; }

        public Servers.ServerAgent Server { get; private set; }

        public int Code { get; private set; }

        public Routes.UrlRoute UrlRoute { get; internal set; }

        public long RequestID { get; internal set; }

        public string Error { get; internal set; }
    }
}
