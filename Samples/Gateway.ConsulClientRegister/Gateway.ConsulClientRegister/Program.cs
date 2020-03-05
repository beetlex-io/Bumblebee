using BeetleX.FastHttpApi.Hosting;
using Consul;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Gateway.ConsulClientRegister
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.UseBeetlexHttp(o =>
                    {
                        o.LogToConsole = true;
                        o.ManageApiEnabled = false;
                        o.Port = 8080;
                        o.SetDebug();
                        o.LogLevel = BeetleX.EventArgs.LogType.Warring;
                    },
                    s =>
                    {

                        var client = new Consul.ConsulClient(c =>
                        {
                            c.Address = new Uri("http://192.168.2.19:8500");
                        });
                        Dictionary<string, string> meta = new Dictionary<string, string>();
                        meta.Add("path", "^/home.*;^/hello.*");
                        client.Agent.ServiceDeregister("api_test1").Wait();
                        client.Agent.ServiceDeregister("api_test2").Wait();
                        client.Agent.ServiceRegister(new AgentServiceRegistration
                        {
                            Tags = new string[] { "Bumblebee" },
                            Address = "192.168.2.18",
                            Port = 8080,
                            Name = "bumblebee_services",
                            Meta = meta,
                            ID = "api_test"
                        }).Wait();
                    },
                    typeof(Program).Assembly);
                });
            builder.Build().Run();
        }
    }
    [BeetleX.FastHttpApi.Controller]
    public class Home
    {
        public string Hello(string name)
        {
            return $"hello {name} {DateTime.Now}";
        }
    }
}
