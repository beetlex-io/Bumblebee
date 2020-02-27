using BeetleX;
using BeetleX.FastHttpApi;
using Bumblebee.Routes;
using System;
using System.Collections.Generic;
using System.Text;
using UrlRoute = Bumblebee.Routes.UrlRoute;

namespace Bumblebee.WSAgents
{
    public class WSClientAdapter : IDisposable
    {
        static WSClientAdapter()
        {
            mDefaultHeader.Add("Host", "Host");
            mDefaultHeader.Add("Upgrade", "Upgrade");
            mDefaultHeader.Add("Connection", "Connection");
            mDefaultHeader.Add("Origin", "Origin");
            mDefaultHeader.Add("Sec-WebSocket-Key", "Sec-WebSocket-Key");
            mDefaultHeader.Add("Sec-WebSocket-Version", "Sec-WebSocket-Version");
        }

        private static Dictionary<string, string> mDefaultHeader = new Dictionary<string, string>();
        public virtual void Dispose()
        {
            if (WSClient != null)
            {
                WSClient.Dispose();
                WSClient = null;
            }
        }

        public Gateway Gateway { get; internal set; }

        public HttpRequest Request { get; internal set; }

        public WSClient WSClient { get; internal set; }

        public virtual UrlRoute GetRouteAgent(Gateway gateway, BeetleX.FastHttpApi.HttpRequest request, UrlRouteAgent urlRouteAgent)
        {
            return urlRouteAgent.UrlRoute;
        }

        public virtual void Init(UrlRouteAgent urlRouteAgent)
        {
            var urlRoute = GetRouteAgent(Gateway, Request, urlRouteAgent);
            var server = urlRoute.GetServerAgent(Request);
            WSClient = server.Agent.GetWSClient();
            WSClient.DataReceive += OnReceive;
            WSClient.Method = Request.Method;
            WSClient.Path = Request.Url;
            WSClient.Origin = Request.Header["Origin"];
            WSClient.Host = Request.Header["Host"];
            WSClient.SecWebSocketKey = Request.Header["Sec-WebSocket-Key"];
            WSClient.SecWebSocketVersion = Request.Header["Sec-WebSocket-Version"];
            var headers = Request.Header.Copy();
            foreach (var item in headers)
            {

                if (!mDefaultHeader.TryGetValue(item.Key, out string value))
                {
                    WSClient.Headers.Add(item.Key, item.Value);
                }
            }
        }

        protected virtual long GetTime(AgentDataFrame frame)
        {
            return 10;
        }

        protected virtual void OnReceive(object sender, WSReceiveArgs e)
        {
            if (e.Error != null)
            {
                var frame = Request?.Server.CreateDataFrame(new { Code = 500, Error = $"{e.Error.Message}" });
                frame?.Send(Request.Session);
                if (Gateway.HttpServer.EnableLog(BeetleX.EventArgs.LogType.Warring))
                {
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Warring,
                        $"Gateway websocket {Request.ID} {Request.RemoteIPAddress} {Request.Method} {Request.BaseUrl} process error {e.Error.Message}@{e.Error.StackTrace}");
                }
                WSClient.ServerAgent.Statistics.Add(Gateway.WEBSOCKET_INNER_ERROR, 10);
                Gateway.Statistics.Add(Gateway.WEBSOCKET_INNER_ERROR, 10);
                Gateway.RequestIncrementCompleted(Request, Gateway.WEBSOCKET_INNER_ERROR, 10, WSClient.ServerAgent);
            }
            else
            {
                var frame = Request?.Server.CreateDataFrame();
                frame.FIN = e.Frame.FIN;
                frame.IsMask = e.Frame.IsMask;
                frame.MaskKey = e.Frame.MaskKey;
                frame.RSV1 = e.Frame.RSV1;
                frame.RSV2 = e.Frame.RSV2;
                frame.RSV3 = e.Frame.RSV3;
                frame.Type = e.Frame.Type;
                frame.Body = e.Frame;
                frame.Send(Request.Session);
                var time = GetTime(e.Frame);
                WSClient.ServerAgent.Statistics.Add(Gateway.WEBSOCKET_SUCCESS, time);
                Gateway.Statistics.Add(Gateway.WEBSOCKET_SUCCESS, time);
                Gateway.RequestIncrementCompleted(Request, Gateway.WEBSOCKET_SUCCESS, time, WSClient.ServerAgent);
            }
        }

        public virtual void Send(BeetleX.FastHttpApi.WebSockets.DataFrame frame)
        {
            AgentDataFrame agentDataFrame = new AgentDataFrame();
            if (frame.Body != null)
            {
                agentDataFrame.Body = (ArraySegment<byte>)frame.Body;
            }
            agentDataFrame.FIN = frame.FIN;
            agentDataFrame.RSV1 = frame.RSV1;
            agentDataFrame.RSV2 = frame.RSV2;
            agentDataFrame.RSV3 = frame.RSV3;
            agentDataFrame.Type = frame.Type;
            WSClient.Send(agentDataFrame);
        }
    }
}
