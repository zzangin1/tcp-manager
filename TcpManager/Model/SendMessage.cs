using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpManager.Model
{
	public class SendMessage
	{
		#region => Field

		public string Cmd;
		public int ReturnValue;

		#endregion => Field

		#region => Constructor

		public SendMessage(string _cmd, int _returnValue = 0)
		{
			Cmd = _cmd;
			ReturnValue = _returnValue;
		}

		#endregion => Constructor
	}
}
