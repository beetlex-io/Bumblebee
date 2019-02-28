using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using BeetleX;

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

        private List<UrlServerInfo> serverItems = new List<UrlServerInfo>();

        private WeightTable weightTable = new WeightTable();

        public int Count => serverItems.Count;

        public string Url { get; private set; }

        public UrlServerInfo[] ServerWeightTable => weightTable.ServerTable;

        public UrlServerInfo[] GetServers
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

                if (wt.Builder())
                {
                    weightTable = wt;
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route refresh weight table");
                }

            }
        }

        public void NewOrModify(string host, int weight, int maxRps)
        {
            host = ServerCenter.GetHost(host);
            if (weight > 10)
                weight = 10;
            if (weight < 0)
                weight = 0;
            var item = serverItems.Find(i => i.Agent.Uri.ToString() == host);
            if (item != null)
            {
                item.Weight = weight;
                item.MaxRPS = maxRps;
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route update server [{host}] weight [{weight}] max rps [{maxRps}] success");
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
                    UrlServerInfo serverItem = new UrlServerInfo(Url, agent);
                    mServerID.TryDequeue(out ulong id);
                    serverItem.ID = id;
                    serverItem.Weight = weight;
                    serverItem.MaxRPS = maxRps;
                    serverItems.Add(serverItem);
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route add server [{host}] weight [{weight}] max rps [{maxRps}] success");
                }

            }
            RefreshWeightTable();
        }

        public void Remove(string host)
        {
            host = ServerCenter.GetHost(host);
            for (int i = 0; i < serverItems.Count; i++)
            {
                if (serverItems[i].Agent.Uri.ToString() == host)
                {
                    ulong id = serverItems[i].ID;
                    serverItems.RemoveAt(i);
                    if (serverItems.Count > 0)
                    {
                        RefreshWeightTable();
                    }
                    mServerID.Enqueue(id);
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route remove server {host} success");
                    return;
                }
            }

        }

        public UrlServerInfo GetAgent(ulong hashCode)
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

        public class UrlServerInfo
        {

            public UrlServerInfo(string url, ServerAgent agent)
            {
                Url = url;
                Agent = agent;
            }

            public string Url { get; set; }

            public ServerAgent Agent { get; set; }

            public ulong ID { get; set; }

            public int Weight { get; set; }

            public override string ToString()
            {
                return $"[ID:{ID}] {Agent.Uri}[{Agent.Available}] weight:{Weight}";
            }

            internal Queue<int> mItems = new Queue<int>();

            public int MaxRPS { get; set; }

            private int mRPS;

            private long mLastTime;

            internal bool ValidateRPS()
            {
                if (MaxRPS == 0)
                    return true;
                long now = TimeWatch.GetElapsedMilliseconds();
                if (now - mLastTime >= 1000)
                    return true;
                return mRPS < MaxRPS;
            }
            internal void Increment()
            {
                if (MaxRPS > 0)
                {
                    long now = TimeWatch.GetElapsedMilliseconds();
                    if (now - mLastTime >= 1000)
                    {
                        mLastTime = now;
                        System.Threading.Interlocked.Exchange(ref mRPS, 1);
                    }
                    else
                    {
                        System.Threading.Interlocked.Increment(ref mRPS);
                    }
                }
            }
        }

        class WeightTable
        {
            public ulong Status { get; set; }

            public WeightTable()
            {

            }

            private const int TABLE_SIZE = 50;

            private UrlServerInfo[] mConnectionsTable = new UrlServerInfo[0];

            public UrlServerInfo[] ServerTable => mConnectionsTable;

            private List<UrlServerInfo> mServerItems = new List<UrlServerInfo>();

            public UrlServerInfo GetAgent(ulong hashCode)
            {
                if (mServerItems.Count == 0)
                    return null;
                int count = 0;
                int arrayIndex = (int)(hashCode % (uint)mConnectionsTable.Length);
                while (count < mConnectionsTable.Length)
                {
                    if (arrayIndex >= TABLE_SIZE)
                        arrayIndex = 0;
                    var server = mConnectionsTable[arrayIndex];
                    if (server.Agent.Available)
                        return server;
                    arrayIndex++;
                    count++;
                }
                return null;
            }

            private void Shuffle(UrlServerInfo[] list)
            {
                Random rng = new Random();
                int n = list.Length;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    UrlServerInfo value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
            }

            public void Add(UrlServerInfo item)
            {
                mServerItems.Add(item);
            }

            public bool Builder()
            {
                mConnectionsTable = new UrlServerInfo[TABLE_SIZE];
                int sum = 0;
                mServerItems.Sort((x, y) => y.Weight.CompareTo(x.Weight));
                if (mServerItems.Count == 0)
                    return false;
                List<UrlServerInfo> availableClients = new List<UrlServerInfo>();
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
                    foreach (UrlServerInfo item in availableClients)
                    {
                        if (item.mItems.Count > 0)
                        {
                            mConnectionsTable[count] = item;
                            item.mItems.Dequeue();
                            count++;
                        }
                    }
                }
                foreach (UrlServerInfo item in availableClients)
                {
                    Status |= item.ID;
                }
                return true;
            }
        }
    }

}
