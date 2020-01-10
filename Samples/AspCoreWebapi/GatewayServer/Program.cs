using System;

namespace GatewayServer
{
    class Program
    {
        private static Bumblebee.Gateway mGateway;

        static void Main(string[] args)
        {
            mGateway = new Bumblebee.Gateway();
            mGateway.HttpOptions(o => { o.LogToConsole = true; o.Port = 80; o.LogLevel = BeetleX.EventArgs.LogType.Error; });
            //添中管理的服务，网关会对应用服务的可用状况进行监控
            mGateway.SetServer("http://192.168.2.18:8001");
            mGateway.SetServer("http://192.168.2.18:8002");
            mGateway.SetServer("http://192.168.2.18:8003");

            //default的是'*',匹配优先级最低
            mGateway.Routes.Default
               .AddServer("http://192.168.2.18:8001", 10)
               .AddServer("http://192.168.2.18:8002", 10)
               .AddServer("http://192.168.2.18:8003", 10);
            mGateway.Open();
            Console.Read();
        }
    }
}
