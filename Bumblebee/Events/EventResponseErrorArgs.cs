using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventResponseErrorArgs : EventRequestArgs
    {
        public EventResponseErrorArgs(HttpRequest request, HttpResponse response, Gateway gateway, string message, int errorCode)
            : base(request, response, gateway)
        {
            Result = new BadGateway(message, errorCode);
            ErrorCode = errorCode;
            Message = message;
        }

        public string Message { get; private set; }

        public IResult Result { get; set; }

        public int ErrorCode { get; private set; }
    }
}
