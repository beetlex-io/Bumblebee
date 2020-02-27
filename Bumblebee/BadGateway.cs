using BeetleX.Buffers;
using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee
{
    public class BadGateway : InnerErrorResult
    {
        public IHeaderItem HTML_UTF8 = new HeaderItem("text/html; charset=utf-8\r\n");

        public BadGateway(string errormsg, int code = 502) : base(code.ToString(), "Bad Gateway", new Exception(errormsg), false)
        {

        }

        public override IHeaderItem ContentType => HTML_UTF8;

        public BadGateway(Exception error, int code = 502) : base(code.ToString(), "Bad Gateway", error, false)
        {

        }

        public override void Write(PipeStream stream, HttpResponse response)
        {
            stream.WriteLine("<html>");
            stream.WriteLine("<body>");
            stream.Write("<h1>");
            stream.WriteLine(Message);
            stream.Write("</h1>");
            if (!string.IsNullOrEmpty(Error))
            {
                stream.Write("<p>");
                stream.WriteLine(Error);
                stream.Write("</p>");
            }
            if (!string.IsNullOrEmpty(SourceCode))
            {
                stream.Write("<p>");
                stream.WriteLine(SourceCode);
                stream.Write("</p>");
            }

            stream.WriteLine("  <hr style=\"margin: 0px; \" /> <p> Beetlex http gateway copyright @  2019-2020 <a href=\"http://beetlex.io\" target=\"_blank\"> beetlex.io</a></p>");
            stream.WriteLine("<body>");
            stream.WriteLine("</html>");
        }

    }
}
