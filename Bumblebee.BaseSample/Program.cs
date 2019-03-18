using System;
using System.Reflection;
using BeetleX.FastHttpApi;
using Bumblebee.Events;
using Bumblebee.Servers;

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
            g.SetServer("http://localhost:5000").AddUrl("*", 0, 0);
            g.Routes.Default.Pluginer.SetRequesting("RequestingTest");
            g.Open();
            Console.Read();
        }



    }


    public class RequestingTest : Plugins.IRequestingHandler
    {
        public string Name => "RequestingTest";

        public string Description => "RequestingTest";

        public void Execute(EventRequestingArgs e)
        {
            e.Gateway.Response(e.Response, new NotFoundResult("Gateway not found!"));
            e.Cancel = true;
        }

        public void Init(Gateway gateway, Assembly assembly)
        {

        }
    }

    public class AgentRequestingTest : Plugins.IAgentRequestingHandler
    {
        public string Name => "AgentRequestingTest";

        public string Description => "AgentRequestingTest";

        public void Execute(EventAgentRequestingArgs e)
        {
            Console.WriteLine("AgentRequestingTest");
        }

        public void Init(Gateway gateway, Assembly assembly)
        {

        }
    }

    public class HeaderWritingTest : Plugins.IHeaderWritingHandler
    {
        public string Name => "HeaderWritingTest";

        public string Description => "HeaderWritingTest";

        public void Execute(EventHeaderWritingArgs e)
        {
            e.Header.Add("username", "henryfan");
        }

        public void Init(Gateway gateway, Assembly assembly)
        {

        }
    }

    public class RequestedTest : Plugins.IRequestedHandler
    {
        public string Name => "RequestedTest";

        public string Description => "RequestedTest";

        public void Execute(EventRequestCompletedArgs e)
        {
            Console.WriteLine("RequestedTest");
        }

        public void Init(Gateway gateway, Assembly assembly)
        {

        }
    }
}
