using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee
{

    public class Statistics
    {
        public Statistics()
        {
            CodeStatistics = new CodeStatistics[701];
            for (int i = 0; i < 701; i++)
            {
                CodeStatistics[i] = new CodeStatistics();
            }

        }

        private long mStatus_1xx;

        private long mStatus_2xx;

        private long mStatus_3xx;

        private long mStatus_4xx;

        private long mStatus_5xx;

        private long mStatusOther;

        public long Status_1xx => mStatus_1xx;

        public long Status_2xx => mStatus_2xx;

        public long Status_3xx => mStatus_3xx;

        public long Status_4xx => mStatus_4xx;

        public long Status_5xx => mStatus_5xx;

        public long All => CodeStatistics[0].Count;

        public CodeStatistics[] CodeStatistics { get; private set; }

        public void Add(int code)
        {
            CodeStatistics[0].Add();
            if (code >= 100 && code < 200)
                System.Threading.Interlocked.Increment(ref mStatus_1xx);
            else if (code >= 200 && code < 300)
                System.Threading.Interlocked.Increment(ref mStatus_2xx);
            else if (code >= 300 && code < 400)
                System.Threading.Interlocked.Increment(ref mStatus_3xx);
            else if (code >= 400 && code < 500)
                System.Threading.Interlocked.Increment(ref mStatus_4xx);
            else if (code >= 500 && code < 600)
                System.Threading.Interlocked.Increment(ref mStatus_5xx);
            else
            {
                System.Threading.Interlocked.Increment(ref mStatusOther);
            }

            if (code >= 701)
            {
                CodeStatistics[700].Add();
            }
            else
            {
                CodeStatistics[code].Add();
            }
        }
    }

    public class CodeStatistics
    {
        public CodeStatistics()
        {

        }
        private long mCount;

        public int Code { get; set; }

        public long Count => mCount;

        public void Add()
        {
            System.Threading.Interlocked.Increment(ref mCount);
        }

    }
}
