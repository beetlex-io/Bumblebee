Bumblebee是一款支持`http`和`websocket`。它的重点是用于对WebAPI微服务集群服务负载和管理；作为微服务应用网关它提供了应用服务负载，故障迁移，安全控制，监控跟踪和日志处理等；不仅如此它强大的插件扩展功能，可以针对实业务情况进行不同的相关插件应用开发满足实际情况的需要。
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
|[BeetleX.Bumblebee.Caching](https://www.nuget.org/packages/BeetleX.Bumblebee.Caching/)|网关缓存插件|
|[BeetleX.Bumblebee.UrlRewrite](https://www.nuget.org/packages/BeetleX.Bumblebee.UrlRewrite/)|Url重写插件|
|[BeetleX.Bumblebee.Consul](https://www.nuget.org/packages/BeetleX.Bumblebee.Consul/)|Consul服务发现插件|
|[BeetleX.Bumblebee.InvalidUrlFilter](https://www.nuget.org/packages/BeetleX.Bumblebee.InvalidUrlFilter/)|请求Url过虑插件|
## 详细使用文档
[http://doc.beetlex.io](http://doc.beetlex.io/#29322e3796694434894fc2e6e8747626)
## 官网部署案例
http://beetlex.io/__system/bumblebee/
## 最新可运行包
https://github.com/IKende/Bumblebee/tree/master/bin

