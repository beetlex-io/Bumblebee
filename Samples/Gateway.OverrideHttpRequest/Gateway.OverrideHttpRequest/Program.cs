using System;
using System.Threading;
using System.Threading.Tasks;
using BeetleX.FastHttpApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gateway.OverrideHttpRequest
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
        private Bumblebee.Gateway g;

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            g = new MyGateway();
            g.HttpOptions(o =>
            {
                o.Port = 80;
                o.LogToConsole = true;
                o.WriteLog = true;
            });
            g.Open();
            return Task.CompletedTask;
        }
        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            g.Dispose();
            return Task.CompletedTask;
        }
    }

  

}
