// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;

namespace Stoffi.GUI.Views
{
	[Register ("StatusBarViewController")]
	partial class StatusBarViewController
	{
		[Outlet]
		MonoMac.AppKit.NSPopUpButton Filter { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton Fullscreen { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField Label { get; set; }

		[Outlet]
		MonoMac.AppKit.NSProgressIndicator Progress { get; set; }

		[Outlet]
		MonoMac.AppKit.NSPopUpButton Quality { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSegmentedControl ViewMode { get; set; }

		[Outlet]
		MonoMac.AppKit.NSPopUpButton Visualizer { get; set; }

		[Action ("FilterChange:")]
		partial void FilterChange (MonoMac.Foundation.NSObject sender);

		[Action ("FullscreenClick:")]
		partial void FullscreenClick (MonoMac.Foundation.NSObject sender);

		[Action ("QualityChange:")]
		partial void QualityChange (MonoMac.Foundation.NSObject sender);

		[Action ("ViewModeChange:")]
		partial void ViewModeChange (MonoMac.Foundation.NSObject sender);

		[Action ("VisualizerChange:")]
		partial void VisualizerChange (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (Filter != null) {
				Filter.Dispose ();
				Filter = null;
			}

			if (Fullscreen != null) {
				Fullscreen.Dispose ();
				Fullscreen = null;
			}

			if (Label != null) {
				Label.Dispose ();
				Label = null;
			}

			if (Progress != null) {
				Progress.Dispose ();
				Progress = null;
			}

			if (Quality != null) {
				Quality.Dispose ();
				Quality = null;
			}

			if (ViewMode != null) {
				ViewMode.Dispose ();
				ViewMode = null;
			}

			if (Visualizer != null) {
				Visualizer.Dispose ();
				Visualizer = null;
			}
		}
	}

	[Register ("StatusBarView")]
	partial class StatusBarView
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
