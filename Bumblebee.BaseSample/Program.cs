using System;
using BeetleX.FastHttpApi;
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
            g.SetServer("http://192.168.2.26:9090").AddUrl("*", 0);
            g.SetServer("http://192.168.2.27:9090").AddUrl("/order.*", 0);
            g.SetServer("http://192.168.2.28:9090").AddUrl("/order.*", 0);
            g.Requesting += (o, e) =>
            {
                Console.WriteLine("Requesting");
                Console.WriteLine($"    Request url ${e.Request.BaseUrl}");
                //e.Cancel = true;
            };
            g.AgentRequesting += (o, e) =>
            {
                Console.WriteLine("Agent requesting:");
                Console.WriteLine($"    Request url ${e.Request.BaseUrl}");
                Console.WriteLine($"    url route {e.UrlRoute}");
                Console.WriteLine($"    agent server {e.Server.Uri}");
                //e.Cancel = true;
            };
            g.Requested += (o, e) =>
            {
                Console.WriteLine("Requested");
                Console.WriteLine($"    Request url ${e.Request.BaseUrl}");
                Console.WriteLine($"    url route {e.UrlRoute}");
                Console.WriteLine($"    agent server {e.Server.Uri}");
                Console.WriteLine($"    response code {e.Code} use time {e.Time}ms");
            };
            g.HeaderWriting += (o, e) =>
            {
                Console.WriteLine("Header Writing");
                Console.WriteLine($"    {e.Server.Uri} {e.Name}:{e.Value}");
                if (e.Name == "Content-Type")
                {
                    e.Write(e.Name, "html");
                    e.Cancel = true;
                }
            };
            g.HeaderWrited += (o, e) =>
            {
                e.Write("compaly", "ikende.com");
                Console.WriteLine("Header Writed");
                Console.WriteLine($"    {e.Server.Uri} header writed");
            };
            g.Open();
            g.AddFilter<NotFountFilter>();
            // g.Routes.GetRoute("*").SetFilter("NotFountFilter");


            Console.Read();
        }

        public class NotFountFilter : Filters.IRequestFilter
        {
            public string Name => "NotFountFilter";

            public void Executed(Gateway gateway, HttpRequest request, HttpResponse response, ServerAgent server, int code, long useTime)
            {

            }

            public bool Executing(Gateway gateway, HttpRequest request, HttpResponse response)
            {
                gateway.Response(response, new NotFoundResult("test"));
                return false;
            }
        }

    }
}
