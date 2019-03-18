using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    public class PluginCenter
    {

        public PluginCenter(Gateway gateway)
        {
            Gateway = gateway;
            GetServerHandlers = new PluginGroup<IGetServerHandler>(gateway);
            LoaderHandlers = new PluginGroup<IGatewayLoader>(gateway);
            AgentRequestingHandler = new PluginGroup<IAgentRequestingHandler>(gateway);
            HeaderWritingHandlers = new PluginGroup<IHeaderWritingHandler>(gateway);
            ResponseErrorHandlers = new PluginGroup<IResponseErrorHandler>(gateway);
            RequestingHandlers = new PluginGroup<IRequestingHandler>(gateway);
            RequestedHandlers = new PluginGroup<IRequestedHandler>(gateway);
        }


        public PluginGroup<IRequestedHandler> RequestedHandlers { get; private set; }

        public PluginGroup<IRequestingHandler> RequestingHandlers { get; private set; }

        public PluginGroup<IResponseErrorHandler> ResponseErrorHandlers { get; private set; }

        public PluginGroup<IHeaderWritingHandler> HeaderWritingHandlers { get; set; }

        public PluginGroup<IAgentRequestingHandler> AgentRequestingHandler { get; private set; }

        public PluginGroup<IGetServerHandler> GetServerHandlers { get; private set; }

        public PluginGroup<IGatewayLoader> LoaderHandlers { get; private set; }

        public Gateway Gateway { get; private set; }

        public void Load(System.Reflection.Assembly assembly)
        {
            foreach (var t in assembly.GetTypes())
            {

                if (t.GetInterface("IAgentRequestingHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        var handler = (IAgentRequestingHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        AgentRequestingHandler.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] agent requesting handler success");
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.Name} agent requesting handler error {e_.Message} {e_.StackTrace}");
                    }
                }


                if (t.GetInterface("IHeaderWritingHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        var handler = (IHeaderWritingHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        HeaderWritingHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] header writing handler success");
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.Name} header writing handler error {e_.Message} {e_.StackTrace}");
                    }
                }


                if (t.GetInterface("IResponseErrorHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        var handler = (IResponseErrorHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        ResponseErrorHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] response error handler success");
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.Name} response error handler error {e_.Message} {e_.StackTrace}");
                    }
                }


                if (t.GetInterface("IRequestingHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        var handler = (IRequestingHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        RequestingHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] requesting handler success");
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.Name} requesting handler error {e_.Message} {e_.StackTrace}");
                    }
                }

                if (t.GetInterface("IRequestedHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        var handler = (IRequestedHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        RequestedHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] requested handler success");
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.Name} requested handler error {e_.Message} {e_.StackTrace}");
                    }
                }

                if (t.GetInterface("IGetServerHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        var handler = (IGetServerHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        GetServerHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] route get server handler success");
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.Name} route get server handler error {e_.Message} {e_.StackTrace}");
                    }
                }
                if (t.GetInterface("IGatewayLoader") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        var handler = (IGatewayLoader)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        LoaderHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] gateway loader handler success");
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.Name} gateway loader handler error {e_.Message} {e_.StackTrace}");
                    }
                }
            }
        }
    }
}
