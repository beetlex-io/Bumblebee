using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Filters
{
    public interface IRequestFilter
    {
        string Name { get; }

        bool Execute(HttpRequest request, HttpResponse response);
    }
}
