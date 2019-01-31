using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
namespace Bumblebee.Servers
{
    public class UrlRouteServerGroup
    {
        public UrlRouteServerGroup(Gateway gateway, string url)
        {
            Gateway = gateway;
            Url = url;
            Available = false;
            for (int i = 0; i < 64; i++)
            {
                mServerID.Enqueue((ulong)1 << i);
            }
        }

        private List<ServerItem> serverItems = new List<ServerItem>();

        private WeightTable weightTable = new WeightTable();

        public int Count => serverItems.Count;

        public string Url { get; private set; }

        public ServerItem[] GetServers
        {
            get
            {
                return serverItems.ToArray();
            }
        }

        private ConcurrentQueue<ulong> mServerID = new ConcurrentQueue<ulong>();

        public Gateway Gateway { get; private set; }

        public bool Available { get; private set; }

        public ulong Status
        {
            get
            {
                ulong status = 0;
                for (int i = 0; i < serverItems.Count; i++)
                {
                    var item = serverItems[i];
                    if (item.Agent.Available)
                        status |= item.ID;
                }
                Available = status > 0;
                return status;
            }
        }

        private void RefreshWeightTable()
        {
            if (Available)
            {
                WeightTable wt = new WeightTable();
                for (int i = 0; i < serverItems.Count; i++)
                {
                    var item = serverItems[i];
                    if (item.Agent.Available)
                    {
                        wt.Add(item);
                    }
                }
                wt.Builder();
                weightTable = wt;
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route refresh weight table");
            }
        }

        public void NewOrModify(string host, int weight = 0)
        {
            if (weight > 10)
                weight = 10;
            if (weight < 0)
                weight = 0;
            var item = serverItems.Find(i => i.Agent.Host == host);
            if (item != null)
            {
                item.Weight = weight;
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route update server [{host}] weight [{weight}] success");
            }
            else
            {
                var agent = Gateway.Agents.Get(host);
                if (agent == null)
                {
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway {Url} route add server error {host} server not found!");
                    return;
                }
                else
                {
                    ServerItem serverItem = new ServerItem
                    {
                        Agent = agent
                    };
                    mServerID.TryDequeue(out ulong id);
                    serverItem.ID = id;
                    serverItem.Weight = weight;
                    serverItems.Add(serverItem);
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route add server [{host}] weight [{weight}] success");
                }

            }
            RefreshWeightTable();
        }

        public void Remove(string host)
        {
            for (int i = 0; i < serverItems.Count; i++)
            {
                if (serverItems[i].Agent.Host == host)
                {
                    ulong id = serverItems[i].ID;
                    serverItems.RemoveAt(i);
                    RefreshWeightTable();
                    mServerID.Enqueue(id);
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route remove server {host} success");
                    return;
                }
            }

        }

        public ServerAgent GetAgent(long hashCode)
        {
            if (Available)
                return weightTable.GetAgent(hashCode);
            else
                return null;
        }

        public void Verify()
        {
            var status = Status;
            if (weightTable.Status != status)
            {
                RefreshWeightTable();
            }
        }


        public class ServerItem
        {
            public ServerAgent Agent { get; set; }

            public ulong ID { get; set; }

            public int Weight { get; set; }

            public override string ToString()
            {
                return $"[ID:{ID}] {Agent.Uri}[{Agent.Available}] weight:{Weight}";
            }

            internal Queue<int> mItems = new Queue<int>();
        }

        class WeightTable
        {
            public ulong Status { get; set; }

            public WeightTable()
            {

            }

            private const int TABLE_SIZE = 50;

            private ServerAgent[] mConnectionsTable;

            private List<ServerItem> mServerItems = new List<ServerItem>();

            public ServerAgent GetAgent(long hashCode)
            {
                if (mServerItems.Count == 0)
                    return null;
                int count = 0;
                int arrayIndex = (int)(hashCode % mConnectionsTable.Length);
                while (count < mConnectionsTable.Length)
                {
                    if (arrayIndex >= TABLE_SIZE)
                        arrayIndex = 0;
                    var server = mConnectionsTable[arrayIndex];
                    if (server.Available)
                        return server;
                    arrayIndex++;
                    count++;
                }
                return null;
            }

            private void Shuffle(ServerItem[] list)
            {
                Random rng = new Random();
                int n = list.Length;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    ServerItem value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
            }

            public void Add(ServerItem item)
            {
                mServerItems.Add(item);
            }

            public void Builder()
            {
                mConnectionsTable = new ServerAgent[TABLE_SIZE];
                int sum = 0;
                mServerItems.Sort((x, y) => y.Weight.CompareTo(x.Weight));
                List<ServerItem> availableClients = new List<ServerItem>();
                for (int i = 0; i < mServerItems.Count; i++)
                {
                    if (mServerItems[i].Agent.Available)
                    {
                        sum += mServerItems[i].Weight;
                        availableClients.Add(mServerItems[i]);
                    }
                }
                int count = 0;
                for (int i = 0; i < availableClients.Count; i++)
                {
                    int size = (int)((double)availableClients[i].Weight / (double)sum * (double)TABLE_SIZE);
                    for (int k = 0; k < size; k++)
                    {
                        availableClients[i].mItems.Enqueue(1);
                        count++;
                        if (count >= TABLE_SIZE)
                            goto END;
                    }
                }
                int index = 0;
                while (count < TABLE_SIZE)
                {
                    availableClients[index % availableClients.Count].mItems.Enqueue(1);
                    index++;
                    count++;
                }
            END:
                count = 0;
                while (count < TABLE_SIZE)
                {
                    foreach (ServerItem item in availableClients)
                    {
                        if (item.mItems.Count > 0)
                        {
                            mConnectionsTable[count] = item.Agent;
                            item.mItems.Dequeue();
                            count++;
                        }
                    }
                }
                foreach (ServerItem item in availableClients)
                {
                    Status |= item.ID;
                }
            }
        }
    }

}
