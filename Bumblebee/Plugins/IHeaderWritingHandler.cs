using Bumblebee.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    public interface IHeaderWritingHandler : IPlugin
    {
        void Execute(EventHeaderWritingArgs e);
    }
}
