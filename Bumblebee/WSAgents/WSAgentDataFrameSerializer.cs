using BeetleX.Buffers;
using BeetleX.FastHttpApi.WebSockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.WSAgents
{
    public class WSAgentDataFrameSerializer : IDataFrameSerializer
    {
        public object FrameDeserialize(DataFrame data, PipeStream stream)
        {
            var len = (int)data.Length;
            var body = System.Buffers.ArrayPool<byte>.Shared.Rent(len);
            stream.Read(body, 0, len);
            return new ArraySegment<byte>(body, 0, len);

        }

        public void FrameRecovery(byte[] buffer)
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }

        public ArraySegment<byte> FrameSerialize(DataFrame packet, object body)
        {
            if (body is AgentDataFrame adf)
            {
                return adf.Body.Value;
            }
            else
            {
                packet.Type = DataPacketType.text;
                var data = System.Buffers.ArrayPool<byte>.Shared.Rent(1024 * 2);
                string text = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                int len = Encoding.UTF8.GetBytes(text, 0, text.Length, data, 0);
                return new ArraySegment<byte>(data, 0, len);
            }
        }
    }
}
