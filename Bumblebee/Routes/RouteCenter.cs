using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;

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

        private List<string> mUrls = new List<string>();

        private ConcurrentDictionary<string, UrlRoute> mUrlRoutes = new ConcurrentDictionary<string, UrlRoute>();

        private UrlRouteAgentDictionary urlRouteAgent = new UrlRouteAgentDictionary();

        private void UpdateUrls()
        {
            List<string> urls = new List<string>();
            urls.AddRange(mUrlRoutes.Keys);
            mUrls = urls;
            mUrls.Sort((x, y) => y.Length.CompareTo(x.Length));
            Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway update route url data table");
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
        }

        private UrlRouteAgent MatchAgent(string url)
        {
            UrlRouteAgent agent = new UrlRouteAgent();
            agent.Version = this.Version;
            agent.Url = url;
            var urls = mUrls;
            for (int i = 0; i < urls.Count; i++)
            {
                string key = urls[i];
                if (System.Text.RegularExpressions.Regex.IsMatch(url, key, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (mUrlRoutes.TryGetValue(key, out UrlRoute item) && item.Servers.Length > 0)
                    {
                        agent.UrlRoute = item;
                        return agent;
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

        public UrlRouteAgent GetAgent(string url)
        {
            ulong urlcode = GetUrlCode(url);
            var item = urlRouteAgent.GetAgent(urlcode);
            if (item == null || item.Version != Version)
            {
                item = MatchAgent(url);
                urlRouteAgent.SetAgent(urlcode, item);
            }
            return item;
        }

        public Gateway Gateway { get; private set; }

        public long Version { get { return mVersion; } }

        public List<string> GetUrls
        {
            get
            {
                return mUrls;
            }
        }

        private ulong GetUrlCode(string url)
        {
            ulong value = (ulong)Math.Abs(url.GetHashCode()) << 16;
            value |= (ushort)url.Length;
            return value;
        }

        public UrlRoute Default
        {
            get; private set;
        }

        public UrlRoute Remove(string url)
        {
            if (mUrlRoutes.TryGetValue(url, out UrlRoute item))
            {
                UpdateUrls();
                System.Threading.Interlocked.Increment(ref mVersion);
            }
            return item;
        }

        public UrlRoute NewOrGet(string url)
        {
            if (url == "*")
                return Default;
            if (!mUrlRoutes.TryGetValue(url, out UrlRoute item))
            {
                item = new UrlRoute(Gateway, url);
                mUrlRoutes[url] = item;
                UpdateUrls();
                System.Threading.Interlocked.Increment(ref mVersion);

            }
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
