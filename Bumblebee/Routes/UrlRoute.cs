using BeetleX.FastHttpApi;
using Bumblebee.Filters;
using Bumblebee.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bumblebee.Routes
{
    public class UrlRoute
    {
        public UrlRoute(Gateway gateway, string url)
        {
            Gateway = gateway;
            Filters = new List<IRequestFilter>();
            Url = url;
            UrlPattern = url;

            var values = url.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length > 1)
            {
                Host = values[0];
                UrlPattern = values[1];
            }
            mServers = new Servers.UrlRouteServerGroup(gateway, url);

        }

        public void BuildHashPattern(string hashPattern = null)
        {
            HashPattern = hashPattern;
            if (!string.IsNullOrEmpty(hashPattern))
            {
                try
                {
                    var requestHashBuilder = new RequestHashBuilder(hashPattern);
                    requestHashBuilder.Build();
                    mRequestHashBuilder = requestHashBuilder;
                    this.Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway build {this.Url} {hashPattern} hash regex success");
                }
                catch (Exception e_)
                {
                    this.Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway build {this.Url} {hashPattern} hash regex error {e_.Message}");
                }
            }
            else
            {
                mRequestHashBuilder = null;
            }
        }

        public string HashPattern { get; set; }

        private RequestHashBuilder mRequestHashBuilder;

        public string Host { get; private set; }

        private List<string> mFilterNames = new List<string>();

        public string[] FilterNames => mFilterNames.ToArray();

        public IEnumerable<FilterInfo> GetFiltersInfo()
        {
            return from a in RequestFilters
                   select new FilterInfo
                   {
                       Assembly = a.GetType().Assembly.GetName().Name,
                       Name = a.Name,
                       Version = a.GetType().Assembly.GetName().Version.ToString()
                   };
        }

        public string Url { get; private set; }

        public string UrlPattern { get; set; }

        public Gateway Gateway { get; internal set; }

        public List<IRequestFilter> Filters { get; private set; }

        public void RemoveFilter(string name)
        {
            for (int i = 0; i < Filters.Count; i++)
            {
                if (Filters[i].Name == name)
                {
                    Filters.RemoveAt(i);
                    mFilterNames.Remove(name);
                    Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Warring, $"{Url} route remove {name} filter");
                    mRequestFilters = Filters.ToArray();
                    return;
                }
            }
        }

        public void SetFilter(string name)
        {
            IRequestFilter filter = Gateway.Filters.GetFilter(name);
            if (filter == null)
            {
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Warring, $"{Url} route add filter error {name} filter not found!");
                return;
            }
            if (!mFilterNames.Contains(name))
                mFilterNames.Add(name);
            LoadFilter(filter);

        }

        public void ReloadFilters()
        {
            foreach (string item in mFilterNames.ToArray())
            {
                SetFilter(item);
                Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"gateway route {Url} load {item} filter success");
            }
        }

        private void LoadFilter(IRequestFilter filter)
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
            Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Info, $"{Url} route load {filter.Name} filter");
            mRequestFilters = Filters.ToArray();
        }

        private IRequestFilter[] mRequestFilters = new IRequestFilter[0];

        public IRequestFilter[] RequestFilters => mRequestFilters;

        public string[] ServerWeightTable
        {
            get
            {
                return (from a in mServers.ServerWeightTable select a.Agent.Uri.ToString()).ToArray();
            }
        }

        public IList<IRequestFilter> GetFilters()
        {
            return mRequestFilters;
        }

        private UrlRouteServerGroup mServers;

        public UrlRouteServerGroup.UrlServerInfo[] Servers
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

        public UrlRouteServerGroup.UrlServerInfo GetServerAgent(HttpRequest request)
        {
            ulong hashcode;
            var requestHashBuilder = mRequestHashBuilder;
            if (requestHashBuilder != null)
            {
                hashcode = requestHashBuilder.GetRequestHashcode(request);
                if (hashcode == 0)
                    hashcode = (ulong)GetRequestHashcode();
            }
            else
                hashcode = (ulong)GetRequestHashcode();
            var result = mServers.GetAgent(hashcode);
            return result;
        }

        public class RequestHashBuilder
        {
            public RequestHashBuilder(string hashPattern)
            {
                HashPattern = hashPattern;
            }

            public void Build()
            {
                if (string.Compare(HashPattern, "Host", true) == 0)
                {
                    Type = RequestHashBuilderType.Host;
                }
                else if (string.Compare(HashPattern, "Url", true) == 0)
                {
                    Type = RequestHashBuilderType.Url;
                }
                else if (string.Compare(HashPattern, "BaseUrl", true) == 0)
                {
                    Type = RequestHashBuilderType.BaseUrl;
                }
                else
                {
                    Type = RequestHashBuilderType.Parameters;

                    string itemPattern = @"\(([hqHQ])\:([a-zA-Z0-9]+)\)";
                    var matches = Regex.Matches(HashPattern, itemPattern, RegexOptions.IgnoreCase);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            string ptype = match.Groups[1].Value.ToLower();
                            if (ptype == "h")
                            {
                                HashPatternParameter hashPatternParameter = new HashPatternParameter();
                                hashPatternParameter.Type = HashPatternParameterType.Header;
                                hashPatternParameter.Name = match.Groups[2].Value;
                                this.PatternParameters.Add(hashPatternParameter);
                            }
                            else if (ptype == "q")
                            {
                                HashPatternParameter hashPatternParameter = new HashPatternParameter();
                                hashPatternParameter.Type = HashPatternParameterType.QueryString;
                                hashPatternParameter.Name = match.Groups[2].Value;
                                this.PatternParameters.Add(hashPatternParameter);
                            }
                        }

                    }

                }
            }

            public RequestHashBuilderType Type { get; set; }

            public static byte[] MD5Hash(string value)
            {
                using (MD5 md5Hash = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));
                    return hash;
                }
            }

            public static ulong GetMD5HashCode(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return 0;
                byte[] data = MD5Hash(value);
                return BitConverter.ToUInt64(data, 8);
            }

            [ThreadStatic]
            private static StringBuilder mHashValue = new StringBuilder();

            private string GetHashValue(HttpRequest request)
            {
                if (mHashValue == null)
                    mHashValue = new StringBuilder();
                mHashValue.Clear();
                foreach (var item in this.PatternParameters)
                {
                    mHashValue.Append(item.GetValue(request)).Append(".");
                }
                return mHashValue.ToString();
            }

            public List<HashPatternParameter> PatternParameters { get; private set; } = new List<HashPatternParameter>();

            public string HashPattern { get; set; }

            public ulong GetRequestHashcode(HttpRequest request)
            {
                ulong result = 0;
                string value = null;
                switch (Type)
                {
                    case RequestHashBuilderType.BaseUrl:
                        value = request.BaseUrl;
                        break;
                    case RequestHashBuilderType.Host:
                        value = request.Host;
                        break;
                    case RequestHashBuilderType.Url:
                        value = request.Url;
                        break;
                    default:
                        value = GetHashValue(request);
                        break;
                }
                result = GetMD5HashCode(value);
                return result;
            }
        }

        public enum RequestHashBuilderType
        {
            Url,
            BaseUrl,
            Host,
            Parameters
        }

        public class HashPatternParameter
        {
            public HashPatternParameterType Type { get; set; }

            public string Name { get; set; }

            public string GetValue(HttpRequest request)
            {
                if (Type == HashPatternParameterType.Header)
                {
                    return request.Header[Name];
                }
                else
                {
                    return request.Data[Name];
                }
            }
        }

        public enum HashPatternParameterType
        {
            Header,
            QueryString,
        }

        internal void FilterExecuted(HttpRequest request, HttpResponse response, ServerAgent server, int code, long useTime)
        {
            var filters = RequestFilters;
            if (filters != null)
            {
                for (int i = 0; i < filters.Length; i++)
                {
                    try
                    {
                        filters[i].Executed(Gateway, request, response, server, code, useTime);
                    }
                    catch (Exception e_)
                    {
                        if (Gateway.HttpServer.EnableLog(BeetleX.EventArgs.LogType.Error))
                        {
                            Gateway.HttpServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway {request.Url} executed {filters[i].Name} error {e_.Message} {e_.StackTrace}");
                        }
                    }
                }
            }
        }

    }
}
