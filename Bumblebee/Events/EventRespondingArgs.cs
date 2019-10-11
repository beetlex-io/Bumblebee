using Bumblebee.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public struct EventRespondingArgs
    {

        public string ResponseStatus { get; set; }

        public BeetleX.FastHttpApi.Header Header { get; set; }

        public Gateway Gateway { get; set; }

        public BeetleX.FastHttpApi.HttpRequest Request { get; set; }

        public ArraySegment<byte> Data { get; set; }

        public bool Completed { get; set; }

        public bool FirstReceive { get; set; }

        public ServerAgent Server { get; set; }
    }
}
