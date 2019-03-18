using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    public interface IPlugin
    {
        string Name { get; }

        void Init(Gateway gateway, System.Reflection.Assembly assembly);

        string Description { get; }
    }



    public class PluginInfo
    {
        public PluginInfo()
        {

        }
        public PluginInfo(IPlugin plugin)
        {
            Name = plugin.Name;
            Version = plugin.GetType().Assembly.GetName().Version.ToString();
            Assembly = plugin.GetType().Assembly.GetName().Name;
            Description = plugin.Description;
        }

        public string Name { get; set; }

        public string Version { get; set; }

        public string Assembly { get; set; }

        public string Description { get; set; }
    }
}
