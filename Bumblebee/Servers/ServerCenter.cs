using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;

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

        public void Verify()
        {
            foreach (var item in mAgents.Values)
            {
                item.Verify();
            }
        }

        public void Remove(string host)
        {
            mAgents.TryRemove(host, out ServerAgent server);
        }

        public ServerAgent Add(string host, int maxConnections)
        {
            ServerAgent result = null;
            try
            {
                Uri uri = new Uri(host);
                result = new ServerAgent(uri, Gateway, maxConnections);
                mAgents[uri.ToString()] = result;
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway add {host} server success");

            }
            catch (Exception e_)
            {
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway add {host} server error {e_.Message}");
            }
            return result;
        }

        public ServerAgent Get(string host)
        {
            mAgents.TryGetValue(host, out ServerAgent item);
            return item;
        }
    }
}
