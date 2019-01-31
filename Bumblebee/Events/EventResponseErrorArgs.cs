using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventResponseErrorArgs : EventRequestArgs
    {
        public EventResponseErrorArgs(HttpRequest request, HttpResponse response, IResult result, int errorCode)
            : base(request, response)
        {
            Result = result;
            ErrorCode = errorCode;
        }
        public IResult Result { get; set; }

        public int ErrorCode { get; private set; }
    }
}
