#define Socket
//#undef Socket
#define TcpClient
#undef TcpClient
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CrossFireLib
{
	public class cfServer
	{
		private volatile bool _running;
		private ManualResetEvent _connectedEvent;
		private Thread _pendingThread;
		private Thread _listenThread;
		private Thread _pingThread;
#if TcpClient
		private TcpListener _listener;
#endif
		private Socket _server;
		private cfUserCollection _users;
		private Stack<Task> _pendingTasks;
		private Predicate<cfUser> predc_keepAlive = (u) => { return u.keepAlive == false; };

		public event EventHandler ServerStarting;
		public event ServerStartedEventHandler ServerStarted;
		public event EventHandler ServerStopping;
		public event ServerStoppedEventHandler ServerStopped;
		public event ClientConnectedEventHandler ClientConnected;
		public event ClientOnlinedEventHandler ClientOnlined;
		public event ClientDisconnectedEventHandler ClientDisconnected;
		internal event DataReceivedEventHandler DataReceived;

		public cfServer()
		{
			this._running = false;

			_connectedEvent = new ManualResetEvent(false);
#if TcpClient
			_listener = new TcpListener(IPAddress.Any, port);
#endif
			_users = new cfUserCollection();
			_pendingTasks = new Stack<Task>();

			if(cfConst.DEBUG)
				cfDebugFactory.initializeDebug();

			this.DataReceived += new DataReceivedEventHandler(OnDataReceived);


			ServerStarting += (o, e) =>
				{
					if (_running)
						throw new Exception("Server already running.");
				};

			ServerStopping += (o, e) =>
				{
					if (!_running)
						throw new Exception("Server already closed");
				};

			ClientDisconnected += (o, e) =>
				{
					if (_users.IndexOf(e.clientIP) == null)
						_users.SendAll(new cfPacket(cfAction.removeUserfromIpAddress, e.clientIP));
				};
		}

		public void Start()
		{
			cfExt.BackgroundThreadStart(StartServer);
		}

		private void StartServer()
		{
			try
			{
				cfDebugFactory.log("Server Started...");

				ServerStarting.CrossInvoke(this, new EventArgs());
				_running = true;
#if TcpClient
				_listener.Start();
#elif Socket
				_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_server.Bind((EndPoint)new IPEndPoint(IPAddress.Any, cfConst.PORT_TCP_SERVER));
				_server.Listen(cfConst.MAX_SOCKET_LISTEN_BACKLOG);
#endif

				_listenThread = cfExt.BackgroundThreadStart(Listen);
				_pendingThread = cfExt.BackgroundThreadStart(ProcessPendingTask);
				_pingThread = cfExt.BackgroundThreadStart(Ping);

				ServerStarted.CrossInvoke(this, new ServerStartedEventArgs(true));
				return;
			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
			}
			ServerStarted.CrossInvoke(this, new ServerStartedEventArgs(false));
		}

		public void Stop()
		{
			cfExt.BackgroundThreadStart(StopServer);
		}

		private void StopServer()
		{
			try
			{
				cfDebugFactory.log("Server Stopped...");

				ServerStopping.CrossInvoke(this, new EventArgs());


				_running = false;
				_connectedEvent.Set();
#if TcpClient
				_listener.Stop();
#elif Socket
				if (_server.Connected)
					_server.Shutdown(SocketShutdown.Both);
				((IDisposable)_server).Dispose();
#endif
				_listenThread.Abort();
				_pendingThread.Abort();
				_pingThread.Abort();

				_pendingTasks.Clear();
				_users.Clear();
				ServerStopped.CrossInvoke(this, new ServerStoppedEventArgs(true));
				return;

			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
			}

			ServerStopped.CrossInvoke(this, new ServerStoppedEventArgs(false));
		}

		private void Listen()
		{
			try
			{
				while (_running)
				{
#if TcpClient
					TcpClient client = _listener.AcceptTcpClient();
#elif Socket
					Socket client = _server.Accept();
#endif
					cfExt.BackgroundThreadStart(ProcessClient, client);
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
			}
			cfDebugFactory.log("Listen thread stop");
		}

		private void ProcessClient(object client)
		{
#if TcpClient
			cfUser user = new cfUser((TcpClient)client);
#elif Socket
			cfUser user = new cfUser((Socket)client);
#endif
			_users.Add(user);
			ClientConnected.CrossInvoke(this, new ClientConnectedEventArgs(user.remoteEP));
			try
			{
				cfDebugFactory.log("Accept Tcp Client...", "local = " + user.localEP.Address.ToString() + ":" + user.localEP.Port.ToString(), "remote = " + user.remoteEP.Address.ToString() + ":" + user.remoteEP.Port.ToString());

				while (user.Connected)
				{
					if (user.CanRead)
					{
						user.OnRead(DataReceived, null);
					}

					Thread.Sleep(cfConst.READ_STREAM_INTERVAL);
				}

			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
				if(e.InnerException != null)
					cfDebugFactory.log(e.InnerException.Message, e.InnerException.StackTrace);
			}
			finally
			{
				ClientDisconnected.CrossInvoke(this, new ClientDisconnectedEventArgs(user.acctualRemoteEP));
				_users.Remove(user);
			}
		}

		private void OnDataReceived(object sender, DataReceivedEventArgs e)
		{
			ProcessTask(new Task() { user = sender as cfUser, action = e.dataset });
		}

		private void ProcessTask(Task task)
		{
			string[] data = task.action.Split(';');
			if (cfExt.equal(data[0], cfAction.online))
			{
				task.user.privateEP = new IPEndPoint(IPAddress.Parse(data[1]), int.Parse(data[2]));
				ClientOnlined.CrossInvoke(this, new ClientOnlinedEventArgs(task.user.remoteEP, task.user.keepAlive));
				cfDebugFactory.log("Client " + task.user.remoteEP.Address.ToString() + ":" + task.user.remoteEP.Port.ToString() + " Online!");
			}
			else if (cfExt.equal(data[0], cfAction.foreverConnection))
			{
				task.user.keepAlive = true;
			}
			else if (cfExt.equal(data[0], cfAction.setupTcpConnection))
			{
				cfDebugFactory.log("Attemp to tell partner setup connection");

				cfUser user = _users.IndexOf(data[1]);

				int interval = int.Parse(data[3]) - cfConst.READ_STREAM_INTERVAL;
				//timeout = cfConst.READ_STREAM_INTERVAL
				//if partner(forever connection) still not connect to server,
				//then tell the client fail to connect to partner
				if (interval < 0)
				{
					cfUser sender = _users.IndexOf(task.user.remoteEP.Address.ToString(), predc_keepAlive);
					if (sender != null)
					{
						sender.OnWritePacket(new cfPacket(cfAction.partnerFailedConnect, data[1], "-1"));
						_users.Remove(sender);
					}
					return;
				}

				task.action = new cfPacket(cfAction.setupTcpConnection, data[1], data[2], interval.ToString()).ToString();

				if (user != null)
				{
					//if partner is found, then tell him to create another connection (temp connection) to server
					//and server create pending task to see sender and partner are both have (temp connection) connect to server.
					user.OnWritePacket(new cfPacket(cfAction.createConnection, data[2]));
					string action = new cfPacket(cfAction.pendingTcpConnection,
						task.user.publicEP.Address.ToString(), task.user.publicEP.Port.ToString(),
						user.publicEP.Address.ToString(), user.publicEP.Port.ToString(), data[2], cfConst.PING_INTERVAL.ToString()).ToString();

					cfDebugFactory.log("Pending setup connection task",
						task.user.publicEP.Address.ToString(), task.user.publicEP.Port.ToString(),
						user.publicEP.Address.ToString(), user.publicEP.Port.ToString());

					_pendingTasks.Push(new Task() { action = action, user = task.user });


				}
				else
					_pendingTasks.Push(task);
			}
			else if (cfExt.equal(data[0], cfAction.setupUdpConnection))
			{

			}
			else if (cfExt.equal(data[0], cfAction.disconnectedFromServer))
			{
				_users.Remove(task.user);
			}
			else if (cfExt.equal(data[0], cfAction.pendingTcpConnection))
			{

				cfUser user = _users.LastIndexOf(data[1], predc_keepAlive);
				cfUser partner = _users.LastIndexOf(data[3], predc_keepAlive);
				if (user == partner && user != null)
					partner = _users.LastIndexOf(data[3], user, predc_keepAlive);

				int interval = int.Parse(data[6]) - cfConst.READ_STREAM_INTERVAL;
				//timeout = cfConst.READ_STREAM_INTERVAL
				//if sender and partner 's (temp connection) do not find, 
				//then tell the one that connection is fail and remove the (temp connection)
				if (interval < 0)
				{
					if (user != null)
					{
						user.OnWritePacket(new cfPacket(cfAction.partnerFailedConnect, data[3], data[4]));
						_users.Remove(user);
					}
					if (partner != null)
					{
						partner.OnWritePacket(new cfPacket(cfAction.partnerFailedConnect, data[1], data[2]));
						_users.Remove(partner);
					}
					return;
				}

				task.action = new cfPacket(cfAction.pendingTcpConnection, data[1], data[2], data[3], data[4], data[5], interval.ToString()).ToString();

				cfDebugFactory.log("pending connection ---", data[1], data[3]);

				if (user != null && partner != null)
				{

					cfDebugFactory.log("Disconnect user = " + user.remoteEP.Address.ToString() + ":" + user.remoteEP.Port.ToString());
					cfDebugFactory.log("Disconnect partner = " + partner.remoteEP.Address.ToString() + ":" + partner.remoteEP.Port.ToString());

					user.OnWritePacket(new cfPacket(cfAction.buildConnectionToPartner, data[3], partner.remoteEP.Port.ToString(), data[5]));
					partner.OnWritePacket(new cfPacket(cfAction.buildConnectionToPartner, data[1], user.remoteEP.Port.ToString(), data[5]));

					//_users.Remove(user);
					//_users.Remove(partner);
				}
				else if (user != null || partner != null)
					_pendingTasks.Push(task);
				//if user and partner both null then no need to pend the task.
			}
			else if (cfExt.equal(data[0], cfAction.ping))
			{
				if (data[1] == cfConst.PING_REQUEST)
					task.user.OnWritePacket(new cfPacket(cfAction.ping, cfConst.PING_RESPONSE));
				else if (data[1] == cfConst.PING_RESPONSE)
					task.user.PingSuccess();
			}
		}

		private void ProcessPendingTask()
		{
			try
			{
				while (_running)
				{
					if(_pendingTasks.Count > 0)
						ProcessTask(_pendingTasks.Pop());
					Thread.Sleep(cfConst.READ_STREAM_INTERVAL);
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

		private void Ping()
		{
			try
			{
				while (_running)
				{
					_users.CheckPing();

					_users.DoPing((user) => { return true; });
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

		private struct Task
		{
			internal cfUser user;
			internal string action;
		}
	}


}
