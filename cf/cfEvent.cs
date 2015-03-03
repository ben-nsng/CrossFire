using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace CrossFireLib
{
	internal interface iConnected
	{
		bool connected { get; }
	}

	internal interface iSuccess
	{
		bool success { get; }
	}

	internal interface iMessage
	{
		byte[] message { get; }
	}

	internal interface iPartnerIPEndPoint
	{
		string partnerIP { get; }
		int partnerPort { get; }
	}

	internal interface iClientIPEndPoint
	{
		string clientIP { get; }
		int clientPort { get; }
	}

	#region cfClient Event

	public delegate void ConnectedEventHandler(object sender, ConnectedEventArgs e);
	public delegate void PartnerConnectedEventHandler(object sender, PartnerConnectedEventArgs e);
	public delegate void PartnerDisconnectedEventHandler(object sender, PartnerDisconnectedEventArgs e);
	public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs e);
	public delegate void MessageSentEventHandler(object sender, MessageSentEventArgs e);
	public delegate void PartnerMessageReceivedEventHandler(object sender, PartnerMessageReceivedEventArgs e);
	internal delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

	public class ConnectedEventArgs : EventArgs, iConnected
	{
		public bool connected { get; private set; }

		public ConnectedEventArgs(bool Connected)
		{
			this.connected = Connected;
		}
	}

	public class PartnerConnectedEventArgs : EventArgs, iConnected, iPartnerIPEndPoint
	{
		public bool connected { get; private set; }
		public string partnerIP { get; private set; }
		public int partnerPort { get; private set; }

		public PartnerConnectedEventArgs(bool Connected, IPEndPoint ipEP)
			: this(Connected, ipEP.Address.ToString(), ipEP.Port.ToString())
		{
		}

		public PartnerConnectedEventArgs(bool Connected, string ip)
			: this(Connected, ip, "-1")
		{
		}

		public PartnerConnectedEventArgs(bool Connected, string ip, string port)
		{
			this.connected = Connected;
			this.partnerIP = ip;
			this.partnerPort = int.Parse(port);
		}

	}

	public class PartnerDisconnectedEventArgs : EventArgs, iPartnerIPEndPoint
	{
		public string partnerIP { get; private set; }
		public int partnerPort { get; private set; }

		public PartnerDisconnectedEventArgs(IPEndPoint ep)
		{
			partnerIP = ep.Address.ToString();
			partnerPort = ep.Port;
		}
	}

	public class MessageReceivedEventArgs : EventArgs, iMessage, iPartnerIPEndPoint
	{
		public byte[] message { get; private set; }
		public string additionalData { get; private set; }
		public string partnerIP { get; private set; }
		public int partnerPort { get; private set; }

		public MessageReceivedEventArgs(IPEndPoint ep, string additionalData, MemoryStream stm, int size)
		{
			this.partnerIP = ep.Address.ToString();
			this.partnerPort = ep.Port;
			this.additionalData = additionalData;
			message = new byte[size];
			stm.Seek(0, SeekOrigin.Begin);
			stm.Read(message, 0, size);
		}
	}

	public class PartnerMessageReceivedEventArgs : EventArgs, iSuccess, iPartnerIPEndPoint
	{
		public bool success { get; private set; }
		public string partnerIP { get; private set; }
		public int partnerPort { get; private set; }
		public long messageSize { get; private set; }
		public int sendingDuration { get; private set; }

		public PartnerMessageReceivedEventArgs(IPEndPoint ep, long messageSize, int sendingDuration)
		{
			this.success = true;
			this.partnerIP = ep.Address.ToString();
			this.partnerPort = ep.Port;
			this.messageSize = messageSize;
			this.sendingDuration = sendingDuration;
		}

	}

	public class MessageSentEventArgs : EventArgs, iSuccess, iMessage, iPartnerIPEndPoint
	{
		public bool success { get; private set; }
		public string additionalData { get; private set; }
		public byte[] message { get; private set; }
		public string partnerIP { get; private set; }
		public int partnerPort { get; private set; }
		
		public MessageSentEventArgs(string additionalData, byte[] message, IPEndPoint ep, bool success)
		{
			this.additionalData = additionalData;
			this.success = success;
			this.message = message;
			this.partnerIP = ep.Address.ToString();
			this.partnerPort = ep.Port;
		}
	}

	internal class DataReceivedEventArgs : EventArgs
	{
		private byte[] _bs;
		private string _dataset;
		private string[] _data;

		public byte[] bs
		{
			get { return _bs; }
		}

		public string[] data
		{
			get { return _data; }
		}

		public string dataset
		{
			get { return _dataset; }
		}

		internal cfAction action
		{
			get { return (cfAction)int.Parse(data[0]); }
		}


		public DataReceivedEventArgs(byte[] bs)
		{

			_bs = bs;

			_dataset = Encoding.UTF8.GetString(bs);
			_dataset = Regex.Replace(_dataset, @"\x00{2,}", "");
			_data = dataset.Split(';');
		}

	}

	#endregion

	#region cfServer Event

	public delegate void ServerStartedEventHandler(object sender, ServerStartedEventArgs e);
	public delegate void ServerStoppedEventHandler(object sender, ServerStoppedEventArgs e);
	public delegate void ClientConnectedEventHandler(object sender, ClientConnectedEventArgs e);
	public delegate void ClientDisconnectedEventHandler(object sender, ClientDisconnectedEventArgs e);
	public delegate void ClientOnlinedEventHandler(object sender, ClientOnlinedEventArgs e);

	public class ServerStartedEventArgs : EventArgs, iSuccess
	{
		public bool success { get; private set; }

		public ServerStartedEventArgs(bool success)
		{
			this.success = success;
		}
	}

	public class ServerStoppedEventArgs : EventArgs, iSuccess
	{
		public bool success { get; private set; }

		public ServerStoppedEventArgs(bool success)
		{
			this.success = success;
		}
	}

	public class ClientConnectedEventArgs : EventArgs, iClientIPEndPoint
	{
		public string clientIP { get; private set; }
		public int clientPort { get; private set; }

		public ClientConnectedEventArgs(IPEndPoint ipEP)
		{
			clientIP = ipEP.Address.ToString();
			clientPort = ipEP.Port;
		}
	}

	public class ClientDisconnectedEventArgs : EventArgs, iClientIPEndPoint
	{
		public string clientIP { get; private set; }
		public int clientPort { get; private set; }

		public ClientDisconnectedEventArgs(IPEndPoint ipEP)
		{
			clientIP = ipEP.Address.ToString();
			clientPort = ipEP.Port;
		}
	}

	public class ClientOnlinedEventArgs : EventArgs, iClientIPEndPoint
	{
		public string clientIP { get; private set; }
		public int clientPort { get; private set; }
		public bool keepAlive { get; private set; }

		public ClientOnlinedEventArgs(IPEndPoint ep, bool keepAlive)
		{
			this.clientIP = ep.Address.ToString();
			this.clientPort = ep.Port;
			this.keepAlive = keepAlive;
		}
	}

	#endregion

}
