using BeetleX.Buffers;
using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventHeaderWriter : EventRequestArgs
    {
        public EventHeaderWriter(HttpRequest request, HttpResponse response, Gateway gateway, PipeStream stream)
            : base(request, response, gateway)
        {
            mStream = stream;
        }

        public Servers.ServerAgent Server { get; internal set; }

        private PipeStream mStream;

        public void Write(string name, string value)
        {
            mStream.Write(name);
            mStream.Write(": ");
            mStream.Write(value);
            mStream.Write(HeaderTypeFactory.LINE_BYTES, 0, 2);
        }

        public void Write(byte[] data)
        {
            mStream.Write(data, 0, data.Length);
        }
    }

    public class EventHeaderWriting : EventHeaderWriter
    {
        public EventHeaderWriting(HttpRequest request, HttpResponse response, Gateway gateway, PipeStream stream)
            : base(request, response, gateway, stream)
        {
            Cancel = false;
        }
        public string Name { get; internal set; }

        public string Value { get; internal set; }

        public bool Cancel { get; set; }
    }
}
