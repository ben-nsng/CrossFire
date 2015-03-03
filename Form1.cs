using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.PeerToPeer;
using System.Net.PeerToPeer.Collaboration;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections;

namespace CrossFireLib
{
	public partial class Form1 : Form
	{
		cfServer server;
		cfClient client;

		/*
		public byte[] fuck(int len)
		{
			Random r = new Random();
			byte[] x = new byte[len];
			int k;
			for (int i = 0; i < len; i++)
			{
				k = 0;
				while(k < 65 || (90 < k && k < 97) || k > 122)
					k = r.Next(0, 256);
				
				x[i] = (byte)k;
			}
			return x;
		}
		*/

		public Form1()
		{
			/*
			for (int i = 10; i < 16; i++)
			{
				FileStream fs = new FileStream("unc\\" + i.ToString("D2"), FileMode.OpenOrCreate, FileAccess.Write);
				for (int j = 0; j < Math.Pow(2, i - 10); j++)
				{
					fs.Seek(1024 * 1024 * j, SeekOrigin.Begin);
					fs.Write(fuck((int)1024 * 1024), 0, (int)1024 * 1024);
				}
				fs.Close();
			}

			return;
			*/

			InitializeComponent();
			this.Text = "CrossFireLib " + "(" + Process.GetCurrentProcess().Id + ")";

			if (!Directory.Exists("unc"))
				Directory.CreateDirectory("unc");

			HolePunchingServer hps = new HolePunchingServer(
				new ServerConfiguration()
				{
					ListenPort = 6893
				});
			server = hps.Server;

			HolePunchingClient hpc = new HolePunchingClient(
				new ClientConfiguration()
				{
					ServerEP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 6893),
					LocalUDPPort = 11000
				});
			client = hpc.Client;

			server.ServerStarting += (o, e) =>
			{
				log("server starting");
				btnServerStart.Enabled = false;
				btnOnline.Enabled = false;
			};
			server.ServerStarted += (o, e) =>
			{
				log("server started");
				if (e.success)
					btnServerStop.Enabled = true;
				else
				{
					btnOnline.Enabled = true;
					btnServerStart.Enabled = true;
				}
			};
			server.ServerStopping += (o, e) =>
			{
				log("server stopping");
				btnServerStop.Enabled = false;
			};
			server.ServerStopped += (o, e) =>
			{
				log("server stopped");
				if (e.success)
				{
					btnServerStart.Enabled = true;
					btnOnline.Enabled = true;
				}
				else
					btnServerStop.Enabled = true;
			};
			server.ClientConnected += (o, e) =>
			{
				log(e.clientIP + ":" + e.clientPort + " connected.");
				cbxPartners.Items.Add(e.clientIP + ":" + e.clientPort);
			};
			server.ClientOnlined += (o, e) =>
			{
				log("keepAlive = " + e.keepAlive + "");
			};
			server.ClientDisconnected += (o, e) =>
			{
				log("client disconnected. " + e.clientIP + ":" + e.clientPort);
				cbxPartners.Items.Remove(e.clientIP + ":" + e.clientPort);
			};

			client.ServerConnecting += (o, e) =>
			{
				log("connecting server.");
				btnOnline.Enabled = false;
				btnServerStart.Enabled = false;
			};
			client.ServerConnected += (o, e) =>
			{
				if (e.connected)
				{
					log("connected server.");
					btnIpConfirm.Enabled = true;
				}
				else
				{
					log("failed to connect server.");
					btnOnline.Enabled = true;
					btnServerStart.Enabled = true;
				}
			};

			client.ServerDisconnected += (o, e) =>
			{
				log("disconnected from server.");
				btnIpConfirm.Enabled = false;
				btnOnline.Enabled = true;
				btnServerStart.Enabled = true;
			};
			client.PartnerConnecting += (o, e) =>
			{
				log("connecting to partner.");
				btnIpConfirm.Enabled = false;
			};
			client.PartnerConnected += (o, e) =>
			{
				if (e.connected)
				{
					log("connected to partner");
					cbxPartners.Items.Add(e.partnerIP + ":" + e.partnerPort);
				}
				else
					log("failed to connect partner");
				log("IP = " + e.partnerIP + ":" + e.partnerPort);
				btnIpConfirm.Enabled = true;
			};
			client.PartnerDisconnected += (o, e) =>
			{
				log("disconnected from partner");
				cbxPartners.Items.Remove(e.partnerIP + ":" + e.partnerPort);
				btnIpConfirm.Enabled = true;
			};

			client.ClientDataReceived += (o, e) =>
			{
				save("ClientDataReceived", e.dataset);
				//MessageBox.Show("OK");
			};

			client.ServerDataReceived += (o, e) =>
			{
				save("ServerDataReceived", e.dataset);
				//MessageBox.Show("OK");
			};

			client.MessageReceived += (o, e) =>
			{
				//log(Encoding.UTF8.GetString(e.message));
				string[] data = e.additionalData.Split(';');

				if (data[0] != "")
					using (FileStream fs = new FileStream("unc\\" + data[0], FileMode.OpenOrCreate, FileAccess.Write))
					{
						fs.Seek(long.Parse(data[1]), SeekOrigin.Begin);
						fs.Write(e.message, 0, e.message.Length);
					}
				else
				{
					log(e.partnerIP + ":" + e.partnerPort + " : " + Encoding.UTF8.GetString(e.message));
				}
			};


			client.MessageSending += (o, e) =>
			{
				log("Messages start to send");
			};

			client.PartnerMessageReceived += (o, e) =>
			{
				log("Partner received message");
				double s = e.sendingDuration / 1000;
				log("used time: " + s + "s");
				if (s == 0)
					log("Speed: instant");
				else
				{
					log("Speed: " + (e.messageSize / s).ToString(".00") + " bytes/s");
					log("Speed: " + (e.messageSize / s / 1024).ToString(".00") + " Kbytes/s");
				}
			};
		}

		private void save(string packet, string s)
		{
			using (StreamWriter sw = new StreamWriter("unc\\" + packet + ".txt", true, Encoding.UTF8))
			{
				sw.Write("[" + s + "] \r\n");
			}
		}

		private void btnServerStart_Click(object sender, EventArgs ex)
		{
			server.Start();
		}

		private void btnServerStop_Click(object sender, EventArgs e)
		{
			server.Stop();
		}

		private void btnOnline_Click(object sender, EventArgs ex)
		{
			btnOnline.Enabled = false;
			client.Connect();
		}

		private void btnIpConfirm_Click(object sender, EventArgs e)
		{
			client.Connect(tbxIP.Text, ProtocolType.Tcp);
		}

		private void log(string t)
		{
			tbxMessage.Text = t + "\r\n" + tbxMessage.Text;
		}

		private void tbxMsgSend_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				if (cbxPartners.SelectedItem != null)
				{
					client.Write(((string)cbxPartners.SelectedItem).Split(':')[0], tbxMsgSend.Text);
					tbxMsgSend.Text = "";
				}

				e.Handled = true;
			}
		}

		private void btnSendFile_Click(object sender, EventArgs e)
		{
			if (cbxPartners.SelectedItem != null)
			{
				OpenFileDialog dia = new OpenFileDialog();
				if (dia.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					if (cbxPartners.SelectedItem != null)
					{
						string filename = dia.FileName.Substring(dia.FileName.LastIndexOf('\\') + 1);
						FileStream fs = new FileStream(dia.FileName, FileMode.Open, FileAccess.Read);
						for (int position = 0; position < fs.Length; position += cfConst.FILE_MAX_BLOCK_SIZES)
						{
							byte[] f;
							fs.Seek(0, SeekOrigin.Current);
							if (position + cfConst.FILE_MAX_BLOCK_SIZES >= fs.Length)
								f = new byte[(int)(fs.Length - position)];
							else
								f = new byte[cfConst.FILE_MAX_BLOCK_SIZES];
							fs.Read(f, 0, f.Length);
							client.Write(((string)cbxPartners.SelectedItem).Split(':')[0], filename + ";" + position + ";", f);
						}
					}
				}
			}
		}

	}


}
