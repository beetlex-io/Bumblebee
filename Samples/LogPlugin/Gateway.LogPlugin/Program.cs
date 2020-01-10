using Bumblebee;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace HttpGateway.LogPlugin
{
    class Program
    {
        private static Gateway g;

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
                o.Port = 80;
                o.LogToConsole = true;
                o.WriteLog = true;
            });
            g.Open();
            g.LoadPlugin(typeof(Bumblebee.Configuration.Config).Assembly, typeof(Program).Assembly);

            g.Pluginer.SetRequested("custom_console_log");
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var ps = new ProcessStartInfo($"http://localhost:{g.HttpServer.Options.Port}/__system/bumblebee/index.html")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            return Task.CompletedTask;
        }
        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            g.Dispose();
            return Task.CompletedTask;
        }
    }
}
