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
            mAgents = new ConcurrentDictionary<string, ServerAgent>();
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
            ServerAgent result = null;
            try
            {
                if (maxConnections > Gateway.AgentMaxConnection)
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
