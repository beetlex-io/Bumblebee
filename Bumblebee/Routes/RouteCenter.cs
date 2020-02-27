using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;
using BeetleX.FastHttpApi;
using BeetleX.EventArgs;

namespace Bumblebee.Routes
{
    public class RouteCenter
    {
        public RouteCenter(Gateway gateway)
        {
            Gateway = gateway;
            Default = new UrlRoute(gateway, "*");
            mUrlStatisticsDB = new UrlStatisticsMemoryDB(gateway);
        }

        private long mVersion;

        private List<UrlRoute> mMatchRoutes = new List<UrlRoute>();

        private ConcurrentDictionary<string, UrlRoute> mUrlRoutes = new ConcurrentDictionary<string, UrlRoute>(StringComparer.OrdinalIgnoreCase);

        private UrlRouteAgentDictionary urlRouteAgent = new UrlRouteAgentDictionary();

        private UrlStatisticsMemoryDB mUrlStatisticsDB;

        private void OnUpdateUrlTable()
        {
            lock (mLockUpdateUrlTable)
            {
                List<UrlRoute> urls = new List<UrlRoute>();
                urls.AddRange(mUrlRoutes.Values);
                mMatchRoutes = (from a in urls
                                orderby a.Host?.Length descending, a.UrlPattern.Length descending
                                select a).ToList();
                mVersion++;
            }
            Gateway.HttpServer.GetLog(LogType.Warring)?.Log(BeetleX.EventArgs.LogType.Warring, $"Gateway update route url data table");

        }

        private (long, List<UrlRoute>) GetMatchRoutes()
        {
            lock (mLockUpdateUrlTable)
            {
                return (mVersion, mMatchRoutes.ToList());
            }
        }

        private object mLockUpdateUrlTable = new object();

        public void UpdateUrlTable()
        {
            OnUpdateUrlTable();
        }

        public void ReloadPlugin()
        {
            try
            {
                foreach (var item in mUrlRoutes.Values)
                    item.Pluginer.Reload();
                Gateway.HttpServer.GetLog(BeetleX.EventArgs.LogType.Info)?.Log(BeetleX.EventArgs.LogType.Info, $"Gateway route reload plugin");

            }
            catch (Exception e_)
            {
                Gateway.HttpServer.GetLog(LogType.Error)?.Log(BeetleX.EventArgs.LogType.Info, $"Gateway route reload plugin error {e_.Message} {e_.StackTrace}");
            }
            finally
            {
                OnUpdateUrlTable();
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
            var routes = GetMatchRoutes();
            UrlRouteAgent agent = new UrlRouteAgent();
            agent.Url = url;
            agent.Version = routes.Item1;
            var urls = routes.Item2;
            agent.Routes = urls;
            for (int i = 0; i < urls.Count; i++)
            {
                var routeItem = urls[i];
                if (System.Text.RegularExpressions.Regex.IsMatch(url, routeItem.UrlPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (!routeItem.HasServer(request) && routeItem.ApiLoader)
                        continue;
                    if (routeItem.Host == null || routeItem.Host.Length == 0)
                    {
                        agent.UrlRoute = routeItem;
                        return agent;
                    }
                    else
                    {

                        if (!string.IsNullOrEmpty(request.Host))
                        {
                            foreach (var item in routeItem.Host)
                            {
                                if (string.Compare(request.Host, item, true) == 0)
                                {
                                    agent.UrlRoute = routeItem;
                                    return agent;
                                }
                            }
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
            // ulong urlcode = GetUrlCode(string.Concat(request.Host, "|", request.GetSourceBaseUrl()));
            string url = string.Concat(request.Host, "|", request.GetSourceBaseUrl(), "|", request.WebSocket);
            var item = urlRouteAgent.GetAgent(url);

            if (item == null || item.Version != Version || item.Routes.Count != mMatchRoutes.Count)
            {
                item = MatchAgent(request);
                urlRouteAgent.SetAgent(url, item);
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



        public UrlRoute NewOrGet(string url, string remark, string hashPattern = null, bool apiLoader = true)
        {
            if (url == "*")
            {
                Default.BuildHashPattern(hashPattern);
                Default.Remark = remark;
                UpdateUrlTable();
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


        public UrlStatisticsMemoryDB UrlStatisticsDB => mUrlStatisticsDB;

        public class UrlStatisticsMemoryDB
        {
            private ConcurrentDictionary<string, UrlStatistics> mStats = new ConcurrentDictionary<string, UrlStatistics>(StringComparer.OrdinalIgnoreCase);

            private ConcurrentDictionary<string, string> mPaths = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            private Gateway mGateway;

            private long mVersion;

            private long mCacheVersion;

            private UrlStatistics[] mCacheStats = new UrlStatistics[0];

            public UrlStatisticsMemoryDB(Gateway gateway)
            {
                mGateway = gateway;
            }


            public void Add(int code, long time, Servers.ServerAgent server, BeetleX.FastHttpApi.HttpRequest request)
            {
                var stats = GetStatistics(request, code);
                stats?.Add(code, time, server, request);
            }

            private UrlStatistics GetStatistics(HttpRequest request, int code)
            {
                string url = request.GetSourceBaseUrl();
                if (!mStats.TryGetValue(url, out UrlStatistics result))
                {
                    if (mStats.Count >= mGateway.MaxStatsUrls)
                        return null;
                    if (code >= 400)
                        return null;
                    result = new UrlStatistics(request.GetSourceBaseUrl());
                    result.Path = request.GetSourcePath();
                    result.Ext = request.Ext;
                    if (mStats.TryAdd(url, result))
                    {
                        mPaths[url] = url;
                        System.Threading.Interlocked.Increment(ref mVersion);
                    }
                    else
                    {
                        mStats.TryGetValue(url, out result);
                    }
                }
                return result;
            }

            public string[] GetPaths()
            {
                return mPaths.Values.ToArray();
            }

            public UrlStatistics[] GetStatistics(string path)
            {
                return (from a in GetStatistics() where a.Path == path select a).ToArray();
            }

            public UrlStatistics[] GetStatistics()
            {
                if (mCacheVersion != mVersion)
                {
                    mCacheStats = mStats.Values.ToArray();
                    mCacheVersion = mVersion;
                }
                return mCacheStats;
            }
        }



        public class UrlRouteAgentDictionary
        {
            private List<ConcurrentDictionary<string, UrlRouteAgent>> mDictionarys = new List<ConcurrentDictionary<string, UrlRouteAgent>>();

            public UrlRouteAgentDictionary()
            {
                for (int i = 0; i < Math.Min(Environment.ProcessorCount, 16); i++)
                {
                    mDictionarys.Add(new ConcurrentDictionary<string, UrlRouteAgent>(StringComparer.OrdinalIgnoreCase));
                }
            }

            private int GetUrlIndex(string url)
            {
                return Math.Abs(url.GetHashCode());
            }

            public void SetAgent(string url, UrlRouteAgent agent)
            {
                ConcurrentDictionary<string, UrlRouteAgent> keyValuePairs = mDictionarys[(int)(GetUrlIndex(url) % (uint)mDictionarys.Count)];
                if(!keyValuePairs.TryAdd(url,agent))
                {
                    keyValuePairs.TryGetValue(url, out UrlRouteAgent old);
                    if (agent.Version > old.Version)
                    {
                        keyValuePairs[url] = agent;
                    }
                }
            }

            public UrlRouteAgent GetAgent(string url)
            {
                ConcurrentDictionary<string, UrlRouteAgent> keyValuePairs = mDictionarys[(int)(GetUrlIndex(url) % (uint)mDictionarys.Count)];
                keyValuePairs.TryGetValue(url, out UrlRouteAgent result);
                return result;
            }
        }

    }
}
