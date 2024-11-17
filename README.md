# TcpManager
하드웨어와 Tcp로 통신할 때를 대비해 TcpManager 구현

## MsgQueueManager
Server, Client 간 통신할 때 직접 통신하지 않고 Send, Recv 데이터를 MsgQueueManager를 통해 관리

## TcpServerManager
TcpServer 코드
다중 클라이언트
각 클라이언트 따로 관리

## TcpClientManager
TcpClient 코드
여러 클라이언트를 구현해 Server에 붙을 수 있다.
