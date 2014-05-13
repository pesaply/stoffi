// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;

namespace Stoffi.GUI.Views
{
	[Register ("VideoViewController")]
	partial class VideoViewController
	{
		[Outlet]
		MonoMac.WebKit.WebView Browser { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField NoVideoBackground { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField NoVideoMessage { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (Browser != null) {
				Browser.Dispose ();
				Browser = null;
			}

			if (NoVideoBackground != null) {
				NoVideoBackground.Dispose ();
				NoVideoBackground = null;
			}

			if (NoVideoMessage != null) {
				NoVideoMessage.Dispose ();
				NoVideoMessage = null;
			}
		}
	}

	[Register ("VideoView")]
	partial class VideoView
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
