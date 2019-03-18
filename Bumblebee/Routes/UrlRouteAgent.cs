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


        public void Execute(HttpRequest request, HttpResponse response)
        {

            var agent = UrlRoute.GetServerAgent(request);
            if (agent == null)
            {
                Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(
                    request, response, UrlRoute.Gateway, $"The {Url} url route server unavailable", Gateway.URL_NODE_SERVER_UNAVAILABLE);
                UrlRoute.Gateway.OnResponseError(erea);
            }
            else
            {

                if (UrlRoute.Pluginer.AgentRequesting(request, response, agent.Agent, UrlRoute))
                {
                    agent.Increment();
                    if (agent.ValidateRPS())
                    {
                        agent.Agent.Execute(request, response, agent, UrlRoute);
                    }
                    else
                    {
                        string error = $"Unable to reach {agent.Agent.Uri} HTTP request, exceeding maximum number of RPS";
                        Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(request, response,
                           UrlRoute.Gateway, error, Gateway.SERVER_MAX_OF_RPS);
                        UrlRoute.Gateway.OnResponseError(erea);
                    }
                }
            }
        }
    }
}
