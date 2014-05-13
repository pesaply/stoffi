using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

using Stoffi.Core;
using Stoffi.Core.Settings;
using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Sources;

using SettingsManager = Stoffi.Core.Settings.Manager;
using PlaylistManager = Stoffi.Core.Playlists.Manager;
using MediaManager = Stoffi.Core.Media.Manager;
using ServiceManager = Stoffi.Core.Services.Manager;

using Stoffi.GUI.Models;

namespace Stoffi.GUI.Views
{
	public partial class TrackListViewController : MonoMac.AppKit.NSViewController
	{
		#region Members

		private Dictionary<string,NSMenuItem> menuItems = new Dictionary<string, NSMenuItem> ();
		private Dictionary<string,NSTableColumn> columns = new Dictionary<string, NSTableColumn> ();
		private List<string> enabledFields = new List<string> ();
		private Timer reloadTracksTimer = null;
		private Timer scrollChangedDelay = null;
		private ListConfig config;
		private bool loadedSelectionYet = false;
		private ViewMode mode = ViewMode.Details;
		private ListConfig[] internetSources = new ListConfig[] {
			SettingsManager.YouTubeListConfig,
			SettingsManager.JamendoListConfig,
			SettingsManager.SoundCloudListConfig
		};

		#endregion

		#region Constructors
		// Called when created from unmanaged code
		public TrackListViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TrackListViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public TrackListViewController () : base ("TrackListView", NSBundle.MainBundle)
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
		public new TrackListView View {
			get {
				return (TrackListView)base.View;
			}
		}

		/// <summary>
		/// Sets a value indicating whether the track list is being populated.
		/// </summary>
		/// <value><c>true</c> if loading track list; otherwise, <c>false</c>.</value>
		public bool IsLoading
		{
			set
			{
				U.GUIContext.Post (_ => {
					if (value) {
						Loading.AlphaValue = 1f;
						Scroller.AlphaValue = 0.1f;
					}
					else {
						Loading.AlphaValue = 0f;
						Scroller.AlphaValue = 1f;
					}
				}, null);
			}
		}

		/// <summary>
		/// Gets or sets the view mode.
		/// </summary>
		/// <value>The mode.</value>
		public ViewMode Mode
		{
			get { return mode; }
			set
			{
				mode = value;
				IconScroller.Hidden = mode != ViewMode.Icons;
				Scroller.Hidden = mode == ViewMode.Icons;
			}
		}

		/// <summary>
		/// Gets or sets the list configuration.
		/// </summary>
		/// <value>The config.</value>
		public ListConfig Config
		{
			get { return config; }
			set
			{
				if (Comparer<ListConfig>.Default.Compare(config, value) != 0)
				{
					if (config != null)
						config.PropertyChanged -= Config_PropertyChanged;
					config = value;
					Refresh ();
					Mode = config.Mode;
					if (config != null)
						config.PropertyChanged += Config_PropertyChanged;
				}
			}
		}

		/// <summary>
		/// Gets or sets the tracks.
		/// </summary>
		/// <value>The tracks.</value>
		public ObservableCollection<Track> Tracks
		{
			get
			{
				var ds = List.DataSource as TrackListDataSource;
				if (ds != null)
					return ds.Tracks;
				return new ObservableCollection<Track>();
			}
			set
			{
				var ds = List.DataSource as TrackListDataSource;
				if (ds != null)
					ds.Tracks = value;
				ReloadGrid ();
			}
		}

		/// <summary>
		/// Sets the navigation pane.
		/// </summary>
		/// <value>The navigation pane.</value>
		public NavigationViewController NavigationPane { get; set; }

		/// <summary>
		/// Gets whether the source of the track list is based on a service on the Internet or not.
		/// </summary>
		public bool IsInternetSource
		{
			get { return internetSources.Contains (config); }
		}

		#endregion

		#region Methods

		#region Public

		public void StartLoadingAnimation(NSObject sender)
		{
			if (LoadingIndicator != null)
				LoadingIndicator.StartAnimation (sender);
		}

		#endregion

		#region Private

		/// <summary>
		/// Play the specified track.
		/// </summary>
		/// <param name="track">Track.</param>
		private void Play(Track track)
		{
			if (track != null) {
				SettingsManager.CurrentActiveNavigation = SettingsManager.CurrentSelectedNavigation;
				MediaManager.Stop ();
				MediaManager.Play (track);
			}
		}

		/// <summary>
		/// Get the track which was clicked.
		/// </summary>
		/// <returns>The clicked track.</returns>
		private Track GetClickedTrack()
		{
			try
			{
				if (List.ClickedRow >= 0)
				{
					var ds = List.DataSource as TrackListDataSource;
					return ds.FilteredAndSortedTracks[List.ClickedRow];
				}
			}
			catch (Exception e) {
				U.L (LogLevel.Warning, "Main", "Could not get clicked track: " + e.Message);
			}
			return null;
		}

		/// <summary>
		/// Refreshs the track list header menu.
		/// </summary>
		private void RefreshColumns()
		{
			if (config == null)
				return;
			enabledFields.Clear ();
			foreach (var c in config.Columns)
				enabledFields.Add (c.Name);
			if (config.HasNumber)
				enabledFields.Insert (config.NumberIndex, config.NumberColumn.Name);

			foreach (var c in columns)
			{
				var name = c.Key;
				var guiColumn = c.Value;
				try
				{
					var item = menuItems[guiColumn.Identifier];
					var enabled = enabledFields.Contains<string>(name);
					guiColumn.Hidden = !enabled;
					item.Hidden = !enabled;
					
					var configColumn = config.GetColumn(name);
					if (enabled && configColumn != null)
					{
						item.State = configColumn.IsVisible ? NSCellStateValue.On : NSCellStateValue.Off;
						guiColumn.Hidden = !configColumn.IsVisible;
						guiColumn.Width = (float)configColumn.Width;
						List.MoveColumn(List.FindColumn((NSString)name), config.Columns.IndexOf(configColumn)+1);
					}
				}
				catch (InvalidOperationException e) {
					U.L (LogLevel.Warning, "Main", "Could not update column " + name + ": " + e.Message); 
				}
			}
		}

		/// <summary>
		/// Refreshs the playlist submenus in the context menu.
		/// </summary>
		private void RefreshMenuPlaylists()
		{
			// clear playlists
			while (MenuItemAddTo.Submenu.ItemArray().Count () > 2)
				MenuItemAddTo.Submenu.RemoveItemAt (0);
			MenuItemRemoveFrom.Submenu.RemoveAllItems ();

			if (SettingsManager.Playlists.Count == 0) {
				MenuItemAddToSeparator.Hidden = true;
				MenuItemRemoveFrom.Enabled = false;
			} else {
				MenuItemAddToSeparator.Hidden = false;
				MenuItemRemoveFrom.Enabled = true;

				foreach (var p in SettingsManager.Playlists) {
					var addItem = new NSMenuItem (p.Name);
					var delItem = new NSMenuItem (p.Name);
					addItem.Activated += ListMenu_AddToClick;
					delItem.Activated += ListMenu_RemoveFromClick;
					MenuItemAddTo.Submenu.InsertItem (addItem, MenuItemAddTo.Submenu.ItemArray ().Count () - 2);
					MenuItemRemoveFrom.Submenu.AddItem (delItem);
				}
			}
			RefreshMenuEnabledPlaylists ();
		}

		/// <summary>
		/// Refreshes the enabled playlists in the context menu depending on the selection.
		/// </summary>
		private void RefreshMenuEnabledPlaylists()
		{
			var tracks = View.SelectedTracks;
			bool disableAll = true;
			foreach (var item in MenuItemRemoveFrom.Submenu.ItemArray()) {
				var p = PlaylistManager.Get (item.Title);
				var b = p != null && PlaylistManager.ContainsAny (p, tracks);
				item.Hidden = !b;
				if (b)
					disableAll = false;
			}
			MenuItemRemoveFrom.Enabled = !disableAll;
			for (int i=0; i < MenuItemAddTo.Submenu.ItemArray().Count () - 2; i++) {
				var item = MenuItemAddTo.Submenu.ItemAt (i);
				var p = PlaylistManager.Get (item.Title);
				var b = p != null && !PlaylistManager.ContainsAll (p, tracks);
				item.Hidden = !b;
			}
		}

		/// <summary>
		/// Refresh the context menu according to the type of selected tracks.
		/// </summary>
		private void RefreshMenu()
		{
			RefreshMenuEnabledPlaylists ();

			var tracks = View.SelectedTracks;
			var b = SettingsManager.GetCollectionState (tracks);
			
			MenuItemBrowse.Hidden = !(b["onlyYouTube"] || b["onlySoundCloud"] || b["onlyJamendo"] || b["onlyRadio"]);

			// only files
			MenuItemCopy.Hidden = !b["onlyFiles"];
			MenuItemMove.Hidden = !b["onlyFiles"];
			MenuItemDelete.Hidden = !b["onlyFiles"];
			MenuItemFinder.Hidden = !b["onlyFiles"];

			// only youtube
			if (b["onlyYouTube"])
				MenuItemBrowse.Title = "Watch on YouTube";

			// only radio
			if (b["onlyRadio"])
				MenuItemBrowse.Title = "Visit website";

			// only soundcloud
			if (b["onlySoundCloud"])
				MenuItemBrowse.Title = "Listen on SoundCloud";

			// only jamendo
			if (b["onlyJamendo"])
				MenuItemBrowse.Title = "Listen on Jamendo";

			// files, radio or playlist
			MenuItemRemove.Hidden = !(b["anyFiles"] || b["anyRadio"] || SettingsManager.CurrentSelectedNavigationIsPlaylist);

			// sharable
			MenuItemShare.Hidden = !(b["onlySharable"] && ServiceManager.Linked);

			MenuItemPlay.Title = b ["isPlaying"] ? "Pause" : "Play";
			MenuItemQueue.Title = b ["isQueued"] ? "Dequeue" : "Queue";

			MenuItemSeparator1.Hidden = MenuItemRemove.Hidden && MenuItemDelete.Hidden;
			MenuItemSeparator2.Hidden = MenuItemCopy.Hidden && MenuItemMove.Hidden;
			MenuItemSeparator3.Hidden = MenuItemBrowse.Hidden;
		}

		/// <summary>
		/// Refreshs the selection according to the configuration.
		/// </summary>
		private void RefreshSelection()
		{
			if (config != null && config.SelectedIndices.Count > 0)
			{
				for (int i=0; i < config.SelectedIndices.Count; i++)
					if (config.SelectedIndices[i] < 0 || config.SelectedIndices[i] >= List.RowCount)
						config.SelectedIndices.RemoveAt(i--);
				List.SelectRows (NSIndexSet.FromArray(config.SelectedIndices.ToArray()), false);
				loadedSelectionYet = true;
			}
		}

		/// <summary>
		/// Refreshs the sorting according to the configuration.
		/// </summary>
		private void RefreshSorting()
		{
			var sorts = new List<NSSortDescriptor> ();
			if (config != null) {
				foreach (var sort in config.Sorts) {
					var split = sort.Split (new char[] { ':' }, 2);
					if (split.Count() == 2)
						sorts.Add (new NSSortDescriptor (split [1], split [0] == "asc"));
				}
			}
			sorts.Reverse ();
			List.SortDescriptors = sorts.ToArray ();

			var ds = List.DataSource as TrackListDataSource;
			if (ds != null)
				ds.SortDescriptors = List.SortDescriptors;
		}

		/// <summary>
		/// Refreshs the track list.
		/// </summary>
		private void Refresh(bool doSortAndFilter = false)
		{
			if (config == null)
				return;

			U.GUIContext.Post (_ => {

				// load scroll position
				float scrollX = 0, scrollY = 0;
				try
				{
					scrollX = (float)config.HorizontalScrollOffset;
					scrollY = (float)config.VerticalScrollOffset;
					if (String.IsNullOrWhiteSpace(config.Filter))
						scrollY = (float)config.VerticalScrollOffsetWithoutSearch;
				}
				catch (Exception e)
				{
					U.L (LogLevel.Warning, "Main", "Could not load scroll position: " + e.Message);
				}

				var ds = List.DataSource as TrackListDataSource;
				IsLoading = config.IsLoading;
				RefreshColumns();
				if (ds != null)
					ds.Filter = config.Filter;
				List.ReloadData ();
				RefreshMenu ();

				// set scroll position
				var scrollPoint = new PointF(scrollX, scrollY);
				List.EnclosingScrollView.ContentView.ScrollToPoint(scrollPoint);
				List.EnclosingScrollView.ReflectScrolledClipView(List.EnclosingScrollView.ContentView);

				// load sorting
				RefreshSorting();

				// load selection
				RefreshSelection();
			},null);
		}

		/// <summary>
		/// Reloads the tracks.
		/// </summary>
		/// <param name="resetScroll">Whether or not the scroll position should be reset according to config.</param>
		private void ReloadTracks(bool resetScroll = false)
		{
			U.GUIContext.Post (_ => {
				List.ReloadData ();
				ReloadGrid();
				RefreshMenu ();
			}, null);
			if (resetScroll && config != null) {
				var scrollX = (float)config.HorizontalScrollOffset;
				var scrollY = config.VerticalScrollOffset;
				scrollY = 0;
				if (String.IsNullOrWhiteSpace(config.Filter))
					scrollY = config.VerticalScrollOffsetWithoutSearch;

				U.GUIContext.Post (_ => {
					var scrollPoint = new PointF((float)scrollX, (float)scrollY);
					List.EnclosingScrollView.ContentView.ScrollToPoint(scrollPoint);
					List.EnclosingScrollView.ReflectScrolledClipView(List.EnclosingScrollView.ContentView);
				}, null);
			}
		}

		/// <summary>
		/// Reload the content of the grid.
		/// TODO: Need to make this A LOT faster!
		/// </summary>
		private void ReloadGrid()
		{
			return;
			var t = "Playlist";
			if (config == SettingsManager.FileListConfig)
				t = "Files";
			else if (config == SettingsManager.YouTubeListConfig)
				t = "YouTube";
			else if (config == SettingsManager.SoundCloudListConfig)
				t = "SoundCloud";
			else if (config == SettingsManager.JamendoListConfig)
				t = "Jamendo";
			else if (config == SettingsManager.RadioListConfig)
				t = "Radio";
			else if (config == SettingsManager.QueueListConfig)
				t = "Queue";
			else if (config == SettingsManager.HistoryListConfig)
				t = "History";
			else if (config == SettingsManager.DiscListConfig)
				t = "Disc";
			
			U.L (LogLevel.Debug, "Track list", "\n" + t);
			U.L (LogLevel.Debug, "Track list", "Get filtered and sorted tracks");
			var tracks = ((TrackListDataSource)List.DataSource).FilteredAndSortedTracks;
			U.L (LogLevel.Debug, "Track list", "Create array");
			var arr = (from i in tracks select new TrackItem (i)).ToArray<NSObject> ();
			U.L (LogLevel.Debug, "Track list", "Set content");
			Grid.Content = arr;
			U.L (LogLevel.Debug, "Track list", "Done");
		}

		/// <summary>
		/// Get an item in the header context menu given the name of the column/track property.
		/// </summary>
		/// <returns>The menu item.</returns>
		/// <param name="columnName">Column name.</param>
		private NSMenuItem FindMenuItem(string columnName)
		{
			if (menuItems.ContainsKey (columnName))
				return menuItems [columnName];
			return null;
		}

		/// <summary>
		/// Gets the name of a column/track property that corresponds to a given item in the header menu.
		/// </summary>
		/// <returns>The column name.</returns>
		/// <param name="item">Item.</param>
		private string GetColumnName(NSMenuItem item)
		{
			foreach (var i in menuItems)
				if (i.Value == item)
					return i.Key;
			return null;
		}

		/// <summary>
		/// Copy or move a set of tracks.
		/// </summary>
		/// <param name="tracks">Tracks.</param>
		/// <param name="keepOriginal">Whether or not to keep the original files.</param>
		private void Copy(List<Track> tracks, bool keepOriginal)
		{
			if (tracks == null || tracks.Count == 0)
				return;

			var openPanel = new NSOpenPanel();
			openPanel.ReleasedWhenClosed = true;
			openPanel.Title = "Select Destination";
			openPanel.CanChooseFiles = false;
			openPanel.CanCreateDirectories = true;
			openPanel.CanChooseDirectories = true;
			openPanel.AllowsMultipleSelection = false;
			var result = openPanel.RunModal();
			if (result == 1)
			{
				var dst = openPanel.Url.Path;
				try{
					if (keepOriginal)
						Files.Copy (tracks, dst);
					else
						Files.Move (tracks, dst);
				}
				catch (Exception e) {
					var verb = keepOriginal ? "Copy" : "Move";
					var alert = new NSAlert ();
					alert.InformativeText = e.Message;
					alert.MessageText = "Could Not " + verb + " Tracks";
					alert.AlertStyle = NSAlertStyle.Critical;
					alert.RunSheetModal (this.View.Window);
				}
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the property changes of the configuration.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			U.GUIContext.Post (_ => {
				switch (e.PropertyName) {
				case "Columns":
					RefreshColumns();
					break;
					
				case "Sorts":
					RefreshSorting();
					break;

				case "Filter":
					if (IsInternetSource)
						return;
					var ds = List.DataSource as TrackListDataSource;
					if (ds != null)
						ds.Filter = config.Filter;
					ReloadTracks (true);
					break;

				case "SelectedIndices":
					RefreshSelection ();
					break;

				case "IsLoading":
					IsLoading = config.IsLoading;
					break;

				case "Mode":
					Mode = config.Mode;
					break;
				}
			}, null);
		}

		/// <summary>
		/// Invoked when the property changes of the settings.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			U.GUIContext.Post (_ => {
				switch (e.PropertyName) {

				case "CurrentTrack":
				case "MediaState":
					RefreshMenu ();
					break;
				}
			}, null);
		}

		/// <summary>
		/// Invoked when the file collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Tracks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
//			if (reloadTracksTimer != null)
//				reloadTracksTimer.Dispose ();
//			reloadTracksTimer = new Timer (DelayedReloadTracks, null, 300, Timeout.Infinite);
		}
		
		/// <summary>
		/// Invoked when the user double clicks the track list.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void List_DoubleClick(object sender, EventArgs e)
		{
			Play(GetClickedTrack ());
		}
		
		/// <summary>
		/// Invoked when the user changes the selected track in the list.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void List_SelectionDidChange(object sender, EventArgs e)
		{
			if (loadedSelectionYet && config != null && List.SelectedRowCount > 0) {

				// remove
				for (int i=0; i < config.SelectedIndices.Count; i++)
				{
					var index = config.SelectedIndices [i];
					if (index >= List.RowCount || !List.SelectedRows.Contains (index))
						config.SelectedIndices.RemoveAt (i--);
				}

				// add
				foreach (var row in List.SelectedRows) {
					if (!config.SelectedIndices.Contains (row)) {
						config.SelectedIndices.Add (row);
					}
				}
			}
			RefreshMenu ();
		}
		
		/// <summary>
		/// Invoked when the user clicks on a column header.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void List_DidClickTableColumnEvent(object sender, NSTableViewTableEventArgs e)
		{
			config.PropertyChanged -= Config_PropertyChanged;
			if (config != null && List.SortDescriptors != null) {
				config.Sorts.Clear ();
				for (int i=0; i < List.SortDescriptors.Count(); i++) {
					var sd = List.SortDescriptors [i];
					config.Sorts.Add ((sd.Ascending ? "asc:" : "desc:") + sd.Key);
				}
				config.Sorts.Reverse ();
			}
			var ds = List.DataSource as TrackListDataSource;
			ds.SortDescriptors = List.SortDescriptors;
			ReloadTracks ();
			config.PropertyChanged += Config_PropertyChanged;
		}
		
		/// <summary>
		/// Invoked when the user moves a column by dragging its header.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void List_DidDragTableColumn(object sender, NSTableViewTableEventArgs e)
		{
			var toPos = List.FindColumn ((NSString)e.TableColumn.Identifier) - 1;
			if (config != null) {
				int fromPos = toPos;
				for (int i=0; i < config.Columns.Count; i++) {
					if (config.Columns [i].Name == e.TableColumn.Identifier) {
						fromPos = i;
						break;
					}
				}

				if (fromPos != toPos && toPos >= 0 && toPos < config.Columns.Count) {
					config.Columns.Move (fromPos, toPos);
				}
			}
		}
		
		/// <summary>
		/// Invoked when the user resizes a column by dragging the handle in the header.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void List_ColumnDidResize(object sender, EventArgs e)
		{
			config.PropertyChanged -= Config_PropertyChanged;
			var colIndex = Header.ResizedColumn-1;
			if (config != null && colIndex >= 0 && colIndex < columns.Count) {
				var c = columns.Values.ElementAt(colIndex) as NSTableColumn;
				var name = columns.Keys.ElementAt(colIndex) as string;
				var col = config.GetColumn (name);
				if (col != null)
					col.Width = c.Width;
			}
			config.PropertyChanged += Config_PropertyChanged;
		}
		
		/// <summary>
		/// Invoked when the user double clicks on a playlist under Add in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void ListMenu_AddToClick(object sender, EventArgs e)
		{
			var item = sender as NSMenuItem;
			PlaylistManager.AddToPlaylist (View.SelectedTracks, item.Title);
		}
		
		/// <summary>
		/// Invoked when the user double clicks on a playlist under Remove in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void ListMenu_RemoveFromClick(object sender, EventArgs e)
		{
			var item = sender as NSMenuItem;
			var playlist = PlaylistManager.Get (item.Title);
			if (playlist != null)
				playlist.Remove (View.SelectedTracks);
		}
		
		/// <summary>
		/// Invoked when the user double clicks on Create new under Add in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		partial void AddToNew(NSObject sender)
		{
			if (NavigationPane == null)
			{
				var alert = new NSAlert();
				alert.MessageText = "Cannot Create Playlist";
				alert.InformativeText = "The connection between the navigation pane and the track list is missing.";
				alert.AlertStyle = NSAlertStyle.Critical;
				alert.RunModal();
				return;
			}

			NavigationPane.AddToPlaylistQueue.Clear();
			foreach (var track in View.SelectedTracks)
				NavigationPane.AddToPlaylistQueue.Add(track);
			NavigationPane.EditNewPlaylist();
		}

		/// <summary>
		/// Invoked when the user toggles the visibility of a column.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ToggleColumn(NSObject sender)
		{
			var item = sender as NSMenuItem;
			var name = GetColumnName(item);
			if (!String.IsNullOrWhiteSpace(name))
			{
				var c = columns[name];
				c.Hidden = !c.Hidden;
				if (item != null)
					item.State = c.Hidden ? NSCellStateValue.Off : NSCellStateValue.On;

				if (config != null)
				{
					var col = config.GetColumn (name);
					if (col != null)
						col.IsVisible = !c.Hidden;
				}
			}
		}

		/// <summary>
		/// Invoked when the user clears the sorting.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ClearSorting(NSObject sender)
		{
			var ds = List.DataSource as TrackListDataSource;
			List.SortDescriptors = new NSSortDescriptor[0];
			ds.SortDescriptors = List.SortDescriptors;
			if (config != null)
				config.Sorts.Clear ();
			ReloadTracks();
		}
		
		/// <summary>
		/// Invoked when a track is modified.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void TrackListDataSource_TracksRefreshed(object sender, EventArgs e)
		{
			if (reloadTracksTimer != null)
				reloadTracksTimer.Dispose ();
			reloadTracksTimer = new Timer (DelayedReloadTracks, null, 300, Timeout.Infinite);
		}

		/// <summary>
		/// Invoked by the timer for a slight delay to reload the track list.
		/// </summary>
		/// <param name="state">State.</param>
		private void DelayedReloadTracks(object state)
		{
			U.GUIContext.Post (_ => {
				List.ReloadData();
				ReloadGrid();
			}, null);
		}

		/// <summary>
		/// Invoked when the scroll position changes.
		/// </summary>
		/// <param name="o">The object</param>
		[Export("boundsDidChangeNotification")]
		public void BoundsDidChangeNotification(NSObject o)
		{
			if (scrollChangedDelay != null)
				scrollChangedDelay.Dispose ();
			scrollChangedDelay = new Timer (DelayedScrollUpdate, o as object, 5000, Timeout.Infinite);
		}

		/// <summary>
		/// Invoked when the user clicks on Listen on... / Visit website in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void Browse(NSObject sender)
		{
			var urls = new SortedSet<string>();
			var tracks = View.SelectedTracks;
			foreach (var track in tracks)
			{
				if (!urls.Contains (track.URL) && !String.IsNullOrWhiteSpace(track.URL))
				{
					urls.Add (track.URL);
				}
			}

			foreach (var url in urls)
				Process.Start ("\""+url+"\"");
		}
		
		/// <summary>
		/// Invoked when the user clicks on Copy in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void Copy(NSObject sender)
		{
			Copy (View.SelectedTracks, true);
		}
		
		/// <summary>
		/// Invoked when the user clicks on Move in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void Move(NSObject sender)
		{
			Copy (View.SelectedTracks, false);
		}
		
		/// <summary>
		/// Invoked when the user clicks on Delete in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void Delete(NSObject sender)
		{
			var tracks = View.SelectedTracks;
			if (tracks == null || tracks.Count == 0)
				return;

			Console.WriteLine("delete()");

			var confirm = new NSAlert();
			confirm.AlertStyle = NSAlertStyle.Warning;
			confirm.MessageText = "Delete files from hard drive.";
			confirm.AddButton("Delete");
			confirm.AddButton("Cancel");
			confirm.InformativeText = "Are you sure you want to delete the selected files permanently frmm the hard drive?";
			var result = confirm.RunSheetModal(this.View.Window);
			if (result == 1000)
			{
				try{
					Files.Delete (tracks);
				}
				catch (Exception e) {
					var alert = new NSAlert ();
					alert.InformativeText = e.Message;
					alert.MessageText = "Could Not Delete Files";
					alert.AlertStyle = NSAlertStyle.Critical;
					alert.RunSheetModal (this.View.Window);
				}
			}
		}
		
		/// <summary>
		/// Invoked when the user clicks on Play/Pause in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void Play(NSObject sender)
		{
			if (SettingsManager.MediaState == MediaState.Playing)
				MediaManager.Pause ();
			else
				Play (View.SelectedTrack);
		}
		
		/// <summary>
		/// Invoked when the user clicks on Queue/Dequeue in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void Queue(NSObject sender)
		{
			var tracks = (List.DataSource as TrackListDataSource).FilteredAndSortedTracks;
			var b = SettingsManager.GetCollectionState (View.SelectedTracks);
			if (config != null)
			{
				var t = new List<Track>();
				foreach (int i in config.SelectedIndices)
					t.Add (tracks[i]);

				if (b["isQueued"])
					MediaManager.Dequeue(t);
				else
					MediaManager.Queue(t);
			}
		}
		
		/// <summary>
		/// Invoked when the user clicks on Remove in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void Remove(NSObject sender)
		{
			int row = List.SelectedRow;
			SettingsManager.RemoveTracks(View.SelectedTracks);
			if (row >= List.RowCount)
				List.SelectRow (List.RowCount - 1, false);
			else
				List.SelectRow (row, false);
		}
		
		/// <summary>
		/// Invoked when the user clicks on View in Finder in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ViewInFinder(NSObject sender)
		{
			var paths = Files.GetFolders(View.SelectedTracks);
			foreach (var p in paths)
				Process.Start ("open", "\""+p+"\"");
		}
		
		/// <summary>
		/// Invoked when the user clicks on Get info in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ViewInfo(NSObject sender)
		{
			foreach (var track in View.SelectedTracks)
			{
				var win = new InfoWindowController(track);
				win.ShowWindow(this.View.Window);
			}
		}

		/// <summary>
		/// Invoked when the user clicks on Share in the list's context menu.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void Share(NSObject sender)
		{
		}

		/// <summary>
		/// Invoked when a playlist is modified.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Playlist_Modified(object sender, ModifiedEventArgs e)
		{
			RefreshMenuPlaylists ();
		}
		
		/// <summary>
		/// Invoked when a playlist is renamed.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Playlist_Renamed(object sender, System.IO.RenamedEventArgs e)
		{
			RefreshMenuPlaylists ();
		}

		/// <summary>
		/// Invoked by a delay timer, updates the scroll position in the config.
		/// </summary>
		/// <param name="state">The notification cast as an object.</param>
		private void DelayedScrollUpdate(object state)
		{
			U.GUIContext.Post (_ => {
				config.PropertyChanged -= Config_PropertyChanged;
				try
				{
					var notification = state as NSNotification;
					var view = notification.Object as NSView;
					var position = view.Bounds.Location;

					if (config != null) {
						config.HorizontalScrollOffset = position.X;
						config.VerticalScrollOffset = position.Y;
						if (String.IsNullOrWhiteSpace(config.Filter))
							config.VerticalScrollOffsetWithoutSearch = position.Y;
					}
				}
				catch (Exception e) {
					U.L (LogLevel.Warning, "Main", "Could not save scroll position: " + e.Message);
				}
				config.PropertyChanged += Config_PropertyChanged;
			}, null);
		}

		#endregion

		#region Overrides

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			
//			SettingsManager.PropertyChanged += Settings_PropertyChanged;

//			var cvi = new MyCollectionViewItem ();
//			Grid.ItemPrototype = cvi;

			var tds = new TrackListDataSource();
			tds.TracksRefreshed += TrackListDataSource_TracksRefreshed;
			List.DataSource = tds;
			List.DoubleClick += List_DoubleClick;
			Header.Menu = HeaderMenu;
			List.EnclosingScrollView.ContentView.PostsBoundsChangedNotifications = true;
			NSNotificationCenter.DefaultCenter.AddObserver (this, new Selector ("boundsDidChangeNotification"),
			                                                NSView.BoundsChangedNotification, List.EnclosingScrollView.ContentView);
			
			List.Menu = ItemMenu;

			columns.Add ("Artist", ColumnArtist);
			columns.Add ("Album", ColumnAlbum);
			columns.Add ("Title", ColumnTitle);
			columns.Add ("Length", ColumnLength);
			columns.Add ("Year", ColumnYear);
			columns.Add ("Genre", ColumnGenre);
			columns.Add ("Track", ColumnTrack);
			columns.Add ("LastPlayed", ColumnLastPlayed);
			columns.Add ("PlayCount", ColumnPlayCount);
			columns.Add ("URL", ColumnURL);
			columns.Add ("Path", ColumnPath);
			columns.Add ("Views", ColumnViews);

			menuItems.Add ("Artist", MenuItemArtist);
			menuItems.Add ("Album", MenuItemAlbum);
			menuItems.Add ("Title", MenuItemTitle);
			menuItems.Add ("Length", MenuItemLength);
			menuItems.Add ("Year", MenuItemYear);
			menuItems.Add ("Genre", MenuItemGenre);
			menuItems.Add ("Track", MenuItemTrack);
			menuItems.Add ("LastPlayed", MenuItemLastPlayed);
			menuItems.Add ("PlayCount", MenuItemPlayCount);
			menuItems.Add ("URL", MenuItemURL);
			menuItems.Add ("Path", MenuItemPath);
			menuItems.Add ("Views", MenuItemViews);

			Refresh (true);
			
			var listDelegate = new TrackListDelegate();
			listDelegate.SelectionDidChangeEvent += List_SelectionDidChange;
			listDelegate.ColumnDidResizeEvent += List_ColumnDidResize;
			listDelegate.DidClickTableColumnEvent += List_DidClickTableColumnEvent;
			listDelegate.DidDragTableColumnEvent += List_DidDragTableColumn;
			List.Delegate = listDelegate;

			PlaylistManager.PlaylistModified += Playlist_Modified;
			PlaylistManager.PlaylistRenamed += Playlist_Renamed;
			RefreshMenuPlaylists();
			RefreshMenu ();
		}

		#endregion

		#endregion
	}
}

