using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public partial class VisualizerViewController : MonoMac.AppKit.NSViewController
	{
		#region Constructors
		// Called when created from unmanaged code
		public VisualizerViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public VisualizerViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public VisualizerViewController () : base ("VisualizerView", NSBundle.MainBundle)
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion
		//strongly typed view accessor
		public new VisualizerView View {
			get {
				return (VisualizerView)base.View;
			}
		}
	}
}

