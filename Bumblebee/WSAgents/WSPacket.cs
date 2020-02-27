using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeetleX;
using BeetleX.Clients;

namespace Bumblebee.WSAgents
{
    public class WSPacket : BeetleX.Clients.IClientPacket
    {
        public EventClientPacketCompleted Completed { get; set; }

        public IClientPacket Clone()
        {
            WSPacket result = new WSPacket();
            result.WSClient = this.WSClient;
            return result;
        }

        public Response Response { get; set; } = new Response();

        private bool OnWSConnected = false;

        private AgentDataFrame mReceiveFrame;

        public WSClient WSClient { get; set; }

        public void Decode(IClient client, Stream stream)
        {
            try
            {
                if (!OnWSConnected)
                {
                    if (Response.Read(stream.ToPipeStream()))
                    {
                        Completed?.Invoke(client, Response);
                        OnWSConnected = true;
                    }
                }
                else
                {
                    if (mReceiveFrame == null)
                        mReceiveFrame = new AgentDataFrame();
                    if (mReceiveFrame.Read(stream.ToPipeStream()) == DataPacketLoadStep.Completed)
                    {
                        Completed?.Invoke(client, mReceiveFrame);
                        mReceiveFrame = null;
                    }
                }
            }
            catch(Exception e_)
            {
                throw new BXException("ws protocol decode error", e_);
            }
            
        }

        public void Dispose()
        {
            WSClient = null;
        }

        public void Encode(object data, IClient client, Stream stream)
        {
            try
            {
                ((AgentDataFrame)data).Write(stream.ToPipeStream());
            }
            catch(Exception e_)
            {
                throw new BXException("ws protocol encode error", e_);
            }
        }
    }
}
