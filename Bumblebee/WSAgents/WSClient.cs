using BeetleX;
using BeetleX.Clients;
using BeetleX.FastHttpApi.WebSockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bumblebee.WSAgents
{
    public class WSClient : IDisposable
    {

        public WSClient(string host) : this(new Uri(host)) { }

        public WSClient(Uri host)
        {
            UriHost = host;
            Host = UriHost.Host;
            byte[] key = new byte[16];
            new Random().NextBytes(key);
            SecWebSocketKey = Convert.ToBase64String(key);
            this.Origin = UriHost.OriginalString;
            this.SSLAuthenticateName = this.Origin;
        }



        public Response Response { get; internal set; }

        public byte[] MaskKey { get; set; }

        public string SecWebSocketKey { get; set; }

        private AsyncTcpClient mNetClient;

        private System.Collections.Concurrent.ConcurrentQueue<AgentDataFrame> mDataFrames = new System.Collections.Concurrent.ConcurrentQueue<AgentDataFrame>();

        private bool OnWSConnected = false;

        private void OnPacketCompleted(IClient client, object message)
        {
            if (message is AgentDataFrame dataFrame)
            {
                OnReceiveMessage(dataFrame);
            }
            else
            {
                OnConnectResponse(null, message as Response);
            }
        }

        public DateTime PingPongTime { get; private set; }

        public event System.EventHandler<WSReceiveArgs> DataReceive;

        public void Ping()
        {
            AgentDataFrame pong = new AgentDataFrame();
            pong.Type = DataPacketType.ping;
            Send(pong);
        }

        public Servers.ServerAgent ServerAgent { get; set; }

        protected virtual void OnDataReceive(WSReceiveArgs e)
        {
            try
            {
                DataReceive?.Invoke(this, e);
            }
            catch (Exception e_)
            {
                try
                {
                    e.Error = new BXException($"ws client receive error {e_.Message}", e_);
                    DataReceive?.Invoke(this, e);
                }
                catch { }
            }
        }

        protected virtual void OnReceiveMessage(AgentDataFrame message)
        {
            if (message.Type == DataPacketType.connectionClose)
            {
                Dispose();
                OnClientError(mNetClient, new ClientErrorArgs { Error = new BXException("ws connection close!"), Message = "ws connection close" });
                return;
            }
            if (message.Type == DataPacketType.ping || message.Type == DataPacketType.pong)
            {
                PingPongTime = DateTime.Now;
                if (message.Type == DataPacketType.ping)
                {
                    AgentDataFrame pong = new AgentDataFrame();
                    pong.Type = DataPacketType.pong;
                    Send(pong);
                }
                return;
            }
            else
            {
                OnDataReceive(message);
            }

        }

        protected virtual void OnDataReceive(AgentDataFrame data)
        {
            if (DataReceive != null)
            {
                WSReceiveArgs e = new WSReceiveArgs();
                e.Client = this;
                e.Frame = data;
                DataReceive(this, e);
            }

        }

        private void OnClientError(IClient c, ClientErrorArgs e)
        {
            if (OnWSConnected)
            {
                if (e.Error is BXException)
                {
                    OnWSConnected = false;
                }
                try
                {
                    WSReceiveArgs wse = new WSReceiveArgs();
                    wse.Client = this;
                    wse.Error = e.Error;
                    DataReceive?.Invoke(this, wse);
                }
                catch { }
            }
            else
            {
                OnConnectResponse(e.Error, null);
            }

        }

        private TaskCompletionSource<bool> mWScompletionSource;

        private void OnWriteConnect()
        {
            var stream = mNetClient.Stream.ToPipeStream();
            stream.WriteLine($"{Method} {Path} HTTP/1.1");
            stream.WriteLine($"Host: {Host}");
            stream.WriteLine($"Upgrade: websocket");
            stream.WriteLine($"Connection: Upgrade");
            foreach (var item in Headers)
            {
                stream.WriteLine($"{item.Key}: {item.Value}");
            }
            stream.WriteLine($"Origin: {Origin}");
            stream.WriteLine($"Sec-WebSocket-Key: {SecWebSocketKey}");
            stream.WriteLine($"Sec-WebSocket-Version: {SecWebSocketVersion}");
            stream.WriteLine("");
            mNetClient.Stream.Flush();
        }

        private object mLockConnect = new object();

        public bool IsConnected => OnWSConnected && mNetClient != null && mNetClient.IsConnected;

        private void Connect()
        {
            if (IsConnected)
            {
                return;
            }
            lock (mLockConnect)
            {
                if (IsConnected)
                {
                    return;
                }
                mWScompletionSource = new TaskCompletionSource<bool>();
                if (mNetClient == null)
                {
                    string protocol = UriHost.Scheme.ToLower();
                    if (!(protocol == "ws" || protocol == "wss"))
                    {
                        OnConnectResponse(new BXException("protocol error! host must [ws|wss]//host:port"), null);
                        mWScompletionSource.Task.Wait();
                    }
                    WSPacket wSPacket = new WSPacket
                    {
                        WSClient = this
                    };
                    if (UriHost.Scheme.ToLower() == "wss")
                    {
                        mNetClient = SocketFactory.CreateSslClient<AsyncTcpClient>(wSPacket, UriHost.Host, UriHost.Port, SSLAuthenticateName);
                    }
                    else
                    {
                        mNetClient = SocketFactory.CreateClient<AsyncTcpClient>(wSPacket, UriHost.Host, UriHost.Port);
                    }
                    mNetClient.LittleEndian = false;
                    mNetClient.PacketReceive = OnPacketCompleted;
                    mNetClient.ClientError = OnClientError;
                }
                mDataFrames = new System.Collections.Concurrent.ConcurrentQueue<AgentDataFrame>();
                bool isnew;
                if (mNetClient.Connect(out isnew))
                {
                    OnWriteConnect();
                }
                else
                {
                    OnConnectResponse(mNetClient.LastError, null);
                }
                mWScompletionSource.Task.Wait();
            }
        }

        protected virtual void OnConnectResponse(Exception exception, Response response)
        {
            Response = response;
            Task.Run(() =>
            {
                if (exception != null)
                {
                    OnWSConnected = false;
                    mWScompletionSource?.TrySetException(exception);
                }
                else
                {
                    if (response.Code != 101)
                    {
                        OnWSConnected = false;
                        mWScompletionSource?.TrySetException(new BXException($"ws connect error {response.Code} {response.Message}"));

                    }
                    else
                    {
                        OnWSConnected = true;
                        mWScompletionSource?.TrySetResult(true);
                    }
                }
            });
        }

        public void Send(AgentDataFrame data)
        {
            Connect();
            mNetClient.Send(data);
        }

        public void Dispose()
        {
            OnWSConnected = false;
            mNetClient.DisConnect();
            mNetClient = null;
        }

        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

        public string SecWebSocketProtocol { get; set; } = "websocket, beetlex";

        public string SecWebSocketVersion { get; set; } = "13";

        public string Method { get; set; } = "GET";

        public string Path { get; set; } = "/";

        public string SSLAuthenticateName { get; set; }

        public string Origin { get; set; }

        public string Host { get; set; }

        public Uri UriHost { get; set; }
    }
}
