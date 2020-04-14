using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bumblebee
{
    public class GatewayConfig
    {

        public GatewayConfig()
        {
            GatewayQueueSize = Environment.ProcessorCount * 100;
            InstanceID = Guid.NewGuid().ToString("N");
        }

        public int AgentMaxConnection { get; set; } = 300;

        public int AgentRequestQueueSize { get; set; } = 500;

        public int GatewayQueueSize { get; set; }

        public bool OutputServerAddress { get; set; }

        public bool StatisticsEnabled { get; set; } = true;

        public string StatisticsExts { get; set; }

        public bool WSEnabled { get; set; } = true;

        public int BufferSize { get; set; }

        public int PoolMaxSize { get; set; }

        public string InstanceID { get; set; }

        public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();

        public List<UrlConfig> Urls { get; set; } = new List<UrlConfig>();

        public PluginConfig PluginConfig { get; set; } = new PluginConfig();

        public Dictionary<string, bool> PluginsStatus = new Dictionary<string, bool>();

        public void From(Gateway gateway)
        {

            this.BufferSize = gateway.BufferSize;
            this.PoolMaxSize = gateway.PoolMaxSize;
            this.StatisticsEnabled = gateway.StatisticsEnabled;
            foreach (var server in gateway.Agents.Servers)
            {
                Servers.Add(new ServerInfo
                {
                    MaxConnections = server.MaxConnections,
                    Uri = server.Uri.ToString(),
                    Remark = server.Remark,
                    Category = server.Category,
                    Command = server.Command,
                    Properties = server.GetProperties()
                }); ;
            }
            this.OutputServerAddress = gateway.OutputServerAddress;
            this.AgentMaxConnection = gateway.AgentMaxConnection;
            this.AgentRequestQueueSize = gateway.AgentRequestQueueSize;
            this.GatewayQueueSize = gateway.GatewayQueueSize;
            this.InstanceID = gateway.InstanceID;
            this.WSEnabled = gateway.WSEnabled;
            this.StatisticsExts = gateway.GetStatisticsExts();
            UrlConfig urlConfig = new UrlConfig();
            urlConfig.From(gateway.Routes.Default);
            Urls.Add(urlConfig);
            foreach (var route in gateway.Routes.Urls)
            {
                urlConfig = new UrlConfig();
                urlConfig.From(route);
                Urls.Add(urlConfig);
            }
            this.PluginConfig = new PluginConfig(gateway.Pluginer);
            this.PluginsStatus = gateway.PluginCenter.PluginsStatus;
        }

        public void To(Gateway gateway)
        {
            if (this.PoolMaxSize > 0)
            {
                gateway.PoolMaxSize = this.PoolMaxSize;
            }
            if (this.BufferSize > 0)
            {
                gateway.BufferSize = this.BufferSize;
            }

            BufferPool.BUFFER_SIZE = gateway.BufferSize;
            BufferPool.POOL_MAX_SIZE = gateway.PoolMaxSize;
            gateway.StatisticsEnabled = this.StatisticsEnabled;
            gateway.AgentRequestQueueSize = this.AgentRequestQueueSize;
            gateway.OutputServerAddress = this.OutputServerAddress;
            gateway.AgentMaxConnection = this.AgentMaxConnection;
            gateway.PluginCenter.PluginsStatus = this.PluginsStatus;
            gateway.GatewayQueueSize = this.GatewayQueueSize;
            gateway.InstanceID = this.InstanceID;
            gateway.SetStatisticsExts(this.StatisticsExts);
            gateway.WSEnabled = this.WSEnabled;
            this.PluginConfig.To(gateway.Pluginer);

            foreach (var server in Servers)
            {
                var agent = gateway.SetServer(server.Uri, server.Category, server.Remark, server.MaxConnections);
                agent.Command = server.Command;
                agent.SetProperties(server.Properties);
            }
            foreach (var s in gateway.Agents.Servers)
            {
                if (Servers.Find(d => new Uri(d.Uri).ToString() == s.Uri.ToString()) == null)
                {
                    gateway.RemoveServer(s.Uri.ToString());
                }

            }
            foreach (var url in Urls)
            {
                url.To(gateway);
            }
            foreach (var u in gateway.Routes.Urls)
            {
                if (Urls.Find(d => d.Url == u.Url) == null)
                {
                    gateway.RemoveRoute(u.Url);
                }
            }
        }

        public class ServerInfo
        {
            public string Uri { get; set; }

            public int MaxConnections { get; set; }

            public string Category { get; set; }

            public string Remark { get; set; }

            public string Command { get; set; }

            public Tuple<string, string>[] Properties { get; set; }
        }

        public class UrlConfig
        {

            public List<RouteServer> Servers { get; set; } = new List<RouteServer>();

            public PluginConfig PluginConfig { get; set; } = new PluginConfig();

            public string Url { get; set; }

            public string Remark { get; set; }

            public string HashPattern { get; set; }

            public int MaxRps { get; set; }

            public long TimeOut { get; set; }

            public string AccessControlAllowOrigin { get; set; }

            public string AccessControlAllowMethods { get; set; } = "*";

            public string AccessControlAllowHeaders { get; set; }

            public int AccessControlMaxAge { get; set; }

            public bool AccessControlAllowCredentials { get; set; }

            public string Vary { get; set; } = "Origin";

            public class RouteServer
            {
                public string Url { get; set; }

                public int Weight { get; set; }

                public int MaxRps { get; set; }

                public bool Standby { get; set; } = false;
            }

            public void From(Routes.UrlRoute urlRoute)
            {
                Url = urlRoute.Url;
                Remark = urlRoute.Remark;
                HashPattern = urlRoute.HashPattern;
                MaxRps = urlRoute.MaxRps;
                TimeOut = urlRoute.TimeOut;
                AccessControlAllowHeaders = urlRoute.AccessControlAllowHeaders;
                AccessControlAllowMethods = urlRoute.AccessControlAllowMethods;
                AccessControlAllowOrigin = urlRoute.AccessControlAllowOrigin;
                AccessControlMaxAge = urlRoute.AccessControlMaxAge;
                Vary = urlRoute.Vary;
                AccessControlAllowCredentials = urlRoute.AccessControlAllowCredentials;
                this.PluginConfig = new PluginConfig(urlRoute.Pluginer);
                foreach (var server in urlRoute.Servers)
                {
                    Servers.Add(new RouteServer { Url = server.Agent.Uri.ToString(), Weight = server.Weight, MaxRps = server.MaxRPS, Standby = server.Standby });
                }
            }

            public void To(Gateway gateway)
            {
                gateway.RemoveRoute(Url);
                Routes.UrlRoute result;
                if (Url == "*")
                {
                    result = gateway.Routes.Default;
                }
                else
                {
                    result = gateway.SetRoute(Url, Remark, HashPattern);
                }
                result.MaxRps = MaxRps;
                result.TimeOut = TimeOut;
                result.Vary = Vary;
                result.AccessControlMaxAge = AccessControlMaxAge;
                result.AccessControlAllowOrigin = AccessControlAllowOrigin;
                result.AccessControlAllowMethods = AccessControlAllowMethods;
                result.AccessControlAllowHeaders = AccessControlAllowHeaders;
                result.AccessControlAllowCredentials = this.AccessControlAllowCredentials;
                this.PluginConfig.To(result.Pluginer);
                foreach (var server in Servers)
                {
                    var r = result.AddServer(server.Url, server.Weight, server.MaxRps, server.Standby);
                    r.TimeOut = TimeOut;
                    r.MaxRps = this.MaxRps;
                }
            }

        }

        private const string CONFIG_FILE = "Gateway.json";

        public static void SaveConfig(Gateway gateway)
        {
            GatewayConfig config = new GatewayConfig();

            config.From(gateway);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(CONFIG_FILE, false, Encoding.UTF8))
            {
                string configData = Newtonsoft.Json.JsonConvert.SerializeObject(config);
                writer.Write(configData);
                writer.Flush();
            }
        }

        public static GatewayConfig LoadConfig()
        {
            if (System.IO.File.Exists(CONFIG_FILE))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(CONFIG_FILE))
                {
                    string configData = reader.ReadToEnd();
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<GatewayConfig>(configData);
                    return config;
                }
            }
            else
            {
                return null;
            }
        }



    }

    public class PluginConfig
    {
        public PluginConfig()
        {

        }

        public PluginConfig(Plugins.Pluginer pluginer)
        {
            Requesting = (from a in pluginer.RequestingInfos select a.Name).ToArray();
            AgentRequesting = (from a in pluginer.AgentRequestingInfos select a.Name).ToArray();
            HeaderWriting = (from a in pluginer.HeaderWritingInfos select a.Name).ToArray();
            Requested = (from a in pluginer.RequestedInfos select a.Name).ToArray();
            ResponseError = (from a in pluginer.ResponseErrorInfos select a.Name).ToArray();
        }

        public string[] Requesting { get; set; }

        public string[] AgentRequesting { get; set; }

        public string[] HeaderWriting { get; set; }

        public string[] Requested { get; set; }

        public string[] ResponseError { get; set; }

        public void To(Plugins.Pluginer pluginer)
        {
            if (Requesting != null)
            {
                foreach (var item in Requesting)
                    pluginer.SetRequesting(item);
            }

            if (AgentRequesting != null)
            {
                foreach (var item in AgentRequesting)
                    pluginer.SetAgentRequesting(item);
            }

            if (HeaderWriting != null)
            {
                foreach (var item in HeaderWriting)
                    pluginer.SetHeaderWriting(item);
            }

            if (Requested != null)
            {
                foreach (var item in Requested)
                    pluginer.SetRequested(item);
            }

            if (ResponseError != null)
            {
                foreach (var item in ResponseError)
                    pluginer.SetResponseError(item);
            }
        }

    }
}
