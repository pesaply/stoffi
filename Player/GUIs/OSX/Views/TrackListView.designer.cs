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
	[Register ("TrackListViewController")]
	partial class TrackListViewController
	{
		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnAlbum { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnArtist { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnGenre { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnLastPlayed { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnLength { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnPath { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnPlayCount { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnTitle { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnTrack { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnURL { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnViews { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn ColumnYear { get; set; }

		[Outlet]
		MonoMac.AppKit.NSCollectionView Grid { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableHeaderView Header { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenu HeaderMenu { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScrollView IconScroller { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenu ItemMenu { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView List { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView Loading { get; set; }

		[Outlet]
		MonoMac.AppKit.NSProgressIndicator LoadingIndicator { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemAddTo { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemAddToSeparator { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemAlbum { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemArtist { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemBrowse { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemCopy { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemDelete { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemFinder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemGenre { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemLastPlayed { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemLength { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemMove { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemPath { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemPlay { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemPlayCount { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemQueue { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemRemove { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemRemoveFrom { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemSeparator1 { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemSeparator2 { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemSeparator3 { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemShare { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemTitle { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemTrack { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemURL { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemViews { get; set; }

		[Outlet]
		MonoMac.AppKit.NSMenuItem MenuItemYear { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScrollView Scroller { get; set; }

		[Action ("AddToNew:")]
		partial void AddToNew (MonoMac.Foundation.NSObject sender);

		[Action ("Browse:")]
		partial void Browse (MonoMac.Foundation.NSObject sender);

		[Action ("ClearSorting:")]
		partial void ClearSorting (MonoMac.Foundation.NSObject sender);

		[Action ("Copy:")]
		partial void Copy (MonoMac.Foundation.NSObject sender);

		[Action ("Delete:")]
		partial void Delete (MonoMac.Foundation.NSObject sender);

		[Action ("Move:")]
		partial void Move (MonoMac.Foundation.NSObject sender);

		[Action ("Play:")]
		partial void Play (MonoMac.Foundation.NSObject sender);

		[Action ("Queue:")]
		partial void Queue (MonoMac.Foundation.NSObject sender);

		[Action ("Remove:")]
		partial void Remove (MonoMac.Foundation.NSObject sender);

		[Action ("Share:")]
		partial void Share (MonoMac.Foundation.NSObject sender);

		[Action ("ToggleColumn:")]
		partial void ToggleColumn (MonoMac.Foundation.NSObject sender);

		[Action ("ViewInFinder:")]
		partial void ViewInFinder (MonoMac.Foundation.NSObject sender);

		[Action ("ViewInfo:")]
		partial void ViewInfo (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (Grid != null) {
				Grid.Dispose ();
				Grid = null;
			}

			if (IconScroller != null) {
				IconScroller.Dispose ();
				IconScroller = null;
			}

			if (ColumnAlbum != null) {
				ColumnAlbum.Dispose ();
				ColumnAlbum = null;
			}

			if (ColumnArtist != null) {
				ColumnArtist.Dispose ();
				ColumnArtist = null;
			}

			if (ColumnGenre != null) {
				ColumnGenre.Dispose ();
				ColumnGenre = null;
			}

			if (ColumnLastPlayed != null) {
				ColumnLastPlayed.Dispose ();
				ColumnLastPlayed = null;
			}

			if (ColumnLength != null) {
				ColumnLength.Dispose ();
				ColumnLength = null;
			}

			if (ColumnPath != null) {
				ColumnPath.Dispose ();
				ColumnPath = null;
			}

			if (ColumnPlayCount != null) {
				ColumnPlayCount.Dispose ();
				ColumnPlayCount = null;
			}

			if (ColumnTitle != null) {
				ColumnTitle.Dispose ();
				ColumnTitle = null;
			}

			if (ColumnTrack != null) {
				ColumnTrack.Dispose ();
				ColumnTrack = null;
			}

			if (ColumnURL != null) {
				ColumnURL.Dispose ();
				ColumnURL = null;
			}

			if (ColumnViews != null) {
				ColumnViews.Dispose ();
				ColumnViews = null;
			}

			if (ColumnYear != null) {
				ColumnYear.Dispose ();
				ColumnYear = null;
			}

			if (Header != null) {
				Header.Dispose ();
				Header = null;
			}

			if (HeaderMenu != null) {
				HeaderMenu.Dispose ();
				HeaderMenu = null;
			}

			if (ItemMenu != null) {
				ItemMenu.Dispose ();
				ItemMenu = null;
			}

			if (List != null) {
				List.Dispose ();
				List = null;
			}

			if (Loading != null) {
				Loading.Dispose ();
				Loading = null;
			}

			if (LoadingIndicator != null) {
				LoadingIndicator.Dispose ();
				LoadingIndicator = null;
			}

			if (MenuItemAddTo != null) {
				MenuItemAddTo.Dispose ();
				MenuItemAddTo = null;
			}

			if (MenuItemAddToSeparator != null) {
				MenuItemAddToSeparator.Dispose ();
				MenuItemAddToSeparator = null;
			}

			if (MenuItemAlbum != null) {
				MenuItemAlbum.Dispose ();
				MenuItemAlbum = null;
			}

			if (MenuItemArtist != null) {
				MenuItemArtist.Dispose ();
				MenuItemArtist = null;
			}

			if (MenuItemBrowse != null) {
				MenuItemBrowse.Dispose ();
				MenuItemBrowse = null;
			}

			if (MenuItemCopy != null) {
				MenuItemCopy.Dispose ();
				MenuItemCopy = null;
			}

			if (MenuItemDelete != null) {
				MenuItemDelete.Dispose ();
				MenuItemDelete = null;
			}

			if (MenuItemFinder != null) {
				MenuItemFinder.Dispose ();
				MenuItemFinder = null;
			}

			if (MenuItemGenre != null) {
				MenuItemGenre.Dispose ();
				MenuItemGenre = null;
			}

			if (MenuItemLastPlayed != null) {
				MenuItemLastPlayed.Dispose ();
				MenuItemLastPlayed = null;
			}

			if (MenuItemLength != null) {
				MenuItemLength.Dispose ();
				MenuItemLength = null;
			}

			if (MenuItemMove != null) {
				MenuItemMove.Dispose ();
				MenuItemMove = null;
			}

			if (MenuItemPath != null) {
				MenuItemPath.Dispose ();
				MenuItemPath = null;
			}

			if (MenuItemPlay != null) {
				MenuItemPlay.Dispose ();
				MenuItemPlay = null;
			}

			if (MenuItemPlayCount != null) {
				MenuItemPlayCount.Dispose ();
				MenuItemPlayCount = null;
			}

			if (MenuItemQueue != null) {
				MenuItemQueue.Dispose ();
				MenuItemQueue = null;
			}

			if (MenuItemRemove != null) {
				MenuItemRemove.Dispose ();
				MenuItemRemove = null;
			}

			if (MenuItemRemoveFrom != null) {
				MenuItemRemoveFrom.Dispose ();
				MenuItemRemoveFrom = null;
			}

			if (MenuItemSeparator1 != null) {
				MenuItemSeparator1.Dispose ();
				MenuItemSeparator1 = null;
			}

			if (MenuItemSeparator2 != null) {
				MenuItemSeparator2.Dispose ();
				MenuItemSeparator2 = null;
			}

			if (MenuItemSeparator3 != null) {
				MenuItemSeparator3.Dispose ();
				MenuItemSeparator3 = null;
			}

			if (MenuItemShare != null) {
				MenuItemShare.Dispose ();
				MenuItemShare = null;
			}

			if (MenuItemTitle != null) {
				MenuItemTitle.Dispose ();
				MenuItemTitle = null;
			}

			if (MenuItemTrack != null) {
				MenuItemTrack.Dispose ();
				MenuItemTrack = null;
			}

			if (MenuItemURL != null) {
				MenuItemURL.Dispose ();
				MenuItemURL = null;
			}

			if (MenuItemViews != null) {
				MenuItemViews.Dispose ();
				MenuItemViews = null;
			}

			if (MenuItemYear != null) {
				MenuItemYear.Dispose ();
				MenuItemYear = null;
			}

			if (Scroller != null) {
				Scroller.Dispose ();
				Scroller = null;
			}
		}
	}

	[Register ("TrackListView")]
	partial class TrackListView
	{
		[Outlet]
		MonoMac.AppKit.NSTableView List { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (List != null) {
				List.Dispose ();
				List = null;
			}
		}
	}
}
