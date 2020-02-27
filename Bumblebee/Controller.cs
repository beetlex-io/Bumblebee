using BeetleX.FastHttpApi;
using Bumblebee.Events;
using Bumblebee.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bumblebee
{
    [BeetleX.FastHttpApi.Controller(BaseUrl = "/__system/bumblebee")]
    [DefaultJsonResultFilter]
    [AccessTokenFilter]
    public class GatewayController
    {

        public GatewayController(Gateway gateway)
        {
            Gateway = gateway;
        }

        public Gateway Gateway { get; set; }

        public object __GATEWAY_ListServer()
        {
            return from a in Gateway.Agents.Servers select new GatewayServerDTO { Host = a.Uri.ToString(), MaxConnections = a.MaxConnections, Available = a.Available };
        }

        public void __GATEWAY_RemoveServer(string server)
        {
            Gateway.RemoveServer(server);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_SetServer(string server, int maxConnections)
        {
            Gateway.SetServer(server, maxConnections);
            Gateway.SaveConfig();
        }

        public object __GATEWAY_ListRoute()
        {
            List<GatewayRouteDTO> result = new List<GatewayRouteDTO>();
            result.AddRange(from a in Gateway.Routes.Urls
                            where a.ApiLoader == true
                            select new GatewayRouteDTO
                            {
                                Url = a.Url,
                                HashPattern = a.HashPattern,
                                Requesting = a.Pluginer.RequestingInfos,
                                AgentRequesting = a.Pluginer.AgentRequestingInfos,
                                HeaderWriting = a.Pluginer.HeaderWritingInfos,
                                Requested = a.Pluginer.RequestedInfos
                            });
            result.Add(new GatewayRouteDTO
            {
                Url = Gateway.Routes.Default.Url,
                HashPattern = Gateway.Routes.Default.HashPattern,
                Requesting = Gateway.Routes.Default.Pluginer.RequestingInfos,
                AgentRequesting = Gateway.Routes.Default.Pluginer.AgentRequestingInfos,
                HeaderWriting = Gateway.Routes.Default.Pluginer.HeaderWritingInfos,
                Requested = Gateway.Routes.Default.Pluginer.RequestedInfos
            });
            result.Sort((x, y) => x.Url.CompareTo(y.Url));
            foreach (var item in result)
                item.Servers = (IEnumerable<GatewayRouteServerDTO>)__GATEWAY_ListRouteServers(item.Url);
            return result;
        }

        public void __GATEWAY_SetRoute(string url, string hashPattern)
        {
            Gateway.SetRoute(url, hashPattern);
            Gateway.SaveConfig();
        }


        public void __GATEWAY_RemoveRoute(string url)
        {
            Gateway.RemoveRoute(url);
            Gateway.SaveConfig();
        }

        public object __GATEWAY_ListRouteServers(string url)
        {
            var result = from a in Gateway.Routes.GetRoute(url)?.Servers
                         select new GatewayRouteServerDTO
                         {
                             Host = a.Agent.Uri.ToString(),
                             Weight = a.Weight,
                             Available = a.Agent.Available,
                             MaxRps = a.MaxRPS
                         };
            return result;
        }

        public void __GATEWAY_SetRouteServer(string url, string server, int weight, int maxRps)
        {
            Gateway.Routes.GetRoute(url)?.AddServer(server, weight, maxRps);
            Gateway.SaveConfig();
            Gateway.Routes.UpdateUrlTable();
        }

        public void __GATEWAY_RemoveRouteServer(string url, string server)
        {
            Gateway.Routes.GetRoute(url)?.RemoveServer(server);
            Gateway.SaveConfig();
            Gateway.Routes.UpdateUrlTable();
        }

        public object __GATEWAY_GetRouteWedithTable(string url)
        {
            return Gateway.Routes.GetRoute(url)?.ServerWeightTable;
        }

        public object __GATEWAY_GetPluginInfo()
        {
            var result = new
            {
                Requesting = Gateway.PluginCenter.RequestingHandlers.Infos,
                AgentRequesting = Gateway.PluginCenter.AgentRequestingHandler.Infos,
                HeaderWriting = Gateway.PluginCenter.HeaderWritingHandlers.Infos,
                Requested = Gateway.PluginCenter.RequestedHandlers.Infos,
                GetServers = Gateway.PluginCenter.GetServerHandlers.Infos
            };
            return result;
        }

        public void __GATEWAY_SetRouteRequesting(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.SetRequesting(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_RemoveRouteRequesting(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.RemoveRequesting(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_SetRouteAgentRequesting(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.SetAgentRequesting(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_RemoveRouteAgentRequesting(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.RemoveAgentRequesting(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_SetRouteHeaderWriting(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.SetHeaderWriting(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_RemoveRouteHeaderWriting(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.RemoveHeaderWriting(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_SetRouteRequested(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.SetRequested(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_RemoveRouteRequested(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.RemoveRequested(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_SetGetServerHandler(string url, string name)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.SetGetServerHandler(name);
            Gateway.SaveConfig();
        }

        public void __GATEWAY_RemoveGetServerHandler(string url)
        {
            Gateway.Routes.GetRoute(url)?.Pluginer.RemoveGetServerHandler();
            Gateway.SaveConfig();
        }


        public StatisticsGroup __GATEWAY_GetStatistics()
        {
            return Gateway.Statistics.GetData();
        }

        public List<StatisticsGroup> __GATEWAY_GetUrlStatistics()
        {
            return Gateway.Routes.GetUrlStatisticsData();
        }

        public List<StatisticsGroup> __GATEWAY_GetUrlServerStatistics()
        {
            return Gateway.Routes.GetUrlServerStatisticsData();
        }

        public List<StatisticsGroup> __GATEWAY_GetServerStatistics()
        {
            return Gateway.Agents.GetServerStatistics();
        }

        public GatewayDTO __Gateway_GetSetting()
        {
            GatewayDTO result = new GatewayDTO();
            result.AgentMaxConnection = Gateway.AgentMaxConnection;
            result.AgentRequestQueueLength = Gateway.AgentRequestQueueLength;
            result.Requesting = Gateway.Pluginer.RequestingInfos;
            result.Requested = Gateway.Pluginer.RequestedInfos;
            result.ResponseError = Gateway.Pluginer.ResponseErrorInfos;
            return result;
        }

        public void __Gateway_Setting(int agentMaxConnnection, int agentRequestQueueLength)
        {
            Gateway.AgentMaxConnection = agentMaxConnnection;
            Gateway.AgentRequestQueueLength = agentRequestQueueLength;
            Gateway.SaveConfig();
        }

        public void __Gateway_SetRequesting(string name)
        {
            Gateway.Pluginer.SetRequesting(name);
            Gateway.SaveConfig();
        }

        public void __Gateway_RemoveRequesting(string name)
        {
            Gateway.Pluginer.RemoveRequesting(name);
            Gateway.SaveConfig();
        }

        public void __Gateway_SetRequested(string name)
        {
            Gateway.Pluginer.SetRequested(name);
            Gateway.SaveConfig();

        }
        public void __Gateway_RemoveRequested(string name)
        {
            Gateway.Pluginer.RemoveRequested(name);
            Gateway.SaveConfig();

        }

    }

    [Bumblebee.Plugins.RouteBinder(RouteUrl = "^/__system/.*", ApiLoader = false)]
    public class GatewayControllerPlugins : Bumblebee.Plugins.IRequestingHandler
    {
        public string Name => "GatewayControllerPlugins";

        public string Description => "Bumblebee admin controller pulugins";

        public void Execute(EventRequestingArgs e)
        {
            e.Cancel = true;
            e.ResultType = ResultType.None;
        }

        public void Init(Gateway gateway, Assembly assembly)
        {

        }
    }

    public class GatewayRouteServerDTO
    {
        public string Host { get; set; }

        public int Weight { get; set; }

        public bool Available { get; set; }

        public int MaxRps { get; set; }
    }

    public class GatewayServerDTO
    {
        public string Host { get; set; }

        public int MaxConnections { get; set; }

        public bool Available { get; set; }
    }

    public class GatewayRouteDTO
    {
        public string Url { get; set; }

        public string HashPattern { get; set; }

        public string Remark { get; set; }

        public IEnumerable<GatewayRouteServerDTO> Servers { get; set; }

        public IEnumerable<PluginInfo> Requesting { get; set; }

        public IEnumerable<PluginInfo> AgentRequesting { get; set; }

        public IEnumerable<PluginInfo> HeaderWriting { get; set; }

        public IEnumerable<PluginInfo> Requested { get; set; }

    }

    public class GatewayDTO
    {
        public int AgentMaxConnection { get; set; }

        public int AgentRequestQueueLength { get; set; }

        public IEnumerable<PluginInfo> Requesting { get; set; }

        public IEnumerable<PluginInfo> Requested { get; set; }

        public IEnumerable<PluginInfo> ResponseError { get; set; }
    }
}
