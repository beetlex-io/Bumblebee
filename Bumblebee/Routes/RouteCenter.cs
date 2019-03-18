using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;
using BeetleX.FastHttpApi;

namespace Bumblebee.Routes
{
    public class RouteCenter
    {
        public RouteCenter(Gateway gateway)
        {
            Gateway = gateway;
            Default = new UrlRoute(gateway, "*");
        }

        private long mVersion;

        private List<UrlRoute> mMatchRoutes = new List<UrlRoute>();

        private ConcurrentDictionary<string, UrlRoute> mUrlRoutes = new ConcurrentDictionary<string, UrlRoute>();

        private UrlRouteAgentDictionary urlRouteAgent = new UrlRouteAgentDictionary();

        private void OnUpdateUrlTable()
        {
            List<UrlRoute> urls = new List<UrlRoute>();
            urls.AddRange(mUrlRoutes.Values);
            urls.Sort((x, y) => y.UrlPattern.Length.CompareTo(x.UrlPattern.Length));
            mMatchRoutes = urls;
            Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway update route url data table");
        }

        public void ReloadPlugin()
        {
            try
            {
                foreach (var item in mUrlRoutes.Values)
                    item.Pluginer.Reload();
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway route reload plugin");
            }
            catch (Exception e_)
            {
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway route reload plugin error {e_.Message} {e_.StackTrace}");
            }
        }

        public UrlRoute GetRoute(string url)
        {
            if (url == "*")
                return Default;
            mUrlRoutes.TryGetValue(url, out UrlRoute urlRoute);
            return urlRoute;
        }

        public void RemoveServer(string host)
        {
            foreach (var item in mUrlRoutes.Values)
                item.RemoveServer(host);
            Default.RemoveServer(host);
        }

        private UrlRouteAgent MatchAgent(HttpRequest request)
        {
            string url = request.BaseUrl;
            UrlRouteAgent agent = new UrlRouteAgent();
            agent.Version = this.Version;
            agent.Url = url;
            var urls = mMatchRoutes;
            for (int i = 0; i < urls.Count; i++)
            {
                var routeItem = urls[i];
                if (System.Text.RegularExpressions.Regex.IsMatch(url, routeItem.UrlPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (routeItem.Servers.Length == 0)
                        continue;
                    if (string.IsNullOrEmpty(routeItem.Host))
                    {
                        agent.UrlRoute = routeItem;
                        return agent;
                    }
                    else
                    {
                        if (string.Compare(routeItem.Host, request.Host, true) == 0)
                        {
                            agent.UrlRoute = routeItem;
                            return agent;
                        }
                    }
                }
            }
            agent.UrlRoute = Default;
            return agent;
        }

        public void Verify()
        {
            foreach (var item in mUrlRoutes.Values)
                item.Verify();
            Default.Verify();
        }

        public UrlRouteAgent GetAgent(HttpRequest request)
        {
            ulong urlcode = GetUrlCode(string.Concat(request.Host, "|", request.BaseUrl));
            var item = urlRouteAgent.GetAgent(urlcode);
            if (item == null || item.Version != Version)
            {
                item = MatchAgent(request);
                urlRouteAgent.SetAgent(urlcode, item);
            }
            return item;
        }

        public Gateway Gateway { get; private set; }

        public long Version { get { return mVersion; } }

        public List<UrlRoute> Urls
        {
            get
            {
                return mMatchRoutes;
            }
        }

        private ulong GetUrlCode(string url)
        {
            ulong value = (ulong)Math.Abs(url.ToLower().GetHashCode()) << 16;
            value |= (ushort)url.Length;
            return value;
        }

        public UrlRoute Default
        {
            get; private set;
        }

        public UrlRoute Remove(string url)
        {
            if (mUrlRoutes.TryRemove(url, out UrlRoute item))
            {
                UpdateUrlTable();
            }
            return item;
        }

        public void UpdateUrlTable()
        {
            OnUpdateUrlTable();
            System.Threading.Interlocked.Increment(ref mVersion);
        }

        public UrlRoute NewOrGet(string url, string hashPattern = null)
        {
            if (url == "*")
            {
                Default.BuildHashPattern(hashPattern);
                return Default;
            }
            if (!mUrlRoutes.TryGetValue(url, out UrlRoute item))
            {
                item = new UrlRoute(Gateway, url);
                mUrlRoutes[url] = item;
                UpdateUrlTable();

            }
            item.BuildHashPattern(hashPattern);
            return item;
        }


        public class UrlRouteAgentDictionary
        {
            private List<ConcurrentDictionary<ulong, UrlRouteAgent>> mDictionarys = new List<ConcurrentDictionary<ulong, UrlRouteAgent>>();

            public UrlRouteAgentDictionary()
            {
                for (int i = 0; i < Math.Min(Environment.ProcessorCount, 16); i++)
                {
                    mDictionarys.Add(new ConcurrentDictionary<ulong, UrlRouteAgent>());
                }
            }

            public void SetAgent(ulong url, UrlRouteAgent agent)
            {
                ConcurrentDictionary<ulong, UrlRouteAgent> keyValuePairs = mDictionarys[(int)(url % (uint)mDictionarys.Count)];
                keyValuePairs[url] = agent;
            }

            public UrlRouteAgent GetAgent(ulong url)
            {
                ConcurrentDictionary<ulong, UrlRouteAgent> keyValuePairs = mDictionarys[(int)(url % (uint)mDictionarys.Count)];
                keyValuePairs.TryGetValue(url, out UrlRouteAgent result);
                return result;
            }
        }

    }
}
