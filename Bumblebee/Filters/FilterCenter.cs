using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;

namespace Bumblebee.Filters
{
    public class FilterCenter
    {
        public string[] Names
        {
            get
            {
                return RequestFilters.Keys.ToArray();
            }
        }

        public ConcurrentDictionary<string, IRequestFilter> RequestFilters { get; private set; }

        public FilterCenter(Gateway gateway)
        {
            Gateway = gateway;
            RequestFilters = new ConcurrentDictionary<string, IRequestFilter>();
        }

        public Gateway Gateway { get; set; }

        public IRequestFilter Remove(string name)
        {
            RequestFilters.TryRemove(name, out IRequestFilter item);
            return item;
        }

        public void Add<T>() where T : IRequestFilter, new()
        {
            T filter = new T();
            Add(filter);
        }

        public void Add(IRequestFilter requestFilter)
        {
            RequestFilters[requestFilter.Name] = requestFilter;
        }

        public IRequestFilter GetFilter(string name)
        {
            IRequestFilter result;
            RequestFilters.TryGetValue(name, out result);
            return result;
        }
    }
}
