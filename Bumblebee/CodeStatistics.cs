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

        const int COUNT = 701;

        public Statistics()
        {
            CodeStatistics = new CodeStatistics[701];
            for (int i = 0; i < 701; i++)
            {
                CodeStatistics[i] = new CodeStatistics(i);
            }

            All = new Bumblebee.CodeStatistics(0, "All");
            Server = "NULL";
            Url = "NULL";
        }

        public string Server { get; set; }

        public string Url { get; set; }

        public CodeStatistics OtherStatus { get; private set; } = new CodeStatistics(0, "Other");

        public CodeStatistics Status_1xx { get; private set; } = new CodeStatistics(0, "1xx");

        public CodeStatistics Status_2xx { get; private set; } = new CodeStatistics(0, "2xx");

        public CodeStatistics Status_3xx { get; private set; } = new CodeStatistics(0, "3xx");

        public CodeStatistics Status_4xx { get; private set; } = new CodeStatistics(0, "4xx");

        public CodeStatistics Status_5xx { get; private set; } = new CodeStatistics(0, "5xx");

        public CodeStatistics All { get; private set; }

        public CodeStatistics[] CodeStatistics { get; private set; }

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
            if (code >= COUNT)
            {
                CodeStatistics[COUNT - 1].Add(time);
            }
            else
            {
                CodeStatistics[code].Add(time);
            }
        }

        public StatisticsData ListStatisticsData(int code)
        {
            return CodeStatistics[code].GetData();
        }

        public StatisticsData[] ListStatisticsData(params int[] codes)
        {
            List<StatisticsData> result = new List<StatisticsData>();
            foreach (var i in codes)
            {
                if (i < COUNT)
                    result.Add(CodeStatistics[i].GetData());
            }
            return result.ToArray();
        }

        public StatisticsData[] ListStatisticsData(int start, int end)
        {
            List<StatisticsData> result = new List<StatisticsData>();
            for (int i = start; i < end; i++)
            {
                if (i < COUNT)
                    result.Add(CodeStatistics[i].GetData());
            }
            return result.ToArray();
        }

        public object ListStatisticsData(int start, int end, Func<StatisticsData, object> selectObj)
        {
            var result = (from item in this.CodeStatistics
                          where item.Count > 0 && item.Code >= start && item.Code < end
                          select selectObj(item.GetData())).ToArray();
            return result;
        }

        public CodeStatistics[] List(Func<CodeStatistics, bool> filters = null)
        {
            if (filters == null)
                return (from a in this.CodeStatistics where a.Count > 0 orderby a.Count descending select a).ToArray();
            else
                return (from a in this.CodeStatistics where a.Count > 0 && filters(a) orderby a.Count descending select a).ToArray();
        }

        public StatisticsGroup GetData()
        {
            StatisticsGroup result = new StatisticsGroup(this);
            result.Server = Server;
            result.Url = Url;
            result.Other = OtherStatus.GetData();
            result._1xx = Status_1xx.GetData();
            result._2xx = Status_2xx.GetData();
            result._3xx = Status_3xx.GetData();
            result._4xx = Status_4xx.GetData();
            result._5xx = Status_5xx.GetData();
            result.All = All.GetData();
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
        }

        public int Code { get; private set; }

        public string Name { get; set; }

        private long mCount;

        public long Count => mCount;

        private double mLastTime;

        private long mLastCount;

        public int Rps
        {
            get
            {
                double time = TimeWatch.GetTotalSeconds() - mLastTime;
                int value = (int)((double)(mCount - mLastCount) / time);
                mLastTime = TimeWatch.GetTotalSeconds();
                mLastCount = mCount;
                return value;
            }
        }

        public void Add(long time)
        {
            System.Threading.Interlocked.Increment(ref mCount);
            if (time <= 10)
                System.Threading.Interlocked.Increment(ref ms10);
            else if (time <= 20)
                System.Threading.Interlocked.Increment(ref ms20);
            else if (time <= 50)
                System.Threading.Interlocked.Increment(ref ms50);
            else if (time <= 100)
                System.Threading.Interlocked.Increment(ref ms100);
            else if (time <= 200)
                System.Threading.Interlocked.Increment(ref ms200);
            else if (time <= 500)
                System.Threading.Interlocked.Increment(ref ms500);
            else if (time <= 1000)
                System.Threading.Interlocked.Increment(ref ms1000);
            else if (time <= 2000)
                System.Threading.Interlocked.Increment(ref ms2000);
            else if (time <= 5000)
                System.Threading.Interlocked.Increment(ref ms5000);
            else if (time <= 10000)
                System.Threading.Interlocked.Increment(ref ms10000);
            else
                System.Threading.Interlocked.Increment(ref msOther);
        }

        public override string ToString()
        {
            return mCount.ToString();
        }

        private long ms10;

        private long ms10LastCount;

        public long Time10ms => ms10;

        private long ms20;

        private long ms20LastCount;

        public long Time20ms => ms20;

        private long ms50;

        private long ms50LastCount;

        public long Time50ms => ms50;

        private long ms100;

        private long ms100LastCount;

        public long Time100ms => ms100;

        private long ms200;

        private long ms200LastCount;

        public long Time200ms => ms200;

        private long ms500;

        private long ms500LastCount;

        public long Time500ms => ms500;

        private long ms1000;

        private long ms1000LastCount;

        public long Time1000ms => ms1000;

        private long ms2000;

        private long ms2000LastCount;

        public long Time2000ms => ms2000;

        private long ms5000;

        private long ms5000LastCount;

        public long Time5000ms => ms5000;

        private long ms10000;

        private long ms10000LastCount;

        public long Time10000ms => ms10000;

        private long msOther;

        private long msOtherLastCount;

        public long TimeOtherms => msOther;

        private double mLastRpsTime = 0;

        private int mGetStatus = 0;

        private StatisticsData mStatisticsData = null;

        public StatisticsData GetData()
        {
            if (mStatisticsData == null || TimeWatch.GetTotalSeconds() - mStatisticsData.CreateTime >= 1)
                if (System.Threading.Interlocked.CompareExchange(ref mGetStatus, 1, 0) == 0)
                {
                    StatisticsData result = new StatisticsData();
                    result.CreateTime = TimeWatch.GetTotalSeconds();
                    result.Count = Count;
                    result.Rps = Rps;
                    result.Name = Name;
                    result.Times.Add(Time10ms);
                    result.Times.Add(Time20ms);
                    result.Times.Add(Time50ms);
                    result.Times.Add(Time100ms);
                    result.Times.Add(Time200ms);
                    result.Times.Add(Time500ms);
                    result.Times.Add(Time1000ms);
                    result.Times.Add(Time2000ms);
                    result.Times.Add(Time5000ms);
                    result.Times.Add(Time10000ms);
                    result.Times.Add(TimeOtherms);
                    double now = TimeWatch.GetTotalSeconds();
                    double time = now - mLastRpsTime;

                    int value = (int)((double)(ms10 - ms10LastCount) / time);
                    ms10LastCount = ms10;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms20 - ms20LastCount) / time);
                    ms20LastCount = ms20;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms50 - ms50LastCount) / time);
                    ms50LastCount = ms50;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms100 - ms100LastCount) / time);
                    ms100LastCount = ms100;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms200 - ms200LastCount) / time);
                    ms200LastCount = ms200;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms500 - ms500LastCount) / time);
                    ms500LastCount = ms500;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms1000 - ms1000LastCount) / time);
                    ms1000LastCount = ms1000;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms2000 - ms2000LastCount) / time);
                    ms2000LastCount = ms2000;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms5000 - ms5000LastCount) / time);
                    ms5000LastCount = ms5000;
                    result.TimesRps.Add(value);


                    value = (int)((double)(ms10000 - ms10000LastCount) / time);
                    ms10000LastCount = ms10000;
                    result.TimesRps.Add(value);


                    value = (int)((double)(msOther - msOtherLastCount) / time);
                    msOtherLastCount = msOther;
                    result.TimesRps.Add(value);

                    mLastRpsTime = now;

                    mStatisticsData = result;

                    mGetStatus = 0;
                }
            return mStatisticsData;
        }

    }

    public class StatisticsGroup
    {
        public StatisticsGroup(Statistics statistics)
        {
            Statistics = statistics;
        }

        public Statistics Statistics { get; set; }

        public String Url { get; set; }

        public string Server { get; set; }

        public StatisticsData All { get; set; }

        public StatisticsData Other { get; set; }

        public StatisticsData _1xx { get; set; }
        public StatisticsData _2xx { get; set; }
        public StatisticsData _3xx { get; set; }
        public StatisticsData _4xx { get; set; }
        public StatisticsData _5xx { get; set; }
    }

    public class StatisticsData
    {
        public StatisticsData()
        {
            Times = new List<long>();
            TimesRps = new List<long>();
        }

        public string Name { get; set; }

        public long Count { get; set; }

        public long Rps { get; set; }

        public List<long> Times { get; set; }

        public List<long> TimesRps { get; set; }


        public double CreateTime { get; set; }

        public IList<TimeData> GetTimeDatas()
        {
            List<TimeData> result = new List<TimeData>();
            result.Add(new TimeData(0, 0, 10, Times[0], TimesRps[0]));
            result.Add(new TimeData(1, 10, 20, Times[1], TimesRps[1]));
            result.Add(new TimeData(2, 20, 50, Times[2], TimesRps[2]));
            result.Add(new TimeData(3, 50, 100, Times[3], TimesRps[3]));
            result.Add(new TimeData(4, 100, 200, Times[4], TimesRps[4]));
            result.Add(new TimeData(5, 200, 500, Times[5], TimesRps[5]));
            result.Add(new TimeData(6, 500, 1000, Times[6], TimesRps[6]));
            result.Add(new TimeData(7, 1000, 2000, Times[7], TimesRps[7]));
            result.Add(new TimeData(8, 2000, 5000, Times[8], TimesRps[8]));
            result.Add(new TimeData(9, 5000, 10000, Times[9], TimesRps[9]));
            result.Add(new TimeData(10, 10000, 0, Times[10], TimesRps[10]));
            return result;
        }

        public class TimeData
        {
            public TimeData(int index, int start, int end, long count, long rps)
            {
                StartTime = start;
                EndTime = end;
                Count = count;
                Rps = rps;
                Index = index;
            }

            public int Index { get; set; }

            public int StartTime { get; set; }

            public int EndTime { get; set; }

            public long Count { get; set; }

            public long Rps { get; set; }

            public string Name
            {
                get
                {
                    string name;
                    if (StartTime > 0 && EndTime > 0)
                    {
                        if (StartTime >= 1000)
                            name = $"{StartTime / 1000}s";
                        else
                            name = $"{StartTime}ms";

                        if (EndTime >= 1000)
                            name += $"-{EndTime / 1000}s";
                        else
                            name += $"-{EndTime}ms";

                    }
                    else if (StartTime > 0)
                    {
                        if (StartTime >= 1000)
                            name = $">{StartTime / 1000}s";
                        else
                            name = $">{StartTime}ms";
                    }
                    else
                    {
                        name = $"<{EndTime}ms";
                    }
                    return name;
                }
            }
        }

    }

    public class UrlStatistics
    {
        public UrlStatistics(string url)
        {
            Statistics.Url = url;
        }

        public string Path { get; set; }

        public Statistics Statistics { get; internal set; } = new Statistics();

        public ConcurrentDictionary<string, ServerStatistics> Servers { get; internal set; } = new ConcurrentDictionary<string, ServerStatistics>();

        public void Add(int code, long time, Servers.ServerAgent server)
        {
            Statistics.Add(code, time);
            if (server != null)
            {
                if (!Servers.TryGetValue(server.UriKey, out ServerStatistics s))
                {
                    lock (Servers)
                    {
                        if (!Servers.TryGetValue(server.UriKey, out s))
                        {
                            s = new ServerStatistics(server.UriKey);
                            s.Statistics.Url = this.Statistics.Url;
                            Servers[s.Host] = s;
                        }
                    }
                }
                s.Add(code, time);
            }
        }

        public class ServerStatistics
        {
            public ServerStatistics(string host)
            {
                Host = host;
            }

            public Statistics Statistics { get; internal set; } = new Statistics();

            public string Host { get; internal set; }

            public void Add(int code, long time)
            {
                Statistics.Add(code, time);
            }
        }

        public UrlStatisticsReport GetResult()
        {
            return new UrlStatisticsReport(this);
        }
    }

    public class UrlStatisticsReport
    {
        public UrlStatisticsReport(UrlStatistics item)
        {
            All = item.Statistics.GetData();
            Statistics = item.Statistics;
            foreach (var s in item.Servers)
            {
                var d = s.Value.Statistics.GetData();
                Servers[d.Server] = d;
            }
        }

        public Statistics Statistics { get; private set; }

        public StatisticsGroup All { get; set; }

        public Dictionary<string, StatisticsGroup> Servers { get; private set; } = new Dictionary<string, StatisticsGroup>();
    }

}
