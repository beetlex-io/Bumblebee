using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Bumblebee.Plugins
{
    public class PluginCenter
    {

        public PluginCenter(Gateway gateway)
        {
            Gateway = gateway;
            GetServerHandlers = new PluginGroup<IGetServerHandler>(PluginType.GetAgentServer, gateway);
            LoaderHandlers = new PluginGroup<IGatewayLoader>(PluginType.Loader, gateway);
            AgentRequestingHandler = new PluginGroup<IAgentRequestingHandler>(PluginType.AgentRequesting, gateway);
            HeaderWritingHandlers = new PluginGroup<IHeaderWritingHandler>(PluginType.HeaderWriting, gateway);
            ResponseErrorHandlers = new PluginGroup<IResponseErrorHandler>(PluginType.ResponseError, gateway);
            RequestingHandlers = new PluginGroup<IRequestingHandler>(PluginType.Requesting, gateway);
            RequestedHandlers = new PluginGroup<IRequestedHandler>(PluginType.Requested, gateway);
            mPluginSettingFolder = AppDomain.CurrentDomain.BaseDirectory + "plugin_settings" + System.IO.Path.DirectorySeparatorChar;
            if (!System.IO.Directory.Exists(mPluginSettingFolder))
                System.IO.Directory.CreateDirectory(mPluginSettingFolder);
        }

        public PluginInfo[] ListPluginInfos()
        {

            return (from a in mPlugins.Values select new PluginInfo(a)).ToArray();
        }

        private string mPluginSettingFolder;

        private Dictionary<string, IPlugin> mPlugins = new Dictionary<string, IPlugin>();

        public Dictionary<string, bool> PluginsStatus { get; internal set; } = new Dictionary<string, bool>();

        public IPlugin GetPlugin(string name)
        {
            mPlugins.TryGetValue(name, out IPlugin result);
            return result;
        }

        public bool PluginIsEnabled(IPlugin item)
        {
            if (item is IPluginStatus status)
            {
                return status.Enabled;
            }
            else
            {
                return true;
            }

        }

        public void SetPluginEnabled(IPlugin item, bool enabled)
        {
            if (item is IPluginStatus)
            {
                PluginsStatus[item.Name] = enabled;
                ((IPluginStatus)item).Enabled = enabled;
                Gateway.SaveConfig();
            }
        }

        private void InitPluginEnabled(IPlugin item)
        {
            if (item is IPluginStatus && PluginsStatus.TryGetValue(item.Name, out bool enabled))
            {
                ((IPluginStatus)item).Enabled = enabled;
            }
        }

        public void SaveSetting(IPlugin plugin, bool overwrite = true)
        {
            if (plugin == null)
                return;
            try
            {
                object config = plugin.SaveSetting();
                if (config != null)
                {

                    string file = mPluginSettingFolder + plugin.Name.Replace(" ", "_") + ".json";
                    if (!overwrite && System.IO.File.Exists(file))
                        return;
                    string data = Newtonsoft.Json.JsonConvert.SerializeObject(config);
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(file, false, Encoding.UTF8))
                    {
                        writer.Write(data);
                        writer.Flush();
                    }
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"{plugin.Name} setting save success");
                }
            }
            catch (Exception e_)
            {
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"{plugin.Name} setting save error {e_.Message}@{e_.StackTrace}");
            }
        }

        public void LoadSetting(IPlugin plugin)
        {
            if (plugin == null)
                return;
            try
            {
                string file = mPluginSettingFolder + plugin.Name.Replace(" ", "_") + ".json";
                if (System.IO.File.Exists(file))
                {
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(file))
                    {
                        string data = reader.ReadToEnd();
                        JToken token = (JToken)Newtonsoft.Json.JsonConvert.DeserializeObject(data);
                        plugin.LoadSetting(token);
                    }
                }
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"{plugin.Name} setting load success");
            }
            catch (Exception e_)
            {
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"{plugin.Name} setting load error {e_.Message}@{e_.StackTrace}");
            }
        }

        public PluginGroup<IRequestedHandler> RequestedHandlers { get; private set; }

        public PluginGroup<IRequestingHandler> RequestingHandlers { get; private set; }

        public PluginGroup<IResponseErrorHandler> ResponseErrorHandlers { get; private set; }

        public PluginGroup<IHeaderWritingHandler> HeaderWritingHandlers { get; set; }

        public PluginGroup<IAgentRequestingHandler> AgentRequestingHandler { get; private set; }

        public PluginGroup<IGetServerHandler> GetServerHandlers { get; private set; }

        public PluginGroup<IGatewayLoader> LoaderHandlers { get; private set; }

        public Gateway Gateway { get; private set; }

        public int Load(System.Reflection.Assembly assembly)
        {
            int count = 0;
            foreach (var t in assembly.GetTypes())
            {
                if (t.GetInterface("IAgentRequestingHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        count++;
                        var handler = (IAgentRequestingHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        AgentRequestingHandler.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] agent requesting handler success");
                        RouteBinderAttribute routeBinderAttribute = t.GetCustomAttribute<RouteBinderAttribute>(false);
                        if (routeBinderAttribute != null)
                        {
                            if (string.IsNullOrEmpty(routeBinderAttribute.RouteUrl))
                            {
                                Gateway.Pluginer.SetAgentRequesting(handler.Name);
                            }
                            else
                            {
                                var route = Gateway.Routes.NewOrGet(routeBinderAttribute.RouteUrl, null, null, routeBinderAttribute.ApiLoader);
                                route.Pluginer.SetAgentRequesting(handler.Name);
                            }
                        }
                        mPlugins[handler.Name] = handler;
                        InitPluginEnabled(handler);
                        LoadSetting(handler);
                        SaveSetting(handler, false);

                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.GetType()} agent requesting handler error {e_.Message} {e_.StackTrace}");
                    }

                }


                if (t.GetInterface("IHeaderWritingHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        count++;
                        var handler = (IHeaderWritingHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        HeaderWritingHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] header writing handler success");
                        RouteBinderAttribute routeBinderAttribute = t.GetCustomAttribute<RouteBinderAttribute>(false);
                        if (routeBinderAttribute != null)
                        {
                            if (string.IsNullOrEmpty(routeBinderAttribute.RouteUrl))
                            {
                                Gateway.Pluginer.SetHeaderWriting(handler.Name);
                            }
                            else
                            {
                                var route = Gateway.Routes.NewOrGet(routeBinderAttribute.RouteUrl, null, null, routeBinderAttribute.ApiLoader);
                                route.Pluginer.SetHeaderWriting(handler.Name);
                            }
                        }
                        mPlugins[handler.Name] = handler;
                        InitPluginEnabled(handler);
                        LoadSetting(handler);
                        SaveSetting(handler, false);
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.GetType()} header writing handler error {e_.Message} {e_.StackTrace}");
                    }
                }


                if (t.GetInterface("IResponseErrorHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        count++;
                        var handler = (IResponseErrorHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        ResponseErrorHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] response error handler success");
                        mPlugins[handler.Name] = handler;
                        InitPluginEnabled(handler);
                        LoadSetting(handler);
                        SaveSetting(handler, false);
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.GetType()} response error handler error {e_.Message} {e_.StackTrace}");
                    }
                }


                if (t.GetInterface("IRequestingHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        count++;
                        var handler = (IRequestingHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        RequestingHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] requesting handler success");
                        RouteBinderAttribute routeBinderAttribute = t.GetCustomAttribute<RouteBinderAttribute>(false);
                        if (routeBinderAttribute != null)
                        {
                            if (string.IsNullOrEmpty(routeBinderAttribute.RouteUrl))
                            {
                                Gateway.Pluginer.SetRequesting(handler.Name);
                            }
                            else
                            {
                                var route = Gateway.Routes.NewOrGet(routeBinderAttribute.RouteUrl, null, null, routeBinderAttribute.ApiLoader);
                                route.Pluginer.SetRequesting(handler.Name);
                            }
                        }
                        mPlugins[handler.Name] = handler;
                        InitPluginEnabled(handler);
                        LoadSetting(handler);
                        SaveSetting(handler, false);
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.GetType()} requesting handler error {e_.Message} {e_.StackTrace}");
                    }
                }

                if (t.GetInterface("IRequestedHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        count++;
                        var handler = (IRequestedHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        RequestedHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] requested handler success");
                        RouteBinderAttribute routeBinderAttribute = t.GetCustomAttribute<RouteBinderAttribute>(false);
                        if (routeBinderAttribute != null)
                        {
                            if (string.IsNullOrEmpty(routeBinderAttribute.RouteUrl))
                            {
                                Gateway.Pluginer.SetRequested(handler.Name);
                            }
                            else
                            {
                                var route = Gateway.Routes.NewOrGet(routeBinderAttribute.RouteUrl, null, null, routeBinderAttribute.ApiLoader);
                                route.Pluginer.SetRequested(handler.Name);
                            }
                        }
                        mPlugins[handler.Name] = handler;
                        InitPluginEnabled(handler);
                        LoadSetting(handler);
                        SaveSetting(handler, false);
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.GetType()} requested handler error {e_.Message} {e_.StackTrace}");
                    }
                }

                if (t.GetInterface("IGetServerHandler") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        count++;
                        var handler = (IGetServerHandler)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        GetServerHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.Name}[{assembly.GetName().Version}] route get server handler success");
                        RouteBinderAttribute routeBinderAttribute = t.GetCustomAttribute<RouteBinderAttribute>(false);
                        if (routeBinderAttribute != null)
                        {
                            if (string.IsNullOrEmpty(routeBinderAttribute.RouteUrl))
                            {
                                Gateway.Pluginer.SetRequested(handler.Name);
                            }
                            else
                            {
                                var route = Gateway.Routes.NewOrGet(routeBinderAttribute.RouteUrl, null, null, routeBinderAttribute.ApiLoader);
                                route.Pluginer.GetServerHandler = handler;
                            }
                        }
                        mPlugins[handler.Name] = handler;
                        InitPluginEnabled(handler);
                        LoadSetting(handler);
                        SaveSetting(handler, false);
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.GetType()} route get server handler error {e_.Message} {e_.StackTrace}");
                    }
                }
                if (t.GetInterface("IGatewayLoader") != null && !t.IsAbstract && t.IsClass)
                {
                    try
                    {
                        count++;
                        var handler = (IGatewayLoader)Activator.CreateInstance(t);
                        handler.Init(Gateway, assembly);
                        LoaderHandlers.Add(handler);
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway load {t.GetType()}[{assembly.GetName().Version}] gateway loader handler success");
                        mPlugins[handler.Name] = handler;
                        InitPluginEnabled(handler);
                        LoadSetting(handler);
                        SaveSetting(handler, false);
                    }
                    catch (Exception e_)
                    {
                        Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway load {t.GetType()} gateway loader handler error {e_.Message} {e_.StackTrace}");
                    }
                }
            }

            return count;
        }


    }
}
