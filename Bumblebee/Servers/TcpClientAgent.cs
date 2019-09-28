using BeetleX.Clients;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Servers
{
    public class TcpClientAgent
    {
        private static long mId = 0;

        public static long GetID()
        {
            return System.Threading.Interlocked.Increment(ref mId);
        }

        public TcpClientAgent(string host, int port)
        {
            Buffer = new byte[1024 * 8];
            Client = BeetleX.SocketFactory.CreateClient<AsyncTcpClient>(host, port);
            Client.Connected = (c) => { c.Socket.NoDelay = true; };
            ID = GetID();
        }


        public TcpClientAgentStatus Status { get; set; } = TcpClientAgentStatus.None;
       

        public long ID { get; set; }

        public AsyncTcpClient Client { get; private set; }

        public byte[] Buffer { get; private set; }
    }

    public enum TcpClientAgentStatus
    {
        None,
        Requesting,
        RequestSuccess,
        RequestError,
        ResponseError,
        ResponseReceive,
        ResponseReceiveBody,
        ResponseReciveHeader,
        ResponseSuccess,
        Free,
    }
}
