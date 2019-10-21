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

        private UrlStatisticsDictionary urlStatisticsDictionary = new UrlStatisticsDictionary();

        private UrlStatisticsDictionary urlServerStatisticsDictionary = new UrlStatisticsDictionary();

        private void OnUpdateUrlTable()
        {
            List<UrlRoute> urls = new List<UrlRoute>();
            urls.AddRange(mUrlRoutes.Values);
            urls.Sort((x, y) => y.UrlPattern.Length.CompareTo(x.UrlPattern.Length));
            mMatchRoutes = urls;
            Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"Gateway update route url data table");
        }

        public void ReloadPlugin()
        {
            try
            {
                foreach (var item in mUrlRoutes.Values)
                    item.Pluginer.Reload();
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"Gateway route reload plugin");
            }
            catch (Exception e_)
            {
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"Gateway route reload plugin error {e_.Message} {e_.StackTrace}");
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
            string url = request.GetSourceBaseUrl();
            UrlRouteAgent agent = new UrlRouteAgent();
            agent.Url = url;
            agent.Version = this.Version;
            var urls = mMatchRoutes;
            agent.Routes = urls;
            for (int i = 0; i < urls.Count; i++)
            {
                var routeItem = urls[i];
                if (System.Text.RegularExpressions.Regex.IsMatch(url, routeItem.UrlPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (routeItem.Servers.Length == 0 && routeItem.ApiLoader)
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
            ulong urlcode = GetUrlCode(string.Concat(request.Host, "|", request.GetSourceBaseUrl()));
            var item = urlRouteAgent.GetAgent(urlcode);
            if (item == null || item.Version != Version || item.Routes.Count != mMatchRoutes.Count)
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

        private object mLockUpdateUrlTable = new object();

        public void UpdateUrlTable()
        {
            OnUpdateUrlTable();
            System.Threading.Interlocked.Increment(ref mVersion);
        }

        public UrlRoute NewOrGet(string url, string remark, string hashPattern = null, bool apiLoader = true)
        {
            if (url == "*")
            {
                Default.BuildHashPattern(hashPattern);
                Default.Remark = remark;
                return Default;
            }
            if (!mUrlRoutes.TryGetValue(url, out UrlRoute item))
            {
                item = new UrlRoute(Gateway, url);
                item.ApiLoader = apiLoader;
                mUrlRoutes[url] = item;
            }
            item.BuildHashPattern(hashPattern);
            item.ApiLoader = apiLoader;
            item.Remark = remark;
            UpdateUrlTable();
            return item;
        }


        public UrlStatistics[] GetUrlStatisticsData()
        {
            return (from a in urlStatisticsDictionary.GetStatistics()
                    select a).ToArray();
        }

        private int mUrlStatisticsCount;

        public int UrlStatisticsCount => mUrlStatisticsCount;

        public UrlStatistics GetUrlStatistics(string url, HttpRequest request, int code)
        {
            var id = GetUrlCode(url);
            var stats = urlStatisticsDictionary.GetStatistics(id);
            if (stats == null)
            {
                if (code == 404)
                    return null;
                lock (urlStatisticsDictionary)
                {
                    if (mUrlStatisticsCount >= Gateway.MaxStatsUrls)
                        return null;
                    stats = urlStatisticsDictionary.GetStatistics(id);
                    if (stats == null)
                    {
                        stats = new UrlStatistics(url);
                        stats.Path = request.Path;
                        urlStatisticsDictionary.SetStatistics(id, stats);
                        System.Threading.Interlocked.Increment(ref mUrlStatisticsCount);
                    }
                }
            }
            return stats;
        }

        public class UrlStatisticsDictionary
        {
            private List<ConcurrentDictionary<ulong, UrlStatistics>> mDictionarys = new List<ConcurrentDictionary<ulong, UrlStatistics>>();

            public UrlStatisticsDictionary()
            {
                for (int i = 0; i < Math.Min(Environment.ProcessorCount, 16); i++)
                {
                    mDictionarys.Add(new ConcurrentDictionary<ulong, UrlStatistics>());
                }
            }

            public void SetStatistics(ulong url, UrlStatistics agent)
            {
                ConcurrentDictionary<ulong, UrlStatistics> keyValuePairs = mDictionarys[(int)(url % (uint)mDictionarys.Count)];
                keyValuePairs[url] = agent;
            }

            public UrlStatistics GetStatistics(ulong url)
            {
                ConcurrentDictionary<ulong, UrlStatistics> keyValuePairs = mDictionarys[(int)(url % (uint)mDictionarys.Count)];
                keyValuePairs.TryGetValue(url, out UrlStatistics result);
                return result;
            }

            public List<UrlStatistics> GetStatistics()
            {
                List<UrlStatistics> result = new List<UrlStatistics>();
                foreach (var item in mDictionarys)
                    foreach (var data in item.Values)
                        result.Add(data);
                return result;
            }
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
