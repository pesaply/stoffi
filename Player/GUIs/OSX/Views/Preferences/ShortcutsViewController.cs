using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public partial class ShortcutsViewController : MonoMac.AppKit.NSViewController, IPreferencesTab
	{
		#region Constructors
		// Called when created from unmanaged code
		public ShortcutsViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public ShortcutsViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public ShortcutsViewController () : base ("ShortcutsView", NSBundle.MainBundle)
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
		public new ShortcutsView View {
			get {
				return (ShortcutsView)base.View;
			}
		}
		#endregion

		#region IPreferencesTab

		public string Name { get { return "Shortcuts"; } }

		public NSImage Icon { get { return NSImage.ImageNamed("shortcuts"); } }

		#endregion
	}
}

