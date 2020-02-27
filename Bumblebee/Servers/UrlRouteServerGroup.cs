using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using BeetleX;
using System.Linq;
using BeetleX.FastHttpApi;
using BeetleX.EventArgs;

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

        private List<UrlServerInfo> mServers = new List<UrlServerInfo>();

        private WeightTable weightTable = new WeightTable();

        public int Count => mServers.Count;

        public string Url { get; private set; }

        public UrlServerInfo[] ServerWeightTable => weightTable.HttpServerTable;

        public UrlServerInfo[] Servers
        {
            get
            {
                return mServers.ToArray();
            }
        }

        private ConcurrentQueue<ulong> mServerID = new ConcurrentQueue<ulong>();

        public Gateway Gateway { get; private set; }

        public bool HttpAvailable { get; private set; }

        public bool WSAvailable { get; private set; }

        public bool Available { get; private set; }

        public ulong Status
        {
            get
            {
                ulong httpStatus = 0;
                ulong wsStatus = 0;
                ulong status = 0;
                for (int i = 0; i < mServers.Count; i++)
                {
                    var item = mServers[i];
                    if (item.Agent.Available)
                        status |= item.ID;
                    if (item.Agent.WebSocket)
                        wsStatus |= item.ID;
                    else
                        httpStatus |= item.ID;
                }
                Available = status > 0;
                HttpAvailable = httpStatus > 0;
                WSAvailable = wsStatus > 0;
                return status;
            }
        }

        private void RefreshWeightTable()
        {
            if (Available)
            {
                WeightTable wt = new WeightTable();
                for (int i = 0; i < mServers.Count; i++)
                {
                    var item = mServers[i];
                    if (item.Agent.Available)
                    {
                        wt.Add(item);
                    }
                }

                if (wt.Builder())
                {
                    weightTable = wt;
                    Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {Url} route refresh weight table");
                }

            }
        }

        public void NewOrModify(string host, int weight, int maxRps, bool standby)
        {
            host = ServerCenter.GetHost(host);
            if (weight > 10)
                weight = 10;
            if (weight < 0)
                weight = 0;
            var item = mServers.Find(i => i.Agent.Uri.ToString() == host);
            if (item != null)
            {
                item.Weight = weight;
                item.MaxRPS = maxRps;
                item.Standby = standby;
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route update server [{host}] weight [{weight}] max rps [{maxRps}] success");
            }
            else
            {
                var agent = Gateway.Agents.Get(host);
                if (agent == null)
                {
                    Gateway.Agents.SetServer(host, Gateway.AgentMaxConnection);
                    agent = Gateway.Agents.Get(host);
                }
                UrlServerInfo serverItem = new UrlServerInfo(Url, agent);
                mServerID.TryDequeue(out ulong id);
                serverItem.ID = id;
                serverItem.Weight = weight;
                serverItem.MaxRPS = maxRps;
                serverItem.Standby = standby;
                mServers.Add(serverItem);
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route add server [{host}] weight [{weight}] max rps [{maxRps}] success");
            }
            RefreshWeightTable();
        }

        public void Remove(string host)
        {
            host = ServerCenter.GetHost(host);
            for (int i = 0; i < mServers.Count; i++)
            {
                if (mServers[i].Agent.Uri.ToString() == host)
                {
                    ulong id = mServers[i].ID;
                    mServers.RemoveAt(i);
                    if (mServers.Count > 0)
                    {
                        RefreshWeightTable();
                    }
                    mServerID.Enqueue(id);
                    Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {Url} route remove server {host} success");
                    return;
                }
            }

        }

        public UrlServerInfo GetAgent(ulong hashCode, HttpRequest request)
        {

            if (Available)
            {
                if (request.WebSocket)
                {
                    if (WSAvailable)
                        return weightTable.GetHttpAgent(hashCode, true);
                }
                else
                {
                    if (HttpAvailable)
                        return weightTable.GetHttpAgent(hashCode);
                }
            }
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

            public bool Standby { get; set; } = false;

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

            private const int TABLE_SIZE = 100;

            private UrlServerInfo[] mHttpServerTable = new UrlServerInfo[0];

            private UrlServerInfo[] mWSServerTable = new UrlServerInfo[0];

            public UrlServerInfo[] HttpServerTable => mHttpServerTable;

            public UrlServerInfo[] WSServerTabe => mWSServerTable;

            private List<UrlServerInfo> mServerItems = new List<UrlServerInfo>();

            public UrlServerInfo GetHttpAgent(ulong hashCode, bool isWebsocket = false)
            {
                var servers = isWebsocket ? mWSServerTable : mHttpServerTable;
                if (servers.Length == 0)
                    return null;
                int count = 0;
                int arrayIndex = (int)(hashCode % (uint)servers.Length);
                while (count < servers.Length)
                {
                    if (arrayIndex >= TABLE_SIZE)
                        arrayIndex = 0;
                    var server = servers[arrayIndex];
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

            private void BuilderHttp()
            {
                mHttpServerTable = new UrlServerInfo[0];
                var servers = (from a in mServerItems where !a.Agent.WebSocket orderby a.Weight descending select a).ToList();
                if (servers.Count == 0)
                    return;
                mHttpServerTable = new UrlServerInfo[TABLE_SIZE];
                int sum = 0;
                List<UrlServerInfo> availableClients = new List<UrlServerInfo>();
                for (int i = 0; i < servers.Count; i++)
                {
                    if (servers[i].Agent.Available)
                    {
                        sum += servers[i].Weight;
                        availableClients.Add(servers[i]);
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
                            mHttpServerTable[count] = item;
                            item.mItems.Dequeue();
                            count++;
                        }
                    }
                }
                foreach (UrlServerInfo item in availableClients)
                {
                    Status |= item.ID;
                }
                Shuffle(mHttpServerTable);
            }

            private void BuilderWebSocket()
            {
                mWSServerTable = new UrlServerInfo[0];
                var servers = (from a in mServerItems where a.Agent.WebSocket orderby a.Weight descending select a).ToList();
                if (servers.Count == 0)
                    return;
                mWSServerTable = new UrlServerInfo[TABLE_SIZE];
                int sum = 0;
                List<UrlServerInfo> availableClients = new List<UrlServerInfo>();
                for (int i = 0; i < servers.Count; i++)
                {
                    if (servers[i].Agent.Available)
                    {
                        sum += servers[i].Weight;
                        availableClients.Add(servers[i]);
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
                            mWSServerTable[count] = item;
                            item.mItems.Dequeue();
                            count++;
                        }
                    }
                }
                foreach (UrlServerInfo item in availableClients)
                {
                    Status |= item.ID;
                }
                Shuffle(mWSServerTable);
            }

            public bool Builder()
            {
                BuilderHttp();
                BuilderWebSocket();
                return true;
            }


        }
    }

}
