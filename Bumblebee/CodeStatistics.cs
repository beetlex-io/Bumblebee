using BeetleX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bumblebee
{

    public class Statistics
    {

        public Statistics()
        {
            All = new Bumblebee.CodeStatistics(0, "All");
            Server = "NULL";
            Url = "NULL";
            Times.Add(new TimeStatistics(0, 10));
            Times.Add(new TimeStatistics(10, 50));
            Times.Add(new TimeStatistics(50, 100));
            Times.Add(new TimeStatistics(100, 500));
            Times.Add(new TimeStatistics(500, 1000));
            Times.Add(new TimeStatistics(1000, 5000));
            Times.Add(new TimeStatistics(5000, 0));
            mData = new StatisticsData(this);
        }




        private StatisticsData mData;

        private ConcurrentDictionary<int, CodeStatistics> mSubStats = new ConcurrentDictionary<int, CodeStatistics>();

        private CodeStatistics[] mCache = new CodeStatistics[0];

        private CodeStatistics GetSubstats(int code)
        {
            if (!mSubStats.TryGetValue(code, out CodeStatistics result))
            {
                result = new CodeStatistics(code);
                if (mSubStats.TryAdd(code, result))
                {
                    mCache = mSubStats.Values.ToArray();
                }
                else
                {
                    mSubStats.TryGetValue(code, out result);
                }
            }
            return result;
        }

        public string Server { get; set; }

        public string Url { get; set; }

        public List<TimeStatistics> Times { get; private set; } = new List<TimeStatistics>();

        public CodeStatistics OtherStatus { get; private set; } = new CodeStatistics(0, "Other");

        public CodeStatistics Status_1xx { get; private set; } = new CodeStatistics(0, "1xx");

        public CodeStatistics Status_2xx { get; private set; } = new CodeStatistics(0, "2xx");

        public CodeStatistics Status_3xx { get; private set; } = new CodeStatistics(0, "3xx");

        public CodeStatistics Status_4xx { get; private set; } = new CodeStatistics(0, "4xx");

        public CodeStatistics Status_5xx { get; private set; } = new CodeStatistics(0, "5xx");

        public CodeStatistics All { get; private set; }

        public void Add(int code, long time)
        {
            All.Add(time);
            if (code >= 100 && code < 200)
                Status_1xx.Add(time);
            else if (code >= 200 && code < 300)
                Status_2xx.Add(time);
            else if (code >= 300 && code < 400)
                Status_3xx.Add(time);
            else if (code >= 400 && code < 500)
                Status_4xx.Add(time);
            else if (code >= 500 && code < 600)
                Status_5xx.Add(time);
            else
            {
                OtherStatus.Add(time);
            }
            if (code >= 700)
            {
                GetSubstats(700).Add(time);

            }
            else
            {
                GetSubstats(code).Add(time);
            }
            for (int i = 0; i < Times.Count; i++)
            {
                var t = Times[i];
                if (t.Match((int)time))
                    t.Add();
            }
        }

        public CodeStatisticsData ListStatisticsData(int code)
        {
            return GetSubstats(code).GetData();
        }

        public CodeStatisticsData[] ListStatisticsData(params int[] codes)
        {
            List<CodeStatisticsData> result = new List<CodeStatisticsData>();
            foreach (var i in codes)
            {
                if (mSubStats.TryGetValue(i, out CodeStatistics item))
                {
                    result.Add(item.GetData());
                }
            }
            return result.ToArray();
        }

        public CodeStatisticsData[] ListStatisticsData(int start, int end)
        {
            List<CodeStatisticsData> result = new List<CodeStatisticsData>();
            foreach (var item in mCache)
            {
                if (item.Code >= start && item.Code < end)
                    result.Add(item.GetData());
            }
            return result.ToArray();
        }

        public object ListStatisticsData(int start, int end, Func<CodeStatisticsData, object> selectObj)
        {
            var result = (from item in mCache
                          where item.Count > 0 && item.Code >= start && item.Code < end
                          select selectObj(item.GetData())).ToArray();
            return result;
        }

        public CodeStatistics[] List(Func<CodeStatistics, bool> filters = null)
        {
            if (filters == null)
                return (from a in this.mCache where a.Count > 0 orderby a.Count descending select a).ToArray();
            else
                return (from a in this.mCache where a.Count > 0 && filters(a) orderby a.Count descending select a).ToArray();
        }

        public StatisticsData GetData()
        {
            StatisticsData result = mData;
            result.Server = Server;
            result.Url = Url;
            result.Other = OtherStatus.GetData();
            result._1xx = Status_1xx.GetData();
            result._2xx = Status_2xx.GetData();
            result._3xx = Status_3xx.GetData();
            result._4xx = Status_4xx.GetData();
            result._5xx = Status_5xx.GetData();
            result.All = All.GetData();
            for (int i = 0; i < Times.Count; i++)
                result.Times[i] = Times[i].GetData();
            return result;
        }
    }

    public class CodeStatistics
    {
        public CodeStatistics(int code, string name = null)
        {
            Code = code;
            if (name == null)
                name = code.ToString();
            mLastTime = BeetleX.TimeWatch.GetTotalSeconds();
            Name = name;
            mStatisticsData = new CodeStatisticsData();
        }

        public int Code { get; private set; }

        public string Name { get; set; }

        private long mCount;

        public long Count => mCount;

        private double mLastTime;

        private long mLastCount;

        public void Add(long time)
        {
            System.Threading.Interlocked.Increment(ref mCount);
        }

        public override string ToString()
        {
            return mCount.ToString();
        }

        private int mGetStatus = 0;

        private CodeStatisticsData mStatisticsData;

        public CodeStatisticsData GetData()
        {
            if (TimeWatch.GetTotalSeconds() - mLastTime > 1)
            {
                if (System.Threading.Interlocked.CompareExchange(ref mGetStatus, 1, 0) == 0)
                {
                    CodeStatisticsData result = mStatisticsData;
                    result.CreateTime = TimeWatch.GetTotalSeconds();
                    result.Count = Count;
                    double now = TimeWatch.GetTotalSeconds();
                    double time = now - mLastTime;
                    result.Increase = (double)(mCount - mLastCount);
                    result.Rps = (int)((result.Increase) / time);

                    mLastTime = now;
                    mLastCount = mCount;
                    mGetStatus = 0;
                }
            }
            return mStatisticsData;
        }

    }

    public class TimeStatistics
    {
        public TimeStatistics(int start, int end)
        {
            mData = new TimeStatisticsData(start, end, 0, 0);
            mData.Name = Name;
            Start = start;
            End = end;
        }

        public string Name
        {
            get
            {
                string name;
                if (Start > 0 && End > 0)
                {
                    if (Start >= 1000)
                        name = $"{Start / 1000}s";
                    else
                        name = $"{Start}ms";

                    if (End >= 1000)
                        name += $"-{End / 1000}s";
                    else
                        name += $"-{End}ms";

                }
                else if (Start > 0)
                {
                    if (Start >= 1000)
                        name = $">{Start / 1000}s";
                    else
                        name = $">{Start}ms";
                }
                else
                {
                    name = $"<{End}ms";
                }
                return name;
            }
        }

        public int Start { get; set; }

        public int End { get; set; }

        private long mCount;

        public long Count => mCount;

        private TimeStatisticsData mData;

        private long mLastCount;

        private double mLastTime;

        public bool Match(int time)
        {
            if (End == 0)
                return time >= Start;
            return time >= Start && time < End;
        }

        public double Increase { get; set; }

        public void Add()
        {
            System.Threading.Interlocked.Increment(ref mCount);
        }

        public TimeStatisticsData GetData()
        {
            double now = TimeWatch.GetTotalSeconds();
            double time = now - mLastTime;
            if (time > 1)
            {
                mData.Count = mCount;
                this.Increase = (double)(mCount - mLastCount);
                mData.Rps = (int)(Increase / (time));
                mLastTime = now;
                mLastCount = mCount;
            }
            return mData;
        }
    }

    public class StatisticsData
    {
        public StatisticsData(Statistics statistics)
        {
            Statistics = statistics;
            Times = new TimeStatisticsData[statistics.Times.Count];
        }

        public Statistics Statistics { get; set; }
        public String Url { get; set; }
        public string Server { get; set; }
        public CodeStatisticsData All { get; set; }
        public CodeStatisticsData Other { get; set; }
        public CodeStatisticsData _1xx { get; set; }
        public CodeStatisticsData _2xx { get; set; }
        public CodeStatisticsData _3xx { get; set; }
        public CodeStatisticsData _4xx { get; set; }
        public CodeStatisticsData _5xx { get; set; }

        public TimeStatisticsData[] Times { get; private set; }
    }

    public class CodeStatisticsData
    {
        public CodeStatisticsData()
        {

        }
        public string Name { get; set; }

        public long Count { get; set; }

        public long Rps { get; set; }

        public double Increase { get; set; }

        public double CreateTime { get; set; }


    }

    public class TimeStatisticsData
    {
        public TimeStatisticsData(int start, int end, long count, long rps)
        {
            StartTime = start;
            EndTime = end;
            Count = count;
            Rps = rps;
        }

        public int StartTime { get; set; }

        public int EndTime { get; set; }

        public long Count { get; set; }

        public long Rps { get; set; }

        public string Name
        {
            get; set;
        }
    }

    public class UrlStatistics
    {
        public UrlStatistics(string url)
        {
            Statistics.Url = url;
            Url = url;
        }

        public string Url { get; private set; }

        private UrlStatisticsData mData = new UrlStatisticsData();

        public string Ext { get; set; }

        public string Path { get; set; }

        public Statistics Statistics { get; internal set; } = new Statistics();

        public ConcurrentDictionary<string, Statistics> Servers { get; internal set; } = new ConcurrentDictionary<string, Statistics>();

        public ConcurrentDictionary<string, Statistics> Domains { get; internal set; } = new ConcurrentDictionary<string, Statistics>();

        public void Add(int code, long time, Servers.ServerAgent server, BeetleX.FastHttpApi.HttpRequest request)
        {
            Statistics.Add(code, time);
            if (server != null)
            {
                if (!Servers.TryGetValue(server.UriKey, out Statistics s))
                {
                    s = new Statistics();
                    s.Server = server.UriKey;
                    s.Url = this.Statistics.Url;
                    if (!Servers.TryAdd(server.UriKey, s))
                    {
                        Servers.TryGetValue(server.UriKey, out s);
                    }
                }
                s.Add(code, time);
            }
            var domain = request.GetHostBase();
            if (!string.IsNullOrEmpty(domain))
            {
                if (!Domains.TryGetValue(domain, out Statistics s))
                {
                    s = new Statistics();
                    s.Server = domain;
                    s.Url = this.Statistics.Url;
                    if (!Domains.TryAdd(domain, s))
                    {
                        Domains.TryGetValue(domain, out s);
                    }
                }
                s.Add(code, time);
            }
        }

        public UrlStatisticsData GetResult()
        {
            mData.Update(this);
            return mData;
        }
    }

    public class UrlStatisticsData
    {

        public void Update(UrlStatistics item)
        {

            All = item.Statistics.GetData();
            UrlInfo = item;
            Statistics = item.Statistics;
            foreach (var s in item.Servers)
            {
                var d = s.Value.GetData();
                Servers[d.Server] = d;
            }

            foreach (var s in item.Domains)
            {
                var d = s.Value.GetData();
                Domains[d.Server] = d;
            }
        }
        public UrlStatistics UrlInfo { get; set; }

        public Statistics Statistics { get; private set; }

        public StatisticsData All { get; set; }

        public Dictionary<string, StatisticsData> Servers { get; private set; } = new Dictionary<string, StatisticsData>();

        public Dictionary<string, StatisticsData> Domains { get; private set; } = new Dictionary<string, StatisticsData>();
    }

}
