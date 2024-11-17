using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TcpManager.Model;

namespace TcpManager
{
	public class TcpClientManager
	{
		#region => Field

		private TcpClient _client;
		private NetworkStream _networkStream;

		private Thread _recvDataThread;

		private bool _isClientConnected = false;

		public MsgQueueManager MsgQueue;

		#endregion => Field

		#region => Property
		#endregion => Property

		#region => Constructor

		public TcpClientManager()
		{
			_client = new TcpClient();
			MsgQueue = new MsgQueueManager(false);
			MsgQueue.StartMsgMonitoring();
			MsgQueue.SendToServer = SendToServer;
		}

		/// <summary>
		/// 인스턴스가 제거될 때 수행할 로직
		/// </summary>
		~TcpClientManager()
		{
			Disconnect();
		}

		#endregion => Constructor

		#region => Method

		/// <summary>
		/// 파라미터 정보를 사용해 서버에 연결
		/// </summary>
		public void Connect(string ip, int port)
		{
			try
			{
				_client.Connect(ip, port);
				_networkStream = _client.GetStream();
				_isClientConnected = true;

				_recvDataThread = new Thread(RecvDataMonitoring);
				_recvDataThread.IsBackground = true;
				_recvDataThread.Start();
			}
			catch
			{
				throw new Exception("Client : Server Connection Fail");
			}
		}

		/// <summary>
		/// 서버와 연결 종료
		/// </summary>
		public void Disconnect()
		{
			StopRecvDataMonitoring();

			if (_networkStream != null)
			{
				_networkStream.Close();
				_networkStream = null;
			}

			if (_client != null)
			{
				_client.Close();
				_client = null;
			}
		}

		/// <summary>
		/// 클라이언트로 들어오는 데이터 모니터링
		/// </summary>
		private void RecvDataMonitoring()
		{
			if (_client == null)
			{
				return;
			}

			while (_isClientConnected)
			{
				if (_networkStream.DataAvailable)
				{
					try
					{
						byte[] data = new byte[1024];
						int dataSize = _networkStream.Read(data, 0, data.Length);

						if (dataSize > 0)
						{
							// Recv Data 처리
							ProcessRecvData(data, dataSize);
						}
					}
					catch
					{
						throw new Exception("Client : Recv Data Fail");
					}
				}
			}
		}

		/// <summary>
		/// RecvDataMonitoring이 돌아가고 있는 Thread 종료
		/// </summary>
		private void StopRecvDataMonitoring()
		{
			if (_recvDataThread != null)
			{
				_isClientConnected = false;

				Stopwatch stopWatch = Stopwatch.StartNew();

				while (_recvDataThread.IsAlive && stopWatch.Elapsed.TotalSeconds <= 5)
				{
					Thread.Sleep(100);
				}

				if (_recvDataThread.IsAlive)
				{
					throw new Exception("Client : RecvDataThread 종료 후 5초 경과하였으나 Thread가 살아있습니다.");
				}
			}

			_recvDataThread = null;
		}

		/// <summary>
		/// 서버로 부터 받은 데이터 처리
		/// </summary>
		private void ProcessRecvData(byte[] data, int dataSize)
		{
			string recvData = Encoding.UTF8.GetString(data, 0, dataSize);

			// Recv Data 처리 로직 구현
			RecvMessage recvMessage = new RecvMessage(recvData);
			MsgQueue.RecvQueue.Enqueue(recvMessage);
		}

		public void SendToServer(string cmd, int returnValue = InterfaceProtocol.RES_DEFAULT)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(InterfaceProtocol.REQ_FIRST_STRING);
			sb.Append(cmd);

			if (returnValue != InterfaceProtocol.RES_DEFAULT)
			{
				sb.Append(returnValue);
			}

			sb.Append(InterfaceProtocol.END_OF_MSG);

			string message = sb.ToString();

			SendData(message);
		}

		/// <summary>
		/// 서버에 데이터 전송
		/// </summary>
		public void SendData(string message)
		{
			if (_isClientConnected == false)
			{
				return;
			}

			try
			{
				byte[] data = Encoding.UTF8.GetBytes(message);
				_networkStream.Write(data, 0, data.Length);
			}
			catch
			{
				throw new Exception("Client : Send Data Fail");
			}
		}

		#endregion => Method
	}
}
