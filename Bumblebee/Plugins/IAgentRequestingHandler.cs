using Bumblebee.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    public interface IAgentRequestingHandler: IPlugin
    {
        void Execute(EventAgentRequestingArgs e);
    }
}
