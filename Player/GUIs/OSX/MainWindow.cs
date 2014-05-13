using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MonoMac.Foundation;
using MonoMac.AppKit;

using Stoffi.Core;

namespace Stoffi.GUI
{
	public partial class MainWindow : MonoMac.AppKit.NSWindow
	{
		#region Constructors
		// Called when created from unmanaged code
		public MainWindow (IntPtr handle) : base (handle)
		{
			Initialize ();
			Title = "Stoffi Music Player";
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindow (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
			this.WillClose += Window_WillClose;
		}
		#endregion

		public void Window_WillClose(object sender, EventArgs e)
		{

			U.IsClosing = true;
			Stoffi.Core.Media.Manager.Clean ();
		}
	}
}

