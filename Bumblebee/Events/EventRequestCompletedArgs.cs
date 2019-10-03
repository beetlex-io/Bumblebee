using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventRequestCompletedArgs : EventArgs
    {
        public EventRequestCompletedArgs(Routes.UrlRoute urlRoute, HttpRequest request, HttpResponse response, Gateway gateway, int code,
            Servers.ServerAgent server, long useTime)

        {
            Gateway = gateway;
            BaseUrl = request.BaseUrl;
            Url = request.Url;
            Code = code;
            Server = server;
            Time = useTime;
            UrlRoute = urlRoute;
            Cookies = request.Cookies.Copy();
            Headers = request.Header.Copy();
            RemoteIPAddress = request.RemoteIPAddress;
            Data = request.Data.Copy();
            Path = request.Path;
            Method = request.Method;

        }

        public string Path { get; set; }

        public Gateway Gateway { get; private set; }

        public string RemoteIPAddress { get; private set; }

        public IDictionary<string, object> Data { get; private set; }

        public IDictionary<string, string> Cookies { get; private set; }

        public IDictionary<string, string> Headers { get; private set; }

        public string Method { get; set; }

        public string Url { get; set; }

        public string BaseUrl { get; set; }

        public long Time { get; set; }

        public Servers.ServerAgent Server { get; private set; }

        public int Code { get; private set; }

        public Routes.UrlRoute UrlRoute { get; internal set; }

        public long RequestID { get; internal set; }

        public string Error { get; internal set; }
    }
}
