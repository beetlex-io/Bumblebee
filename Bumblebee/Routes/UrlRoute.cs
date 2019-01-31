using BeetleX.FastHttpApi;
using Bumblebee.Filters;
using Bumblebee.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Routes
{
    public class UrlRoute
    {
        public UrlRoute(Gateway gateway, string url)
        {
            Gateway = gateway;
            Filters = new List<IRequestFilter>();
            Url = url;
            mServers = new Servers.UrlRouteServerGroup(gateway, url);

        }

        public string Url { get; private set; }

        public Gateway Gateway { get; internal set; }

        public List<IRequestFilter> Filters { get; private set; }

        public void RemoveFilter(string name)
        {
            for (int i = 0; i < Filters.Count; i++)
            {
                if (Filters[i].Name == name)
                {
                    Filters.RemoveAt(i);
                    mRequestFilters = Filters.ToArray();
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Warring, $"{Url} route remove {name} filter");
                    return;
                }
            }
        }

        public void AddFilter<T>() where T : IRequestFilter, new()
        {
            AddFilter(new T());
        }

        public void AddFilter(string name)
        {
            IRequestFilter filter = Gateway.Filters.GetFilter(name);
            if (filter == null)
            {
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Warring, $"{Url} route add filter error {name} filter not found!");
                return;
            }
            AddFilter(filter);

        }

        public void AddFilter(IRequestFilter filter)
        {

            for (int i = 0; i < Filters.Count; i++)
            {
                if (Filters[i].Name == filter.Name)
                {
                    Filters[i] = filter;
                    return;
                }
            }
            Filters.Add(filter);
            Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"{Url} route add {filter.Name} filter");
            mRequestFilters = Filters.ToArray();
        }

        private IRequestFilter[] mRequestFilters = new IRequestFilter[0];

        public IRequestFilter[] RequestFilters => mRequestFilters;

        public IList<IRequestFilter> GetFilters()
        {
            return mRequestFilters;
        }

        private UrlRouteServerGroup mServers;

        public UrlRouteServerGroup.ServerItem[] Servers
        {
            get
            {
                return mServers.GetServers;
            }
        }

        public UrlRoute AddServer(string host, int wediht = 0)
        {
            mServers.NewOrModify(host, wediht);
            return this;
        }

        public UrlRoute ChangeServerWedith(string host, int wediht = 0)
        {
            mServers.NewOrModify(host, wediht);
            return this;
        }

        public UrlRoute RemoveServer(string host)
        {
            mServers.Remove(host);
            return this;
        }

        public void Verify()
        {
            mServers.Verify();
        }

        private long mRequestHashCode = 1;

        public long GetRequestHashcode()
        {

            return System.Threading.Interlocked.Increment(ref mRequestHashCode);
        }

        public ServerAgent GetServerAgent(HttpRequest request)
        {
            var hashcode = GetRequestHashcode();
            var result = mServers.GetAgent(hashcode);
            return result;
        }

    }
}
