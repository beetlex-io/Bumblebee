using Bumblebee.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    public interface IRequestedHandler : IPlugin
    {
        void Execute(EventRequestCompletedArgs e);
    }
}
