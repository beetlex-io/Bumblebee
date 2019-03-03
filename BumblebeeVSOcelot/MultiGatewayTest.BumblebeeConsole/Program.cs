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
            BufferPool.BUFFER_SIZE = 1024 * 8;
            BufferPool.POOL_MAX_SIZE = 1024 * 200;
            g = new Gateway();
            g.HttpOptions(o => { o.UrlIgnoreCase = false; });

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
