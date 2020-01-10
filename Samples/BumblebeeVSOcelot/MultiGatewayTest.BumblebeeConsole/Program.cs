using BeetleX.Buffers;
using Bumblebee;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiGatewayTest.BumblebeeConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<HttpServerHosted>();
                });
            builder.Build().Run();
        }
    }

    public class HttpServerHosted : IHostedService
    {
        private Gateway g;

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            g = new Gateway();
            g.HttpOptions(o =>
            {
                o.Port = 9001;
                o.LogToConsole = true;
            });
            g.Open();
            g.Routes.Default.AddServer("http://192.168.2.19:8080");
            g.SaveConfig();
            return Task.CompletedTask;
        }
        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            g.Dispose();
            return Task.CompletedTask;
        }
    }
}
