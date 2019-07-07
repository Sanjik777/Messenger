using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerWPF
{
	public class ClientInfo
	{
		public string Name { get; set; }
		public TcpClient ClientSocket { get; set; }
	}
}
