#define Socket
//#undef Socket
#define TcpClient
#undef TcpClient
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace CrossFireLib
{
	internal class cfUser
	{
		#region cfUser Field and Property

#if TcpClient
		private TcpClient _client;
#elif Socket
		private Socket _socket;
#endif
		private IPEndPoint _privateEP;
		private volatile bool _keepAlive;
		private ReceiveFileData receivingFileData;
		private volatile bool _pingCheck = true;
		//private Type _connectionType;
		//private ProtocolType _protocol;
		private Stack<ManualResetEvent> _channelLocks;
		private volatile bool _isSendingMessage;
		private volatile int _startSendingTime;
		private IPEndPoint _acctualRemoteEP;
		private EndPoint _remoteEndPoint;

#if TcpClient
		private NetworkStream stream
		{
			get { return _client.GetStream(); }
		}
#endif

		/// <summary>
		/// Gets a value indicating whether the user is connected.
		/// </summary>
		internal bool Connected
		{
			get
			{
#if TcpClient
				return _client.Connected;
#elif Socket
				return _socket == null ? false : _socket.Connected;
#endif
			}
		}

		/// <summary>
		/// Gets a value indicating whether the user's stream can be read.
		/// </summary>
		internal bool CanRead
		{
			get
			{
#if TcpClient
				return stream.CanRead;
#elif Socket
				return _socket == null ? false : true;
#endif
			}
		}

		/// <summary>
		/// Gets a value indicating whether the user's stream can be written.
		/// </summary>
		internal bool CanWrite
		{
			get
			{
#if TcpClient
				return stream.CanWrite;
#elif Socket
				return true;
#endif
			}
		}

		/// <summary>
		/// Gets the user's private network IP address and Port.
		/// </summary>
		internal IPEndPoint privateEP
		{
			set { if (_privateEP == null) _privateEP = value; }
			get { return (IPEndPoint)_privateEP; }
		}

		/// <summary>
		/// Gets the user's public network IP address and Port.
		/// </summary>
		internal IPEndPoint publicEP
		{
			get { return remoteEP; }
		}

		/// <summary>
		/// Gets the user's connected partner's IP address and Port.
		/// </summary>
		public IPEndPoint remoteEP
		{
			get
			{
#if TcpClient
				return (IPEndPoint)_client.Client.RemoteEndPoint;
#elif Socket
				return (IPEndPoint)_socket.RemoteEndPoint;
#endif
			}
		}

		/// <summary>
		/// Gets the user's connected partner's IP address and Port. This value is set when initialize. Not Garantee the value is corrent.
		/// </summary>
		public IPEndPoint acctualRemoteEP
		{
			get { return _acctualRemoteEP; }
		}

		/// <summary>
		/// Get the user's local IP address and Port.
		/// </summary>
		internal IPEndPoint localEP
		{
			get
			{
#if TcpClient
				return (IPEndPoint)_client.Client.LocalEndPoint;
#elif Socket
				return (IPEndPoint)_socket.LocalEndPoint;
#endif
			}
		}

		/// <summary>
		/// Gets a value indicating that whether the user connects to server forever or will be disconnected to perform hole punching.
		/// </summary>
		public bool keepAlive
		{
			set { _keepAlive = value; }
			get { return _keepAlive; }
		}

		internal bool pingCheck
		{
			get { return !_pingCheck; }
		}

		/// <summary>
		/// Gets a value indicating response has received. If response received, ping is set to true.
		/// </summary>
		internal bool ping { get; private set; }

		/// <summary>
		/// Indicates whether the user is receiving file.
		/// </summary>
		internal bool isReceivingMessage { get; set; }

		/// <summary>
		/// Indicates whether the sender sending file to this user
		/// </summary>
		internal bool isSendingMessage
		{
			get { return _isSendingMessage; }
			set
			{
				lock (this)
				{
					_isSendingMessage = value;
					
					if (!value && sendingQueueCount > 0)
						_channelLocks.Pop().Set();
					if (value)
						_startSendingTime = Environment.TickCount;
				}
			}
		}

		/// <summary>
		/// Gets a value that indicates number of message queuing to send to this user
		/// </summary>
		internal int sendingQueueCount
		{
			get { return _channelLocks.Count; }
		}

		/// <summary>
		/// Indicates the protocol using by this user
		/// </summary>
		internal ProtocolType protocol
		{
			get { return _socket.ProtocolType; }
		}

		/// <summary>
		/// Gets a value that indicates the duration of sending message.
		/// </summary>
		internal int sendingTimeTick
		{
			get { return Environment.TickCount - this._startSendingTime; }
		}

		#endregion

#if TcpClient

		public cfUser()
			: this(new TcpClient())
		{
		}

		public cfUser(TcpClient client)
		{
			this._client = client;
			initialize();

			_protocol = ProtocolType.Tcp;

		}

		internal void Reset(TcpClient client)
		{
			this._client = client;
			initialize();
		}

#elif Socket

		public cfUser()
		{
		}

		public cfUser(Socket client)
		{
			this._socket = client;

			initialize();
		}

		public cfUser(Socket client, IPEndPoint remoteEndPoint)
			: this(client)
		{
			this._remoteEndPoint = remoteEndPoint;
		}

		internal void Reset(Socket client)
		{
			this._socket = client;
			this._socket.SendBufferSize = cfConst.SOCKET_SEND_BUFFER_SIZES;
			this._socket.ReceiveBufferSize = cfConst.SOCKET_RECEIVE_BUFFER_SIZES;
			initialize();
		}

#endif

		private void initialize()
		{
			this.ping = true;
			this._acctualRemoteEP = remoteEP;
			this._channelLocks = new Stack<ManualResetEvent>();
			this._keepAlive =
			this.isReceivingMessage =
			this.isSendingMessage = false;
		}

		internal void PingSuccess()
		{
			this.ping = true;
		}

		internal void ResetPing()
		{
			this.ping = false;
		}

		internal void SendMessageLock(object _lock)
		{
			ManualResetEvent resetevent = new ManualResetEvent(false);
			_channelLocks.Push(resetevent);
			Monitor.Exit(_lock);
			resetevent.Reset();
			resetevent.WaitOne();
			this.isSendingMessage = true;
		}

		#region OnRead And OnWrite

		internal void OnRead(DataReceivedEventHandler DataReceived, MessageReceivedEventHandler MessageReceived)
		{
			byte[] bs;
			bool available = false;

#if TcpClient
			available = stream.DataAvailable;
#elif Socket
			available = _socket.Available > 0;
#endif


			while (available)
			{
				if (isReceivingMessage)
				{
					bs = new byte[cfConst.MAX_TRANS_UNITS];
					int read = _socket.Receive(bs);
					//int read = stream.Read(bs, 0, cfConst.FILE_MAX_BLOCK_SIZES);

					receivingFileData.Write(bs, read);

					//ReceivedFile.CrossInvoke(this, new ReceivedFileEventArgs(file, receivingFileData.additionData, receivingFileData.Offset(read)));

					if (receivingFileData.EndofFile())
					{
						MessageReceived.CrossInvoke(this, new MessageReceivedEventArgs(this.remoteEP, receivingFileData.additionData, receivingFileData.stm, receivingFileData.filesize));
						isReceivingMessage = false;
						OnWritePacket(new cfPacket(cfAction.doneReceiveMessage, receivingFileData.filesize.ToString()));
					}
				}
				else
				{
					bs = new byte[cfConst.PACKET_SIZE];
					int read = 0;

#if TcpClient
					read = stream.Read(bs, 0, cfConst.PACKET_SIZE);
#elif Socket
					if (protocol == ProtocolType.Tcp)
						read = _socket.Receive(bs);
					else if (protocol == ProtocolType.Udp)
					{
						EndPoint ep = (EndPoint)this.remoteEP;
						read = _socket.ReceiveFrom(bs, ref _remoteEndPoint);
					}
#endif
					if (read != 0)
					{
						DataReceivedEventArgs e = new DataReceivedEventArgs(bs);

						if (e.action == cfAction.receiveMessage)
						{
							isReceivingMessage = true;
							receivingFileData = new ReceiveFileData(cfExt.base64Decode(e.data[1]), int.Parse(e.data[2]));
						}
						else
						{
							DataReceived.CrossInvoke(this, e);
						}
					}
				}
			}
		}

		internal void OnWritePacket(cfPacket packet)
		{
			cfExt.BackgroundThreadStart(OnWritePacket, packet);
		}

		private void OnWritePacket(object packet)
		{
			if (this.isSendingMessage || _channelLocks.Count > 0)
			{
				Thread.Sleep(cfConst.WAIT_CONNECT_INTERVAL);
				OnWritePacket(packet);
				return;
			}
			byte[] bs = ((cfPacket)packet).ToByte();

			try
			{
#if TcpClient
				stream.Write(bs, 0, cfConst.PACKET_SIZE);
#elif Socket
				if (protocol == ProtocolType.Tcp)
					_socket.Send(bs);
				else if (protocol == ProtocolType.Udp)
					_socket.SendTo(bs, _remoteEndPoint);
#endif

			}
			catch (Exception e)
			{
				cfDebugFactory.log(e.Message, e.StackTrace);
			}
		}

		internal void OnWriteMessage(cfPacket packet, byte[] bs)
		{
			cfExt.BackgroundThreadStart(OnWriteMessage, new object[] { packet, bs });

		}

		private void OnWriteMessage(object packet)
		{
			/*
			if (this.isSendingMessage)
			{
				Thread.Sleep(cfConst.WAIT_CONNECT_INTERVAL);
				OnWriteMessage(packet);
				return;
			}
			this.isSendingMessage = true;*/
			byte[] bpacket = ((cfPacket)(((object[])packet)[0])).ToByte();

#if TcpClient
			stream.Write(bpacket, 0, cfConst.PACKET_SIZE);
#elif Socket
			if (protocol == ProtocolType.Tcp)
				_socket.Send(bpacket);
			else if (protocol == ProtocolType.Udp)
				_socket.SendTo(bpacket, _remoteEndPoint);
#endif

			byte[] bs = (byte[])((object[])packet)[1];
			if (bs.Length <= cfConst.FILE_MAX_BLOCK_SIZES)
			{
#if TcpClient
				stream.Write(bs, 0, bs.Length);
#elif Socket
				_socket.Blocking = true;
				_socket.Send(bs);
#endif
			}
			else
				throw new Exception("Filesize exceeds limit.");
		}

		#endregion

		internal void Dispose()
		{
#if TcpClient
			if (this.Connected)
				this.stream.Close();
			this._client.Client.Close();
#elif Socket
			cfDebugFactory.log("User Disposing");
			if (Connected)
			{
				_socket.Shutdown(SocketShutdown.Both);
				_socket.Disconnect(true);
			}
			_socket.Close();
			((IDisposable)_socket).Dispose();
#endif
		}

		private struct ReceiveFileData
		{
			private string _additionData;
			private int _filesize;
			private int _wrttenlen;
			private MemoryStream _mstm;

			internal string additionData
			{
				get { return _additionData; }
			}

			internal int filesize
			{
				get { return _filesize; }
			}

			internal MemoryStream stm
			{
				get { return _mstm; }
			}

			public ReceiveFileData(string additionData, int filesize)
			{
				_additionData = additionData;
				_wrttenlen = 0;
				_filesize = filesize;
				_mstm = new MemoryStream(cfConst.FILE_MAX_BLOCK_SIZES);
			}

			public void Write(byte[] bs, int size)
			{
				_mstm.Seek(_wrttenlen, SeekOrigin.Begin);
				_mstm.Write(bs, 0, size);
				_wrttenlen += size;
			}

			public bool EndofFile()
			{
				return _wrttenlen >= _filesize;
			}
		}
	}

	internal class cfUserCollection : IEnumerable
	{
		private List<cfUser> _userCollection;
		private object _lock = new object();

		internal cfUser this[int i]
		{
			get { return _userCollection[i]; }
		}

		internal cfUserCollection()
		{
			_userCollection = new List<cfUser>();
		}

		public cfUserEnumerator GetEnumerator()
		{
			return new cfUserEnumerator(this._userCollection);
		}

		internal bool Contains(cfUser user)
		{
			return _userCollection.Contains(user);
		}

		internal int Count()
		{
			return _userCollection.Count;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		internal void Add(cfUser user)
		{
			_userCollection.Add(user);
		}

		internal void Remove(cfUser user)
		{
			lock (_userCollection)
			{
				user.Dispose();
				_userCollection.Remove(user);
			}
		}

		internal void Remove(string ip)
		{
			lock (_userCollection)
			{
				for (int i = 0; i < this.Count(); i++)
					if (this[i].remoteEP.Address.ToString() == ip)
					{
						Remove(this[i]);
						i--;
					}
			}
		}

		internal void Clear()
		{
			lock (_userCollection)
			{
				while (_userCollection.Count > 0)
					Remove(_userCollection[0]);
			}
		}

		internal cfUser IndexOf(string ip)
		{
			lock (_userCollection)
			{
				foreach (cfUser user in _userCollection)
					if (user.publicEP.Address.ToString() == ip)
						return user;
			}
			return null;
		}

		internal cfUser IndexOf(string ip, Predicate<cfUser> predicate)
		{
			lock (_userCollection)
			{
				foreach (cfUser user in _userCollection)
					if (user.publicEP.Address.ToString() == ip && predicate(user))
						return user;
			}
			return null;
		}

		internal cfUser LastIndexOf(string ip, Predicate<cfUser> predicate)
		{
			lock (_userCollection)
			{
				for (int i = this.Count() - 1; i >= 0; i--)
				{
					if (this[i].publicEP.Address.ToString() == ip && predicate(this[i]))
						return this[i];
				}
			}
			return null;
		}

		internal cfUser LastIndexOf(string ip, cfUser position, Predicate<cfUser> predicate)
		{
			lock (_userCollection)
			{
				for (int i = _userCollection.IndexOf(position) - 1; i >= 0; i--)
				{
					if (this[i].publicEP.Address.ToString() == ip && predicate(this[i]))
						return this[i];
				}
			}
			return null;
		}

		internal void DoPing(Predicate<cfUser> predicate)
		{
			foreach (cfUser user in this)
			{
				if (predicate(user))
				{
					if (user.isReceivingMessage || user.isSendingMessage)
						continue;

					try
					{
						user.OnWritePacket(new cfPacket(cfAction.ping, cfConst.PING_REQUEST));
					}
					catch (IOException)
					{
					}
					catch (Exception e)
					{
						cfDebugFactory.log(e.Message, e.StackTrace);
					}
				}
			}
		}

		internal void CheckPing()
		{
			lock (_userCollection)
			{
				for (int i = 0; i < this.Count(); i++)
				{
					if (this[i].isReceivingMessage || this[i].isSendingMessage)
						continue;

					if (this[i].pingCheck)
					{
						if (!this[i].ping || !this[i].Connected)
						{
							Remove(this[i]);
							i--;
						}
						else
							this[i].ResetPing();
					}
				}
			}
		}

		internal cfUser GetAvailableUser(string ip)
		{
			IPAddress ipaddress;
			if (!IPAddress.TryParse(ip, out ipaddress))
				return null;

			List<cfUser> availableUser = new List<cfUser>();
			//find if there are users that are not receiving message
			foreach (cfUser user in this)
			{
				lock (user)
				{
					if (user.remoteEP.Address.ToString() == ip && !user.isSendingMessage)
					{
						cfDebugFactory.log(Thread.CurrentThread.ManagedThreadId.ToString() + ": " + "isSendingMessage = false");
						user.isSendingMessage = true;
						return user;
					}
					else
						availableUser.Add(user);
				}
			}

			//no availableUser and find the user that have min sending queue
			//and queue the job here
			if (availableUser.Count > 0)
			{
				Monitor.Enter(_lock);
				cfUser selected = availableUser.Where(u0 => u0.sendingQueueCount ==
					availableUser.Min(u1 => u1.sendingQueueCount)).First();
				cfDebugFactory.log(Thread.CurrentThread.ManagedThreadId.ToString() + ": " + "Queue - " + selected.sendingQueueCount);
				cfDebugFactory.log(Thread.CurrentThread.ManagedThreadId.ToString() + ": " + "Lock");
				selected.SendMessageLock(_lock);
				cfDebugFactory.log(Thread.CurrentThread.ManagedThreadId.ToString() + ": " + "Unlock");
				return selected;
			}

			return null;
		}

		internal void SendAll(cfPacket packet)
		{
			foreach (cfUser user in this)
				user.OnWritePacket(packet);
		}

	}

	internal class cfUserEnumerator : IEnumerator
	{
		private List<cfUser> _users;
		private int index;

		internal cfUserEnumerator(List<cfUser> users)
		{
			this._users = users;
			index = -1;
		}

		public void Reset()
		{
			index = -1;
		}

		public cfUser Current
		{
			get { return _users[index]; }
		}

		object IEnumerator.Current
		{
			get { return Current; }
		}

		public bool MoveNext()
		{
			index++;
			return index < _users.Count;
		}

	}

}
