using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using BeetleX.FastHttpApi;
using Bumblebee.Plugins;
using Bumblebee.Events;
using BeetleX.EventArgs;

namespace Bumblebee.Plugins
{
    public class Pluginer
    {
        public Pluginer(Gateway gateway, Routes.UrlRoute urlRoute)
        {
            Gateway = gateway;
            UrlRoute = urlRoute;
        }

        #region requesting

        private ConcurrentDictionary<string, IRequestingHandler> mRequestingHandlerMap = new ConcurrentDictionary<string, IRequestingHandler>();

        private IRequestingHandler[] mRequestingHandlers = new IRequestingHandler[0];

        public PluginInfo[] RequestingInfos => (from a in mRequestingHandlerMap.Values select new PluginInfo(a)).ToArray();

        public void SetRequesting(string name)
        {
            var item = Gateway.PluginCenter.RequestingHandlers.Get(name);
            if (item == null)
            {
                Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {name} requesting handler not found");
            }
            else
            {
                mRequestingHandlerMap[name] = item;
                mRequestingHandlers = (from a in mRequestingHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} set {name} requesting handler ");
            }
        }

        public void RemoveRequesting(string name)
        {
            mRequestingHandlerMap.TryRemove(name, out IRequestingHandler item);
            mRequestingHandlers = (from a in mRequestingHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
            Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} remove {name} requesting handler ");
        }


        public (bool, ResultType) Requesting(HttpRequest request, HttpResponse response)
        {
            var items = mRequestingHandlers;
            if (items.Length > 0)
            {
                Events.EventRequestingArgs e = new Events.EventRequestingArgs(request, response, Gateway);
                for (int i = 0; i < items.Length; i++)
                {
                    if (!e.Cancel && Gateway.PluginCenter.PluginIsEnabled(items[i]))
                    {
                        try
                        {
                            items[i].Execute(e);
                        }
                        catch (Exception e_)
                        {
                            Gateway.HttpServer.GetLog(LogType.Error)?
                                .Log(LogType.Error, $"gateway {request.ID} {request.RemoteIPAddress} {request.Method} {request.GetSourceUrl()} {items[i].Name} requesting plugin process error {e_.Message}@{e_.StackTrace}");
                        }

                    }
                }
                return (!e.Cancel, e.ResultType);
            }
            return (true, ResultType.Completed);
        }


        private void ReloadRequesting()
        {
            foreach (var item in mRequestingHandlerMap.Keys)
            {
                SetRequesting(item);
            }
        }


        #endregion


        #region agent requesting
        private ConcurrentDictionary<string, IAgentRequestingHandler> mAgentRequestingHandlerMap = new ConcurrentDictionary<string, IAgentRequestingHandler>();

        private IAgentRequestingHandler[] mAgentRequestingHandlers = new IAgentRequestingHandler[0];

        public PluginInfo[] AgentRequestingInfos => (from a in mAgentRequestingHandlerMap.Values select new PluginInfo(a)).ToArray();

        public void SetAgentRequesting(string name)
        {
            var item = Gateway.PluginCenter.AgentRequestingHandler.Get(name);
            if (item == null)
            {
                Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {name} agent requesting handler not found");
            }
            else
            {
                mAgentRequestingHandlerMap[name] = item;
                mAgentRequestingHandlers = (from a in mAgentRequestingHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} set {name} agent requesting handler ");
            }
        }

        public void RemoveAgentRequesting(string name)
        {
            mAgentRequestingHandlerMap.TryRemove(name, out IAgentRequestingHandler item);
            mAgentRequestingHandlers = (from a in mAgentRequestingHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
            Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} remove {name} agent requesting handler ");
        }

        public bool AgentRequesting(HttpRequest request, HttpResponse response, Servers.ServerAgent server, Routes.UrlRoute urlRoute)
        {
            var items = mAgentRequestingHandlers;
            if (items.Length > 0)
            {
                Events.EventAgentRequestingArgs e = new Events.EventAgentRequestingArgs(request, response, Gateway, server, urlRoute);
                for (int i = 0; i < items.Length; i++)
                {
                    if (!e.Cancel && Gateway.PluginCenter.PluginIsEnabled(items[i]))
                    {
                        try
                        {
                            items[i].Execute(e);
                        }
                        catch (Exception e_)
                        {
                            Gateway.HttpServer.GetLog(LogType.Error)?
                                .Log(LogType.Error, $"gateway {request.ID} {request.RemoteIPAddress} {request.Method} {request.GetSourceUrl()} {items[i].Name} agent requesting plugin process error {e_.Message}@{e_.StackTrace}");
                        }
                    }
                }
                return !e.Cancel;
            }
            return true;
        }

        private void ReloadAgentRequesting()
        {
            foreach (var item in mAgentRequestingHandlerMap.Keys)
            {
                SetAgentRequesting(item);
            }
        }

        #endregion


        #region header writing

        private ConcurrentDictionary<string, IHeaderWritingHandler> mHeaderWritingHandlerMap = new ConcurrentDictionary<string, IHeaderWritingHandler>();

        private IHeaderWritingHandler[] mHeaderWritingHandlers = new IHeaderWritingHandler[0];

        public PluginInfo[] HeaderWritingInfos => (from a in mHeaderWritingHandlerMap.Values select new PluginInfo(a)).ToArray();

        public void SetHeaderWriting(string name)
        {
            var item = Gateway.PluginCenter.HeaderWritingHandlers.Get(name);
            if (item == null)
            {
                Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {name} header writing handler not found");
            }
            else
            {
                mHeaderWritingHandlerMap[name] = item;
                mHeaderWritingHandlers = (from a in mHeaderWritingHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} set {name} header writing handler ");
            }
        }

        public void RemoveHeaderWriting(string name)
        {
            mHeaderWritingHandlerMap.TryRemove(name, out IHeaderWritingHandler item);
            mHeaderWritingHandlers = (from a in mHeaderWritingHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
            Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} remove {name} header writing handler ");
        }

        public void HeaderWriting(HttpRequest request, HttpResponse response, Header header)
        {
            var items = mHeaderWritingHandlers;
            if (items.Length > 0)
            {
                Events.EventHeaderWritingArgs e = new Events.EventHeaderWritingArgs(request, response, Gateway, header);
                for (int i = 0; i < items.Length; i++)
                {
                    if (Gateway.PluginCenter.PluginIsEnabled(items[i]))
                    {
                        try
                        {
                            items[i].Execute(e);
                        }
                        catch (Exception e_)
                        {
                            Gateway.HttpServer.GetLog(LogType.Error)?
                                .Log(LogType.Error, $"gateway {request.ID} {request.RemoteIPAddress} {request.Method} {request.GetSourceUrl()} {items[i].Name} header writing plugin process error {e_.Message}@{e_.StackTrace}");
                        }
                    }
                }
            }
        }

        private void ReloadHeaderWriting()
        {
            foreach (var item in mHeaderWritingHandlerMap.Keys)
            {
                SetHeaderWriting(item);
            }
        }

        #endregion


        #region requested

        private ConcurrentDictionary<string, IRequestedHandler> mRequestedHandlerMap = new ConcurrentDictionary<string, IRequestedHandler>();

        private IRequestedHandler[] mRequestedHandlers = new IRequestedHandler[0];

        public PluginInfo[] RequestedInfos => (from a in mRequestedHandlerMap.Values select new PluginInfo(a)).ToArray();

        public void SetRequested(string name)
        {
            var item = Gateway.PluginCenter.RequestedHandlers.Get(name);
            if (item == null)
            {
                Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {name} requested handler not found");
            }
            else
            {
                mRequestedHandlerMap[name] = item;
                mRequestedHandlers = (from a in mRequestedHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} set {name} requested handler ");
            }
        }

        public void RemoveRequested(string name)
        {
            mRequestedHandlerMap.TryRemove(name, out IRequestedHandler item);
            mRequestedHandlers = (from a in mRequestedHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
            Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} remove {name} requested handler ");
        }

        public bool RequestedEnabled
        {
            get
            {
                bool enabled = false;
                var items = mRequestedHandlers;
                if (items.Length > 0)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (Gateway.PluginCenter.PluginIsEnabled(items[i]))
                            return true;
                    }
                }
                return enabled;
            }
        }

        public void Requested(Events.EventRequestCompletedArgs e)
        {
            try
            {
                var items = mRequestedHandlers;
                if (items.Length > 0)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (Gateway.PluginCenter.PluginIsEnabled(items[i]))
                        {
                            try
                            {
                                items[i].Execute(e);
                            }
                            catch (Exception e_)
                            {
                                Gateway.HttpServer.GetLog(LogType.Error)?
                                    .Log(LogType.Error, $"gateway {e.RequestID} {e.RemoteIPAddress} {e.Method} {e.SourceUrl} {items[i].Name} requested plugin process error {e_.Message}@{e_.StackTrace}");
                            }
                        }
                    }
                }
            }
            catch (Exception e_)
            {
                if (Gateway.HttpServer.EnableLog(BeetleX.EventArgs.LogType.Error))
                {
                    Gateway.HttpServer.GetLog(LogType.Error)?.Log(BeetleX.EventArgs.LogType.Error, $"gateway {e.RequestID} {e.RemoteIPAddress} {UrlRoute?.Url} process requeted event error {e_.Message}{e_.StackTrace}");
                }
            }
        }

        private void ReloadRequested()
        {
            foreach (var item in mRequestedHandlerMap.Keys)
            {
                SetRequested(item);
            }
        }

        #endregion


        #region response error

        private ConcurrentDictionary<string, IResponseErrorHandler> mResponseErrorHandlerMap = new ConcurrentDictionary<string, IResponseErrorHandler>();

        private IResponseErrorHandler[] responseErrorHandlers = new IResponseErrorHandler[0];

        public PluginInfo[] ResponseErrorInfos => (from a in mResponseErrorHandlerMap.Values select new PluginInfo(a)).ToArray();

        public void SetResponseError(string name)
        {
            var item = Gateway.PluginCenter.ResponseErrorHandlers.Get(name);
            if (item == null)
            {
                Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {name} response handler not found");
            }
            else
            {
                mResponseErrorHandlerMap[name] = item;
                responseErrorHandlers = (from a in mResponseErrorHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} set {name} response error handler ");
            }

        }

        public void RemoveResponseError(string name)
        {
            mResponseErrorHandlerMap.TryRemove(name, out IResponseErrorHandler value);
            responseErrorHandlers = (from a in mResponseErrorHandlerMap.Values orderby (int)a.Level descending select a).ToArray();
            Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} remove {name} response error handler ");
        }

        public void ResponseError(EventResponseErrorArgs e)
        {
            var items = responseErrorHandlers;
            if (items.Length > 0)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (Gateway.PluginCenter.PluginIsEnabled(items[i]))
                    {
                        try
                        {
                            items[i].Exeucte(e);
                        }
                        catch (Exception e_)
                        {
                            Gateway.HttpServer.GetLog(LogType.Error)?
                                .Log(LogType.Error, $"gateway {e.Request.ID} {e.Request.RemoteIPAddress} {e.Request.Method} {e.Request.GetSourceUrl()} {items[i].Name} response error plugin process error {e_.Message}@{e_.StackTrace}");
                        }
                    }
                }
            }
        }

        private void ReloadResponseError()
        {
            foreach (var item in mResponseErrorHandlerMap.Keys)
            {
                SetResponseError(item);
            }
        }

        #endregion


        #region get server

        public IGetServerHandler GetServerHandler { get; set; }

        public void SetGetServerHandler(string name)
        {
            var item = Gateway.PluginCenter.GetServerHandlers.Get(name);
            if (item == null)
            {
                Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {name} get server handler not found");
            }
            else
            {
                GetServerHandler = item;
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} set {name} get server handler ");
            }
        }

        public void RemoveGetServerHandler()
        {
            GetServerHandler = null;
        }

        private void ReloadGetServerHandler()
        {
            var item = GetServerHandler;
            if (item != null)
            {
                SetGetServerHandler(item.Name);
            }
        }

        #endregion


        #region responding

        private ConcurrentDictionary<string, IRespondingHandler> mRespondingHandlerMap = new ConcurrentDictionary<string, IRespondingHandler>();

        private IRespondingHandler[] mRespondingHandlers = new IRespondingHandler[0];

        public PluginInfo[] RespondingInfos => (from a in mRespondingHandlerMap.Values select new PluginInfo(a)).ToArray();

        public void SetResponding(string name)
        {
            var item = Gateway.PluginCenter.RespondingHandlers.Get(name);
            if (item == null)
            {
                Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"gateway {name} responding handler not found");
            }
            else
            {
                mRespondingHandlerMap[name] = item;
                mRespondingHandlers = (from a in mRespondingHandlerMap.Values orderby (int)a.Level descending select (a)).ToArray();
                Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} set {name} responding handler ");
            }
        }

        public void RemoveResponding(string name)
        {
            mRespondingHandlerMap.TryRemove(name, out IRespondingHandler item);
            mRespondingHandlers = (from a in mRespondingHandlerMap.Values orderby (int)a.Level descending select (a)).ToArray();
            Gateway.HttpServer.GetLog(LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"gateway {UrlRoute?.Url} remove {name} responding handler ");
        }


        public void Responding(EventRespondingArgs e)
        {
            var items = mRespondingHandlers;
            if (items.Length > 0)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (Gateway.PluginCenter.PluginIsEnabled(items[i]))
                        items[i].Execute(e);
                }
            }
        }


        private void ReloadResponding()
        {
            foreach (var item in mRespondingHandlerMap.Keys)
            {
                SetResponding(item);
            }
        }


        #endregion




        public Routes.UrlRoute UrlRoute { get; private set; }

        public Gateway Gateway { get; private set; }

        public void Reload()
        {
            this.ReloadResponseError();
            this.ReloadAgentRequesting();
            this.ReloadHeaderWriting();
            this.ReloadRequested();
            this.ReloadRequesting();
            this.ReloadGetServerHandler();
            this.ReloadResponding();
        }
    }
}
