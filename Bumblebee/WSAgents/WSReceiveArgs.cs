using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.WSAgents
{
    public class WSReceiveArgs:System.EventArgs
    {
        public WSClient Client { get; internal set; }

        public AgentDataFrame Frame { get; internal set; }

        public Exception Error { get; internal set; }
    }
}
