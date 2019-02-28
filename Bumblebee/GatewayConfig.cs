using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee
{
    public class GatewayConfig
    {

        public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();

        public List<UrlConfig> Urls { get; set; } = new List<UrlConfig>();

        public void From(Gateway gateway)
        {
            foreach (var server in gateway.Agents.Servers)
            {
                Servers.Add(new ServerInfo { MaxConnections = server.MaxConnections, Uri = server.Uri.ToString() });
            }
            UrlConfig urlConfig = new UrlConfig();
            urlConfig.From(gateway.Routes.Default);
            Urls.Add(urlConfig);
            foreach (var route in gateway.Routes.Urls)
            {
                urlConfig = new UrlConfig();
                urlConfig.From(route);
                Urls.Add(urlConfig);
            }

        }

        public void To(Gateway gateway)
        {
            foreach (var server in Servers)
            {
                gateway.SetServer(server.Uri, server.MaxConnections);
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
        }

        public class UrlConfig
        {
            public List<RouteServer> Servers { get; set; } = new List<RouteServer>();

            public List<string> Filters { get; set; } = new List<string>();

            public string Url { get; set; }

            public string HashPattern { get; set; }

            public class RouteServer
            {
                public string Url { get; set; }

                public int Weight { get; set; }

                public int MaxRps { get; set; }
            }

            public void From(Routes.UrlRoute urlRoute)
            {
                Url = urlRoute.Url;
                HashPattern = urlRoute.HashPattern;
                foreach (var filter in urlRoute.FilterNames)
                {
                    Filters.Add(filter);
                }
                foreach (var server in urlRoute.Servers)
                {
                    Servers.Add(new RouteServer { Url = server.Agent.Uri.ToString(), Weight = server.Weight, MaxRps = server.MaxRPS });
                }
            }

            public void To(Gateway gateway)
            {
                gateway.RemoveRoute(Url);
                var result = gateway.SetRoute(Url, HashPattern);
                foreach (var filter in Filters)
                {
                    result.SetFilter(filter);
                }
                foreach (var server in Servers)
                {
                    result.AddServer(server.Url, server.Weight, server.MaxRps);
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
}
