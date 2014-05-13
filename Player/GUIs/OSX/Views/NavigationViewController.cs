using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Settings;

using SettingsManager = Stoffi.Core.Settings.Manager;
using PlaylistManager = Stoffi.Core.Playlists.Manager;

using Stoffi.GUI.Models;

namespace Stoffi.GUI.Views
{
	public partial class NavigationViewController : MonoMac.AppKit.NSViewController
	{
		#region Members
		public List<Track> AddToPlaylistQueue = new List<Track> ();
		#endregion

		#region Constructors
		// Called when created from unmanaged code
		public NavigationViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public NavigationViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public NavigationViewController () : base ("NavigationView", NSBundle.MainBundle)
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
		public new NavigationView View {
			get {
				return (NavigationView)base.View;
			}
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Selects the Create New item and enters edit mode.
		/// </summary>
		public void EditNewPlaylist()
		{
			var row = -1;
			var ds = Tree.DataSource as NavigationDataSource;
			for (int r=0; r < ds.Navigations.Count; r++)
			{
				var n = ds.Navigations [r];
				if (n.IsPlaylist && n.EditOnSelect)
				{
					row = r;
					break;
				}
			}
			if (row >= 0)
			{
				Tree.SelectRow (row, false);
				Tree.EditColumn(0, row, new NSEvent(), true);
			}
		}

		/// <summary>
		/// Gets or sets the filter of tracks to add to a dynamic playlist after it has been created.
		/// </summary>
		/// <value>The add to playlist filter.</value>
		public string AddToPlaylistFilter { get; set; }

		#endregion

		#region Private

		/// <summary>
		/// Refreshs the navigation tree selection.
		/// </summary>
		private void RefreshSelection()
		{
			var ds = Tree.DataSource as NavigationDataSource;
			for (int i=0; i < ds.Navigations.Count; i++) {
				if (Tree.SelectedRow != i && SettingsManager.CurrentSelectedNavigation == ds.Navigations[i].ID) {
					Tree.SelectRow (i, false);
					break;
				}
			}
		}

		/// <summary>
		/// Inserts a playlist at a given position.
		/// </summary>
		/// <param name="playlist">Playlist.</param>
		/// <param name="index">Index.</param>
		private void InsertPlaylist(Playlist playlist, int index)
		{
			var ds = Tree.DataSource as NavigationDataSource;

			// find the row in the tree corresponding to 'index'
			var row = ds.GetRowOfPlaylist(index);

			// make sure 'row' is legal
			if (row < 0 || row >= ds.Navigations.Count)
				return;

			// create item and insert at index 'row'
			ds.Navigations.Insert (row, new Navigation (playlist.Name, "playlist", playlist.NavigationID, true) { IsPlaylist =  true });

			Tree.ReloadData ();
		}

		/// <summary>
		/// Add a playlist item at the end of the list.
		/// </summary>
		/// <param name="playlist">Playlist.</param>
		private void AddPlaylist(Playlist playlist)
		{
			InsertPlaylist (playlist, SettingsManager.Playlists.Count);
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the navigation tree is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		partial void Tree_Click(NSObject sender)
		{
			var ds = Tree.DataSource as NavigationDataSource;
			if (Tree.SelectedRow >= 0 && Tree.SelectedRow < ds.Navigations.Count)
			{
				var navigation = ds.Navigations[Tree.SelectedRow];
				if (navigation.EditOnSelect)
				{
					Tree.EditColumn(0, Tree.SelectedRow, new NSEvent(), true);
				}
				else if (!navigation.IsHeader)
				{
					SettingsManager.CurrentSelectedNavigation = navigation.ID;
				}
			}
		}

		/// <summary>
		/// Invoked when an item in the tree was edited.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Tree_ItemEdited(object sender, RenamedEventArgs e)
		{
			try
			{
				var textField = sender as NSTextField;
				var oldName = e.OldName;
				var newName = e.Name;
				var ds = Tree.DataSource as NavigationDataSource;
				var item = ds.Navigations [Tree.SelectedRow];
				if (item.EditOnSelect)
				{
					textField.StringValue = oldName;
					if (String.IsNullOrWhiteSpace(AddToPlaylistFilter))
					{
						var p = PlaylistManager.Create(newName, true);
						if (AddToPlaylistQueue.Count > 0)
							PlaylistManager.AddToPlaylist(AddToPlaylistQueue, p.Name);
						AddToPlaylistQueue.Clear();
					}
					else
					{
						PlaylistManager.CreateDynamic(newName, AddToPlaylistFilter);
						AddToPlaylistFilter = null;
					}
				}
				else
				{
					PlaylistManager.Rename(oldName, newName);
				}
			}
			catch (Exception exc) {
				U.L (LogLevel.Error, "Navigation", "Could not handle edit of item: " + exc.Message);
			}
		}

		/// <summary>
		/// Invoked when the property changes of the settings.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			U.GUIContext.Post (_ => 
			                   {
				switch (e.PropertyName) {
				case "CurrentSelectedNavigation":
					RefreshSelection();
					break;
				}
			}, null);
		}

		/// <summary>
		/// Invoked when a playlist is created, changed or deleted.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Playlists_PlaylistModified(object sender, ModifiedEventArgs e)
		{
			U.GUIContext.Post (_ => {
				var playlist = sender as Playlist;
				var ds = Tree.DataSource as NavigationDataSource;
				switch (e.Type)
				{
				case ModifyType.Created:
					var index = PlaylistManager.IndexOf(playlist);
					InsertPlaylist(playlist, index);
					if (e.Interactive)
					{
						var row = ds.GetRowOfPlaylist(index);
						Tree.SelectRow(row, false);
						SettingsManager.CurrentSelectedNavigation = playlist.NavigationID;
					}
					break;
					
				case ModifyType.Removed:
					var item = ds.GetPlaylistItem(playlist);
					ds.Navigations.Remove(item);
					Tree.ReloadData();
					break;
				}
			}, null);
		}

		/// <summary>
		/// Invoked when a playlist is renamed.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Playlists_PlaylistRenamed(object sender, RenamedEventArgs e)
		{
			U.GUIContext.Post (_ => {
				var ds = Tree.DataSource as NavigationDataSource;
				var item = ds.GetPlaylistItem(e.OldName);
				item.Name = e.Name;
				Tree.ReloadData();
			}, null);
		}

		/// <summary>
		/// Invoked when the user right-clicks a playlist and chooses Remove.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void RemovePlaylist (NSObject sender)
		{
			var ds = Tree.DataSource as NavigationDataSource;
			var item = ds.Navigations[Tree.SelectedRow];
			if (item == null || !item.IsPlaylist || item.EditOnSelect)
				return;
			PlaylistManager.Remove(item.Name);
		}
		
		/// <summary>
		/// Invoked when the user right-clicks a playlist and chooses Rename.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void RenamePlaylist (NSObject sender)
		{
			Tree.EditColumn(0, Tree.SelectedRow, new NSEvent(), true);
		}
		
		/// <summary>
		/// Invoked when the user right-clicks a playlist and chooses Share.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void SharePlaylist (NSObject sender)
		{
			var alert = new NSAlert();
			alert.MessageText = "TODO";
			alert.InformativeText = "Not yet implemented.";
			alert.RunModal();
		}

		#endregion

		#region Overrides

		public override void AwakeFromNib ()
		{
			SettingsManager.PropertyChanged += Settings_PropertyChanged;
			PlaylistManager.PlaylistModified += Playlists_PlaylistModified;
			PlaylistManager.PlaylistRenamed += Playlists_PlaylistRenamed;

			// replace source list with our custom tree
//			var tree = new NavigationTree ();
//			ClipView.ReplaceSubviewWith (Tree, tree);
//			var size = ClipView.Frame.Size;
//			tree.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
//			View.AutoresizesSubviews = true;
//			tree.SetFrameSize (size);
//			Tree = tree;

			// create tree
			var s = new NavigationDataSource ();
			s.Navigations.Add (new Navigation ("Now playing"));
			s.Navigations.Add (new Navigation ("Video", "video"));
			s.Navigations.Add (new Navigation ("Visualizer", "visualizer"));

			s.Navigations.Add (new Navigation ("Music"));
			s.Navigations.Add (new Navigation ("Files", "files"));
			s.Navigations.Add (new Navigation ("YouTube", "youtube"));
			s.Navigations.Add (new Navigation ("SoundCloud", "soundcloud"));
			s.Navigations.Add (new Navigation ("Radio", "radio"));
			s.Navigations.Add (new Navigation ("Jamendo", "jamendo"));

			s.Navigations.Add (new Navigation ("Timeline"));
			s.Navigations.Add (new Navigation ("Queue", "queue"));
			s.Navigations.Add (new Navigation ("History", "history"));

			s.Navigations.Add (new Navigation ("Playlists"));

			foreach (var playlist in SettingsManager.Playlists)
				s.Navigations.Add (new Navigation (playlist.Name, "playlist", playlist.NavigationID, true) { IsPlaylist = true });

			s.Navigations.Add (new Navigation ("Create new", "NSAddTemplate", true, true) { IsPlaylist = true });

			// set all delegates and stuff
			var treeDelegate = new NavigationDelegate(PlaylistMenu);
			treeDelegate.ItemEdited += Tree_ItemEdited;
			Tree.PlaylistMenu = PlaylistMenu;
			Tree.Delegate = treeDelegate;
			Tree.DataSource = s;

			RefreshSelection ();
			
			base.AwakeFromNib ();
		}

		#endregion

		#endregion
	}
}

