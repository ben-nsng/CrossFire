using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace CrossFireLib
{
	public class HolePunchingClient
	{
		private cfClient _clientInstance = null;
		private ClientConfiguration _clientConfig;

		public cfClient Client
		{
			get
			{
				if (_clientInstance == null)
					_clientInstance = new cfClient(_clientConfig.ServerEP);
				return _clientInstance;
			}
		}

		public HolePunchingClient(ClientConfiguration config)
		{
			_clientConfig = config;

			cfConst.PORT_UDP_CLIENT = config.LocalUDPPort;
		}
	}

	public struct ClientConfiguration
	{
		public IPEndPoint ServerEP;
		public int LocalUDPPort;
	}
}
