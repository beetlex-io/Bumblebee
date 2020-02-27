using BeetleX.FastHttpApi;
using Bumblebee.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventServerRequestingArgs : EventRequestArgs
    {
        public EventServerRequestingArgs(RequestAgent requestAgent, HttpRequest request, HttpResponse response, Gateway gateway)
        : base(request, response, gateway)
        {
            RequestAgent = RequestAgent;
        }
        public RequestAgent RequestAgent { get; internal set; }

        public bool Cancel { get; set; } = false;
    }
}
