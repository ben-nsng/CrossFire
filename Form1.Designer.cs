namespace CrossFireLib
{
	partial class Form1
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.btnServerStop = new System.Windows.Forms.Button();
			this.btnServerStart = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.cbxPartners = new System.Windows.Forms.ComboBox();
			this.btnOnline = new System.Windows.Forms.Button();
			this.btnIpConfirm = new System.Windows.Forms.Button();
			this.tbxMsgSend = new System.Windows.Forms.TextBox();
			this.tbxMessage = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.tbxIP = new System.Windows.Forms.TextBox();
			this.btnSendFile = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.btnServerStop);
			this.groupBox1.Controls.Add(this.btnServerStart);
			this.groupBox1.Location = new System.Drawing.Point(409, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(121, 102);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Server";
			// 
			// btnServerStop
			// 
			this.btnServerStop.Enabled = false;
			this.btnServerStop.Location = new System.Drawing.Point(17, 51);
			this.btnServerStop.Name = "btnServerStop";
			this.btnServerStop.Size = new System.Drawing.Size(75, 23);
			this.btnServerStop.TabIndex = 1;
			this.btnServerStop.Text = "Stop";
			this.btnServerStop.UseVisualStyleBackColor = true;
			this.btnServerStop.Click += new System.EventHandler(this.btnServerStop_Click);
			// 
			// btnServerStart
			// 
			this.btnServerStart.Location = new System.Drawing.Point(17, 22);
			this.btnServerStart.Name = "btnServerStart";
			this.btnServerStart.Size = new System.Drawing.Size(75, 23);
			this.btnServerStart.TabIndex = 0;
			this.btnServerStart.Text = "Start";
			this.btnServerStart.UseVisualStyleBackColor = true;
			this.btnServerStart.Click += new System.EventHandler(this.btnServerStart_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.cbxPartners);
			this.groupBox2.Controls.Add(this.btnOnline);
			this.groupBox2.Controls.Add(this.btnIpConfirm);
			this.groupBox2.Controls.Add(this.tbxMsgSend);
			this.groupBox2.Controls.Add(this.tbxMessage);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.tbxIP);
			this.groupBox2.Location = new System.Drawing.Point(12, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(391, 239);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Client";
			// 
			// cbxPartners
			// 
			this.cbxPartners.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbxPartners.FormattingEnabled = true;
			this.cbxPartners.Location = new System.Drawing.Point(8, 213);
			this.cbxPartners.Name = "cbxPartners";
			this.cbxPartners.Size = new System.Drawing.Size(173, 20);
			this.cbxPartners.TabIndex = 8;
			// 
			// btnOnline
			// 
			this.btnOnline.Location = new System.Drawing.Point(310, 24);
			this.btnOnline.Name = "btnOnline";
			this.btnOnline.Size = new System.Drawing.Size(75, 23);
			this.btnOnline.TabIndex = 7;
			this.btnOnline.Text = "Online";
			this.btnOnline.UseVisualStyleBackColor = true;
			this.btnOnline.Click += new System.EventHandler(this.btnOnline_Click);
			// 
			// btnIpConfirm
			// 
			this.btnIpConfirm.Enabled = false;
			this.btnIpConfirm.Location = new System.Drawing.Point(200, 23);
			this.btnIpConfirm.Name = "btnIpConfirm";
			this.btnIpConfirm.Size = new System.Drawing.Size(75, 23);
			this.btnIpConfirm.TabIndex = 6;
			this.btnIpConfirm.Text = "Connect";
			this.btnIpConfirm.UseVisualStyleBackColor = true;
			this.btnIpConfirm.Click += new System.EventHandler(this.btnIpConfirm_Click);
			// 
			// tbxMsgSend
			// 
			this.tbxMsgSend.Location = new System.Drawing.Point(187, 211);
			this.tbxMsgSend.Name = "tbxMsgSend";
			this.tbxMsgSend.Size = new System.Drawing.Size(196, 22);
			this.tbxMsgSend.TabIndex = 4;
			this.tbxMsgSend.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbxMsgSend_KeyPress);
			// 
			// tbxMessage
			// 
			this.tbxMessage.Location = new System.Drawing.Point(6, 52);
			this.tbxMessage.Multiline = true;
			this.tbxMessage.Name = "tbxMessage";
			this.tbxMessage.ReadOnly = true;
			this.tbxMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.tbxMessage.Size = new System.Drawing.Size(379, 153);
			this.tbxMessage.TabIndex = 5;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 34);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(15, 12);
			this.label1.TabIndex = 3;
			this.label1.Text = "IP";
			// 
			// tbxIP
			// 
			this.tbxIP.Location = new System.Drawing.Point(27, 24);
			this.tbxIP.Name = "tbxIP";
			this.tbxIP.Size = new System.Drawing.Size(167, 22);
			this.tbxIP.TabIndex = 2;
			this.tbxIP.Text = "0.0.0.0";
			// 
			// btnSendFile
			// 
			this.btnSendFile.Location = new System.Drawing.Point(426, 120);
			this.btnSendFile.Name = "btnSendFile";
			this.btnSendFile.Size = new System.Drawing.Size(75, 23);
			this.btnSendFile.TabIndex = 2;
			this.btnSendFile.Text = "Send File";
			this.btnSendFile.UseVisualStyleBackColor = true;
			this.btnSendFile.Click += new System.EventHandler(this.btnSendFile_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(542, 263);
			this.Controls.Add(this.btnSendFile);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form1";
			this.ShowIcon = false;
			this.Text = "Form1";
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button btnServerStop;
		private System.Windows.Forms.Button btnServerStart;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Button btnIpConfirm;
		private System.Windows.Forms.TextBox tbxMsgSend;
		private System.Windows.Forms.TextBox tbxMessage;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbxIP;
		private System.Windows.Forms.Button btnOnline;
		private System.Windows.Forms.ComboBox cbxPartners;
		private System.Windows.Forms.Button btnSendFile;
	}
}

