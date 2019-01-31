using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventRequestArgs
    {
        public EventRequestArgs(HttpRequest request, HttpResponse response)
        {
            Request = request;
            Response = response;
        }

        public HttpRequest Request { get; internal set; }

        public HttpResponse Response { get; internal set; }
    }
}
