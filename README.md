# Bumblebee
.net core fast http gateway components 

# sample
```
    class Program
    {
        private static Gateway g;
        static void Main(string[] args)
        {
            g = new Gateway();
            g.HttpOptions(h => h.LogToConsole = true);
            g.AddServer("http://192.168.2.25:9090").AddUrl("*", 3);
            g.AddServer("http://192.168.2.26:9090").AddUrl("*", 10);
            g.Open();
            Console.Read();
        }
    }
```
open gateway default 8080 port
http://192.168.2.25:9090 weight 3
http://192.168.2.26:9090 weight 10
