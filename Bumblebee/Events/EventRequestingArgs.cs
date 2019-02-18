using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventRequestingArgs : EventRequestArgs
    {
        public EventRequestingArgs(HttpRequest request, HttpResponse response,Gateway gateway) : base(request, response,gateway)
        {
            Cancel = true;
        }

        public bool Cancel { get; set; }
    }
}
