using BeetleX;
using BeetleX.EventArgs;
using BeetleX.FastHttpApi;
using BeetleX.FastHttpApi.WebSockets;
using Bumblebee.Routes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.WSAgents
{
    public class WSMessagesBus
    {

        protected virtual UrlRouteAgent GetRouteAgent(Gateway gateway, HttpRequest request, DataFrame frame)
        {
            return gateway.Routes.GetAgent(request);
        }

        public virtual void CloseSession(ISession session)
        {
            WSClientAdapter result = (WSClientAdapter)session["__ws_agent_adapter"];
            result?.Dispose();
        }

        public virtual WSClientAdapter GetAdapter(Gateway gateway, BeetleX.FastHttpApi.HttpRequest request, UrlRouteAgent urlRouteAgent,DataFrame frame)
        {
            WSClientAdapter result = (WSClientAdapter)request.Session["__ws_agent_adapter"];
            if (result == null)
            {
                result = CreateAdapter();
                result.Request = request;
                result.Gateway = gateway;
                
                result.Init(urlRouteAgent);
                
                request.Session["__ws_agent_adapter"] = result;
                
            }
            return result;
        }

        public void Execute(Gateway gateway, WebSocketReceiveArgs e)
        {
            try
            {
                var ip = e.Request.RemoteIPAddress;
                if (gateway.HttpServer.EnableLog(LogType.Info))
                {
                    gateway.HttpServer.Log(LogType.Info, $"Gateway websocket {e.Request.ID} {ip} {e.Request.Method} {e.Request.Url} receive");
                }
                var item = GetRouteAgent(gateway, e.Request, e.Frame);
                if (item == null)
                {
                    if (gateway.HttpServer.EnableLog(LogType.Info))
                    {
                        gateway.HttpServer.Log(LogType.Info, $"Gateway websocket {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.Url} request cluster server unavailable");
                    }
                    gateway.RequestIncrementCompleted(e.Request, Gateway.CLUSTER_SERVER_UNAVAILABLE, 1,null);
                    var frame = e.Server.CreateDataFrame(new { Code = Gateway.WEBSOCKET_INNER_ERROR, Error = $"Gateway cluster server unavailable" });
                    frame.Send(e.Request.Session);
                }
                else
                {
                    if (gateway.HttpServer.EnableLog(LogType.Info))
                    {
                        gateway.HttpServer.Log(LogType.Info, $"Gateway websocket {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.Url} {item.UrlRoute.Url}'s route executing");
                    }
                    item.ExecuteWS(e.Request, e.Frame);
                }
            }
            catch (Exception e_)
            {
                if (gateway.HttpServer.EnableLog(BeetleX.EventArgs.LogType.Error))
                {
                    gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error,
                        $"Gateway websocket {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.BaseUrl} process error {e_.Message}@{e_.StackTrace}");
                }
                gateway.RequestIncrementCompleted(e.Request, Gateway.WEBSOCKET_INNER_ERROR, 1);
                var frame = e.Server.CreateDataFrame(new { Code = Gateway.WEBSOCKET_INNER_ERROR, Error = $"Process error {e_.Message}" });
                frame.Send(e.Request.Session);
            }
        }

        protected virtual WSClientAdapter CreateAdapter()
        {
            return new WSClientAdapter();
        }
    }
}
