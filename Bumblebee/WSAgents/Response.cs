using BeetleX.Buffers;
using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.WSAgents
{
    public class Response
    {

        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

        public int? Code { get; set; }

        public string Message { get; set; }

        public string HttpVersion { get; set; }

        public bool Read(PipeStream stream)
        {
            while(stream.TryReadLine(out string line))
            {
                if (string.IsNullOrEmpty(line))
                    return true;
                if(Code==null)
                {
                    var result = HttpParse.AnalyzeResponseLine(line.AsSpan());
                    Code = result.Item2;
                    HttpVersion = result.Item1;
                    Message = result.Item3;
                }
                else
                {
                    var header = HttpParse.AnalyzeHeader(line.AsSpan());
                    Headers[header.Item1] = header.Item2;
                }
            }
            return false;
        }
    }
}
