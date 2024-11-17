using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpManager.Model
{
	public static class InterfaceProtocol
	{
		/// <summary>
		/// Command (Length : 5)
		/// </summary>
		public const string STEST = "STEST";
		public const string CTEST = "CTEST";

		/// <summary>
		/// Msg Format
		/// </summary>
		public const string REQ_FIRST_STRING = "@";
		public const string RES_FIRST_STRING = "$";
		public const int RES_DEFAULT = 0;
		public const int RES_ACK = 1;
		public const int RES_COMPLETE = 2;
		public const char END_OF_MSG = '\n';
	}
}
