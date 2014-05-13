using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public partial class CloudViewController : MonoMac.AppKit.NSViewController, IPreferencesTab
	{
		#region Constructors
		// Called when created from unmanaged code
		public CloudViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public CloudViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public CloudViewController () : base ("CloudView", NSBundle.MainBundle)
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion

		#region Properties
		//strongly typed view accessor
		public new CloudView View {
			get {
				return (CloudView)base.View;
			}
		}
		#endregion

		#region IPreferencesTab

		public string Name { get { return "Cloud"; } }

		public NSImage Icon { get { return NSImage.ImageNamed("cloud"); } }

		#endregion
	}
}

