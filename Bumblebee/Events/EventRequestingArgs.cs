using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventRequestingArgs : EventRequestArgs
    {
        public EventRequestingArgs(HttpRequest request, HttpResponse response, Gateway gateway) : base(request, response, gateway)
        {
            Cancel = false;
            ResultType = ResultType.Completed;
        }

        public bool Cancel { get; set; }

        public ResultType ResultType { get; set; }
    }

    public enum ResultType
    {
        None,
        Completed
    }

    public class EventAgentRequestingArgs : EventRequestingArgs
    {
        public EventAgentRequestingArgs(HttpRequest request, HttpResponse response, Gateway gateway, Servers.ServerAgent server, Routes.UrlRoute urlRoute) :
            base(request, response, gateway)
        {
            Server = server;
            UrlRoute = urlRoute;
        }

        public Routes.UrlRoute UrlRoute { get; private set; }

        public Servers.ServerAgent Server
        {
            get;
            private set;

        }

    }
}
