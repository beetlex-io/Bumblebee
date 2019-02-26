using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;
using BeetleX;
using System.Net.Sockets;
using Bumblebee.Events;
using System.Threading.Tasks;

namespace Bumblebee.Servers
{
    class RequestAgent
    {

        public RequestAgent(TcpClientAgent clientAgent, ServerAgent serverAgent, HttpRequest request, HttpResponse response,
            UrlRouteServerGroup.UrlServerInfo urlServerInfo, Routes.UrlRoute urlRoute)
        {
            mTransferEncoding = false;
            mRequestLength = 0;
            Code = 0;
            Server = serverAgent;
            Request = request;
            Response = response;
            mClientAgent = clientAgent;
            mClientAgent.Client.ClientError = OnSocketError;
            mClientAgent.Client.DataReceive = OnReveive;
            mBuffer = mClientAgent.Buffer;
            Status = RequestStatus.None;
            UrlServerInfo = urlServerInfo;
            UrlRoute = urlRoute;
            mStartTime = TimeWatch.GetElapsedMilliseconds();

        }

        private long mStartTime;

        private byte[] mBuffer;

        private TcpClientAgent mClientAgent;

        private int mRequestLength;

        private bool mTransferEncoding = false;

        public UrlRouteServerGroup.UrlServerInfo UrlServerInfo { get; private set; }

        public Routes.UrlRoute UrlRoute { get; set; }

        public HttpRequest Request { get; private set; }

        public long Time { get; set; }

        public HttpResponse Response { get; private set; }

        public ServerAgent Server { get; private set; }

        public int Code { get; set; }

        public RequestStatus Status { get; set; }

        private void OnSocketError(IClient c, ClientErrorArgs e)
        {
            HttpApiServer httpApiServer = Server.Gateway.HttpServer;
            if (httpApiServer.EnableLog(BeetleX.EventArgs.LogType.Error))
                httpApiServer.Log(BeetleX.EventArgs.LogType.Error, $"gateway request {Server.Host}:{Server.Port} error {e.Message}@{e.Error.InnerException?.Message} status {Status}");

            if (Status == RequestStatus.Requesting)
            {
                EventResponseErrorArgs erea;
                if (e.Error is SocketException)
                {
                    Code = Gateway.SOCKET_ERROR_CODE;
                    erea = new EventResponseErrorArgs(Request, Response, UrlRoute.Gateway, e.Error.Message, Gateway.SERVER_NET_ERROR);
                }
                else
                {
                    Code = Gateway.PROCESS_ERROR_CODE;
                    erea = new EventResponseErrorArgs(Request, Response, UrlRoute.Gateway, e.Error.Message, Gateway.SERVER_AGENT_PROCESS_ERROR);
                }
                OnCompleted(erea);
            }
            else
            {
                Code = Gateway.OTHRER_ERROR_CODE;
                if (Status > RequestStatus.None)
                {
                    OnCompleted(null);
                }
            }
        }

        public PipeStream GetRequestStream()
        {
            return Request.Session.Stream.ToPipeStream();
        }

        private void ResponseStatus(PipeStream pipeStream)
        {
            if (Status == RequestStatus.Responding)
            {
                var indexof = pipeStream.IndexOf(HeaderTypeFactory.LINE_BYTES);
                if (indexof.EofData != null)
                {
                    pipeStream.Read(mBuffer, 0, indexof.Length);
                    GetRequestStream().Write(mBuffer, 0, indexof.Length);
                    var result = HttpParse.AnalyzeResponseLine(new ReadOnlySpan<byte>(mBuffer, 0, indexof.Length - 2));
                    Code = result.Item2;
                    Status = RequestStatus.RespondingHeader;
                }
            }
        }

        private void ResponseHeader(PipeStream pipeStream)
        {
            PipeStream agentStream = GetRequestStream();
            if (Status == RequestStatus.RespondingHeader)
            {
                var indexof = pipeStream.IndexOf(HeaderTypeFactory.LINE_BYTES);
                while (indexof.End != null)
                {
                    pipeStream.Read(mBuffer, 0, indexof.Length);

                    if (indexof.Length == 2)
                    {
                        UrlRoute.Gateway.OnHeaderWrited(Request, Response, agentStream, Server);
                        agentStream.Write(mBuffer, 0, indexof.Length);
                        Status = RequestStatus.RespondingBody;
                        return;
                    }
                    else
                    {
                        var header = HttpParse.AnalyzeHeader(new ReadOnlySpan<byte>(mBuffer, 0, indexof.Length - 2));
                        if (string.Compare(header.Item1, HeaderTypeFactory.TRANSFER_ENCODING, true) == 0 && string.Compare(header.Item2, "chunked", true) == 0)
                        {
                            mTransferEncoding = true;
                        }
                        if (string.Compare(header.Item1, HeaderTypeFactory.CONTENT_LENGTH, true) == 0)
                        {
                            mRequestLength = int.Parse(header.Item2);
                        }
                        if (string.Compare(header.Item1, HeaderTypeFactory.SERVER, true) == 0)
                        {
                            agentStream.Write(Gateway.GATEWAY_SERVER_HEDER, 0, Gateway.GATEWAY_SERVER_HEDER.Length);
                        }
                        else
                        {
                            if (UrlRoute.Gateway.OnHeaderWriting(Request, Response, agentStream, Server, header.Item1, header.Item2))
                            {
                                agentStream.Write(mBuffer, 0, indexof.Length);
                            }
                        }
                    }
                    indexof = pipeStream.IndexOf(HeaderTypeFactory.LINE_BYTES);
                }
            }
        }

        private void ResponseBody(PipeStream pipeStream)
        {
            PipeStream agentStream = GetRequestStream();
            if (Status == RequestStatus.RespondingBody)
            {
                if (mTransferEncoding)
                {
                    while (pipeStream.Length > 0)
                    {
                        var len = pipeStream.Read(mBuffer, 0, mBuffer.Length);
                        agentStream.Write(mBuffer, 0, len);
                        bool end = true;
                        for (int i = 0; i < 5; i++)
                        {
                            if (HeaderTypeFactory.CHUNKED_BYTES[i] != mBuffer[len - 5 + i])
                            {
                                end = false;
                                break;
                            }
                        }
                        if (end)
                        {
                            OnCompleted(null);
                            Request.Session.Stream.Flush();
                            return;
                        }
                        else
                        {

                            Request.Session.Stream.Flush();

                        }
                    }
                }
                else
                {
                    if (mRequestLength == 0)
                    {
                        OnCompleted(null);
                        Request.Session.Stream.Flush();
                        return;
                    }
                    while (pipeStream.Length > 0)
                    {
                        if (mRequestLength > 0)
                        {
                            var len = pipeStream.Read(mBuffer, 0, mBuffer.Length);
                            mRequestLength -= len;
                            agentStream.Write(mBuffer, 0, len);
                        }
                        if (mRequestLength == 0)
                        {
                            OnCompleted(null);
                            Request.Session.Stream.Flush();
                            return;
                        }
                        else
                        {
                            Request.Session.Stream.Flush();
                        }
                    }
                }
            }
        }

        private void OnReveive(IClient c, ClientReceiveArgs reader)
        {
            PipeStream stream = reader.Stream.ToPipeStream();
            if (Status >= RequestStatus.Responding)
            {
                ResponseStatus(stream);
                ResponseHeader(stream);
                ResponseBody(stream);
            }
            else
            {
                stream.ReadFree((int)stream.Length);
            }
        }

        public void Execute()
        {
            var request = Request;
            var response = Response;
            Status = RequestStatus.Requesting;
            mClientAgent.Client.Connect();
            if (mClientAgent.Client.IsConnected)
            {
                try
                {
                    PipeStream pipeStream = mClientAgent.Client.Stream.ToPipeStream();
                    byte[] buffer = mBuffer;
                    int offset = 0;
                    var len = Encoding.UTF8.GetBytes(request.Method, 0, request.Method.Length, buffer, offset);
                    offset += len;

                    buffer[offset] = HeaderTypeFactory._SPACE_BYTE;
                    offset++;

                    len = Encoding.UTF8.GetBytes(request.Url, 0, request.Url.Length, buffer, offset);
                    offset += len;


                    buffer[offset] = HeaderTypeFactory._SPACE_BYTE;
                    offset++;

                    for (int i = 0; i < HeaderTypeFactory.HTTP_V11_BYTES.Length; i++)
                    {
                        buffer[offset + i] = HeaderTypeFactory.HTTP_V11_BYTES[i];
                    }
                    offset += HeaderTypeFactory.HTTP_V11_BYTES.Length;

                    buffer[offset] = HeaderTypeFactory._LINE_R;
                    offset++;

                    buffer[offset] = HeaderTypeFactory._LINE_N;
                    offset++;

                    pipeStream.Write(buffer, 0, offset);


                    request.Header.Write(pipeStream);
                    pipeStream.Write(HeaderTypeFactory.LINE_BYTES, 0, 2);
                    int bodylength = request.Length;
                    while (bodylength > 0)
                    {
                        len = request.Stream.Read(buffer, 0, buffer.Length);
                        pipeStream.Write(buffer, 0, len);
                        bodylength -= len;
                    }
                    Status = RequestStatus.Responding;
                    mClientAgent.Client.Stream.Flush();
                }
                catch (Exception e_)
                {
                    string error = $" request to {Server.Host}:{Server.Port} error {e_.Message}@{e_.StackTrace}";
                    EventResponseErrorArgs eventResponseErrorArgs =
                        new EventResponseErrorArgs(request, response, UrlRoute.Gateway, error, Gateway.SERVER_NET_ERROR);
                    try
                    {
                        if (mClientAgent.Client != null)
                            mClientAgent.Client.DisConnect();
                    }
                    finally
                    {
                        OnCompleted(eventResponseErrorArgs);
                    }
                    return;
                }
            }
        }

        private int mCompletedStatus = 0;

        private void OnCompleted(EventResponseErrorArgs error)
        {
            if (System.Threading.Interlocked.CompareExchange(ref mCompletedStatus, 1, 0) == 0)
            {
                Time = TimeWatch.GetElapsedMilliseconds() - mStartTime;
                mClientAgent.Client.ClientError = null;
                mClientAgent.Client.DataReceive = null;
                Server.Push(mClientAgent);
                try
                {
                    UrlRoute.FilterExecuted(this.Request, this.Response, this.Server, this.Code, this.Time);
                    Completed?.Invoke(this);
                }
                catch (Exception e_)
                {
                    if (Request.Server.EnableLog(BeetleX.EventArgs.LogType.Error))
                    {
                        Request.Server.Log(BeetleX.EventArgs.LogType.Error, $"gateway request {Server.Host}:{Server.Port} process completed event error {e_.Message}@{e_.StackTrace}");
                    }
                }
                finally
                {
                    Request.ClearStream();
                    if (error != null)
                    {
                        Server.Gateway.OnResponseError(error);
                    }
                    else
                        Request.Recovery();
                }

            }
        }

        public Action<RequestAgent> Completed { get; set; }

        public enum RequestStatus : int
        {
            None = 1,
            Requesting = 2,
            Responding = 8,
            RespondingHeader = 32,
            RespondingBody = 64
        }
    }
}
