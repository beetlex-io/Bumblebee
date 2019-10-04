using System;
using System.Reflection;
using BeetleX.FastHttpApi;
using Bumblebee.Events;
using Bumblebee.Servers;
using Newtonsoft.Json.Linq;

namespace Bumblebee.BaseSample
{
    class Program
    {
        private static Gateway g;
        static void Main(string[] args)
        {
            g = new Gateway();
            g.HttpOptions(h =>
            {
                h.Port = 9090;
                h.LogToConsole = true;

            });
            g.LoadPlugin(typeof(Program).Assembly);
            g.SetServer("http://192.168.2.25:9090").AddUrl("*", 0, 0);
            g.SetServer("http://192.168.2.26:9090").AddUrl("*", 0, 0);
            g.Open();
            g.Pluginer.SetRequesting("RequestingTest");
            g.Pluginer.SetRequested("RequestedTest");
            Console.Read();
        }



    }


    public class RequestingTest : Plugins.IRequestingHandler
    {
        public string Name => "RequestingTest";

        public string Description => "RequestingTest";

        public void Execute(EventRequestingArgs e)
        {
            //e.Gateway.Response(e.Response, new NotFoundResult("Gateway not found!"));
            //e.Cancel = true;
            Console.WriteLine($"{e.Request.Url} requesting");
        }

        public void Init(Gateway gateway, Assembly assembly)
        {

        }

        public void LoadSetting(JToken setting)
        {

        }

        public object SaveSetting()
        {
            return null;
        }
    }

    public class RequestedTest : Plugins.IRequestedHandler
    {
        public string Name => "RequestedTest";

        public string Description => "RequestedTest";

        public void Execute(EventRequestCompletedArgs e)
        {
            Console.WriteLine($"{e.Url} request to {e.Server.Uri} user time {e.Time}ms");
        }

        public void Init(Gateway gateway, Assembly assembly)
        {

        }

        public void LoadSetting(JToken setting)
        {

        }

        public object SaveSetting()
        {
            return null;
        }
    }
}
