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
                    result = UrlRoute.RequestFilters[i].Execute(request, response);
                    if (!result)
                        break;
                }
                return result;
            }
            catch (Exception e_)
            {
                BadGateway badGateway = new BadGateway($"execute url filter error {e_.Message}");
                UrlRoute.Gateway.OnResponseError(new Events.EventResponseErrorArgs(request, response, badGateway, BadGateway.URL_FILTER_ERROR));
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
                    BadGateway result = new BadGateway($"The {Url} url route server unavailable");
                    Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(
                        request, response, result, BadGateway.URL_NODE_SERVER_UNAVAILABLE);
                    UrlRoute.Gateway.OnResponseError(erea);
                }
                else
                {
                    agent.Execute(request, response);
                }
            }
        }
    }
}
