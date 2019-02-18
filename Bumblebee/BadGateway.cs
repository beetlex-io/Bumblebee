using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee
{
    public class BadGateway : InnerErrorResult
    {
        public BadGateway(string errormsg) : base("502", "Bad Gateway", new Exception(errormsg), false)
        {

        }

        public BadGateway(Exception error) : base("502", "Bad Gateway", error, false)
        {

        }

      


    }
}
