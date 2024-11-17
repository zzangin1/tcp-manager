using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpManager.Model
{
	public class RecvMessage
	{
		#region => Field

		public Guid CmdID;
		public string Cmd;

		#endregion => Field

		#region => Constructor

		public RecvMessage(string cmd)
		{
			CmdID = Guid.NewGuid(); // Message 식별을 위해 고유 ID 부여
			Cmd = cmd;
		}

		#endregion => Constructor
	}
}
