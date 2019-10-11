using System;
using System.Collections.Generic;
using System.Text;
using BeetleX.FastHttpApi;
using System.Threading;
using System.Threading.Tasks;
using Bumblebee.Events;
using Bumblebee.Plugins;
using BeetleX.Buffers;
using Bumblebee.Servers;
using BeetleX.EventArgs;
using Bumblebee.Routes;

namespace Bumblebee
{
    public class Gateway : IDisposable
    {

        public const int CLUSTER_SERVER_UNAVAILABLE = 590;

        public const int URL_NODE_SERVER_UNAVAILABLE = 591;

        public const int GATEWAY_QUEUE_OVERFLOW = 592;

        public const int URL_FILTER_ERROR = 592;

        public const int REMOTE_CLIENT_CLOSE = 560;

        public const int SERVER_SOCKET_ERROR = 570;

        public const int SERVER_AGENT_PROCESS_ERROR = 571;

        public const int SERVER_AGENT_QUEUE_OVERFLOW = 572;

        public const int SERVER_MAX_OF_RPS = 573;

        public const int SERVER_NETWORK_READ_STREAM_ERROR = 574;

        public const int SERVER_OTHRER_ERROR_CODE = 580;

        public const int SERVER_PROCESS_ERROR_CODE = 582;

        public static int BufferSize { get; set; } = 1024 * 8;

        public static int PoolMaxSize { get; set; } = 1024 * 10;

        static Gateway()
        {
            GATEWAY_SERVER_HEDER = Encoding.UTF8.GetBytes("Server: Bumblebee(BeetleX)\r\n");
            KEEP_ALIVE = Encoding.UTF8.GetBytes("Connection: keep-alive\r\n");
        }

        public Gateway()
        {
            BufferPool.BUFFER_SIZE = BufferSize;
            BufferPool.POOL_MAX_SIZE = PoolMaxSize;
            HttpServer = new HttpApiServer();
            Routes = new Routes.RouteCenter(this);
            Agents = new Servers.ServerCenter(this);
            this.PluginCenter = new PluginCenter(this);
            this.Pluginer = new Pluginer(this, null);
            Statistics.Server = "Gateway";
            AgentMaxSocketError = 3;
            MaxStatsUrls = 2000;
            AgentMaxConnection = 200;
            AgentRequestQueueSize = 2000;
            ThreadQueues = (Environment.ProcessorCount / 2);
            if (ThreadQueues == 0)
                ThreadQueues = 1;
            GatewayQueueSize = Environment.ProcessorCount * 500;
            InstanceID = Guid.NewGuid().ToString("N");
        }

        public string InstanceID { get; internal set; }

        public int ThreadQueues { get; set; }

        public bool OutputServerAddress { get; set; } = false;

        public int AgentMaxConnection { get; set; }

        public int AgentMaxSocketError { get; set; }

        public int AgentRequestQueueSize { get; set; }

        public int GatewayQueueSize { get; set; }

        public int MaxStatsUrls { get; set; }

        public PluginCenter PluginCenter { get; private set; }

        public Gateway LoadPlugin(params System.Reflection.Assembly[] assemblies)
        {
            if (assemblies == null)
                return this;
            foreach (var item in assemblies)
            {
                if (this.PluginCenter.Load(item) > 0)
                {
                    Routes.ReloadPlugin();
                    this.Pluginer.Reload();
                }
            }
            return this;
        }

        private System.Threading.Timer mVerifyTimer;

        internal static byte[] GATEWAY_SERVER_HEDER;

        internal static byte[] KEEP_ALIVE;

        private void OnVerifyTimer(Object state)
        {
            mVerifyTimer.Change(-1, -1);
            try
            {
                Routes.Verify();
                Task.Run(() => { Agents.Verify(); });

            }
            catch (Exception e_)
            {
                if (HttpServer.EnableLog(BeetleX.EventArgs.LogType.Error))
                    HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"Gateway verify processing errors {e_.Message}");
            }
            finally
            {
                mVerifyTimer.Change(1000, 1000);
            }
        }

        public Pluginer Pluginer { get; private set; }

        public HttpApiServer HttpServer { get; internal set; }

        public Routes.RouteCenter Routes { get; private set; }

        public Servers.ServerCenter Agents { get; private set; }

        public Statistics Statistics { get; private set; } = new Statistics();

        public Servers.ServerAgent SetServer(string host, string category, string remark, int maxConnections = 200)
        {
            return Agents.SetServer(host, maxConnections, category, remark);
        }

        public Servers.ServerAgent SetServer(string host, int maxConnections = 200)
        {
            return Agents.SetServer(host, maxConnections);

        }

        public Gateway RemoveServer(string host)
        {
            Agents.Remove(host);
            Routes.RemoveServer(host);
            return this;
        }

        public Routes.UrlRoute SetRoute(string url, string remark, string hashPattern = null)
        {
            var result = Routes.NewOrGet(url, remark, hashPattern);
            return result;
        }

        public Gateway RemoveRoute(string url)
        {
            Routes.Remove(url);
            return this;
        }

        public Gateway HttpOptions(Action<HttpOptions> handler)
        {
            handler?.Invoke(HttpServer.Options);
            return this;
        }

        private void OnRequest(object sender, EventHttpRequestArgs e)
        {
            try
            {
                e.Cancel = true;
                var ip = e.Request.RemoteIPAddress;
                if (HttpServer.EnableLog(LogType.Info))
                {
                    HttpServer.Log(LogType.Info, $"Gateway {e.Request.ID} {e.Request.Method} {e.Request.Url} request from {ip}");
                }
                HttpServer.RequestExecting();
                var result = this.Pluginer.Requesting(e.Request, e.Response);
                if (result.Item1)
                {
                    var item = Routes.GetAgent(e.Request);
                    if (item == null)
                    {
                        if (HttpServer.EnableLog(LogType.Info))
                        {
                            HttpServer.Log(LogType.Info, $"Gateway {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.Url} request cluster server unavailable");
                        }
                        EventResponseErrorArgs error = new EventResponseErrorArgs(
                            e.Request, e.Response, this, "Cluster server unavailable", Gateway.CLUSTER_SERVER_UNAVAILABLE
                            );
                        OnResponseError(error);
                    }
                    else
                    {
                        result = item.UrlRoute.Pluginer.Requesting(e.Request, e.Response);
                        if (result.Item1)
                        {
                            if (HttpServer.EnableLog(LogType.Info))
                            {
                                HttpServer.Log(LogType.Info, $"Gateway {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.Url} request {item.UrlRoute.Url}'s route executing");
                            }
                            if (IOQueue.Count > GatewayQueueSize)
                            {
                                Events.EventResponseErrorArgs erea = new Events.EventResponseErrorArgs(
                   e.Request, e.Response, this, $"The gateway queue overflow!", Gateway.GATEWAY_QUEUE_OVERFLOW);
                                this.OnResponseError(erea);
                            }
                            else
                            {
                                AddRequest(new Tuple<UrlRouteAgent, HttpRequest, HttpResponse>(item, e.Request, e.Response));
                            }
                        }
                        else
                        {
                            if (HttpServer.EnableLog(LogType.Info))
                            {
                                HttpServer.Log(LogType.Info, $"Gateway {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.Url} request {item.UrlRoute.Url}'s route executing cancel!");
                            }
                            e.Cancel = result.Item2 == ResultType.Completed;
                            return;
                        }
                    }
                }
                else
                {
                    if (HttpServer.EnableLog(LogType.Info))
                    {
                        HttpServer.Log(LogType.Info, $"Gateway {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.Url} request gateway executing cancel!");
                    }
                    e.Cancel = result.Item2 == ResultType.Completed;
                    return;
                }
            }
            catch (Exception e_)
            {
                if (HttpServer.EnableLog(BeetleX.EventArgs.LogType.Error))
                {
                    HttpServer.Log(BeetleX.EventArgs.LogType.Error,
                        $"Gateway {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.BaseUrl} process error {e_.Message}@{e_.StackTrace}");
                }
            }

        }

        public void Response(HttpResponse response, object result)
        {
            HttpServer.RequestExecuted();
            response.Result(result);
        }

        internal void OnResponseError(EventResponseErrorArgs e)
        {
            if (HttpServer.EnableLog(BeetleX.EventArgs.LogType.Warring))
            {
                HttpServer.Log(BeetleX.EventArgs.LogType.Warring, $"Gateway {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.Url} error {e.Message}");
            }
            if (Pluginer.RequestedEnabled)
            {
                EventRequestCompletedArgs se = new EventRequestCompletedArgs(
                           null,
                           e.Request,
                           e.Response,
                           this,
                           e.ErrorCode,
                           null,
                           1,
                           e.Request.ID,
                           e.Message
                           );
                Pluginer.Requested(se);
            }
            IncrementRequestCompleted(e.Request, e.ErrorCode, 1, null);
            this.Pluginer.ResponseError(e);
            if (e.Result != null)
            {
                e.Response.Result(e.Result);
            }
        }

        internal void OnResponding(RequestAgent request, ArraySegment<byte> data, bool completed)
        {
            if (request.Code == 200)
            {
                EventRespondingArgs e = new EventRespondingArgs();
                e.Completed = completed;
                e.FirstReceive = request.BodyReceives == 1;
                e.Data = data;
                e.Gateway = this;
                e.ResponseStatus = request.ResponseStatus;
                e.Header = request.ResponseHeader;
                e.Request = request.Request;
                e.Server = request.Server;
                this.Pluginer.Responding(e);
            }
        }

        public void IncrementRequestCompleted(HttpRequest request, int code, long time, Servers.ServerAgent server = null)
        {
            HttpServer.RequestExecuted();
            if ((code >= 200 && code < 400) || (code >= 500 && code < 600))
            {
                var stats = Routes.GetUrlStatistics(request.BaseUrl);
                stats.Add(code, time, server);
            }
            else
            {
                if (Routes.UrlStatisticsCount < this.MaxStatsUrls && code != 404)
                {
                    var stats = Routes.GetUrlStatistics(request.BaseUrl);
                    stats.Add(code, time, server);
                }

            }
            Statistics.Add(code, time);
        }

        internal void OnRequestCompleted(Servers.RequestAgent success)
        {
            IncrementRequestCompleted(success.Request, success.Code, success.Time, success.Server);
            if (Pluginer.RequestedEnabled)
                Pluginer.Requested(success.GetEventRequestCompletedArgs());
        }

        public void Open()
        {
            BufferPool.BUFFER_SIZE = BufferSize;
            BufferPool.POOL_MAX_SIZE = PoolMaxSize;
            HttpServer[GATEWAY_TAG] = this;
            HttpServer.ModuleManager.AssemblyLoding += (o, e) =>
            {
                LoadPlugin(e.Assembly);
            };
            if (HttpServer.Options.CacheLogMaxSize < 1000)
                HttpServer.Options.CacheLogMaxSize = 1000;
            mIOQueue = new BeetleX.Dispatchs.DispatchCenter<Tuple<UrlRouteAgent, HttpRequest, HttpResponse>>(OnRouteExecute,
              ThreadQueues);
            HttpServer.Options.UrlIgnoreCase = false;
            HttpServer.Open();
            HttpServer.HttpRequesting += OnRequest;
            LoadConfig();
            PluginCenter.Load(typeof(Gateway).Assembly);
            HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"Gateway server started [v:{this.GetType().Assembly.GetName().Version}]");
            mVerifyTimer = new Timer(OnVerifyTimer, null, 1000, 1000);

        }

        public void Dispose()
        {
            ((IDisposable)HttpServer).Dispose();
        }



        public void SaveConfig()
        {
            try
            {
                GatewayConfig.SaveConfig(this);
                HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"Gateway save config success");
            }
            catch (Exception e_)
            {
                HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"Gateway save config error  {e_.Message}");
            }

        }

        public void LoadConfig()
        {
            try
            {
                var config = GatewayConfig.LoadConfig();
                if (config != null)
                    LoadConfig(config);
            }
            catch (Exception e_)
            {
                HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"Gateway load config error  {e_.Message}");
            }
        }

        public void LoadConfig(GatewayConfig config)
        {
            try
            {
                config.To(this);
                HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"Gateway load config success");
            }
            catch (Exception e_)
            {
                HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"Gateway load config error  {e_.Message}");
            }
        }

        const string GATEWAY_TAG = "GATEWAY_TAG";

        public static Gateway GetGateway(HttpApiServer httpApiServer)
        {
            return (Gateway)httpApiServer[GATEWAY_TAG];
        }


        private BeetleX.Dispatchs.DispatchCenter<Tuple<UrlRouteAgent, HttpRequest, HttpResponse>> mIOQueue;

        public BeetleX.Dispatchs.DispatchCenter<Tuple<UrlRouteAgent, HttpRequest, HttpResponse>> IOQueue => mIOQueue;

        internal void AddRequest(Tuple<UrlRouteAgent, HttpRequest, HttpResponse> e)
        {
            mIOQueue.Enqueue(e, 3);
        }

        private void OnRouteExecute(Tuple<UrlRouteAgent, HttpRequest, HttpResponse> e)
        {
            try
            {
                //if (!requestAgent.Request.Session.IsDisposed)
                //    requestAgent.Execute();
                //else
                //{
                //    requestAgent.Cancel();
                //}
                if (!e.Item2.Session.IsDisposed)
                    e.Item1.Execute(e.Item2, e.Item3);
            }
            catch (Exception e_)
            {
                if (HttpServer.EnableLog(LogType.Error))
                {
                    HttpServer.Log(LogType.Error,
                        $"Gateway {e.Item2.RemoteIPAddress} {e.Item2.Method} {e.Item2.Url} route executing error {e_.Message}{e_.StackTrace}");
                }
            }
        }

    }
}
