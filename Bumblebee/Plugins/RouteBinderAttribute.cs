using System;
using System.Collections.Generic;
using System.Text;

namespace Bumblebee.Plugins
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RouteBinderAttribute : Attribute
    {
        public RouteBinderAttribute()
        {

        }

        public bool ApiLoader { get; set; } = true;

        public string RouteUrl { get; set; }
    }
}
