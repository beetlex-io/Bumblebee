using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;
using BeetleX.EventArgs;

namespace Bumblebee.Servers
{
    public class ServerCenter
    {
        public ServerCenter(Gateway gateway)
        {
            Gateway = gateway;
            mAgents = new ConcurrentDictionary<string, ServerAgent>(StringComparer.OrdinalIgnoreCase);
        }

        public List<StatisticsData> GetServerStatistics()
        {
            List<StatisticsData> result = new List<StatisticsData>();
            foreach (var item in mAgents.Values)
            {
                result.Add(item.Statistics.GetData());
            }
            return result;
        }

        private ConcurrentDictionary<string, ServerAgent> mAgents;

        public Gateway Gateway { get; private set; }

        public ServerAgent[] Servers
        {
            get
            {
                return mAgents.Values.ToArray();
            }
        }

        public static string GetHost(string host)
        {
            Uri uri = new Uri(host);
            return uri.ToString();
        }

        public void Verify()
        {
            foreach (var item in mAgents.Values)
            {
                item.Verify();
            }
        }

        public ServerAgent Get(string host)
        {
            mAgents.TryGetValue(GetHost(host), out ServerAgent server);
            return server;
        }

        public void Remove(string host)
        {
            mAgents.TryRemove(GetHost(host), out ServerAgent server);
        }

        public ServerAgent SetServer(string host, int maxConnections)
        {
            return SetServer(host, maxConnections, null,null);
        }

        public ServerAgent SetServer(string host, int maxConnections,string category,string remark)
        {
            ServerAgent result = null;
            try
            {
                
                if (maxConnections==0)
                    maxConnections = Gateway.AgentMaxConnection;
                if (mAgents.TryGetValue(GetHost(host), out result))
                {
                    result.MaxConnections = maxConnections;
                    
                }
                else
                {
                    result = new ServerAgent(new Uri(host), Gateway, maxConnections);
                    mAgents[GetHost(host)] = result;
                }
                result.Category = category;
                result.Remark = remark;
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway set {host} server max connections {maxConnections} success");

            }
            catch (Exception e_)
            {
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway set {host} server max connections {maxConnections} error {e_.Message}{e_.StackTrace}");
            }
            return result;
        }

    }
}
