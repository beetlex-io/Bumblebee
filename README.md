Bumblebee是一款基于`http 1.1`协议实现的服务网关，它可以应用到所有基于`http 1.1`的通讯服务上。它的重点是用于对WebAPI微服务集群服务负载和管理；作为微服务应用网关它提供了应用服务负载，故障迁移，安全控制，监控跟踪和日志处理等；不仅如此它强大的插件扩展功能，可以针对实业务情况进行不同的相关插件应用开发满足实际情况的需要。
![](https://i.imgur.com/uIb9y7I.jpg)
## 主要功能
- 服务管理，可以针对业务需要可以添加管理相应的服务应用
- 动态路由管理，可以针对不同请求路径制定不同的负载方案；负载的方案调整都具备热更能力，并不需要重启即可完成相关调整。
- 负载策略多样性，可以针对不同的路径和服务制定不同的负载方式，包括有：动太一致性，权重负载和请求限制等.
- 自动的负载故障和恢复迁移，组件对服务的可用性会进行一个可靠的管理，根据服务的可用性进行动态负载策略调整.
- 完善的插件扩展机制，可以制定如管理，监控，日志和安全访问等等功能。
- 支持`https`可以制定更安全的通讯服务管理
- 支持`windows`,`linux`等多平台

## 性能测试(Bumblebee vs Ocelot)
**测试服务配置** E3 1230v2 16G windows 2008  Network:10Gb

**测试工具** ab和bombardier

**测试代码** [https://github.com/IKende/Bumblebee/tree/master/BumblebeeVSOcelot](https://github.com/IKende/Bumblebee/tree/master/BumblebeeVSOcelot)


**测试内容** 分别启用500,1000和2000个连接进行请求并发测试

## ab测试结果
![](https://i.imgur.com/rE97kRQ.png)
## bombardier测试结果
![](https://i.imgur.com/6BfQVjo.png)
