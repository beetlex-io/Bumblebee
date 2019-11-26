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

namespace Bumblebee
{
    public class Gateway : IDisposable
    {

        public const int CACHE_HIT = 311;

        public const int CLUSTER_SERVER_UNAVAILABLE = 590;

        public const int URL_NODE_SERVER_UNAVAILABLE = 591;

        public const int GATEWAY_QUEUE_OVERFLOW = 592;

        public const int URL_FILTER_ERROR = 593;

        public const int IP_LIMITS_ERROR = 594;

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

        public int PoolMaxSize { get; set; } = 1024 * 10;

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
            AgentMaxConnection = 200;
            AgentRequestQueueSize = 2000;
            ThreadQueues = (Environment.ProcessorCount / 2);
            if (ThreadQueues == 0)
                ThreadQueues = 1;
            GatewayQueueSize = Environment.ProcessorCount * 100;
            InstanceID = Guid.NewGuid().ToString("N");
            GATEWAY_VERSION = $"BeetleX/Bumblebee[{GetType().Assembly.GetName().Version.ToString()}]";
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

        public event System.EventHandler<Events.EventServerStatusChangeArgs> ServerStatusChanged;

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
                    HttpServer.Log(LogType.Info, $"Gateway {e.Request.ID} {ip} {e.Request.Method} {e.Request.Url}");
                }
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

        public void Response(HttpResponse response, object result)
        {
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
            RequestIncrementCompleted(e.Request, e.ErrorCode, 1, null);
            this.Pluginer.ResponseError(e);
            if (e.Result != null)
            {
                e.Response.Result(e.Result);
            }
        }

        internal void OnRequestCompleted(Servers.RequestAgent success)
        {
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
            HttpServer.IncrementResponsed(request, null, time, code, null);
            if (StatisticsEnabled)
            {
                Statistics.Add(code, time);
                if (Statistical(request))
                    Routes.UrlStatisticsDB.Add(code, time, server, request);
            }
            try
            {
                RequestIncrement.Invoke(this, new EventRequestIncrementArgs(request, code, time, server));
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
            HttpServer.Options.UrlIgnoreCase = false;
            HttpServer.Options.AgentRewrite = true;
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
