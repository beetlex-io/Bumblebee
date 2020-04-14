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
using System.Collections.Concurrent;
using System.Linq;
using BeetleX.FastHttpApi.WebSockets;
using Bumblebee.WSAgents;
using System.Reflection;
using System.Runtime;

namespace Bumblebee
{
    public class Gateway : IDisposable
    {

        public const int CACHE_HIT = 311;

        public const int CLUSTER_SERVER_UNAVAILABLE = 590;

        public const int URL_NODE_SERVER_UNAVAILABLE = 591;

        public const int WEBSOCKET_INNER_ERROR = 510;

        public const int WEBSOCKET_SUCCESS = 210;

        public const int GATEWAY_QUEUE_OVERFLOW = 592;

        public const int URL_FILTER_ERROR = 593;

        public const int IP_LIMITS_ERROR = 509;

        public const int URL_LIMITS_ERROR = 595;

        public const int REMOTE_CLIENT_CLOSE = 560;

        public const int SERVER_SOCKET_ERROR = 570;

        public const int SERVER_AGENT_PROCESS_ERROR = 571;

        public const int SERVER_AGENT_QUEUE_OVERFLOW = 572;

        public const int SERVER_MAX_OF_RPS = 573;

        public const int SERVER_NETWORK_READ_STREAM_ERROR = 574;

        public const int SERVER_OTHRER_ERROR_CODE = 580;

        public const int SERVER_PROCESS_ERROR_CODE = 582;

        public int BufferSize { get; set; } = 1024 * 4;

        public int PoolMaxSize { get; set; } = 1024 * 500;

        static Gateway()
        {
            GATEWAY_SERVER_HEDER = Encoding.UTF8.GetBytes("Server: Bumblebee(BeetleX)\r\n");
            KEEP_ALIVE = Encoding.UTF8.GetBytes("Connection: keep-alive\r\n");
        }

        public Gateway()
        {
            HttpServer = new HttpApiServer();
            Routes = new Routes.RouteCenter(this);
            Agents = new Servers.ServerCenter(this);
            this.PluginCenter = new PluginCenter(this);
            this.Pluginer = new Pluginer(this, null);
            Statistics.Server = "Gateway";
            AgentMaxSocketError = 3;
            MaxStatsUrls = 20000;
            AgentMaxConnection = 500;
            AgentRequestQueueSize = 500;
            ThreadQueues = (Environment.ProcessorCount / 2);
            if (ThreadQueues == 0)
                ThreadQueues = 1;
            GatewayQueueSize = Environment.ProcessorCount * 100;
            InstanceID = Guid.NewGuid().ToString("N");
            GATEWAY_VERSION = $"beetlex.io";
            TimeoutFactory = new TimeoutFactory(this);

        }

        public const string GATEWAY_HEADER = "Gateway";

        public static string GATEWAY_VERSION = "";

        public string InstanceID { get; internal set; }

        public int ThreadQueues { get; set; }

        public bool OutputServerAddress { get; set; } = false;

        public bool StatisticsEnabled { get; set; } = true;

        public int AgentMaxConnection { get; set; }

        public int AgentMaxSocketError { get; set; }

        public int AgentRequestQueueSize { get; set; }

        public int GatewayQueueSize { get; set; }

        public int MaxStatsUrls { get; set; }

        public WSMessagesBus WSMessagesBus { get; set; } = new WSMessagesBus();

        public bool WSEnabled { get; set; } = true;

        public event System.EventHandler<Events.EventServerStatusChangeArgs> ServerStatusChanged;

        public event System.EventHandler<Events.EventServerRequestingArgs> ServerHttpRequesting;

        public event System.EventHandler<RequestAgent> ServerHttpRequested;

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

        internal Servers.TimeoutFactory TimeoutFactory { get; private set; }

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

        private void OnHttpDisconnect(object sender, SessionEventArgs e)
        {
            WSMessagesBus.CloseSession(e.Session);
        }

        protected virtual void OnHttpRequest(object sender, EventHttpRequestArgs e)
        {
            try
            {
                e.Cancel = true;
                var ip = e.Request.RemoteIPAddress;
                if (HttpServer.EnableLog(LogType.Info))
                {
                    HttpServer.Log(LogType.Info, $"Gateway {e.Request.ID} {ip} {e.Request.Method} {e.Request.Url}");
                }
                var result = this.Pluginer.Requesting(e.Request, e.Response);
                if (result.Item1)
                {
                    var item = Routes.GetAgent(e.Request);
                    if (e.Request.Data["debug_route"] != null)
                        HttpServer.Log(LogType.Warring, $"DEBUG {e.Request.GetSourceUrl()} route to {item.UrlRoute.Url}");
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
                                AddProxyRequest(new Tuple<UrlRouteAgent, HttpRequest, HttpResponse>(item, e.Request, e.Response));
                                //item.Execute(e.Request, e.Response);
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

        private void OnRouteExecute(Tuple<UrlRouteAgent, HttpRequest, HttpResponse> e)
        {
            try
            {
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

        public void Response(HttpResponse response, object result)
        {
            response.Result(result);
        }

        internal EventServerRequestingArgs OnServerHttpRequesting(Servers.RequestAgent requestAgent)
        {
            EventServerRequestingArgs e = null;
            try
            {

                if (ServerHttpRequesting != null)
                {
                    e = new EventServerRequestingArgs(requestAgent, requestAgent.Request, requestAgent.Response, this);
                    ServerHttpRequesting?.Invoke(this, e);
                }
            }
            catch (Exception e_)
            {
                if (HttpServer.EnableLog(LogType.Error))
                {
                    HttpServer.Log(LogType.Error,
                        $"Gateway {requestAgent.Request.RemoteIPAddress} {requestAgent.Request.Method} {requestAgent.Request.Url} server http requesting event error {e_.Message}{e_.StackTrace}");
                }
            }
            return e;
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
            this.Pluginer.ResponseError(e);
            if (e.Result != null)
            {
                e.Response.Result(e.Result);
            }
            RequestIncrementCompleted(e.Request, e.ErrorCode, 1, null);
        }

        internal void OnRequestCompleted(Servers.RequestAgent success)
        {
            try
            {
                ServerHttpRequested?.Invoke(this, success);
            }
            catch (Exception e_)
            {
                if (HttpServer.EnableLog(BeetleX.EventArgs.LogType.Warring))
                {
                    HttpServer.Log(BeetleX.EventArgs.LogType.Warring,
                        $"Gateway {success.Request.ID} {success.Request.RemoteIPAddress} {success.Request.Method} {success.Request.Url} error {e_.Message}@{e_.StackTrace}");
                }
            }
            HttpServer.IncrementResponsed(success.Request, null, success.Time, success.Code, success.Message);
            RequestIncrementCompleted(success.Request, success.Code, success.Time, success.Server);
            if (Pluginer.RequestedEnabled)
                Pluginer.Requested(success.GetEventRequestCompletedArgs());
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

        public void RequestIncrementCompleted(HttpRequest request, int code, long time, Servers.ServerAgent server = null)
        {
            if (StatisticsEnabled)
            {
                Statistics.Add(code, time);
                if (Statistical(request))
                    Routes.UrlStatisticsDB.Add(code, time, server, request);
            }
            try
            {
                RequestIncrement?.Invoke(this, new EventRequestIncrementArgs(request, code, time, server));
            }
            catch (Exception e_)
            {
                if (HttpServer.EnableLog(LogType.Error))
                {
                    HttpServer.Log(LogType.Error, $"Gateway {request.ID} {request.RemoteIPAddress} {request.Method} {request.Url} request increment event error {e_.Message}@{e_.StackTrace}");
                }
            }
        }

        public event EventHandler<Events.EventRequestIncrementArgs> RequestIncrement;

        internal void OnServerChangeStatus(ServerAgent server, bool available)
        {
            if (ServerStatusChanged != null)
            {
                try
                {
                    Events.EventServerStatusChangeArgs e = new EventServerStatusChangeArgs();
                    e.Server = server;
                    e.Gateway = this;
                    e.Available = available;
                    ServerStatusChanged(this, e);
                }
                catch (Exception e_)
                {
                    if (HttpServer.EnableLog(LogType.Error))
                    {
                        HttpServer.Log(LogType.Error, $"Gateway server change status event error {e_.Message}@{e_.StackTrace} ");
                    }
                }
            }
        }

        protected virtual void OnWebsocketReceive(object sender, WebSocketReceiveArgs e)
        {
            WSMessagesBus.Execute(this, e);
        }

        public void Open()
        {
            HttpServer[GATEWAY_TAG] = this;
            HttpServer.ModuleManager.AssemblyLoding += (o, e) =>
            {
                LoadPlugin(e.Assembly);
            };
            if (HttpServer.Options.CacheLogMaxSize < 1000)
                HttpServer.Options.CacheLogMaxSize = 1000;
            mIOQueue = new BeetleX.Dispatchs.DispatchCenter<Tuple<UrlRouteAgent, HttpRequest, HttpResponse>>(OnRouteExecute,
              ThreadQueues);
            // HttpServer.Options.UrlIgnoreCase = false;
            if (HttpServer.Options.MaxWaitQueue > 0 && HttpServer.Options.MaxWaitQueue < 500)
                HttpServer.Options.MaxWaitQueue = 1000;
            HttpServer.Options.AgentRewrite = true;
            HttpServer.FrameSerializer = new WSAgents.WSAgentDataFrameSerializer();
            if (WSEnabled)
                HttpServer.WebSocketReceive = OnWebsocketReceive;
            HttpServer.WriteLogo = OutputLogo;
            HttpServer.Open();
            HttpServer.HttpRequesting += OnHttpRequest;
            HttpServer.HttpDisconnect += OnHttpDisconnect;
            LoadConfig();
            HeaderTypeFactory.SERVAR_HEADER_BYTES = Encoding.ASCII.GetBytes("Server:" + GATEWAY_VERSION + "\r\n");
            PluginCenter.Load(typeof(Gateway).Assembly);

            mVerifyTimer = new Timer(OnVerifyTimer, null, 1000, 1000);

            ProxyBufferPool = new BufferPool(BufferSize, BufferPool.POOL_SIZE, PoolMaxSize);
        }

        private void OutputLogo()
        {
            AssemblyCopyrightAttribute productAttr = typeof(Gateway).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            var logo = "\r\n";
            logo += " -----------------------------------------------------------------------------\r\n";
            logo +=
@"          ____                  _     _         __   __
         |  _ \                | |   | |        \ \ / /
         | |_) |   ___    ___  | |_  | |   ___   \ V / 
         |  _ <   / _ \  / _ \ | __| | |  / _ \   > <  
         | |_) | |  __/ |  __/ | |_  | | |  __/  / . \ 
         |____/   \___|  \___|  \__| |_|  \___| /_/ \_\ 

                    http and websocket gateway framework   

";
            logo += " -----------------------------------------------------------------------------\r\n";
            logo += $" {productAttr.Copyright}\r\n";
            logo += $" ServerGC    [{GCSettings.IsServerGC}]\r\n";
            logo += $" BeetleX     Version [{typeof(BeetleX.BXException).Assembly.GetName().Version}]\r\n";
            logo += $" FastHttpApi Version [{ typeof(HttpApiServer).Assembly.GetName().Version}] \r\n";
            logo += $" Bumblebee   Version [{ typeof(Gateway).Assembly.GetName().Version}] \r\n";
            logo += " -----------------------------------------------------------------------------\r\n";
            foreach (var item in HttpServer.BaseServer.Options.Listens)
            {
                logo += $" {item}\r\n";
            }
            logo += " -----------------------------------------------------------------------------\r\n";

            HttpServer.Log(LogType.Info, logo);


        }

        public BeetleX.Buffers.BufferPool ProxyBufferPool { get; private set; }

        public void Dispose()
        {
            ((IDisposable)HttpServer).Dispose();
        }

        public void SaveConfig()
        {
            try
            {
                GatewayConfig.SaveConfig(this);
                HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"Gateway save config success");
            }
            catch (Exception e_)
            {
                HttpServer.GetLog(LogType.Error)?.Log(BeetleX.EventArgs.LogType.Error, $"Gateway save config error  {e_.Message}");
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
                HttpServer.GetLog(LogType.Error)?.Log(BeetleX.EventArgs.LogType.Error, $"Gateway load config error  {e_.Message}");
            }
        }

        public void LoadConfig(GatewayConfig config)
        {
            try
            {
                config.To(this);
                HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"Gateway load config success");
            }
            catch (Exception e_)
            {
                HttpServer.GetLog(LogType.Error)?.Log(BeetleX.EventArgs.LogType.Error, $"Gateway load config error  {e_.Message}");
            }
        }

        const string GATEWAY_TAG = "GATEWAY_TAG";

        public static Gateway GetGateway(HttpApiServer httpApiServer)
        {
            return (Gateway)httpApiServer[GATEWAY_TAG];
        }


        private BeetleX.Dispatchs.DispatchCenter<Tuple<UrlRouteAgent, HttpRequest, HttpResponse>> mIOQueue;

        public BeetleX.Dispatchs.DispatchCenter<Tuple<UrlRouteAgent, HttpRequest, HttpResponse>> IOQueue => mIOQueue;

        public void AddProxyRequest(Tuple<UrlRouteAgent, HttpRequest, HttpResponse> e)
        {
            mIOQueue.Enqueue(e, 3);
        }

        private Dictionary<string, string> mStatsExts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool Statistical(HttpRequest request)
        {
            if (mStatsExts.Count == 0 || string.IsNullOrEmpty(request.Ext))
            {
                return true;
            }
            return mStatsExts.ContainsKey(request.Ext);
        }

        public void SetStatisticsExts(string exts)
        {
            if (!string.IsNullOrEmpty(exts))
            {
                Dictionary<string, string> statsExts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in exts.Split(';'))
                {
                    statsExts[item] = item;
                }
                mStatsExts = statsExts;
            }
        }

        public string GetStatisticsExts()
        {
            if (mStatsExts?.Count == 0)
                return "";
            return string.Join(";", mStatsExts.Values.ToArray());
        }


    }
}
