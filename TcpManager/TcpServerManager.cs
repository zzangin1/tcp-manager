using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TcpManager.Model;

namespace TcpManager
{
	public class TcpServerManager
	{
		#region => Field

		private static TcpServerManager _instance;
		private TcpListener _server;
		private List<TcpClient> _clients;

		MsgQueueManager _msgQueueManager;

		private Thread _serverThread;
		private List<Thread> _recvDataThreads;
		private Thread _recvDataThread;

		private bool _isServerRunning = false;

		#endregion => Field

		#region => Property

		/// <summary>
		/// 싱글톤 패턴 적용
		/// </summary>
		public static TcpServerManager Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new TcpServerManager();
				}

				return _instance;
			}
		}

		#endregion => Property

		#region => Constructor

		public TcpServerManager()
		{
			// 9999번 포트로 대기
			_server = new TcpListener(IPAddress.Any, 9999);
			_msgQueueManager = new MsgQueueManager(true);
			_clients = new List<TcpClient>();
			_recvDataThreads = new List<Thread>();
			_msgQueueManager.StartMsgMonitoring();
			_msgQueueManager.SendToClient = SendToClient;
		}

		/// <summary>
		/// 인스턴스가 제거되었을 때 수행할 로직
		/// </summary>
		~TcpServerManager()
		{
			Stop();
		}

		#endregion => Constructor

		#region => Method

		/// <summary>
		/// Tcp Server 시작
		/// </summary>
		public void Start()
		{
			_serverThread = new Thread(ServerMonitoring);
			_serverThread.IsBackground = true;
			_serverThread.Start();
		}

		/// <summary>
		/// Tcp Server 종료
		/// </summary>
		public void Stop()
		{
			_msgQueueManager.StopMsgMonitoringThread();
			StopServerThread();
			StopRecvDataThread();
		}

		/// <summary>
		/// 서버에 필요한 기능 시작 및 모니터링
		/// </summary>
		private void ServerMonitoring()
		{
			_server.Start();
			_isServerRunning = true;

			while (_isServerRunning)
			{
				if (_server.Pending())
				{
					TcpClient client = _server.AcceptTcpClient();

					lock (_clients)
					{
						_clients.Add(client);
					}

					Thread recvThread = new Thread(() => RecvDataMonitoring(client));
					recvThread.IsBackground = true;
					recvThread.Start();

					lock (_recvDataThreads)
					{
						_recvDataThreads.Add(recvThread);
					}
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		/// <summary>
		/// 서버로 들어오는 데이터 모니터링
		/// </summary>
		private void RecvDataMonitoring(TcpClient _client)
		{
			if (_client == null)
			{
				return;
			}

			var networkStream = _client.GetStream();
			byte[] data = new byte[1024];

			while (_client.Connected)
			{
				if (networkStream.DataAvailable)
				{
					try
					{
						int dataSize = networkStream.Read(data, 0, data.Length);

						if (dataSize > 0)
						{
							// Recv Data 처리
							ProcessRecvData(data, dataSize, _client);
						}
					}
					catch
					{
						throw new Exception("Server : Recv Data Fail");
					}
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		/// <summary>
		/// ServerMonitoring이 돌아가고 있는 Thread 종료
		/// </summary>
		private void StopServerThread()
		{
			if (_serverThread != null)
			{
				_isServerRunning = false;

				_server.Stop();

				Stopwatch stopwatch = Stopwatch.StartNew();

				while (_serverThread.IsAlive && stopwatch.Elapsed.TotalSeconds <= 5)
				{
					Thread.Sleep(100);
				}

				if (_serverThread.IsAlive)
				{
					throw new Exception("Server : ServerThread 종료 후 5초 경과하였으나 Thread가 살아있습니다.");
				}
			}

			_serverThread = null;
		}

		/// <summary>
		/// RecvDataMonitoring이 돌아가고 있는 Thread 종료
		/// </summary>
		private void StopRecvDataThread()
		{
			foreach (var client in _clients)
			{
				if (client.Connected)
				{
					try
					{
						var networkStream = client.GetStream();
						networkStream.Close();
					}
					catch
					{
						throw new Exception("Server : NetworkStream Close Fail");
					}

					client.Close();
				}
			}

			lock (_recvDataThread)
			{
				foreach (var recvThread in _recvDataThreads)
				{
					if (recvThread.IsAlive)
					{
						var stopWatch = Stopwatch.StartNew();

						while (recvThread.IsAlive && stopWatch.Elapsed.TotalSeconds <= 5)
						{
							Thread.Sleep(100);
						}

						if (recvThread.IsAlive)
						{
							throw new Exception("Server : RecvDataThread 종료 후 5초 경과하였으나 Thread가 살아있습니다.");
						}
					}
				}

				_recvDataThreads.Clear();
			}

			_clients.Clear();
		}

		/// <summary>
		/// 클라이언트로 부터 받은 데이터 처리
		/// </summary>
		private void ProcessRecvData(byte[] data, int dataSize, TcpClient client)
		{
			string recvData = Encoding.UTF8.GetString(data, 0, dataSize);

			// Recv Data 처리 로직 구현
			string processData = recvData.Substring(1, 5);

			// Client에 응답 Data 전송
			RecvMessage recvMessage = new RecvMessage(recvData);
			SendMessage sendMessage = new SendMessage(client, processData, InterfaceProtocol.RES_ACK);
			_msgQueueManager.RecvQueue.Enqueue(recvMessage);
			_msgQueueManager.SendQueue.Enqueue(sendMessage);
		}

		public void SendToClient(TcpClient _client, string _cmd, int _returnValue = InterfaceProtocol.RES_DEFAULT)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(InterfaceProtocol.RES_FIRST_STRING);
			sb.Append(_cmd);

			if (_returnValue != InterfaceProtocol.RES_DEFAULT)
			{
				sb.Append(_returnValue);
			}

			sb.Append(InterfaceProtocol.END_OF_MSG);

			SendData(sb.ToString(), _client);
		}

		/// <summary>
		/// 클라이언트에 Data 전송
		/// </summary>
		public void SendData(string _message, TcpClient _targetClient)
		{
			if (_targetClient == null || _targetClient.Connected == false)
			{
				return;
			}

			try
			{
				byte[] data = Encoding.UTF8.GetBytes(_message);
				NetworkStream networkStream = _targetClient.GetStream();
				networkStream.Write(data, 0, data.Length);
			}
			catch
			{
				throw new Exception("Server : Send Data Fail");
			}
		}

		#endregion => Method
	}
}
