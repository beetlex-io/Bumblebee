using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using BeetleX.EventArgs;
using System.Net.Sockets;
using System.Threading.Tasks;
namespace Bumblebee.Servers
{
    public class ServerAgent : IDisposable
    {

        public ServerAgent(Uri uri, Gateway gateway, int maxConnections = 100)
        {
            Uri = uri;
            UriKey = uri.ToString();
            ServerName = uri.ToString();
            ServerID = GetServerID();
            Host = uri.Host;
            Port = uri.Port;
            Available = false;
            MaxConnections = maxConnections;
            Gateway = gateway;
            requestAgentsPool = new ConcurrentQueue<TcpClientAgent>();
            mWaitQueue = new ConcurrentQueue<TaskCompletionSource<TcpClientAgent>>();
            for (int i = 0; i < 10; i++)
            {
                var item = new TcpClientAgent(Host, Port);
                requestAgentsPool.Enqueue(item);
                Agents.Add(item);
            }
            mConnections = 10;
            mCount = 10;
            Statistics.Server = ServerName;
            this.Available = false;

            mQueueWaitMaxLength = gateway.AgentRequestQueueSize;
        }

        public string UriKey { get; set; }

        private int mQueueWaitMaxLength;

        private static ushort mID = 1;

        private static ushort GetServerID()
        {
            lock (typeof(ServerAgent))
            {

                mID++;
                if (mID >= ushort.MaxValue)
                    mID = 1;
                return mID;
            }
        }

        public string Category { get; set; }

        public string Remark { get; set; }

        public string ServerName { get; set; }

        public uint ServerID { get; set; }

        private System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

        private int mVerifyStatus = 0;

        public async void Verify()
        {
            if (System.Threading.Interlocked.CompareExchange(ref mVerifyStatus, 1, 0) == 0)
            {
                try
                {
                    var value = await client.GetStreamAsync(Uri);
                    OnSocketError(false);
                }
                catch (Exception e_)
                {

                    if (e_ is SocketException || e_.InnerException is SocketException)
                    {
                        OnSocketError(true);
                    }
                    else
                    {
                        OnSocketError(false);
                    }
                    if (Gateway.HttpServer.EnableLog(LogType.Debug))
                    {
                        Gateway.HttpServer.Log(LogType.Debug, $"Gateway verify {Uri} server error {e_.Message}  available[{Available}]");
                    }
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref mVerifyStatus, 0);
                }
            }
        }

        private void OnSocketError(bool value)
        {
            if (value)
            {
                var count = System.Threading.Interlocked.Increment(ref mSocketErrors);
                if (count >= Gateway.AgentMaxSocketError)
                {
                    if (this.Available)
                    {
                        if (Gateway.HttpServer.EnableLog(LogType.Info))
                        {
                            Gateway.HttpServer.Log(LogType.Info, $"Gateway {Uri} server not available");
                        }
                    }
                    this.Available = false;
                }
            }
            else
            {
                mSocketErrors = 0;
                if (!this.Available)
                {
                    if (Gateway.HttpServer.EnableLog(LogType.Info))
                    {
                        Gateway.HttpServer.Log(LogType.Info, $"Gateway {Uri} server available");
                    }
                }
                this.Available = true;
            }
        }

        private int mSocketErrors = 0;

        private bool mDisposed = false;

        public Statistics Statistics { get; private set; } = new Statistics();

        private ConcurrentQueue<TcpClientAgent> requestAgentsPool;

        private ConcurrentQueue<TaskCompletionSource<TcpClientAgent>> mWaitQueue;

        public int WaitQueue => mWaitLength;

        private object mLockPool = new object();

        private int mCount;

        private int mConnections;

        private int mWaitLength;

        public int Count => mCount;

        public Gateway Gateway { get; private set; }

        public List<TcpClientAgent> Agents { get; private set; } = new List<TcpClientAgent>();

        public Uri Uri { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool Available { get; set; }

        public int MaxConnections { get; internal set; }

        internal Task<TcpClientAgent> PopClient()
        {
            TcpClientAgent tcpClientAgent;

            TaskCompletionSource<TcpClientAgent> result = new TaskCompletionSource<TcpClientAgent>();
            if (requestAgentsPool.TryDequeue(out tcpClientAgent))
            {
                result.SetResult(tcpClientAgent);
            }
            else
            {
                if (mConnections > MaxConnections)
                {
                    if (mWaitLength < mQueueWaitMaxLength)
                    {
                        System.Threading.Interlocked.Increment(ref mWaitLength);
                        mWaitQueue.Enqueue(result);
                    }
                    else
                    {
                        result.SetResult(null);
                    }
                }
                else
                {
                    System.Threading.Interlocked.Increment(ref mConnections);
                    tcpClientAgent = new TcpClientAgent(Host, Port);
                    lock (Agents)
                        Agents.Add(tcpClientAgent);
                    result.SetResult(tcpClientAgent);
                }
            }
            return result.Task;

        }

        internal void Push(TcpClientAgent agent)
        {

            if (mDisposed)
            {
                agent.Client.DisConnect();
            }
            else
            {
                if (mWaitQueue.TryDequeue(out TaskCompletionSource<TcpClientAgent> result))
                {
                    System.Threading.Interlocked.Decrement(ref mWaitLength);
                    //result.SetResult(agent);
                    Task.Run(() => result.SetResult(agent));
                }
                else
                {
                    agent.Status = TcpClientAgentStatus.Free;
                    requestAgentsPool.Enqueue(agent);
                }

            }

        }

        public async void Execute(HttpRequest request, HttpResponse response, UrlRouteServerGroup.UrlServerInfo serverInfo, Routes.UrlRoute urlRoute)
        {
            try
            {
                var clientAgent = await PopClient();

                if (clientAgent == null)
                {
                    string error = $"Unable to reach {Host}:{Port} request queue overflow!";
                    Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(request, response,
                       Gateway, error, Gateway.SERVER_AGENT_QUEUE_OVERFLOW);
                    Gateway.OnResponseError(erea);
                    Gateway.ProcessError(Gateway.SERVER_AGENT_QUEUE_OVERFLOW, request);
                    Statistics.Add(Gateway.SERVER_AGENT_QUEUE_OVERFLOW, 0);
                    if (request.Server.EnableLog(LogType.Info))
                    {
                        request.Server.Log(LogType.Info, $"Gateway {request.ID} {request.RemoteIPAddress} {request.Method} {request.Url} route to {Uri} error  exceeding maximum number of connections");
                    }
                }
                else
                {
                    clientAgent.Status = TcpClientAgentStatus.None;
                    RequestAgent agent = new RequestAgent(clientAgent, this, request, response, serverInfo, urlRoute);
                    agent.Completed = OnCompleted;
                    agent.Execute();
                }
            }
            catch (Exception e_)
            {
                if (urlRoute.Gateway.HttpServer.EnableLog(LogType.Error))
                {
                    urlRoute.Gateway.HttpServer.Log(LogType.Error, $"Gateway {request.RemoteIPAddress} {request.Method} {request.Url} route to {Uri} error {e_.Message}{e_.StackTrace}");
                }
            }
        }

        private void OnCompleted(RequestAgent requestAgent)
        {
            try
            {

                if (requestAgent.Code == Gateway.SERVER_SOCKET_ERROR)
                {
                    var count = System.Threading.Interlocked.Increment(ref mSocketErrors);
                    OnSocketError(true);
                }
                else
                {
                    OnSocketError(false);
                }
                this.Statistics.Add(requestAgent.Code, requestAgent.Time);
            }
            finally
            {
                Gateway.OnRequestCompleted(requestAgent);
            }

        }

        public void Dispose()
        {
            mDisposed = true;
            lock (Agents)
                Agents.Clear();
            while (requestAgentsPool.TryDequeue(out TcpClientAgent result))
            {
                result.Client.DisConnect();
            }
        }

        public override string ToString()
        {
            return string.Format("({0}){1}", this.Available ? 1 : 0, Uri);
        }

        public ServerAgent AddUrl(string url, string hashPattern, int weight, int maxRps)
        {
            var route = Gateway.SetRoute(url, hashPattern).AddServer(this.Uri.ToString(), weight, maxRps);
            return this;
        }

        public ServerAgent AddUrl(string url, int weight, int maxRps)
        {
            return AddUrl(url, null, weight, maxRps);
        }
    }
}
