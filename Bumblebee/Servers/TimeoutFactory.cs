using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using BeetleX.EventArgs;

namespace Bumblebee.Servers
{
    class TimeoutFactory
    {
        private List<ConcurrentDictionary<long, RequestAgent>> mItems = new List<ConcurrentDictionary<long, RequestAgent>>();

        public TimeoutFactory(Gateway gateway)
        {
            mGateway = gateway;
            for (int i = 0; i < 64; i++)
            {
                mItems.Add(new ConcurrentDictionary<long, RequestAgent>());
            }
            mTimer = new System.Threading.Timer(OnTimeout, null, 1000, 1000);
        }

        private Gateway mGateway;

        private System.Threading.Timer mTimer;


        private ConcurrentDictionary<long, RequestAgent> GetTable(RequestAgent request)
        {
            return mItems[(int)(request.RequestID % mItems.Count)];
        }

        public void Add(RequestAgent requestAgent)
        {
            var table = GetTable(requestAgent);
            table[requestAgent.RequestID] = requestAgent;
        }


        public void Remove(RequestAgent requestAgent)
        {
            var table = GetTable(requestAgent);
            table.TryRemove(requestAgent.RequestID, out RequestAgent result);
        }

        private void OnTimeout(object state)
        {
            try
            {
                var time = BeetleX.TimeWatch.GetElapsedMilliseconds();
                for(int i=0;i<mItems.Count;i++)
                {
                    foreach(var item in mItems[i].Values)
                    {
                        if(time>item.TimerOutValue)
                        {
                            item.TimeOut();
                        }
                    }
                }
            }
            catch (Exception e_)
            {

                mGateway.HttpServer.GetLog(BeetleX.EventArgs.LogType.Error)?.Log(BeetleX.EventArgs.LogType.Error, $"Gateway process request timeout error  {e_.Message}");
            }
        }

    }
}
