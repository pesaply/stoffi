// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;

namespace Stoffi.GUI.Views
{
	[Register ("EqualizerWindowController")]
	partial class EqualizerWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSButton DeleteButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton EditButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField Name { get; set; }

		[Outlet]
		MonoMac.AppKit.NSPopUpButton Profiles { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Slider125 { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Slider16K { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Slider1K { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Slider250 { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Slider2K { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Slider4K { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Slider500 { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider Slider8K { get; set; }

		[Outlet]
		MonoMac.AppKit.NSSlider SliderEcho { get; set; }

		[Action ("NameChange:")]
		partial void NameChange (MonoMac.Foundation.NSObject sender);

		[Action ("ProfileAdd:")]
		partial void ProfileAdd (MonoMac.Foundation.NSObject sender);

		[Action ("ProfileChange:")]
		partial void ProfileChange (MonoMac.Foundation.NSObject sender);

		[Action ("ProfileDel:")]
		partial void ProfileDel (MonoMac.Foundation.NSObject sender);

		[Action ("ProfileEdit:")]
		partial void ProfileEdit (MonoMac.Foundation.NSObject sender);

		[Action ("SliderChange:")]
		partial void SliderChange (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (DeleteButton != null) {
				DeleteButton.Dispose ();
				DeleteButton = null;
			}

			if (EditButton != null) {
				EditButton.Dispose ();
				EditButton = null;
			}

			if (Name != null) {
				Name.Dispose ();
				Name = null;
			}

			if (Profiles != null) {
				Profiles.Dispose ();
				Profiles = null;
			}

			if (Slider125 != null) {
				Slider125.Dispose ();
				Slider125 = null;
			}

			if (Slider16K != null) {
				Slider16K.Dispose ();
				Slider16K = null;
			}

			if (Slider1K != null) {
				Slider1K.Dispose ();
				Slider1K = null;
			}

			if (Slider250 != null) {
				Slider250.Dispose ();
				Slider250 = null;
			}

			if (Slider2K != null) {
				Slider2K.Dispose ();
				Slider2K = null;
			}

			if (Slider4K != null) {
				Slider4K.Dispose ();
				Slider4K = null;
			}

			if (Slider500 != null) {
				Slider500.Dispose ();
				Slider500 = null;
			}

			if (Slider8K != null) {
				Slider8K.Dispose ();
				Slider8K = null;
			}

			if (SliderEcho != null) {
				SliderEcho.Dispose ();
				SliderEcho = null;
			}
		}
	}

	[Register ("EqualizerWindow")]
	partial class EqualizerWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
