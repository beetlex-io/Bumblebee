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
            g.SetServer("http://localhost:5000").AddUrl("*", 0, 0);
            g.Pluginer.SetResponseError("CustomResponError");
            g.Open();
            Console.Read();
        }



    }

    public class CustomResponError : Plugins.IResponseErrorHandler
    {
        public string Name => "CustomResponError";

        public string Description => "CustomResponError";

        public bool Enabled { get; set; } = true;

        public void Exeucte(EventResponseErrorArgs e)
        {
            Console.WriteLine(e.Result);
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

    public class RequestingTest : Plugins.IRequestingHandler
    {
        public string Name => "RequestingTest";

        public string Description => "RequestingTest";

        public bool Enabled { get; set; } = true;

        public void Execute(EventRequestingArgs e)
        {
            e.Gateway.Response(e.Response, new NotFoundResult("Gateway not found!"));
            e.Cancel = true;
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

    public class AgentRequestingTest : Plugins.IAgentRequestingHandler
    {
        public string Name => "AgentRequestingTest";

        public string Description => "AgentRequestingTest";

        public bool Enabled { get; set; } = true;

        public void Execute(EventAgentRequestingArgs e)
        {
            Console.WriteLine("AgentRequestingTest");
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

    public class HeaderWritingTest : Plugins.IHeaderWritingHandler
    {
        public string Name => "HeaderWritingTest";

        public string Description => "HeaderWritingTest";

        public bool Enabled { get; set; } = true;

        public void Execute(EventHeaderWritingArgs e)
        {
            e.Header.Add("username", "henryfan");
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

        public bool Enabled { get; set; } = true;

        public void Execute(EventRequestCompletedArgs e)
        {
            Console.WriteLine("RequestedTest");
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
