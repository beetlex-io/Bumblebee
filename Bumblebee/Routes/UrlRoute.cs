using BeetleX.FastHttpApi;

using Bumblebee.Plugins;
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
            Url = url;
            UrlPattern = url;

            var values = url.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length > 1)
            {
                Host = values[0];
                UrlPattern = values[1];
            }
            mServers = new Servers.UrlRouteServerGroup(gateway, url);
            this.Pluginer = new Pluginer(Gateway, this);
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

        public bool ApiLoader { get; set; } = true;

        private RequestHashBuilder mRequestHashBuilder;

        public string Host { get; private set; }

        public IGetServerHandler GetServerHandler { get; set; }

        public string Url { get; private set; }

        public string UrlPattern { get; set; }

        public Gateway Gateway { get; internal set; }

        public string[] ServerWeightTable
        {
            get
            {
                return (from a in mServers.ServerWeightTable select a.Agent.Uri.ToString()).ToArray();
            }
        }

        private UrlRouteServerGroup mServers;

        public Pluginer Pluginer { get; private set; }

        public UrlRouteServerGroup.UrlServerInfo[] Servers
        {
            get
            {
                return mServers.Servers;
            }
        }

        public string Remark { get; set; }

        public UrlRoute AddServer(params string[] hosts)
        {
            if (hosts != null)
                foreach (var item in hosts)
                {
                    AddServer(item, 10, 0);
                }
            return this;
        }
        public UrlRoute AddServer(string host, int wediht)
        {
            AddServer(host, wediht,0);
            return this;
        }
        public UrlRoute AddServer(string host, int wediht, int maxRps)
        {
            mServers.NewOrModify(host, wediht, maxRps);
            return this;
        }

        public UrlRoute ChangeServerWedith(string host, int wediht, int maxRps)
        {
            mServers.NewOrModify(host, wediht, maxRps);
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
            UrlRouteServerGroup.UrlServerInfo result;
            result = Pluginer.GetServerHandler?.GetServer(Gateway, request, this.Servers);
            if (result == null)
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
                result = mServers.GetAgent(hashcode);
            }
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
    }
}
