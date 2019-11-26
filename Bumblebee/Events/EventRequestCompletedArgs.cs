using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public struct EventRequestCompletedArgs
    {
        public EventRequestCompletedArgs(Routes.UrlRoute urlRoute, HttpRequest request, HttpResponse response, Gateway gateway, int code,
            Servers.ServerAgent server, long useTime, long requestid, string error)

        {
            Error = error;
            RequestID = requestid;
            Gateway = gateway;
            UrlRewrite = request.IsRewrite;
            SourceBaseUrl = request.GetSourceBaseUrl();
            SourceUrl = request.GetSourceUrl();
            SourcePath = request.GetSourcePath();
            Url = request.Url;
            BaseUrl = request.BaseUrl;
            Path = request.Path;
            Code = code;
            Server = server;
            Time = useTime;
            UrlRoute = urlRoute;
            Cookies = request.Cookies.Copy();
            Headers = request.Header.Copy();
            RemoteIPAddress = request.RemoteIPAddress;
            Data = request.Data.Copy();
            Method = request.Method;
            Host = request.GetHostBase();

        }

        public string Host { get; set; }

        public string Path { get; set; }

        public string Url { get; set; }

        public string BaseUrl { get; set; }

        public Gateway Gateway { get; set; }

        public string RemoteIPAddress { get; set; }

        public IDictionary<string, object> Data { get; set; }

        public IDictionary<string, string> Cookies { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public string Method { get; set; }

        public string SourcePath { get; set; }

        public string SourceUrl { get; set; }

        public string SourceBaseUrl { get; set; }

        public bool UrlRewrite { get; set; }

        public long Time { get; set; }

        public Servers.ServerAgent Server { get; set; }

        public int Code { get; private set; }

        public Routes.UrlRoute UrlRoute { get; set; }

        public long RequestID { get; set; }

        public string Error { get; set; }
    }
}
