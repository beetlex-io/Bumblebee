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
            Routes = new Routes.RouteCenter(this);
            Agents = new Servers.ServerCenter(this);
            this.PluginCenter = new PluginCenter(this);
            this.Pluginer = new Pluginer(this, null);
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

        public PluginCenter PluginCenter { get; private set; }

        public void LoadPlugin(System.Reflection.Assembly assembly)
        {
            this.PluginCenter.Load(assembly);
            Routes.ReloadPlugin();
            this.Pluginer.Reload();
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

        public Pluginer Pluginer { get; private set; }

        public HttpApiServer HttpServer { get; internal set; }

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
                if (this.Pluginer.Requesting(e.Request, e.Response))
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
                        if (item.UrlRoute.Pluginer.Requesting(e.Request, e.Response))
                        {
                            item.Execute(e.Request, e.Response);
                        }
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
            // ResponseError?.Invoke(this, e);
            this.Pluginer.ResponseError(e);
            if (e.Result != null)
            {
                e.Response.Result(e.Result);
            }
        }


        internal void OnRequestCompleted(Servers.RequestAgent success)
        {
            HttpServer.RequestExecuted();
            Pluginer.Requested(success);
        }

        public void Open()
        {
            BufferPool.BUFFER_SIZE = AgentBufferSize;
            BufferPool.POOL_MAX_SIZE = AgentBufferPoolSize;
            HttpServer[GATEWAY_TAG] = this;
            HttpServer.ModuleManager.AssemblyLoding += (o, e) =>
            {
                LoadPlugin(e.Assembly);
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
