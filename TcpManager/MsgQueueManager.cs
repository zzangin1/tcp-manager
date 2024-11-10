using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpManager.Model;

namespace TcpManager
{
	public class MsgQueueManager
	{
		#region => Field

		// 멀티 스레드 안전을 위해 ConcurrentQueue로 구현
		public ConcurrentQueue<RecvMessage> RecvQueue;
		public ConcurrentQueue<SendMessage> SendQueue;

		Thread m_recvMsgThread;
		Thread m_sendMsgThread;

		private bool m_recvMsgThreadRunning = false;
		private bool m_sendMsgThreadRunning = false;

		#endregion => Field

		#region => Property
		#endregion => Property

		#region => Constructor

		public MsgQueueManager()
		{
			RecvQueue = new ConcurrentQueue<RecvMessage>();
			SendQueue = new ConcurrentQueue<SendMessage>();
		}

		#endregion => Constructor

		#region => Method

		/// <summary>
		/// Thread 시작
		/// </summary>
		public void StartMsgMonitoring()
		{
			m_recvMsgThreadRunning = true;
			m_recvMsgThread = new Thread(RecvMsgMonitoring);
			m_recvMsgThread.IsBackground = true;
			m_recvMsgThread.Start();

			m_sendMsgThreadRunning = true;
			m_sendMsgThread = new Thread(SendMsgMonitoring);
			m_sendMsgThread.IsBackground = true;
			m_sendMsgThread.Start();
		}

		/// <summary>
		/// Thread 종료
		/// </summary>
		public void StopMsgMonitoringThread()
		{
			m_recvMsgThreadRunning = false;

			if (m_recvMsgThread != null)
			{
				m_recvMsgThread = null;
			}

			m_sendMsgThreadRunning = false;

			if (m_sendMsgThread != null)
			{
				m_sendMsgThread = null;
			}
		}

		/// <summary>
		/// Client로 부터 받은 Command 실행
		/// Queue에 데이터 있을 시 수행
		/// </summary>
		private void RecvMsgMonitoring()
		{
			while (m_recvMsgThreadRunning)
			{
				if (RecvQueue.Count > 0)
				{
					RecvQueue.TryDequeue(out RecvMessage _recvMsg);

					try
					{
						// 클라이언트로 부터 받은 Command 수행 로직
					}
					catch
					{
						throw new Exception("");
					}
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		/// <summary>
		/// Client에 응답 Command 전송
		/// Queue에 데이터 있을 시 수행
		/// </summary>
		private void SendMsgMonitoring()
		{
			while (m_sendMsgThreadRunning)
			{
				if (SendQueue.Count > 0)
				{
					SendQueue.TryDequeue(out SendMessage sendMsg);

					try
					{
						// Client에 응답 Command 전송 로직
						TcpServerManager.Instance.SendToClient(sendMsg.Client, sendMsg.Cmd, sendMsg.ReturnValue);
					}
					catch
					{
						throw new Exception("SendQueue : Send");
					}
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		#endregion => Method
	}
}
