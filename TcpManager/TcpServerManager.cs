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

		private static TcpServerManager m_instance;
		private TcpListener m_server;
		private List<TcpClient> m_clients;

		MsgQueueManager m_msgQueueManager;

		private Thread m_serverThread;
		private List<Thread> m_recvDataThreads;
		private Thread m_recvDataThread;

		private bool m_isServerRunning = false;

		#endregion => Field

		#region => Property

		/// <summary>
		/// 싱글톤 패턴 적용
		/// </summary>
		public static TcpServerManager Instance
		{
			get
			{
				if (m_instance == null)
				{
					m_instance = new TcpServerManager();
				}

				return m_instance;
			}
		}

		#endregion => Property

		#region => Constructor

		public TcpServerManager()
		{
			// 9999번 포트로 대기
			m_server = new TcpListener(IPAddress.Any, 9999);
			m_msgQueueManager = new MsgQueueManager();
			m_clients = new List<TcpClient>();
			m_recvDataThreads = new List<Thread>();
			m_msgQueueManager.StartMsgMonitoring();
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
			m_serverThread = new Thread(ServerMonitoring);
			m_serverThread.IsBackground = true;
			m_serverThread.Start();
		}

		/// <summary>
		/// Tcp Server 종료
		/// </summary>
		public void Stop()
		{
			m_msgQueueManager.StopMsgMonitoringThread();
			StopServerThread();
			StopRecvDataThread();
		}

		/// <summary>
		/// 서버에 필요한 기능 시작 및 모니터링
		/// </summary>
		private void ServerMonitoring()
		{
			m_server.Start();
			m_isServerRunning = true;

			while (m_isServerRunning)
			{
				if (m_server.Pending())
				{
					TcpClient client = m_server.AcceptTcpClient();

					lock (m_clients)
					{
						m_clients.Add(client);
					}

					Thread recvThread = new Thread(() => RecvDataMonitoring(client));
					recvThread.IsBackground = true;
					recvThread.Start();

					lock (m_recvDataThreads)
					{
						m_recvDataThreads.Add(recvThread);
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
			if (m_serverThread != null)
			{
				m_isServerRunning = false;

				m_server.Stop();

				Stopwatch stopwatch = Stopwatch.StartNew();

				while (m_serverThread.IsAlive && stopwatch.Elapsed.TotalSeconds <= 5)
				{
					Thread.Sleep(100);
				}

				if (m_serverThread.IsAlive)
				{
					throw new Exception("Server : ServerThread 종료 후 5초 경과하였으나 Thread가 살아있습니다.");
				}
			}

			m_serverThread = null;
		}

		/// <summary>
		/// RecvDataMonitoring이 돌아가고 있는 Thread 종료
		/// </summary>
		private void StopRecvDataThread()
		{
			foreach (var client in m_clients)
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

			lock (m_recvDataThread)
			{
				foreach (var recvThread in m_recvDataThreads)
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

				m_recvDataThreads.Clear();
			}

			m_clients.Clear();
		}

		/// <summary>
		/// 클라이언트로 부터 받은 데이터 처리
		/// </summary>
		private void ProcessRecvData(byte[] _data, int _dataSize, TcpClient _client)
		{
			string recvData = Encoding.UTF8.GetString(_data, 0, _dataSize);

			// Recv Data 처리 로직 구현

			// Client에 응답 Data 전송
			RecvMessage recvMessage = new RecvMessage(recvData);
			SendMessage sendMessage = new SendMessage(_client, recvMessage.Cmd, 1);
			m_msgQueueManager.RecvQueue.Enqueue(recvMessage);
			m_msgQueueManager.SendQueue.Enqueue(sendMessage);
		}

		public void SendToClient(TcpClient _client, string _cmd, int _returnValue = 0)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(_cmd);

			if (_returnValue != 0)
			{
				sb.Append(_returnValue);
			}

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
