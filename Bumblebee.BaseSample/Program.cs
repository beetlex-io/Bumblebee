using System;
using BeetleX.FastHttpApi;
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
          
             g.SetServer("http://localhost:9000").AddUrl("*", 0);
            //g.SetServer("http://192.168.2.26:9090").AddUrl("*", 0);
            //g.SetServer("http://192.168.2.27:9090").AddUrl("/order.*", 0);
            //g.SetServer("http://192.168.2.28:9090").AddUrl("/order.*", 0);
            g.Open();
            Console.Read();
        }

     
    }
}
