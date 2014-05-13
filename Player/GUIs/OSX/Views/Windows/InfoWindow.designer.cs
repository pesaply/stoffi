// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace Stoffi.GUI.Views
{
	[Register ("InfoWindowController")]
	partial class InfoWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSImageView AlbumArt { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField AlbumLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField AlbumValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField Artist { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField ArtistLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField ArtistValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField BitrateLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField BitrateValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField ChannelsLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField ChannelsValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField CodecsLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField CodecsValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView General { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton GeneralButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField GeneralLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSBox GeneralLine { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField GenreLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField GenreValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField LastPlayedLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField LastPlayedValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField LengthLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField LengthValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView More { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton MoreButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField MoreLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField PathLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField PathValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField PlayCountLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField PlayCountValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField SamplingLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField SamplingValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField Title { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField TitleLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField TitleValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField TrackLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField TrackValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField URLLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField URLValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField ViewsLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField ViewsValue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField YearLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField YearValue { get; set; }

		[Action ("ChangeAlbum:")]
		partial void ChangeAlbum (MonoMac.Foundation.NSObject sender);

		[Action ("ChangeAlbumArt:")]
		partial void ChangeAlbumArt (MonoMac.Foundation.NSObject sender);

		[Action ("ChangeArtist:")]
		partial void ChangeArtist (MonoMac.Foundation.NSObject sender);

		[Action ("ChangeGenre:")]
		partial void ChangeGenre (MonoMac.Foundation.NSObject sender);

		[Action ("ChangeTitle:")]
		partial void ChangeTitle (MonoMac.Foundation.NSObject sender);

		[Action ("ChangeTrack:")]
		partial void ChangeTrack (MonoMac.Foundation.NSObject sender);

		[Action ("ChangeYear:")]
		partial void ChangeYear (MonoMac.Foundation.NSObject sender);

		[Action ("ToggleGeneral:")]
		partial void ToggleGeneral (MonoMac.Foundation.NSObject sender);

		[Action ("ToggleMore:")]
		partial void ToggleMore (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (AlbumArt != null) {
				AlbumArt.Dispose ();
				AlbumArt = null;
			}

			if (AlbumLabel != null) {
				AlbumLabel.Dispose ();
				AlbumLabel = null;
			}

			if (AlbumValue != null) {
				AlbumValue.Dispose ();
				AlbumValue = null;
			}

			if (Artist != null) {
				Artist.Dispose ();
				Artist = null;
			}

			if (ArtistLabel != null) {
				ArtistLabel.Dispose ();
				ArtistLabel = null;
			}

			if (ArtistValue != null) {
				ArtistValue.Dispose ();
				ArtistValue = null;
			}

			if (BitrateLabel != null) {
				BitrateLabel.Dispose ();
				BitrateLabel = null;
			}

			if (BitrateValue != null) {
				BitrateValue.Dispose ();
				BitrateValue = null;
			}

			if (CodecsLabel != null) {
				CodecsLabel.Dispose ();
				CodecsLabel = null;
			}

			if (CodecsValue != null) {
				CodecsValue.Dispose ();
				CodecsValue = null;
			}

			if (General != null) {
				General.Dispose ();
				General = null;
			}

			if (GeneralLine != null) {
				GeneralLine.Dispose ();
				GeneralLine = null;
			}

			if (GenreLabel != null) {
				GenreLabel.Dispose ();
				GenreLabel = null;
			}

			if (GenreValue != null) {
				GenreValue.Dispose ();
				GenreValue = null;
			}

			if (LastPlayedLabel != null) {
				LastPlayedLabel.Dispose ();
				LastPlayedLabel = null;
			}

			if (LastPlayedValue != null) {
				LastPlayedValue.Dispose ();
				LastPlayedValue = null;
			}

			if (LengthLabel != null) {
				LengthLabel.Dispose ();
				LengthLabel = null;
			}

			if (LengthValue != null) {
				LengthValue.Dispose ();
				LengthValue = null;
			}

			if (More != null) {
				More.Dispose ();
				More = null;
			}

			if (ChannelsLabel != null) {
				ChannelsLabel.Dispose ();
				ChannelsLabel = null;
			}

			if (ChannelsValue != null) {
				ChannelsValue.Dispose ();
				ChannelsValue = null;
			}

			if (PathLabel != null) {
				PathLabel.Dispose ();
				PathLabel = null;
			}

			if (PathValue != null) {
				PathValue.Dispose ();
				PathValue = null;
			}

			if (PlayCountLabel != null) {
				PlayCountLabel.Dispose ();
				PlayCountLabel = null;
			}

			if (PlayCountValue != null) {
				PlayCountValue.Dispose ();
				PlayCountValue = null;
			}

			if (SamplingLabel != null) {
				SamplingLabel.Dispose ();
				SamplingLabel = null;
			}

			if (SamplingValue != null) {
				SamplingValue.Dispose ();
				SamplingValue = null;
			}

			if (Title != null) {
				Title.Dispose ();
				Title = null;
			}

			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}

			if (TitleValue != null) {
				TitleValue.Dispose ();
				TitleValue = null;
			}

			if (TrackLabel != null) {
				TrackLabel.Dispose ();
				TrackLabel = null;
			}

			if (TrackValue != null) {
				TrackValue.Dispose ();
				TrackValue = null;
			}

			if (URLLabel != null) {
				URLLabel.Dispose ();
				URLLabel = null;
			}

			if (URLValue != null) {
				URLValue.Dispose ();
				URLValue = null;
			}

			if (ViewsLabel != null) {
				ViewsLabel.Dispose ();
				ViewsLabel = null;
			}

			if (ViewsValue != null) {
				ViewsValue.Dispose ();
				ViewsValue = null;
			}

			if (YearLabel != null) {
				YearLabel.Dispose ();
				YearLabel = null;
			}

			if (YearValue != null) {
				YearValue.Dispose ();
				YearValue = null;
			}

			if (MoreLabel != null) {
				MoreLabel.Dispose ();
				MoreLabel = null;
			}

			if (GeneralButton != null) {
				GeneralButton.Dispose ();
				GeneralButton = null;
			}

			if (GeneralLabel != null) {
				GeneralLabel.Dispose ();
				GeneralLabel = null;
			}

			if (MoreButton != null) {
				MoreButton.Dispose ();
				MoreButton = null;
			}
		}
	}

	[Register ("InfoWindow")]
	partial class InfoWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
