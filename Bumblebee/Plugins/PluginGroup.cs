using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bumblebee.Plugins
{
    public class PluginGroup<T> where T : IPlugin
    {

        public PluginGroup(PluginType type, Gateway gateway)
        {

            Gateway = gateway;
        }

        public PluginInfo[] Infos
        {
            get
            {
                return (from a in mPlugins.Values
                        select new PluginInfo(a)).ToArray();
                     ;
            }
        }

        public PluginType Type { get; private set; }

        private ConcurrentDictionary<string, T> mPlugins = new ConcurrentDictionary<string, T>();

        public Gateway Gateway { get; private set; }

        public IPlugin Remove(string name)
        {
            mPlugins.TryRemove(name, out T item);
            return item;
        }
        public void Add(T requestFilter)
        {
            mPlugins[requestFilter.Name] = requestFilter;
        }

        public T Get(string name)
        {
            T result;
            mPlugins.TryGetValue(name, out result);
            return result;
        }
    }
}
