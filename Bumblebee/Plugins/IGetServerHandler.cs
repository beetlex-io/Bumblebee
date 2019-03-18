using BeetleX.FastHttpApi;
using Bumblebee.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    public interface IGetServerHandler:IPlugin
    {
        UrlRouteServerGroup.UrlServerInfo GetServer(Gateway gateway, HttpRequest request, UrlRouteServerGroup.UrlServerInfo[] servers);
    }
}
