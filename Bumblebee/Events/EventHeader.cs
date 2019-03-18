using BeetleX.Buffers;
using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventHeaderWritingArgs : EventRequestArgs
    {
        public EventHeaderWritingArgs(HttpRequest request, HttpResponse response, Gateway gateway, Header header)
            : base(request, response, gateway)
        {
            Header = header;
        }

        public Servers.ServerAgent Server { get; internal set; }

        public Header Header { get; internal set; }
    }
}
