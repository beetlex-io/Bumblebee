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
            ServerName = uri.ToString();
            ServerID = GetServerID();
            Host = uri.Host;
            Port = uri.Port;
            Available = false;
            MaxConnections = maxConnections;
            Gateway = gateway;
            requestAgentsPool = new Queue<TcpClientAgent>(gateway.AgentMaxConnection);
            mClientWaitQueue = new Queue<TaskCompletionSource<TcpClientAgent>>(gateway.AgentRequestQueueLength);
            for (int i = 0; i < 10; i++)
            {
                requestAgentsPool.Enqueue(new TcpClientAgent(Host, Port));
            }
            mConnections = 10;
            mCount = 10;
            this.Available = false;

            mQueueWaitMaxLength = gateway.AgentRequestQueueLength;
        }

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
                    if (Gateway.HttpServer.EnableLog(LogType.Info))
                    {
                        Gateway.HttpServer.Log(LogType.Info, $"Verify {Uri} server error {e_.Message}  server available {Available}");
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
                    this.Available = false;
                }
            }
            else
            {
                mSocketErrors = 0;
                this.Available = true;
            }
        }

        private int mSocketErrors = 0;

        private bool mDisposed = false;

        private Queue<TcpClientAgent> requestAgentsPool;

        private Queue<TaskCompletionSource<TcpClientAgent>> mClientWaitQueue;

        private object mLockPool = new object();

        private int mCount;

        private int mConnections;

        public int Count => mCount;

        public Gateway Gateway { get; private set; }

        public Uri Uri { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool Available { get; set; }

        public int MaxConnections { get; internal set; }

        internal Task<TcpClientAgent> PopClient()
        {
            TcpClientAgent tcpClientAgent;
            lock (mLockPool)
            {
                TaskCompletionSource<TcpClientAgent> result = new TaskCompletionSource<TcpClientAgent>();
                if (requestAgentsPool.Count > 0)
                {
                    tcpClientAgent = requestAgentsPool.Dequeue();
                    result.SetResult(tcpClientAgent);
                }
                else
                {
                    mConnections++;
                    if (mConnections > MaxConnections)
                    {
                        mConnections--;
                        if (mClientWaitQueue.Count < mQueueWaitMaxLength)
                            mClientWaitQueue.Enqueue(result);
                        else
                            result.SetResult(null);
                    }
                    else
                    {
                        tcpClientAgent = new TcpClientAgent(Host, Port);
                        result.SetResult(tcpClientAgent);
                    }
                }
                return result.Task;
            }
        }

        internal void Push(TcpClientAgent agent)
        {

            lock (mLockPool)
            {
                if (mDisposed)
                {
                    agent.Client.DisConnect();
                }
                else
                {
                    if (mClientWaitQueue.Count > 0)
                    {
                        var item = mClientWaitQueue.Dequeue();
                        item.SetResult(agent);
                        //Task.Run(() => item.SetResult(agent));
                    }
                    else
                    {
                        requestAgentsPool.Enqueue(agent);
                    }
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
                    string error = $"Unable to reach {Host}:{Port} HTTP request, exceeding maximum number of connections";
                    Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(request, response,
                       Gateway, error, Gateway.SERVER_MAX_OF_CONNECTIONS);
                    Gateway.OnResponseError(erea);
                    if (request.Server.EnableLog(LogType.Info))
                    {
                        request.Server.Log(LogType.Info, $"gateway {request.RemoteIPAddress} {request.Method} {request.Url} route to {Uri} error  exceeding maximum number of connections");
                    }
                }
                else
                {
                    RequestAgent agent = new RequestAgent(clientAgent, this, request, response, serverInfo, urlRoute);
                    agent.Completed = OnCompleted;
                    Gateway.AddRequest(agent);
                    //agent.Execute();
                }
            }
            catch (Exception e_)
            {
                if (urlRoute.Gateway.HttpServer.EnableLog(LogType.Error))
                {
                    urlRoute.Gateway.HttpServer.Log(LogType.Error, $"gateway {request.RemoteIPAddress} {request.Method} {request.Url} route to {Uri} error {e_.Message}{e_.StackTrace}");
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
            }
            finally
            {
                Gateway.OnRequestCompleted(requestAgent);
            }

        }

        public void Dispose()
        {
            mDisposed = true;
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
