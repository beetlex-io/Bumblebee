using Bumblebee.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    public interface IResponseErrorHandler: IPlugin
    {
        void Exeucte(EventResponseErrorArgs e);
    }
}
