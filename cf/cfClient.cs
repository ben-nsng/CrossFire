#define Socket
//#undef Socket
#define TcpClient
#undef TcpClient
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Text;

namespace CrossFireLib
{
	public class cfClient
	{

		private cfUser _server;
		private IPEndPoint _serverEP;
		private Thread _processThread;
		private Thread _pingThread;
		private List<Thread> _tcpClientThreads;
		private List<Thread> _udpClientThreads;
		private cfUserCollection _tcpUsers;
		private cfUserCollection _udpUsers;
		private volatile bool connectingPartner;
		private Stack<ManualResetEvent> _waitConnectEvent;

		public event EventHandler ServerConnecting;
		public event ConnectedEventHandler ServerConnected;
		public event EventHandler ServerDisconnected;
		public event EventHandler PartnerConnecting;
		public event PartnerConnectedEventHandler PartnerConnected;
		public event PartnerDisconnectedEventHandler PartnerDisconnected;
		public event EventHandler MessageSending;
		public event MessageSentEventHandler MessageSent;
		public event MessageReceivedEventHandler MessageReceived;
		public event PartnerMessageReceivedEventHandler PartnerMessageReceived;
		internal event DataReceivedEventHandler ServerDataReceived;
		internal event DataReceivedEventHandler ClientDataReceived;

		/// <summary>
		/// Get a value that determines whether client connects to server.
		/// </summary>
		private bool Connected
		{
			get { return _server.Connected; }
		}

		private bool CanRead
		{
			get { return _server.CanRead; }
		}

		private IPEndPoint local
		{
			get { return (IPEndPoint)_server.localEP; }
		}

		public cfClient(IPEndPoint server)
		{
			_server = new cfUser();
			_serverEP = server;
			_tcpClientThreads = new List<Thread>();
			_udpClientThreads = new List<Thread>();
			_tcpUsers = new cfUserCollection();
			_udpUsers = new cfUserCollection();
			_waitConnectEvent = new Stack<ManualResetEvent>();
			connectingPartner = false;

			if (cfConst.DEBUG)
				cfDebugFactory.initializeDebug();

			this.ServerConnecting += (o, e) =>
				{
					if (Connected)
						throw new Exception("Server already connected.");
				};
			this.PartnerConnecting += (o, e) =>
				{
					connectingPartner = true;
				};
			this.PartnerConnected += (o, e) =>
				{
					connectingPartner = false;
					if (_waitConnectEvent.Count > 0)
						_waitConnectEvent.Pop().Set();
				};
			this.ServerDataReceived += (o, e) =>
				{
					ProcessTask(e.dataset);
				};
			this.ClientDataReceived += (o, e) =>
				{
					ProcessTask(e.dataset, o as cfUser);
				};
		}

		#region Connection

		/// <summary>
		/// This function must be called before using other function. Client connects to server and retrieve partner's information from server.
		/// </summary>
		public void Connect()
		{
			cfExt.BackgroundThreadStart(ConnectToServer);
		}

		private void ConnectToServer()
		{
			try
			{
				if (!Connected)
				{
					ServerConnecting.CrossInvoke(this, new EventArgs());
#if TcpClient
					TcpClient client = new TcpClient();
					client.Connect(_serverEP);
#elif Socket
					Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					client.Connect(_serverEP);
#endif
					_server = new cfUser(client);
					_server.OnWritePacket(new cfPacket(cfAction.foreverConnection));
					_server.OnWritePacket(new cfPacket(cfAction.online, _server.localEP.Address.ToString(), _server.localEP.Port.ToString()));

					cfDebugFactory.log("Connecting To Server...");

					_processThread = cfExt.BackgroundThreadStart(ProcessData);
					_pingThread = cfExt.BackgroundThreadStart(Ping);

					ServerConnected.CrossInvoke(this, new ConnectedEventArgs(true));
				}
			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);

				ServerConnected.CrossInvoke(this, new ConnectedEventArgs(false));
			}
		}

		/// <summary>
		/// Disconnect from server.
		/// </summary>
		public void Disconnect()
		{
			cfExt.BackgroundThreadStart(DisconnectFromServer);
		}

		private void DisconnectFromServer()
		{
			try
			{
				_tcpUsers.Clear();
				_udpUsers.Clear();
				_tcpClientThreads.Clear();
				_pingThread.Abort();
				_processThread.Abort();
				connectingPartner = false;

				foreach (Thread t in _tcpClientThreads)
					if (t.IsAlive)
						t.Abort();

				foreach (Thread t in _udpClientThreads)
					if (t.IsAlive)
						t.Abort();

				while (_waitConnectEvent.Count > 0)
					_waitConnectEvent.Pop().Set();

				if (Connected)
					_server.Dispose();
			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
			}
			finally
			{
				ServerDisconnected.CrossInvoke(this, new EventArgs());
			}
		}

		/// <summary>
		/// Create a connection to partner.
		/// </summary>
		/// <param name="ip">Partner's IP</param>
		/// <param name="protocol">Tcp or Udp</param>
		public void Connect(string ip, ProtocolType protocol)
		{
			IPAddress add;
			if (!IPAddress.TryParse(ip, out add) || !Connected)
				return;

			if (connectingPartner)
				LockConnect();

			if (protocol == ProtocolType.Tcp)
			{
				_server.OnWritePacket(new cfPacket(cfAction.setupTcpConnection, ip, cfConst.TCP_STRING, cfConst.PING_INTERVAL.ToString()));
				cfExt.BackgroundThreadStart(CreateConnectionToServer, cfConst.TCP_STRING);
			}
			else if (protocol == ProtocolType.Udp)
			{
				_server.OnWritePacket(new cfPacket(cfAction.setupUdpConnection, ip, cfConst.UDP_STRING, cfConst.PING_INTERVAL.ToString()));
				//cfExt.BackgroundThreadStart(CreateConnectionToServer, cfConst.UDP_STRING);
			}
			else
				throw new NotSupportedException();
		}

		private void LockConnect()
		{
			ManualResetEvent e = new ManualResetEvent(false);
			_waitConnectEvent.Push(e);
			e.Reset();
			e.WaitOne();
		}

		#endregion

		#region Write Message

		/// <summary>
		/// Delivers message to Partner.
		/// </summary>
		/// <param name="ip">Partner's IP</param>
		/// <param name="message">Message to be delivered</param>
		public void Write(string ip, string message)
		{
			Write(ip, Encoding.UTF8.GetBytes(message));
		}

		/// <summary>
		/// Delivers message to Partner.
		/// </summary>
		/// <param name="ip">Partner's IP</param>
		/// <param name="bs">Byte data to be delivered</param>
		public void Write(string ip, byte[] bs)
		{
			Write(ip, "", bs);
		}

		/// <summary>
		/// Delivers message to Partner with additional Data
		/// </summary>
		/// <param name="ip">Partner's IP</param>
		/// <param name="data">Additional Data</param>
		/// <param name="message">Message to be delivered</param>
		public void Write(string ip, string data, string message)
		{
			Write(ip, data, Encoding.UTF8.GetBytes(message));
		}

		/// <summary>
		/// Delivers message to Partner with additional Data
		/// </summary>
		/// <param name="ip">Partner's IP</param>
		/// <param name="data">Additional Data</param>
		/// <param name="bs">Byte data to be delivered</param>
		public void Write(string ip, string data, byte[] bs)
		{
			cfExt.BackgroundThreadStart(Write, new object[] { ip, data, bs });

		}

		private void Write(object info)
		{
			string ip = (string)((object[])info)[0];
			string data = (string)((object[])info)[1];
			byte[] bs = (byte[])((object[])info)[2];

			cfUser receiver = _tcpUsers.GetAvailableUser(ip);
			if (receiver != null && bs.Length > 0)
			{
				MessageSending.CrossInvoke(this, new EventArgs());
				receiver.OnWriteMessage(new cfPacket(cfAction.receiveMessage, cfExt.base64Encode(data), bs.Length.ToString()), bs);
				MessageSent.CrossInvoke(this, new MessageSentEventArgs(data, bs, receiver.remoteEP, true));
			}
			else
				MessageSent.CrossInvoke(this, new MessageSentEventArgs(data, bs, receiver.remoteEP, false));
		}

		#endregion

		#region Network Stream Processor (Server)

		//Deal with server message
		private void ProcessData()
		{
			_server.OnWritePacket(new cfPacket(cfAction.online, local.Address.ToString(), local.Port.ToString()));

			try
			{
				while (Connected)
				{
					if (CanRead)
					{
						_server.OnRead(ServerDataReceived, null);
					}
					Thread.Sleep(cfConst.READ_STREAM_INTERVAL);
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
				if (e.InnerException != null)
					cfDebugFactory.log(e.InnerException.Message, e.InnerException.StackTrace);
			}
			finally
			{
				Disconnect();
			}
		}

		//client -> server
		private void ProcessTask(string action)
		{
			string[] data = action.Split(';');
			if (cfExt.equal(data[0], cfAction.createConnection))
			{
				CreateConnectionToServer(data[1]);
			}
			else if (cfExt.equal(data[0], cfAction.ping))
			{
				if (data[1] == cfConst.PING_REQUEST)
					_server.OnWritePacket(new cfPacket(cfAction.ping, cfConst.PING_RESPONSE));
				else if (data[1] == cfConst.PING_RESPONSE)
					_server.PingSuccess();
			}
			else if (cfExt.equal(data[0], cfAction.removeUserfromIpAddress))
			{
				_tcpUsers.Remove(data[1]);
			}
		}

		#endregion

		#region Network Stream TCP Processor (Client)

		//Deal with to client message
		private void ProcessClient(object o)
		{
			cfUser user = (cfUser)o;

			try
			{
				while (Connected && user.Connected)
				{
					if (user.CanRead)
					{
						user.OnRead(ClientDataReceived, MessageReceived);
					}

					Thread.Sleep(cfConst.READ_STREAM_INTERVAL);
				}
			}
			catch (TargetInvocationException e)
			{
				cfDebugFactory.log(e.InnerException.Message, e.InnerException.StackTrace);
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
				if (e.InnerException != null)
					cfDebugFactory.log(e.InnerException.Message, e.InnerException.StackTrace);
			}
			finally
			{
				PartnerDisconnected.CrossInvoke(this, new PartnerDisconnectedEventArgs(user.acctualRemoteEP));

				_tcpUsers.Remove(user);

			}
		}

		//client -> client
		private void ProcessTask(string action, cfUser user)
		{
			string[] data = action.Split(';');

			if (cfExt.equal(data[0], cfAction.buildConnectionToPartner))
			{
				try
				{
					if (user.remoteEP.Address.ToString() != _serverEP.Address.ToString())
						throw new Exception("Already Connected!");

					user.OnWritePacket(new cfPacket(cfAction.disconnectedFromServer));
					int port = user.localEP.Port;
					//_tcpUsers.Remove(user);
					user.Dispose();
					cfDebugFactory.log("Attemp to create connection to " + data[1] + ":" + data[2]);



#if TcpClient
					TcpClient tcp = new TcpClient(new IPEndPoint(IPAddress.Any, port));
					tcp.Connect(data[1], int.Parse(data[2]));
					user.Reset(tcp);
#elif Socket
					Socket partner;
					if (data[3] == cfConst.TCP_STRING)
					{
						partner = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
						partner.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
						partner.Bind((EndPoint)new IPEndPoint(IPAddress.Any, port));
						partner.Connect(IPAddress.Parse(data[1]), int.Parse(data[2]));
						user.Reset(partner);
					}
					else if (data[3] == cfConst.UDP_STRING)
					{
						partner = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
						partner.Bind((EndPoint)new IPEndPoint(IPAddress.Any, port));
						user.Reset(partner);
					}

#endif

					PartnerConnected.CrossInvoke(this, new PartnerConnectedEventArgs(true, user.remoteEP));

					cfDebugFactory.log("Connection OK");
				}
				catch (Exception e)
				{
					cfDebugFactory.log(e.Message, e.StackTrace);
					PartnerConnected.CrossInvoke(this, new PartnerConnectedEventArgs(false, data[1], data[2]));
				}
			}
			else if (cfExt.equal(data[0], cfAction.ping))
			{
				if (data[1] == cfConst.PING_REQUEST)
				{
					user.OnWritePacket(new cfPacket(cfAction.ping, cfConst.PING_RESPONSE));
				}
				else if (data[1] == cfConst.PING_RESPONSE)
				{
					user.PingSuccess();
				}
			}
			else if (cfExt.equal(data[0], cfAction.partnerFailedConnect))
			{

				PartnerConnected.CrossInvoke(this, new PartnerConnectedEventArgs(false, data[1], data[2]));
				throw new Exception("Fail to connect partner.");
			}
			else if (cfExt.equal(data[0], cfAction.doneReceiveMessage))
			{
				cfDebugFactory.log("send finished");
				PartnerMessageReceived.CrossInvoke(this, new PartnerMessageReceivedEventArgs(user.acctualRemoteEP, long.Parse(data[1]), user.sendingTimeTick));
				user.isSendingMessage = false;
			}
		}

		private void Ping()
		{
			try
			{
				while (Connected)
				{
					_tcpUsers.CheckPing();
					_tcpUsers.DoPing((user) => { return user.remoteEP.Address != _serverEP.Address; });

					_server.OnWritePacket(new cfPacket(cfAction.ping, cfConst.PING_REQUEST));
					if (_server.pingCheck)
					{
						if (!_server.ping || !_server.Connected)
							_server.Dispose();
						else
							_server.ResetPing();
					}
					Thread.Sleep(cfConst.PING_INTERVAL);
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
			}
		}

		#endregion

		#region Network Stream UDP Processor (Client)

		private void ProcessUDPClient(object o)
		{
			cfUser user = (cfUser)o;

			//_server.OnWritePacket(new cfPacket(
		}

		#endregion

		private void CreateConnectionToServer(object protocol)
		{
			cfDebugFactory.log("Create connection to Server.");

			try
			{
				PartnerConnecting.CrossInvoke(this, new EventArgs());
#if TcpClient
				TcpClient client = new TcpClient();
				client.Connect(_serverEP);
#elif Socket
				cfUser user = null;
				Socket client = null;
				if ((string)protocol == cfConst.TCP_STRING)
				{
					client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					client.Connect(_serverEP);
					user = new cfUser(client);
					_tcpClientThreads.Add(cfExt.BackgroundThreadStart(ProcessClient, user));
					_tcpUsers.Add(user);
				}
				else if ((string)protocol == cfConst.UDP_STRING)
				{
					client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					client.Bind((EndPoint)new IPEndPoint(IPAddress.Any, cfConst.PORT_UDP_CLIENT));
					user = new cfUser(client, _serverEP);
					cfExt.BackgroundThreadStart(ProcessUDPClient, client);
					//_tcpClientThreads.Add(cfExt.BackgroundThreadStart(ProcessUDPClient, client));
				}
#endif

				cfDebugFactory.log("Connect", user.remoteEP.Address.ToString());

				cfDebugFactory.log("Connection OK");
			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
				PartnerConnected.CrossInvoke(this, new PartnerConnectedEventArgs(false, _serverEP));
			}
		}
	}
}
