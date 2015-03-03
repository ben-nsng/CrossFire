using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace CrossFireLib
{
	internal class cfDebugFactory
	{
		internal static bool debug = cfConst.DEBUG;
		private static bool init = false;
		internal static Action<string> _log;
		internal static void log(params string[] ss)
		{
			if (!debug)
				return;

			_log(DateTime.Now.ToString("--- dd/MM/yyyy HH:mm:ss:fff ---"));
			foreach (string s in ss)
				_log(s);
		}

		internal static void initializeDebug()
		{
			if (init)
				return;
			init = !init;
			if (cfDebugFactory.debug)
			{
				Form a = new Form();
				a.Size = new Size(new Point(400, 300));
				a.MaximizeBox = false;
				a.MinimizeBox = false;
				a.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
				a.Show();
				a.Text = "cfDebugFactory";

				TextBox b = new TextBox();
				b.Size = new Size(new Point(390, 260));
				b.ReadOnly = true;
				b.Multiline = true;
				b.ScrollBars = ScrollBars.Vertical;

				a.Controls.Add(b);

				Action<string> log = (s) =>
				{
					lock (b)
					{
						b.Text = s + "\r\n" + b.Text;

						if (b.Text.Length > 10000)
							b.Text = "";
					}
				};

				cfDebugFactory._log = (s) =>
				{
					if (b.InvokeRequired)
						b.Invoke(log, s);
					else
						log(s);
				};

			}
		}
	}
}
