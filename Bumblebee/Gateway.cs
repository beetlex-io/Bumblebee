using System;
using System.Collections.Generic;
using System.Text;
using BeetleX.FastHttpApi;
using System.Threading;
using System.Threading.Tasks;
using Bumblebee.Events;
using Bumblebee.Filters;
using BeetleX.Buffers;
using Bumblebee.Servers;
using BeetleX.EventArgs;

namespace Bumblebee
{
    public class Gateway : IDisposable
    {


        public const int CLUSTER_SERVER_UNAVAILABLE = 590;

        public const int URL_NODE_SERVER_UNAVAILABLE = 591;

        public const int URL_FILTER_ERROR = 592;

        public const int SERVER_SOCKET_ERROR = 570;

        public const int SERER_READ_STREAM_ERROR = 574;

        public const int SERVER_AGENT_PROCESS_ERROR = 571;

        public const int SERVER_MAX_OF_CONNECTIONS = 572;

        public const int SERVER_MAX_OF_RPS = 573;

        public const int SERVER_OTHRER_ERROR_CODE = 580;

        public const int SERVER_PROCESS_ERROR_CODE = 582;

        static Gateway()
        {

            GATEWAY_SERVER_HEDER = Encoding.UTF8.GetBytes("Server: Bumblebee(BeetleX)\r\n");
            KEEP_ALIVE = Encoding.UTF8.GetBytes("Connection: keep-alive\r\n");
        }

        public Gateway()
        {
            HttpServer = new HttpApiServer();
            Filters = new Filters.FilterCenter(this);
            Routes = new Routes.RouteCenter(this);
            Agents = new Servers.ServerCenter(this);
            HttpServer.Options.IOQueueEnabled = true;
            HttpServer.Options.UrlIgnoreCase = false;
            AgentMaxSocketError = 5;
            MaxStatsUrls = 1000;
            AgentMaxConnection = 300;
            AgentRequestQueueLength = 2000;
            int threads = (Environment.ProcessorCount / 2);
            if (threads == 0)
                threads = 1;
            multiThreadDispatcher = new BeetleX.Dispatchs.DispatchCenter<RequestAgent>(OnExecuteRequest,
                Math.Min(threads, 16));
            AgentBufferSize = 1024 * 8;
            AgentBufferPoolSize = 1024 * 200;
        }

        public int AgentBufferSize { get; set; }

        public int AgentBufferPoolSize { get; set; }

        public int AgentMaxConnection { get; set; }

        public int AgentMaxSocketError { get; set; }

        public int AgentRequestQueueLength { get; set; }

        public int MaxStatsUrls { get; set; }

        public void LoadFilters(System.Reflection.Assembly assembly)
        {
            foreach (var t in assembly.GetTypes())
            {
                try
                {
                    if (t.GetInterface("IRequestFilter") != null && !t.IsAbstract && t.IsClass)
                    {
                        var filter = (IRequestFilter)Activator.CreateInstance(t);
                        Filters.Add(filter);
                        HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] filter success");
                    }
                }
                catch (Exception e_)
                {
                    HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.Name} filter error {e_.Message} {e_.StackTrace}");
                }
            }
            Routes.ReloadFilters();
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
                    HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"Verify processing errors {e_.Message}");
            }
            finally
            {
                mVerifyTimer.Change(1000, 1000);
            }
        }

        public HttpApiServer HttpServer { get; internal set; }

        public Filters.FilterCenter Filters { get; private set; }

        public Routes.RouteCenter Routes { get; private set; }

        public Servers.ServerCenter Agents { get; private set; }

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

        public Routes.UrlRoute SetRoute(string url, string hashPattern = null)
        {
            var result = Routes.NewOrGet(url, hashPattern);
            return result;
        }

        public Gateway RemoveRoute(string url)
        {
            Routes.Remove(url);
            return this;
        }

        public Gateway AddFilter<T>() where T : Filters.IRequestFilter, new()
        {
            Filters.Add<T>();
            return this;
        }

        public Gateway HttpOptions(Action<HttpOptions> handler)
        {
            handler?.Invoke(HttpServer.Options);
            return this;
        }

        private void OnRequest(object sender, EventHttpRequestArgs e)
        {
            var url = e.Request.BaseUrl;
            if (url.Length > 2 && url[1] == '_' && url[2] == '_')
                return;
            try
            {
                var ip = e.Request.RemoteIPAddress;
                HttpServer.RequestExecting();
                if (OnRequesting(e.Request, e.Response))
                {
                    var item = Routes.GetAgent(e.Request);
                    if (item == null)
                    {
                        EventResponseErrorArgs error = new EventResponseErrorArgs(
                            e.Request, e.Response, this, "Cluster server unavailable", Gateway.CLUSTER_SERVER_UNAVAILABLE
                            );
                        OnResponseError(error);
                    }
                    else
                    {
                        item.Execute(e.Request, e.Response);
                    }
                }
            }
            catch (Exception e_)
            {
                if (HttpServer.EnableLog(BeetleX.EventArgs.LogType.Error))
                {
                    HttpServer.Log(BeetleX.EventArgs.LogType.Error,
                        $"gateway process {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.BaseUrl} error {e_.Message}@{e_.StackTrace}");
                }
            }
            finally
            {

                e.Cancel = true;
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
                HttpServer.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.Url} error {e.Message}");
            }

            HttpServer.RequestExecuted();
            ResponseError?.Invoke(this, e);
            if (e.Result != null)
            {
                e.Response.Result(e.Result);
            }
        }

        internal void OnRequestCompleted(Servers.RequestAgent success)
        {
            HttpServer.RequestExecuted();
            if (Requested != null)
            {
                EventRequestCompletedArgs e = new EventRequestCompletedArgs(success.UrlRoute, success.Request, success.Response, this, success.Code, success.Server, success.Time);
                Requested(this, e);
            }
        }

        public event EventHandler<EventRequestCompletedArgs> Requested;

        public void Open()
        {
            BufferPool.BUFFER_SIZE = AgentBufferSize;
            BufferPool.POOL_MAX_SIZE = AgentBufferPoolSize;
            HttpServer[GATEWAY_TAG] = this;
            HttpServer.ModuleManager.AssemblyLoding += (o, e) =>
            {
                LoadFilters(e.Assembly);
            };

            HttpServer.Open();
            HttpServer.HttpRequesting += OnRequest;
            LoadConfig();
            HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway server started [v:{this.GetType().Assembly.GetName().Version}]");
            mVerifyTimer = new Timer(OnVerifyTimer, null, 1000, 1000);

        }

        public void Dispose()
        {
            ((IDisposable)HttpServer).Dispose();
        }

        public event EventHandler<EventResponseErrorArgs> ResponseError;

        public event EventHandler<EventRequestingArgs> Requesting;

        public event EventHandler<EventAgentRequestingArgs> AgentRequesting;

        protected bool OnRequesting(HttpRequest request, HttpResponse response)
        {

            if (Requesting != null)
            {
                EventRequestingArgs e = new EventRequestingArgs(request, response, this);
                e.Cancel = false;
                Requesting?.Invoke(this, e);
                return !e.Cancel;
            }
            return true;
        }


        internal bool OnAgentRequesting(HttpRequest request, HttpResponse response, Servers.ServerAgent server, Routes.UrlRoute urlRoute)
        {
            if (AgentRequesting != null)
            {
                EventAgentRequestingArgs e = new EventAgentRequestingArgs(request, response, this, server, urlRoute);
                e.Cancel = false;
                AgentRequesting?.Invoke(this, e);
                return !e.Cancel;
            }
            return true;
        }

        public void SaveConfig()
        {
            try
            {
                GatewayConfig.SaveConfig(this);
                HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway save config success");
            }
            catch (Exception e_)
            {
                HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway save config error  {e_.Message}");
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
                HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load config error  {e_.Message}");
            }
        }

        public void LoadConfig(GatewayConfig config)
        {
            try
            {
                config.To(this);
                HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load config success");
            }
            catch (Exception e_)
            {
                HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load config error  {e_.Message}");
            }
        }

        const string GATEWAY_TAG = "GATEWAY_TAG";

        public static Gateway GetGateway(HttpApiServer httpApiServer)
        {
            return (Gateway)httpApiServer[GATEWAY_TAG];
        }

        #region header write events

        public event EventHandler<Events.EventHeaderWriter> HeaderWrited;

        internal void OnHeaderWrited(HttpRequest request, HttpResponse response, PipeStream stream, Servers.ServerAgent server)
        {
            if (HeaderWrited != null)
            {
                Events.EventHeaderWriter eventHeaderWriter = new EventHeaderWriter(request, response, this, stream);
                eventHeaderWriter.Server = server;
                HeaderWrited?.Invoke(this, eventHeaderWriter);
            }
        }

        public event EventHandler<Events.EventHeaderWriting> HeaderWriting;

        internal bool OnHeaderWriting(HttpRequest request, HttpResponse response, PipeStream stream, Servers.ServerAgent server, string name, string value)
        {
            if (HeaderWriting != null)
            {
                Events.EventHeaderWriting eventHeaderWriting = new EventHeaderWriting(request, response, this, stream);
                eventHeaderWriting.Name = name;
                eventHeaderWriting.Value = value;
                eventHeaderWriting.Server = server;
                HeaderWriting?.Invoke(this, eventHeaderWriting);
                return !eventHeaderWriting.Cancel;
            }
            return true;
        }

        #endregion


        private BeetleX.Dispatchs.DispatchCenter<RequestAgent> multiThreadDispatcher;

        internal void AddRequest(RequestAgent requestAgent)
        {
            multiThreadDispatcher.Enqueue(requestAgent, 10);
        }

        private void OnExecuteRequest(RequestAgent requestAgent)
        {
            try
            {
                requestAgent.Execute();
            }
            catch (Exception e_)
            {
                if (HttpServer.EnableLog(LogType.Error))
                {
                    HttpServer.Log(LogType.Error, $"gateway {requestAgent.Request.Url} route to {requestAgent.Server.Uri} error {e_.Message}{e_.StackTrace}");
                }
            }
        }

    }
}
