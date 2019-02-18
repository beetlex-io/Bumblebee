# Bumblebee
基于BeetleX.FastHttpApi扩展的HTTP服务网关，可以实现高性能自定义的WebAPI网关服务；通过`Bumblebee`可以灵活地控制WebAPI应用服务负载，故障迁移，安全控制，监控跟踪和日志处理等.
## Bumblebee Console Server
在Bumblebee基础上扩展可配置运行的网关服务程序，通过配置文件即可实现WebAPI应用服务负载和故障迁移功能。
### Console Server Http配置
`HttpConfig.Json`文件
```
{
  "HttpConfig": {
    "Host": "",
    "Port": 9090,               //监听端口
    "SSL": false,               //是否开启HTTPS
    "CertificateFile": "",      //证书文件
    "CertificatePassword": "",  //证书密码
  }
}
```
### Console Server API服务配置
'Gateway.json'文件
```
{
  "Servers": [                          //网关负载的服务列表
    {
      "Uri": "http://localhost:9000/",  //服务地址
      "MaxConnections": 100             //最大连接数
    }
  ],
  "Urls": [ //负载的URL列表
    {
      "Url": "*",                       //匹配的URL路径，* 是优先级最低，URL会优先长匹配
      "HashPattern": null,              //一致性服务指向模式设置，不配的情况下依据权重优先级
      "Servers": [                      //负载服务列表
        {
          "Url": "http://localhost:9000/",  //服务地址
          "Weight": 0                       //权重
        }
      ]
    }
  ]
}
```
### 请求过虑处理
提供两种方式进行请求过虑处理,这两种方式都可以Header，Cookies等方式来判断用户来源
#### Requesting事件 
```
      g.Requesting += (o, e) => {
            e.Gateway.Response(e.Response, new NotFoundResult("test"));
            e.Cancel = true;
      };
```
#### IRequestFilter
可以实现IRequestFilter接口，并附加到对应的Url上
```
    public interface IRequestFilter
    {
        string Name { get; }

        bool Executing(Gateway gateway, HttpRequest request, HttpResponse response);

        void Executed(Gateway gateway, HttpRequest request, HttpResponse response, ServerAgent server, int code,long useTime);
    }
```
## 日志跟踪和统计
由于网关涉及的并发处理量比较大，所以默认情况是不会对请求记录详细日志和处理状态情况。可以通过IRequestFilter或Requested事件来登记这些历史记录和统计
```
    g.Requested += (o, e) => {
                //e.Request
                //e.Response
                //e.Code
                //e.Time
    };
```
## 性能指标
在E3-1230v2的四核上测试结果是，7万多的RPS代理转发，占用带宽7G
