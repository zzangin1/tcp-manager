using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpManager
{
	public class TcpClientManager
	{
		#region => Field

		private TcpClient m_client;
		private NetworkStream m_networkStream;

		private Thread m_recvDataThread;

		private bool m_isClientConnected = false;

		#endregion => Field

		#region => Property
		#endregion => Property

		#region => Constructor

		public TcpClientManager()
		{
			m_client = new TcpClient();
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
		public void Connect(string _ip, int _port)
		{
			try
			{
				m_client.Connect(_ip, _port);
				m_networkStream = m_client.GetStream();
				m_isClientConnected = true;

				m_recvDataThread = new Thread(RecvDataMonitoring);
				m_recvDataThread.IsBackground = true;
				m_recvDataThread.Start();
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

			if (m_networkStream != null)
			{
				m_networkStream.Close();
				m_networkStream = null;
			}

			if (m_client != null)
			{
				m_client.Close();
				m_client = null;
			}
		}

		/// <summary>
		/// 클라이언트로 들어오는 데이터 모니터링
		/// </summary>
		private void RecvDataMonitoring()
		{
			if (m_client == null)
			{
				return;
			}

			while (m_isClientConnected)
			{
				if (m_networkStream.DataAvailable)
				{
					try
					{
						byte[] data = new byte[1024];
						int dataSize = m_networkStream.Read(data, 0, data.Length);

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
			if (m_recvDataThread != null)
			{
				m_isClientConnected = false;

				Stopwatch stopWatch = Stopwatch.StartNew();

				while (m_recvDataThread.IsAlive && stopWatch.Elapsed.TotalSeconds <= 5)
				{
					Thread.Sleep(100);
				}

				if (m_recvDataThread.IsAlive)
				{
					throw new Exception("Client : RecvDataThread 종료 후 5초 경과하였으나 Thread가 살아있습니다.");
				}
			}

			m_recvDataThread = null;
		}

		/// <summary>
		/// 서버로 부터 받은 데이터 처리
		/// </summary>
		private void ProcessRecvData(byte[] _data, int _dataSize)
		{
			string recvData = Encoding.UTF8.GetString(_data, 0, _dataSize);

			// Recv Data 처리 로직 구현
		}

		/// <summary>
		/// 서버에 데이터 전송
		/// </summary>
		public void SendData(string _message)
		{
			if (m_isClientConnected == false)
			{
				return;
			}

			try
			{
				byte[] data = Encoding.UTF8.GetBytes(_message);
				m_networkStream.Write(data, 0, data.Length);
			}
			catch
			{
				throw new Exception("Client : Send Data Fail");
			}
		}

		#endregion => Method
	}
}
