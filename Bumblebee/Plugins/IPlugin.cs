using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{

    public interface IPluginStatus
    {
        bool Enabled { get; set; }
    }


    public interface IPlugin
    {
        string Name { get; }

        void Init(Gateway gateway, System.Reflection.Assembly assembly);

        string Description { get; }

        void LoadSetting(JToken setting);

        Object SaveSetting();

    }

    public enum PluginType
    {
        Requesting,
        Requested,
        ResponseError,
        Loader,
        HeaderWriting,
        GetAgentServer,
        AgentRequesting
    }





    public class PluginInfo
    {

        public PluginInfo(IPlugin plugin)
        {
            if (plugin is IRequestedHandler)
                Type = PluginType.Requested.ToString();
            if (plugin is IRequestingHandler)
                Type = PluginType.Requesting.ToString();
            if (plugin is IGatewayLoader)
                Type = PluginType.Loader.ToString();
            if (plugin is IHeaderWritingHandler)
                Type = PluginType.HeaderWriting.ToString();
            if (plugin is IGetServerHandler)
                Type = PluginType.GetAgentServer.ToString();
            if (plugin is IAgentRequestingHandler)
                Type = PluginType.AgentRequesting.ToString();
            Name = plugin.Name;
            Version = plugin.GetType().Assembly.GetName().Version.ToString();
            Assembly = plugin.GetType().Assembly.GetName().Name;
            Description = plugin.Description;
            if (plugin is IPluginStatus status)
            {
                Status = true;
                Enabled = status.Enabled;
            }
            else
            {
                Enabled = true;
            }
        }

        public string Type { get; set; }

        public string Name { get; set; }

        public bool Status { get; set; }

        public bool Enabled { get; set; }

        public string Version { get; set; }

        public string Assembly { get; set; }

        public string Description { get; set; }
    }
}
