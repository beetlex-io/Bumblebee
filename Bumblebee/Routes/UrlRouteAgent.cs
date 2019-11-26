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

        public void Execute(HttpRequest request, HttpResponse response)
        {

            if (request.Server.EnableLog(LogType.Debug))
            {
                request.Server.Log(LogType.Debug, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s get urlroute agent!");
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
                        string error = $"Unable to reach {agent.Agent.Uri} HTTP request, exceeding maximum number of RPS";
                        Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(request, response,
                           UrlRoute.Gateway, error, Gateway.SERVER_MAX_OF_RPS);
                        UrlRoute.Gateway.OnResponseError(erea);
                        if (request.Server.EnableLog(LogType.Info))
                        {
                            request.Server.Log(LogType.Info, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} request {UrlRoute.Url}'s route server exceeding maximum number of RPS");
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
