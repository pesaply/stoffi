using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public partial class AppsViewController : MonoMac.AppKit.NSViewController, IPreferencesTab
	{
		#region Constructors
		// Called when created from unmanaged code
		public AppsViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public AppsViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public AppsViewController () : base ("AppsView", NSBundle.MainBundle)
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
		public new AppsView View {
			get {
				return (AppsView)base.View;
			}
		}
		#endregion

		#region IPreferencesTab

		public string Name { get { return "Apps"; } }

		public NSImage Icon { get { return NSImage.ImageNamed("apps"); } }

		#endregion
	}
}

