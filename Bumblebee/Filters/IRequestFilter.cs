using BeetleX.FastHttpApi;
using Bumblebee.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Filters
{
    public interface IRequestFilter
    {
        string Name { get; }

        bool Executing(Gateway gateway, HttpRequest request, HttpResponse response);

        void Executed(Gateway gateway, HttpRequest request, HttpResponse response, ServerAgent server, int code,long useTime);
    }
}
