using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventRequestCompletedArgs : EventRequestArgs
    {
        public EventRequestCompletedArgs(HttpRequest request, HttpResponse response, int code)
            : base(request, response)
        {
            Code = code;
        }
        public int Code { get; private set; }
    }
}
