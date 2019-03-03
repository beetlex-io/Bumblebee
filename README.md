Bumblebee是`.netcore`下开源基于`BeetleX.FastHttpApi`扩展的HTTP微服务网关组件，它的主要作用是针对WebAPI集群服务作一个集中的转发和管理；作为应用网关它提供了应用服务负载，故障迁移，安全控制，监控跟踪和日志处理等。它最大的一个特点是基于`C#`开发，你可以针对自己业务的需要对它进行扩展具体的业务功能。
## 组件部署
组件的部署一般根据自己的需要进行引用扩展功能，如果你只需要简单的应用服务负载、故障迁移和恢复等功能只需要下载[Bumblebee.ConsoleServer](https://github.com/IKende/Bumblebee/tree/master/Bumblebee.ConsoleServer)编译部署即可（暂没提供编译好的版本）。`Bumblebee.ConsoleServer`提供两个配置文件描述'HttpConfig.json'和'Gateway.json'分别用于配置HTTP服务和网关对应的负载策略。
## 性能测试(Bumblebee vs Ocelot)
**测试服务配置** E3 1230v2 16G windows 2008  Network:10Gb

**测试工具** ab和bombardier

**测试代码** [https://github.com/IKende/Bumblebee/tree/master/BumblebeeVSOcelot](https://github.com/IKende/Bumblebee/tree/master/BumblebeeVSOcelot)


**测试内容** 分别启用500,1000和2000个连接进行请求并发测试

## ab测试结果
![](https://i.imgur.com/rE97kRQ.png)
## bombardier测试结果
![](https://i.imgur.com/6BfQVjo.png)
## 可运行在什么系统
任何运行.net core 2.1或更高版本的操作系统(liinux,windows等)
## HTTP配置
'HttpConfig.json'是用于配置网关的HTTP服务信息，主要包括服务端，HTTPs和可处理的最大连接数等。
```
{
  "HttpConfig": {
    "Host": "",               //服务绑定的地址，不指定的情况默认绑定所有IPAddress.Any
    "Port": 9090,          //网关对外服务端口
    "SSL": false,          //是否开启HTTPs服务，如果开启默认绑定443端口
    "CertificateFile": "",          //证书文件
    "CertificatePassword": ",  //证书密码
    "UseIPv6":true                  //是否开启ipv6
  }
}
```
## 网关策略配置
'Gateway.json'主要用于配置负载的服务信息，主要包括负载的服务应用 和负载策略等
```
{
  "Servers": [  //需要负载的服务应列表
    {
      "Uri": "http://192.168.2.19:9090/",  //服务地址，可指定域名
      "MaxConnections": 1000   //指向服务的最大连接数
    },
    {
      "Uri": "http://192.168.2.25:9090/",
      "MaxConnections": 1000
    }
  ],
  "Urls": [  //负载的Url策略
    {
      "Url": "*",   //*是优先级最低的匹配策略，优先级采用长正则匹配
      "HashPattern": null, //一致负载描述，不配置的情况采用权重描述
      "Servers": [   //对应Url负载的服务应
        {
          "Url": "http://192.168.2.19:9090/", //服务地址，可指定域名
          "Weight": 10 ,  //对应的权重，区间在0-10之前，0一般情况不参与负载，只有当其他服务不可用的情况才加入
           "MaxRps": 0  //RPS限制，默认零是作任何限制
        },
        {
          "Url": "http://192.168.2.25:9090/",
          "Weight": 5
        }
      ]
    }
  ]
}
```
### HashPattern
如果需要一致性负载的时候需要设置，可以通过获到Url,Header,QueryString等值作为一致性负载值。设置方式如下：
```
[host|url|baseurl|(h:name)|(q:name)]
```
可以根据实际情况选择其中一种方式

- **Host**
使用Header的Host作为一致性转发

- **url**
使用整个Url作为一致性转发

- **baseurl**
使用整个BaseUrl作为一致性转发

- **h:name**
使用某个Header值作为一致性转发

- **q:name**
使用某个QueryString值作为一致性转发

## 应用扩展
`Bumblebee`只是一件组件，最终肯定需要针对业务需求来扩展它来实现相关功能；在讲解之前先看一下组件执行代理负载的流程图：


![](http://img2.tomap.me/images/02/1550798852958_image.png)

组件提供了不同的事件事件和一组过虑器来实现功能扩展，通过事件和过虑器可以对请求进行验证，拦截，日志记录和监控处理等功能。以下简单地预览一下相关事件的实现
```
            g.HeaderWriting += (o, e) =>
            {
                System.Console.WriteLine($"{e.Server.Uri} {e.Name}:{e.Value}");
                if (e.Name == "Content-Type")
                {
                    e.Write(e.Name, "html");
                    e.Cancel = true;
                }
            };
            g.HeaderWrited += (o, e) =>
            {
                e.Write("compaly", "ikende.com");
                System.Console.WriteLine($"{e.Server.Uri} header writed");
            };
            g.Requesting += (o, e) =>
            {
                Console.WriteLine("Requesting");
                Console.WriteLine($"    Request url ${e.Request.BaseUrl}");
                //e.Cancel = true;
            };
            g.AgentRequesting += (o, e) =>
            {
                Console.WriteLine("agent requesting:");
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
```
## 如何验证请求
对于微服务网关来说，统一控制用户请求的有效性是重要的功能；虽然组件没有集成这些策略配置，不过可以通过制定组件的事件或`IRequestFilter`来实现控制。
### Requesting事件
`Requesting`是网关组件接受请求后触发的事件，通过这个事件可以对来源的一些请求信息进行验证，并决定是否继续转发下去；定义事件代码如下:
```
    g.Requesting += (o, e) =>
    {
            //e.Request
            //e.Response
            e.Gateway.Response(e.Response, new NotFoundResult("test"));
            e.Cancel = true;
    };
```
通过设置`e.Cancel`属性来确定是否转发来源的请求。
### IRequestFilter
`IRequestFilter`是组件针对相应Url请求处理的过虑器，可以实现这一接口对某些请求的Url进行控制处理。接口实现方式大致如下：
```
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
```
添加Filter到网关，并设置到`*`上.
```
            g.AddFilter<NotFountFilter>();
            g.Routes.GetRoute("*").SetFilter("NotFountFilter");
```
## 断熔扩展
同样组件并不提供服务断熔的处理，但通过扩展的确可以轻松地完成这个工作。首先可以在`Requested`事件统计完成的情况，参考指标可以是，url信息，`5xx`状态、加响应延时等进行一个连续计数并生成断熔策略，通过这些策略数据就可以在`Requesting`或`IRequestFilter`对相应的请求进行控制。大概的扩展流程如下:


![](http://img2.tomap.me/images/02/1550801221806_image.png)


## 监控统计
由于网关需要处理大量的请求转和规则处理，所以组件默认并没有提供详细的监控和日志功能，不过组件同样提供事件方式来制定这些数据的记录。用户可能通过事件把数据记录到自有的系统中进行分析统计，这些数据主要包括：Header,Cookie,QueryString,http请求的状态和处理损耗的时间.事件定义如下:
```
            g.Requested += (o, e) =>
            {
                //e.Request 请求信息
                //e.Response 响应信息
                //e.Code   Http状态
                //e.Time   执行完成时间，单位毫秒
                //e.Server 接收请求的服务
            };
```
以下是针组件数据收集的一些统计扩展实例.


![](http://img2.tomap.me/images/02/1550802462603_image.png)



![](http://img3.tomap.me/images/07/1550802481058_image.png)



![](http://img3.tomap.me/images/03/1550802504547_image.png)



## 性能测试
作为网关，性能和可靠性比较重要，毕竟它是服务之首；以下是针对Bumblebee作为代理网关的测试，主要测试不同数据情况下的性能指标。测试配置描述

- 网关服务器:e3-1230v2,部署Bumblebee
- webapi服务器:e5-2676v2,部署webapi
- 测试服务器:e5-2676v2,测试工具bombardier
- 测试带宽环境:10Gb


### plaintext
```
D:\>bombardier.exe -c 500 -n 1000000 http://192.168.2.18:9090/home/plaintext
Bombarding http://192.168.2.18:9090/home/plaintext with 1000000 request(s) using
 500 connection(s)
 1000000 / 1000000 [===============================================] 100.00% 9s
Done!
Statistics        Avg      Stdev        Max
  Reqs/sec    104050.45   15852.09  133791.97
  Latency        4.80ms    10.35ms      3.06s
  HTTP codes:
    1xx - 0, 2xx - 1000000, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:    19.15MB/s
```
###  json
```
D:\>bombardier.exe -c 500 -n 1000000 http://192.168.2.18:9090/home/json
Bombarding http://192.168.2.18:9090/home/json with 1000000 request(s) using 500
connection(s)
 1000000 / 1000000 [===============================================] 100.00% 9s
Done!
Statistics        Avg      Stdev        Max
  Reqs/sec    105541.22    9336.18  126993.02
  Latency        4.73ms     1.45ms   337.02ms
  HTTP codes:
    1xx - 0, 2xx - 1000000, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:    20.90MB/s
```
###  employees
```
D:\>bombardier.exe -c 500 -n 1000000 http://192.168.2.18:9090/home/employees
Bombarding http://192.168.2.18:9090/home/employees with 1000000 request(s) using
 500 connection(s)
 1000000 / 1000000 [==============================================] 100.00% 14s
Done!
Statistics        Avg      Stdev        Max
  Reqs/sec     69943.34    8672.45   91544.97
  Latency        7.02ms     2.75ms   641.04ms
  HTTP codes:
    1xx - 0, 2xx - 1000000, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:   361.74MB/s
```
### orders
```
D:\>bombardier.exe -c 500 -n 1000000 http://192.168.2.18:9090/home/orders
Bombarding http://192.168.2.18:9090/home/orders with 1000000 request(s) using 50
0 connection(s)
 1000000 / 1000000 [==============================================] 100.00% 12s
Done!
Statistics        Avg      Stdev        Max
  Reqs/sec     78498.29   15013.95  101544.42
  Latency        6.22ms     5.33ms   689.04ms
  HTTP codes:
    1xx - 0, 2xx - 1000000, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:   260.52MB/s
D:\>
```
