using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpManager.Model
{
	public class SendMessage
	{
		#region => Field

		public string Cmd;
		public int ReturnValue;
		public TcpClient Client;

		#endregion => Field

		#region => Constructor

		/// <summary>
		/// 단일 클라이언트일 때 Server에서 Client로 보내기 전 Queue 데이터
		/// </summary>
		public SendMessage(string _cmd, int _returnValue = 0)
		{
			Cmd = _cmd;
			ReturnValue = _returnValue;
		}

		/// <summary>
		/// 멀티 클라이언트일 때 Server에서 Client로 보내기 전 Queue 데이터
		/// </summary>
		public SendMessage(TcpClient _client, string _cmd, int _returnValue = 0)
		{
			Cmd = _cmd;
			ReturnValue = _returnValue;
			Client = _client;
		}

		#endregion => Constructor
	}
}
