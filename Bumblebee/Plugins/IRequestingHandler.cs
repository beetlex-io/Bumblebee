using Bumblebee.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    public interface IRequestingHandler : IPlugin
    {
        void Execute(EventRequestingArgs e);
    }
}
