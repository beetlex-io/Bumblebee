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

        protected bool ExecuteFilter(HttpRequest request, HttpResponse response)
        {
            bool result = true;
            try
            {
                for (int i = 0; i < UrlRoute.RequestFilters.Length; i++)
                {
                    result = UrlRoute.RequestFilters[i].Executing(UrlRoute.Gateway, request, response);
                    if (!result)
                        break;
                }
                return result;
            }
            catch (Exception e_)
            {
                UrlRoute.Gateway.OnResponseError(new Events.EventResponseErrorArgs(request, response, UrlRoute.Gateway, $"execute url filter error {e_.Message}", Gateway.URL_FILTER_ERROR));
                return false;
            }

        }

        public void Execute(HttpRequest request, HttpResponse response)
        {
            if (ExecuteFilter(request, response))
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
                    agent.Agent.Execute(request, response, agent, UrlRoute);
                }
            }
        }
    }
}
