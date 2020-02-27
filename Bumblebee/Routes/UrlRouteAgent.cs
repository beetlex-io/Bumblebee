using BeetleX.EventArgs;
using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Routes
{
    public class UrlRouteAgent
    {

        public long Version { get; set; }

        public string Url { get; set; }

        public UrlRoute UrlRoute { get; set; }

        public List<UrlRoute> Routes { get; set; }

        public void ExecuteWS(HttpRequest request, BeetleX.FastHttpApi.WebSockets.DataFrame dataFrame)
        {
            if (request.Server.EnableLog(LogType.Debug))
            {
                request.Server.Log(LogType.Debug, $"Gateway websocket {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s get urlroute agent!");
            }
            var agent = UrlRoute.GetServerAgent(request);
            if (agent == null)
            {
                if (request.Server.EnableLog(LogType.Info))
                {
                    request.Server.Log(LogType.Info, $"Gateway websocket {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s route server unavailable");
                }

                UrlRoute.Gateway.RequestIncrementCompleted(request, Gateway.URL_NODE_SERVER_UNAVAILABLE, 1,null);
                var frame = request.Server.CreateDataFrame(new { Code = Gateway.WEBSOCKET_INNER_ERROR, Error = $"Gateway websocket {UrlRoute.Url}'s route server unavailable" });
                frame.Send(request.Session);
            }
            else
            {
                if (request.Server.EnableLog(LogType.Debug))
                {
                    request.Server.Log(LogType.Debug, $"Gateway websocket {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s AgentRequesting event!");
                }
                agent.Increment();
                if (agent.ValidateRPS())
                {
                    if (request.Server.EnableLog(LogType.Debug))
                    {
                        request.Server.Log(LogType.Debug, $"Gateway websocket {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s agent execute!");
                    }
                    var wsagent = UrlRoute.Gateway.WSMessagesBus.GetAdapter(UrlRoute.Gateway, request, this, dataFrame);
                    wsagent.Send(dataFrame);
                    // agent.Agent.Execute(request, response, agent, UrlRoute);
                }
                else
                {
                    string error = $"Unable to reach {agent.Agent.Uri} websocket request, exceeding maximum number of RPS";

                    if (request.Server.EnableLog(LogType.Info))
                    {
                        request.Server.Log(LogType.Info, $"Gateway websocket {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s route server exceeding maximum number of RPS");
                    }
                    var frame = request.Server.CreateDataFrame(new { Code = Gateway.SERVER_MAX_OF_RPS, Error = error });
                    frame.Send(request.Session);
                    agent.Agent.Statistics.Add(Gateway.SERVER_MAX_OF_RPS, 1);
                }

            }
        }

        public void Execute(HttpRequest request, HttpResponse response)
        {
            if (request.Server.EnableLog(LogType.Debug))
            {
                request.Server.Log(LogType.Debug, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s get urlroute agent!");
            }

            if (!UrlRoute.ValidateRPS())
            {
                string error = $"Unable to reach [{UrlRoute.Url} route {request.Url}] in  HTTP request, exceeding maximum number of rps limit";
                Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(request, response,
                   UrlRoute.Gateway, error, Gateway.SERVER_MAX_OF_RPS);
                UrlRoute.Gateway.OnResponseError(erea);
                if (request.Server.EnableLog(LogType.Info))
                {
                    request.Server.Log(LogType.Info, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s route server exceeding maximum number of rps limit");
                }
                return;
            }

            var agent = UrlRoute.GetServerAgent(request);
            if (agent == null)
            {
                if (request.Server.EnableLog(LogType.Info))
                {
                    request.Server.Log(LogType.Info, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s route server unavailable");
                }

                Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(
                    request, response, UrlRoute.Gateway, $"The {Url} url route server unavailable", Gateway.URL_NODE_SERVER_UNAVAILABLE);
                UrlRoute.Gateway.OnResponseError(erea);
            }
            else
            {
                if (request.Server.EnableLog(LogType.Debug))
                {
                    request.Server.Log(LogType.Debug, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s AgentRequesting event!");
                }
                if (UrlRoute.Pluginer.AgentRequesting(request, response, agent.Agent, UrlRoute))
                {
                    agent.Increment();
                    if (agent.ValidateRPS())
                    {
                        if (request.Server.EnableLog(LogType.Debug))
                        {
                            request.Server.Log(LogType.Debug, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s agent execute!");
                        }
                        agent.Agent.Execute(request, response, agent, UrlRoute);
                    }
                    else
                    {
                        string error = $"Unable to reach {agent.Agent.Uri} HTTP request, exceeding maximum number of rps limit";
                        Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(request, response,
                           UrlRoute.Gateway, error, Gateway.SERVER_MAX_OF_RPS);
                        UrlRoute.Gateway.OnResponseError(erea);
                        if (request.Server.EnableLog(LogType.Info))
                        {
                            request.Server.Log(LogType.Info, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s route server exceeding maximum number of rps limit");
                        }
                    }
                }
                else
                {
                    if (request.Server.EnableLog(LogType.Info))
                    {
                        request.Server.Log(LogType.Info, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s route server exceeding cancel");
                    }
                }
            }
        }
    }
}
