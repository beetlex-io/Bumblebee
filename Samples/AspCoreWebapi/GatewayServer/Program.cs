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
               .AddServer("http://192.168.2.18:8001", 10, 100)
               .AddServer("http://192.168.2.18:8002", 10, 100)
               .AddServer("http://192.168.2.18:8003", 10, 100);
            //以上是手动代码的方式来构建网关，实际上可以通过配置'Gateway.json'来描述即可,具本查看https://github.com/IKende/Bumblebee
            mGateway.Open();
            Console.Read();
        }
    }
}
