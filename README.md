## 简介

c#版基于touchsocket的pitaya客户端，加入了response处理，利用线程安全的字典和System.Threading.Channels实现了发送request后阻塞等待response，且支持设置请求的超时时间。
请求等待期间可接受服务端发来的任意消息，避免了touchsocket内置WaitingClient的问题——"发送完数据，在等待时，如果收到其他返回数据，则可能得到错误结果“，解决这个问题主要依赖的原理是response和request中的messageId一致。
对于服务端发来的push消息，采用了上层主动注册回调的方式处理，因为客户端无需阻塞等待push，收到push消息就用注册好的回调进行处理即可。网络层搭建稳定后就可以愉快地用unity+pitaya开发网游了。

## 立即体验

首先要启动Pitaya服务器，仓库链接见下方“相关仓库”。demo使用的pitaya/examples/demo/cluster，聊天用的。

cd pitaya/examples/testing

docker compose up -d  (启动pitaya-demo的依赖服务，nats和etcd)

cd pitaya/examples/demo/cluster

go run main.go (启动connector service)

cd pitaya/examples/demo/cluster （另开一个终端）

go run main.go -type=room_srv -frontend=false  (启动room service)

然后启动此项目。

Rider开发环境可以直接运行pitaya_client_test中的start方法。

控制台输出：

![81c7de0f0c12b71373c0f67b02ab1c1a](https://github.com/Maingol/PitayaPartner/assets/58876284/311aac94-8078-4c5f-a0be-9fc51f8acddc)


## 相关仓库

感谢TouchSocket作者——若汝棋茗。TouchSocket的仓库链接：https://github.com/RRQM/TouchSocket

感谢Pitaya开发团队。Pitaya的仓库链接：https://github.com/topfreegames/pitaya

## 特别鸣谢

[![JetBrains](https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.svg)]( https://jb.gg/OpenSourceSupport)


## 如果觉得项目对你有帮助，请支持一下

- 微信

![image](https://github.com/Maingol/PitayaPartner/assets/58876284/b20fca46-bcda-4105-a968-b4cbe323a497)




- 支付宝

![image](https://github.com/Maingol/PitayaPartner/assets/58876284/5b5c8dd1-276c-427a-973f-0b2075e0eb2d)
