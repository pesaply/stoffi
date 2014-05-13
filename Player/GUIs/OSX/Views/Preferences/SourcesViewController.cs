using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public partial class SourcesViewController : MonoMac.AppKit.NSViewController, IPreferencesTab
	{
		#region Constructors
		// Called when created from unmanaged code
		public SourcesViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public SourcesViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public SourcesViewController () : base ("SourcesView", NSBundle.MainBundle)
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
		public new SourcesView View {
			get {
				return (SourcesView)base.View;
			}
		}
		#endregion

		#region IPreferencesTab

		public string Name { get { return "Sources"; } }

		public NSImage Icon { get { return NSImage.ImageNamed("sources"); } }

		#endregion
	}
}

