using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

		Thread _recvMsgThread;
		Thread _sendMsgThread;

		private bool _recvMsgThreadRunning = false;
		private bool _sendMsgThreadRunning = false;

		private bool _isServer;

		// Server To Client Delegate
		public Action<TcpClient, string, int> SendToClient;

		// Client To Server Delegate
		public Action<string, int> SendToServer;

		#endregion => Field

		#region => Property
		#endregion => Property

		#region => Constructor

		/// <summary>
		/// _isServer = true => Server
		/// _isServer = false => Client
		/// </summary>
		public MsgQueueManager(bool isServer)
		{
			RecvQueue = new ConcurrentQueue<RecvMessage>();
			SendQueue = new ConcurrentQueue<SendMessage>();
			_isServer = isServer;
		}

		#endregion => Constructor

		#region => Method

		/// <summary>
		/// Thread 시작
		/// </summary>
		public void StartMsgMonitoring()
		{
			_recvMsgThreadRunning = true;
			_recvMsgThread = new Thread(RecvMsgMonitoring);
			_recvMsgThread.IsBackground = true;
			_recvMsgThread.Start();

			_sendMsgThreadRunning = true;
			_sendMsgThread = new Thread(SendMsgMonitoring);
			_sendMsgThread.IsBackground = true;
			_sendMsgThread.Start();
		}

		/// <summary>
		/// Thread 종료
		/// </summary>
		public void StopMsgMonitoringThread()
		{
			_recvMsgThreadRunning = false;

			if (_recvMsgThread != null)
			{
				_recvMsgThread = null;
			}

			_sendMsgThreadRunning = false;

			if (_sendMsgThread != null)
			{
				_sendMsgThread = null;
			}
		}

		/// <summary>
		/// Client로 부터 받은 Command 실행
		/// Queue에 데이터 있을 시 수행
		/// </summary>
		private void RecvMsgMonitoring()
		{
			while (_recvMsgThreadRunning)
			{
				if (RecvQueue.Count > 0)
				{
					RecvQueue.TryDequeue(out RecvMessage recvMsg);

					try
					{
						// 클라이언트로 부터 받은 Reqest 처리 로직
						if (_isServer)
						{

						}
						// 서버로 부터 받은 Response 처리 로직
						else
						{

						}
					}
					catch
					{
						throw new Exception("RecvQueue : ");
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
			while (_sendMsgThreadRunning)
			{
				if (SendQueue.Count > 0)
				{
					SendQueue.TryDequeue(out SendMessage sendMsg);

					try
					{
						// 응답 Command 전송 로직

						// MsgQueue가 Server에서 사용될 때
						if (_isServer)
						{
							SendToClient?.Invoke(sendMsg.Client, sendMsg.Cmd, sendMsg.ReturnValue);
						}
						// MsgQueue가 Client에서 사용될 때
						else
						{
							SendToServer?.Invoke(sendMsg.Cmd, sendMsg.ReturnValue);
						}
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
