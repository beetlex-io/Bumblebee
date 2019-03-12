using BeetleX.FastHttpApi;
using Bumblebee.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Routes
{
    public interface IUrlRouteGetServerHandler
    {
        UrlRouteServerGroup.UrlServerInfo GetServer(HttpRequest request, UrlRouteServerGroup.UrlServerInfo[] servers);
    }
}
