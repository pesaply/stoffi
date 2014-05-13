// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;

namespace Stoffi.GUI
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSView ContentContainer { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSplitView HorizontalSplit { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView Navigation { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton PlayPauseButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton Repeat { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSearchField Search { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton Shuffle { get; set; }

		[Outlet]
		Stoffi.GUI.Views.StatusBarView StatusBar { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField TrackInfoMinus { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField TrackInfoPlus { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider TrackInfoSeek { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField TrackInfoText { get; set; }

		[Outlet]
		Stoffi.GUI.Views.TrackListView TrackList { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSplitView VerticalSplit { get; set; }

		[Outlet]
		Stoffi.GUI.Views.VideoView Video { get; set; }

		[Outlet]
		Stoffi.GUI.Views.VisualizerView Visualizer { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Volume { get; set; }

		[Action ("NextClick:")]
		partial void NextClick (MonoMac.Foundation.NSObject sender);

		[Action ("PlayPauseClick:")]
		partial void PlayPauseClick (MonoMac.Foundation.NSObject sender);

		[Action ("PreviousClick:")]
		partial void PreviousClick (MonoMac.Foundation.NSObject sender);

		[Action ("RepeatClick:")]
		partial void RepeatClick (MonoMac.Foundation.NSObject sender);

		[Action ("SearchEdit:")]
		partial void SearchEdit (MonoMac.Foundation.NSObject sender);

		[Action ("SeekChange:")]
		partial void SeekChange (MonoMac.Foundation.NSObject sender);

		[Action ("ShuffleClick:")]
		partial void ShuffleClick (MonoMac.Foundation.NSObject sender);

		[Action ("VolumeChange:")]
		partial void VolumeChange (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (StatusBar != null) {
				StatusBar.Dispose ();
				StatusBar = null;
			}

			if (ContentContainer != null) {
				ContentContainer.Dispose ();
				ContentContainer = null;
			}

			if (HorizontalSplit != null) {
				HorizontalSplit.Dispose ();
				HorizontalSplit = null;
			}

			if (Navigation != null) {
				Navigation.Dispose ();
				Navigation = null;
			}

			if (PlayPauseButton != null) {
				PlayPauseButton.Dispose ();
				PlayPauseButton = null;
			}

			if (Repeat != null) {
				Repeat.Dispose ();
				Repeat = null;
			}

			if (Search != null) {
				Search.Dispose ();
				Search = null;
			}

			if (Shuffle != null) {
				Shuffle.Dispose ();
				Shuffle = null;
			}

			if (TrackInfoMinus != null) {
				TrackInfoMinus.Dispose ();
				TrackInfoMinus = null;
			}

			if (TrackInfoPlus != null) {
				TrackInfoPlus.Dispose ();
				TrackInfoPlus = null;
			}

			if (TrackInfoSeek != null) {
				TrackInfoSeek.Dispose ();
				TrackInfoSeek = null;
			}

			if (TrackInfoText != null) {
				TrackInfoText.Dispose ();
				TrackInfoText = null;
			}

			if (TrackList != null) {
				TrackList.Dispose ();
				TrackList = null;
			}

			if (VerticalSplit != null) {
				VerticalSplit.Dispose ();
				VerticalSplit = null;
			}

			if (Video != null) {
				Video.Dispose ();
				Video = null;
			}

			if (Visualizer != null) {
				Visualizer.Dispose ();
				Visualizer = null;
			}

			if (Volume != null) {
				Volume.Dispose ();
				Volume = null;
			}
		}
	}

	[Register ("MainWindow")]
	partial class MainWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
