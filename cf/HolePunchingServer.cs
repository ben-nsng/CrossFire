using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace CrossFireLib
{
	public class HolePunchingServer
	{
		private cfServer _serverInstance = null;
		private ServerConfiguration _serverConfig;
		
		public cfServer Server
		{
			get
			{
				if (_serverInstance == null)
					_serverInstance = new cfServer();
				return _serverInstance;
			}
		}


		public HolePunchingServer(ServerConfiguration config)
		{
			_serverConfig = config;

			cfConst.PORT_TCP_SERVER = config.ListenPort;
		}



	}

	public struct ServerConfiguration
	{
		public int ListenPort;
	}


}
