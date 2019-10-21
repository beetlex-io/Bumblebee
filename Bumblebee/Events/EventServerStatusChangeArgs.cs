using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Events
{
    public class EventServerStatusChangeArgs
    {
        public Gateway Gateway { get; internal set; }

        public Servers.ServerAgent Server { get; internal set; }

        public bool Available { get; internal set; }
    }
}
