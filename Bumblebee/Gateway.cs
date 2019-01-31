using System;
using System.Collections.Generic;
using System.Text;
using BeetleX.FastHttpApi;
using System.Threading;
using System.Threading.Tasks;
using Bumblebee.Events;
using Bumblebee.Filters;

namespace Bumblebee
{
    public class Gateway : IDisposable
    {

        static Gateway()
        {

            GATEWAY_SERVER_HEDER = Encoding.UTF8.GetBytes("Server: Bumblebee(BeetleX)\r\n");
        }

        public Gateway()
        {
            HttpServer = new HttpApiServer();
            HttpServer.Options.Port = 8080;
            Filters = new Filters.FilterCenter(this);
            Routes = new Routes.RouteCenter(this);
            Agents = new Servers.ServerCenter(this);

        }


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
                    HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name} filter error {e_.Message}");
                }
            }
        }

        private System.Threading.Timer mVerifyTimer;

        internal static byte[] GATEWAY_SERVER_HEDER;

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

        public Statistics Statistics { get; private set; } = new Statistics();

        public Servers.ServerAgent AddServer(string host, int maxConnections = 200)
        {
            return Agents.Add(host, maxConnections);

        }

        public Gateway RemoveServer(string host)
        {
            Agents.Remove(host);
            Routes.RemoveServer(host);
            return this;
        }

        public Routes.UrlRoute AddRoute(string url, params string[] hosts)
        {
            var result = Routes.NewOrGet(url);
            if (hosts != null)
            {
                foreach (var item in hosts)
                {
                    result.AddServer(item);
                }
            }
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
            try
            {
                if (OnRequesting(e.Request, e.Response))
                {
                    var item = Routes.GetAgent(e.Request.BaseUrl);
                    if (item == null)
                    {
                        BadGateway result = new BadGateway("Cluster server unavailable");
                        EventResponseErrorArgs error = new EventResponseErrorArgs(
                            e.Request, e.Response, result, BadGateway.CLUSTER_SERVER_UNAVAILABLE
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

        internal void OnRequestCompleted(Servers.RequestAgent agent)
        {
            this.Statistics.Add(agent.Code);
            EventRequestCompletedArgs e = new EventRequestCompletedArgs(agent.Request, agent.Response, agent.Code);
            Requested?.Invoke(this, e);
        }

        public event EventHandler<EventRequestCompletedArgs> Requested;

        public void Open()
        {
            HttpServer.Open();
            HttpServer.HttpRequesting += OnRequest;
            HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway server started");
            mVerifyTimer = new Timer(OnVerifyTimer, null, 1000, 1000);
        }

        public void Dispose()
        {
            ((IDisposable)HttpServer).Dispose();
        }

        public event EventHandler<EventResponseErrorArgs> ResponseError;

        internal void OnResponseError(EventResponseErrorArgs e)
        {
            ResponseError?.Invoke(this, e);
            if (e.Result != null)
            {
                e.Response.Result(e.Result);
            }
        }

        public event EventHandler<EventRequestingArgs> Requesting;

        protected bool OnRequesting(HttpRequest request, HttpResponse response)
        {
            if (Requesting != null)
            {
                EventRequestingArgs e = new EventRequestingArgs(request, response);
                e.Cancel = false;
                Requesting?.Invoke(this, e);
                return !e.Cancel;
            }
            return true;
        }
    }
}
