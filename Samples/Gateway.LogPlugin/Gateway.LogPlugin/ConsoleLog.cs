using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Bumblebee;
using Bumblebee.Events;
using Bumblebee.Plugins;
using Newtonsoft.Json.Linq;

namespace HttpGateway.LogPlugin
{
    class ConsoleLog : Bumblebee.Plugins.IRequestedHandler
    {
        public string Name => "custom_console_log";

        public string Description => "custom_console_log";

        public PluginLevel Level => PluginLevel.None;


        public void Execute(EventRequestCompletedArgs e)
        {
            Console.WriteLine($"{DateTime.Now} {e.RemoteIPAddress} {e.Gateway.InstanceID} {e.RequestID} {e.SourceUrl} {e.Code}");
        }

        public void Init(Gateway gateway, Assembly assembly)
        {

        }

        public void LoadSetting(JToken setting)
        {

        }

        public object SaveSetting()
        {
            return null;
        }
    }
}
