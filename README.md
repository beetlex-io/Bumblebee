Bumblebee是`.netcore`下开源基于`BeetleX.FastHttpApi`扩展的HTTP微服务网关组件，它的主要作用是针对WebAPI集群服务作一个集中的转发和管理；作为应用网关它提供了应用服务负载，故障迁移，安全控制，监控跟踪和日志处理等。它最大的一个特点是基于`C#`开发，你可以针对自己业务的需要对它进行扩展具体的业务功能。
![](https://i.imgur.com/uIb9y7I.jpg)
## 独立部署+Web管理
[https://ikende.com/blog/126.html](https://ikende.com/blog/126.html)
## 组件部署
组件的部署一般根据自己的需要进行引用扩展功能，如果你只需要简单的应用服务负载、故障迁移和恢复等功能只需要下载[Bumblebee.ConsoleServer](https://github.com/IKende/Bumblebee/tree/master/bin)下载需要版本的zip即可。`Bumblebee.ConsoleServer`提供两个配置文件描述'HttpConfig.json'和'Gateway.json'分别用于配置HTTP服务和网关对应的负载策略。
## 系统要求
任何运行.net core 2.1或更高版本的操作系统(liinux,windows等)，不同操作系统安装可查看[https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
## 运行网关
- windows `run.bat 或 dotnet Bumblebee.ConsoleServer.dll `
- linux `./run.sh 或 dotnet Bumblebee.ConsoleServer.dll`

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

## 性能测试(Bumblebee vs Ocelot)
**测试服务配置** E3 1230v2 16G windows 2008  Network:10Gb

**测试工具** ab和bombardier

**测试代码** [https://github.com/IKende/Bumblebee/tree/master/BumblebeeVSOcelot](https://github.com/IKende/Bumblebee/tree/master/BumblebeeVSOcelot)


**测试内容** 分别启用500,1000和2000个连接进行请求并发测试

## ab测试结果
![](https://i.imgur.com/rE97kRQ.png)
## bombardier测试结果
![](https://i.imgur.com/6BfQVjo.png)
