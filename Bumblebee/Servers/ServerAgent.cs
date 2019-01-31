using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using BeetleX.EventArgs;
using System.Net.Sockets;

namespace Bumblebee.Servers
{
    public class ServerAgent : IDisposable
    {

        public const int OTHRER_ERROR_CODE = 1;

        public const int SOCKET_ERROR_CODE = 8;

        public const int PROCESS_ERROR_CODE = 10;

        public ServerAgent(Uri uri, Gateway gateway, int maxConnections = 100)
        {
            Uri = uri;
            Host = uri.Host;
            Port = uri.Port;
            Available = true;
            MaxConnections = maxConnections;
            Gateway = gateway;
            for (int i = 0; i < 10; i++)
            {
                requestAgentsPool.Push(new TcpClientAgent(Host, Port));
            }
            mConnections = 10;
            mCount = 10;
            MaxSocketErrors = 5;
            this.Available = false;
        }

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
                if (count >= MaxSocketErrors)
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

        public int MaxSocketErrors { get; set; }

        private int mSocketErrors = 0;

        private bool mDisposed = false;

        public Statistics Statistics { get; private set; } = new Statistics();

        private ConcurrentStack<TcpClientAgent> requestAgentsPool = new ConcurrentStack<TcpClientAgent>();

        private int mCount;

        private int mConnections;

        public int Count=>mCount;

        public Gateway Gateway { get; private set; }

        public Uri Uri { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public bool Available { get; set; }

        public int MaxConnections { get; internal set; }

        internal TcpClientAgent PopClient()
        {
            if (!requestAgentsPool.TryPop(out TcpClientAgent result))
            {
                var count = System.Threading.Interlocked.Increment(ref mConnections);
                if (count > MaxConnections)
                {
                    System.Threading.Interlocked.Decrement(ref mConnections);
                    return null;
                }
                result = new TcpClientAgent(Host, Port);
            }
            else
            {
                System.Threading.Interlocked.Decrement(ref mCount);
            }
            return result;
        }

        internal void Push(TcpClientAgent agent)
        {
            if (mDisposed)
            {
                agent.Client.DisConnect();
            }
            else
            {
                requestAgentsPool.Push(agent);
                System.Threading.Interlocked.Increment(ref mCount);
            }
        }

        public void Execute(HttpRequest request, HttpResponse response)
        {
            TcpClientAgent clientAgent = PopClient();
            if (clientAgent == null)
            {
                string error = $"Unable to reach {Host}:{Port} HTTP request, exceeding maximum number of connections";
                if (Gateway.HttpServer.EnableLog(LogType.Error))
                {
                    Gateway.HttpServer.Log(LogType.Error, error);
                }
                BadGateway result = new BadGateway(error);
                Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(request, response,
                    result, BadGateway.SERVER_MAX_OF_CONNECTIONS);
                Gateway.OnResponseError(erea);
            }
            else
            {
                RequestAgent agent = new RequestAgent(clientAgent, this, request, response);
                agent.Completed = OnCompleted;
                agent.Execute();
            }
        }

        private void OnCompleted(RequestAgent requestAgent)
        {
            try
            {
                if (requestAgent.Code == SOCKET_ERROR_CODE)
                {
                    var count = System.Threading.Interlocked.Increment(ref mSocketErrors);
                    OnSocketError(true);
                }
                else
                {
                    OnSocketError(false);
                }
                this.Statistics.Add(requestAgent.Code);
                if (Gateway.HttpServer.EnableLog(LogType.Info))
                {
                    Gateway.HttpServer.Log(LogType.Info,
                        $"gateway {requestAgent.Request.RemoteIPAddress} {requestAgent.Request.Method} {requestAgent.Request.Url} request to {Host}:{Port} completed code {requestAgent.Code}");
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
            while (requestAgentsPool.TryPop(out TcpClientAgent result))
            {
                result.Client.DisConnect();
            }
        }

        public override string ToString()
        {
            return string.Format("({0}){1}", this.Available ? 1 : 0, Uri);
        }

        public ServerAgent AddUrl(string url, int weight = 0)
        {
            var route = Gateway.AddRoute(url).AddServer(this.Uri.ToString(), weight);
            return this;
        }
    }
}
