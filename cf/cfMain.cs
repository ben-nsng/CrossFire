using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;

namespace CrossFireLib
{

	internal static class cfConst
	{
		internal const int PORT_TCP_DEFAULT = 6893;

		internal static int PORT_TCP_SERVER = PORT_TCP_DEFAULT;

		internal const int PORT_UDP_CLIENT_DEFAULT = 11000;

		internal static int PORT_UDP_CLIENT = PORT_UDP_CLIENT_DEFAULT;

		//262144
		//524288
		//2097152

		internal const int FILE_MAX_BLOCK_SIZES = 1472;

		internal const int SOCKET_SEND_BUFFER_SIZES = 2097152;

		internal const int SOCKET_RECEIVE_BUFFER_SIZES = 2097152;

		internal const int MAX_TRANS_UNITS = 1500;

		internal const bool DEBUG = true;

		internal const int PING_INTERVAL = 8000;

		internal const string PING_REQUEST = "REQUEST";

		internal const string PING_RESPONSE = "RESPONSE";

		internal const int WAIT_CONNECT_INTERVAL = 5000;

		/// <summary>
		/// READ_STREAM_INTERVAL must be less than PING_INTERVAL
		/// </summary>
		internal const int READ_STREAM_INTERVAL = 100;

		internal const int PACKET_SIZE = 256;

		internal const int MAX_SOCKET_LISTEN_BACKLOG = 10;

		internal const string UDP_STRING = "UDP";

		internal const string TCP_STRING = "TCP";
	}

	internal struct cfPacket
	{
		private cfAction action;
		private string[] args;

		internal cfPacket(cfAction action, params string[] args)
		{
			this.action = action;
			this.args = args;
		}

		public static implicit operator cfPacket(string s)
		{
			string[] data = s.Split(';');
			if (data.Length == 0)
				return null;

			string[] args = new string[data.Length - 1];
			cfAction action;
			try
			{
				action = (cfAction)int.Parse(data[0]);
			}
			catch { action = cfAction.nil; }
			for (int i = 1; i < data.Length; i++)
				args[i - 1] = data[i];
			return new cfPacket(action, args);
		}

		public byte[] ToByte()
		{
			string ss = ((int)action).ToString() + ";";
			foreach (string s in args)
				ss += s + ";";

			byte[] bs = new byte[cfConst.PACKET_SIZE];
			byte[] data = Encoding.UTF8.GetBytes(ss);
			data.CopyTo(bs, 0);
			return bs;
		}

		public override string ToString()
		{
			string ss = (int)action + ";";
			foreach (string s in args)
				ss += s + ";";
			return ss;
		}
	}

	
	internal static class cfExt
	{
		/*
        internal static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bf.Serialize(ms, obj);
			return ms.ToArray();
		}
		*/

		internal static void CrossInvoke(this MulticastDelegate mdelgt, object sender, EventArgs e)
		{
			if (mdelgt != null)
				foreach (Delegate delgt in mdelgt.GetInvocationList())
					delgt.CrossInvoke(sender, e);
		}

		private static void CrossInvoke(this Delegate delgt, object sender, EventArgs e)
		{
			try
			{
				if (delgt != null)
				{
					if (delgt.Target is Control && ((Control)delgt.Target).InvokeRequired)
						((Control)delgt.Target).Invoke(delgt, new object[] { sender, e });
					else
						delgt.Method.Invoke(delgt.Target, new object[] { sender, e });
				}
			}
			catch (TargetInvocationException ex)
			{
				cfDebugFactory.log(ex.InnerException.Message, ex.InnerException.StackTrace);
			}
		}

		internal static Thread BackgroundThreadStart(ThreadStart ts)
		{
			Thread t = new Thread(ts);
			t.IsBackground = true;
			t.Start();
			return t;
		}

		internal static Thread BackgroundThreadStart(ParameterizedThreadStart pts, object state)
		{
			Thread t = new Thread(pts);
			t.IsBackground = true;
			t.Start(state);
			return t;
		}

		internal static bool equal(string data, cfAction action)
		{
			return data == ((int)action).ToString();
		}

		internal static string base64Encode(string str)
		{
			byte[] encbuff = System.Text.Encoding.UTF8.GetBytes(str);
			return Convert.ToBase64String(encbuff);
		}

		internal static string base64Decode(string str)
		{
			byte[] decbuff = Convert.FromBase64String(str);
			return System.Text.Encoding.UTF8.GetString(decbuff);
		}
	}

	internal enum cfAction
	{
		nil,
		//*****Server -> Client

		//tell client to create # of connection to server
		//data[]
		createConnection = 0x3000,

		//tell client exchange info and to build up connection to another client
		//data[remote IP address, remote port]
		buildConnectionToPartner,

		removeUserfromIpAddress,

		//*****Client -> Server

		//tell server client going to online and pass private ipendpoint to server
		//data[private IP address, private port]
		online = 0x4000,

		//tell server this connection intented to create "real" connection to client
		//data[]
		foreverConnection,

		//tell server client need to build up # of connection to server
		//data[partner's ip, protocol, ping interval]
		setupTcpConnection,

		//data[]
		setupUdpConnection,

		//data[]
		disconnectedFromServer,

		//*****Client -> Client
		//for future use
		sendMessage = 0x5000,

		//send message to partner
		//data[message]
		receiveMessage,

		//ping client
		//data[request] or data[response]
		ping,

		//partner failed connect
		//data[partner ip, partner port or -1]
		partnerFailedConnect,

		doneReceiveMessage,

		//*****Server -> Server
		//Pending Action

		//intended to create connection for client and client's partner
		//data[client IP address, client Port, client's partner IP address, client's partner Port, ping interval]
		pendingTcpConnection = 0x6000

	}
}
