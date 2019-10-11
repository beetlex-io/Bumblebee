Bumblebee是一款基于`http 1.1`协议实现的服务网关，它可以应用到所有基于`http 1.1`的通讯服务上。它的重点是用于对WebAPI微服务集群服务负载和管理；作为微服务应用网关它提供了应用服务负载，故障迁移，安全控制，监控跟踪和日志处理等；不仅如此它强大的插件扩展功能，可以针对实业务情况进行不同的相关插件应用开发满足实际情况的需要。
![](https://i.imgur.com/uIb9y7I.jpg)
## 主要功能
- 服务管理，可以针对业务需要可以添加管理相应的服务应用
- 动态路由管理，可以针对不同请求路径制定不同的负载方案；负载的方案调整都具备热更能力，并不需要重启即可完成相关调整。
- 负载策略多样性，可以针对不同的路径和服务制定不同的负载方式，包括有：动太一致性，权重负载和请求限制等.
- 自动的负载故障和恢复迁移，组件对服务的可用性会进行一个可靠的管理，根据服务的可用性进行动态负载策略调整.
- 完善的插件扩展机制，可以制定如管理，监控，日志和安全访问等等功能。
- 支持`https`可以制定更安全的通讯服务应用
- 支持`windows`,`linux`等多平台
## 可用插件
|名称|功能描述|
|----|-------|
|[BeetleX.Bumblebee.Configuration](https://www.nuget.org/packages/BeetleX.Bumblebee.Configuration/)|配置管理插件，用于网关管理，负载配置，日志查看和插件管理等|
|[BeetleX.Bumblebee.Jwt](https://www.nuget.org/packages/BeetleX.Bumblebee.Jwt/)|JWT验证插件，可以通过这插件配置统一请求验证|
|[BeetleX.Bumblebee.Logs](https://www.nuget.org/packages/BeetleX.Bumblebee.Logs/)|请求日志记录插件，可以配置把请求日志存储到文件或数据库|
|[BeetleX.Bumblebee.ConcurrentLimits](https://www.nuget.org/packages/BeetleX.Bumblebee.ConcurrentLimits/)|并发控制插件，可以对请求的IP和相关URL配置并发限制，控制服务平稳运行|
|[BeetleX.Bumblebee.Caching](https://www.nuget.org/packages/BeetleX.Bumblebee.Caching/)|网关请求缓存插件|


**[相关使用文档](https://github.com/IKende/Bumblebee/wiki)**

## 部署使用
 新建一个控制台程序后引用组件
```
BeetleX.Bumblebee
```
然后编写以下代码
``` csharp
        private static Gateway g;
        static void Main(string[] args)
        {
            g = new Gateway();
            g.HttpOptions(h =>
            {
                h.Port = 80;
            });
            g.SetServer("http://192.168.2.25:9090").AddUrl("*", 0, 0);
            g.SetServer("http://192.168.2.26:9090").AddUrl("*", 0, 0);
            g.Open();
            Console.Read();
        }
```
以上代码是在本机`80`端口部署一个网关服务，并把请求负载到`http://192.168.2.25:9090`和`http://192.168.2.26:9090`这样使用比较麻烦，如果你想自己制定一些特别的需求才需要这样做。
## 引用管理插件
组件很多功能可以通过插件扩展的方式引入，以下是引入一个管理插件，通过这个插件对网关进行一个可视化操作。
```
BeetleX.Bumblebee.Configuration
```
这是一个可视化网关管理的插件，只要引用上即可通过插件提供的管理界面来进行网关配置
``` csharp
    class Program
    {
        static Gateway gateway;
        static void Main(string[] args)
        {
            gateway = new Gateway();
            gateway.HttpOptions(o =>
            {
                o.Port = 80;
                o.LogToConsole = true;
                o.LogLevel = BeetleX.EventArgs.LogType.Error;
            });
            gateway.Open();
            gateway.LoadPlugin(typeof(Bumblebee.Configuration.Management).Assembly);
            Console.Read();
        }
    }
```
或直接下载编译好的版本执行`dotnet GatewayServer.dll`
[https://github.com/IKende/Bumblebee/blob/master/bin/Bumblebee1.0.6.zip](https://github.com/IKende/Bumblebee/blob/master/bin/Bumblebee1.0.6.zip)

运行后即可通过以下地址访问管理界面`http://localhost/__system/bumblebee/`
![image](https://user-images.githubusercontent.com/2564178/65938281-24aa3b80-e455-11e9-8113-05ce661ee635.png)
默认登陆用户名和密码是`admin`和`123456`,建议登陆后在配置页面上修改登陆密码。登陆后就进入网关的基础监控页面
![image](https://user-images.githubusercontent.com/2564178/65939079-66d47c80-e457-11e9-926b-df64e5ff7ee3.png)
当服务和路由配置好后，就可能通过这个页面查看网关的运行情况；主要包括网关的基础资源信息，服务应用状况和不同`Url`的请求情况。由于这个插件还在完善中所以提供的功能并不够，只是一般的配置和监控。
### 服务配置简介
![image](https://user-images.githubusercontent.com/2564178/65813190-43fe5a00-e204-11e9-82fd-8ae273fc6f62.png)
服务配置比较简单，只需要把服务地址添加进来即可；`Max`是指网关连接到服务的最大连接数，可以根据应用的并发情况进行配置最大连接数；在并发中即使最大连接数被占用完也不会引起服务异常，组件还针对每个服务分配一个队列，只有当连接数被分配完后并且队列也满的情况才会拒绝请求。
### 路由配置简介
![image](https://user-images.githubusercontent.com/2564178/65813269-9ee48100-e205-11e9-96ae-823b8a7b4052.png)
可以根据不同的`Url`制定不同的负载策略，策略调整保存后会马上生效并不需要重启服务程序。

### 插件管理
![image](https://user-images.githubusercontent.com/2564178/66125055-94b4ef00-e618-11e9-9a02-799e70cddb00.png)
主要用于管理网关的插件，用于启用，停用或配置插件相关信息
![image](https://user-images.githubusercontent.com/2564178/65938394-6e932180-e455-11e9-947e-db0a5cfcb708.png)
### 日志查看
这个主要是查看网关处理的日志，请求转发日志由于量比较大这个管理插件暂没有实现接管，使用者可以写插件来记录相关API转发的详细日志。
![image](https://user-images.githubusercontent.com/2564178/65813304-2631f480-e206-11e9-8a06-a799edcba51c.png)
## 性能测试对比(Bumblebee vs Ocelot)
**测试服务配置** E3 1230v2 16G windows 2008  Network:10Gb

**测试工具** ab和bombardier

**测试代码** [https://github.com/IKende/Bumblebee/tree/master/BumblebeeVSOcelot](https://github.com/IKende/Bumblebee/tree/master/BumblebeeVSOcelot)


**测试内容** 分别启用500,1000和2000个连接进行请求并发测试

## ab测试结果
![](https://i.imgur.com/rE97kRQ.png)
## bombardier测试结果
![](https://i.imgur.com/6BfQVjo.png)
