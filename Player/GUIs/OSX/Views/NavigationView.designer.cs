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
	[Register ("NavigationViewController")]
	partial class NavigationViewController
	{
		[Outlet]
		MonoMac.AppKit.NSClipView ClipView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenu PlaylistMenu { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem ShareMenuItem { get; set; }

		[Outlet]
		Stoffi.GUI.Models.NavigationTree Tree { get; set; }

		[Action ("RemovePlaylist:")]
		partial void RemovePlaylist (MonoMac.Foundation.NSObject sender);

		[Action ("RenamePlaylist:")]
		partial void RenamePlaylist (MonoMac.Foundation.NSObject sender);

		[Action ("SharePlaylist:")]
		partial void SharePlaylist (MonoMac.Foundation.NSObject sender);

		[Action ("Tree_Click:")]
		partial void Tree_Click (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (PlaylistMenu != null) {
				PlaylistMenu.Dispose ();
				PlaylistMenu = null;
			}

			if (ShareMenuItem != null) {
				ShareMenuItem.Dispose ();
				ShareMenuItem = null;
			}

			if (Tree != null) {
				Tree.Dispose ();
				Tree = null;
			}

			if (ClipView != null) {
				ClipView.Dispose ();
				ClipView = null;
			}
		}
	}

	[Register ("NavigationView")]
	partial class NavigationView
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
