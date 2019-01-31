using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventRequestingArgs : EventRequestArgs
    {
        public EventRequestingArgs(HttpRequest request, HttpResponse response) : base(request, response)
        {
            Cancel = true;
        }

        public bool Cancel { get; set; }
    }
}
