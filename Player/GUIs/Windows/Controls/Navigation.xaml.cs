/**
 * Navigation.xaml.cs
 * 
 * The left-hand navigation tree.
 * 
 * * * * * * * * *
 * 
 * Copyright 2013 Simplare
 * 
 * This code is part of the Stoffi Music Player Project.
 * Visit our website at: stoffiplayer.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version
 * 3 of the License, or (at your option) any later version.
 * 
 * See stoffiplayer.com/license for more information.
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Plugins;
using Stoffi.Core.Settings;
using PlaylistManager = Stoffi.Core.Playlists.Manager;
using PluginManager = Stoffi.Core.Plugins.Plugins;
using SettingsManager = Stoffi.Core.Settings.Manager;
using ServiceManager = Stoffi.Core.Services.Manager;

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// Interaction logic for Navigation.xaml
	/// </summary>
	public partial class Navigation : UserControl
	{
		#region Members

		public ContextMenu playlistMenu;
		public List<TreeViewItem> historyList = new List<TreeViewItem>();
		public MenuItem playlistMenuSave = new MenuItem();
		public MenuItem playlistMenuRename = new MenuItem();
		public MenuItem playlistMenuRemove = new MenuItem();
		public MenuItem playlistMenuShare = new MenuItem();
		private TreeViewItem rightClickedTvi;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the queue of tracks to add to a playlist after it has been created
		/// </summary>
		public List<object> AddToPlaylistQueue { get; set; }

		/// <summary>
		/// Gets or sets the filter of tracks to add to a dynamic playlist after it has been created.
		/// </summary>
		public string AddToPlaylistFilter { get; set; }

		/// <summary>
		/// Gets whether one of the selected items has focus or not.
		/// </summary>
		public bool ItemIsFocused
		{
			get
			{
				foreach (TreeViewItem i in NavigationTree.Items)
					if (IsItemOrChildrenFocused(i)) return true;
				return false;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a navigation class
		/// </summary>
		public Navigation()
		{
			//U.L(LogLevel.Debug, "NAVIGATION", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "NAVIGATION", "Initialized");
			FavoritesIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Favorite.ico", 16, 16);
			NowPlayingIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Library.ico", 16, 16);
			VideoIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Video.ico", 16, 16);
			VisualizerIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Visualizer.ico", 16, 16);
			MusicIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Library.ico", 16, 16);
			FilesIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/FileAudio.ico", 16, 16);
			RadioIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Radio.ico", 16, 16);
			YoutubeIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/YouTube.ico", 16, 16);
			SoundCloudIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/SoundCloud.ico", 16, 16);
			JamendoIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Jamendo.ico", 16, 16);
			QueueIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Queue.ico", 16, 16);
			HistoryIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Clock.ico", 16, 16);
			PlaylistsIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/DiscAudio.ico", 16, 16);
			CreateNewIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/DiscAudioPlus.ico", 16, 16);

			FileSearchIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Search.ico", 16, 16);
			RadioSearchIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Search.ico", 16, 16);
			YoutubeSearchIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Search.ico", 16, 16);
			SoundCloudSearchIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Search.ico", 16, 16);
			JamendoSearchIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Search.ico", 16, 16);
			QueueSearchIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Search.ico", 16, 16);
			HistorySearchIcon.Source = Utilities.GetIcoImage("pack://application:,,,/Images/Icons/Search.ico", 16, 16);

			RefreshSearchIndicator(FileSearchIcon, SettingsManager.FileListConfig);
			RefreshSearchIndicator(RadioSearchIcon, SettingsManager.RadioListConfig);
			RefreshSearchIndicator(YoutubeSearchIcon, SettingsManager.YouTubeListConfig);
			RefreshSearchIndicator(SoundCloudSearchIcon, SettingsManager.SoundCloudListConfig);
			RefreshSearchIndicator(JamendoSearchIcon, SettingsManager.JamendoListConfig);
			RefreshSearchIndicator(RadioSearchIcon, SettingsManager.RadioListConfig);
			RefreshSearchIndicator(QueueSearchIcon, SettingsManager.QueueListConfig);
			RefreshSearchIndicator(HistorySearchIcon, SettingsManager.HistoryListConfig);

			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);
			PluginManager.Installed += new EventHandler<PluginEventArgs>(PluginManager_Installed);
			PluginManager.Uninstalled += new EventHandler<PluginEventArgs>(PluginManager_Uninstalled);

			//U.L(LogLevel.Debug, "NAVIGATION", "Created");
		}

		#endregion

		#region Methods

		#region Override

		/// <summary>
		/// Invoked when the navigation is initialized.
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnInitialized(EventArgs e)
		{
			BorderThickness = new Thickness(0);

			playlistMenu = new ContextMenu();

			playlistMenuSave = new MenuItem();
			playlistMenuSave.Header = U.T("MenuSavePlaylist");
			playlistMenuSave.Click += SavePlaylist_Click;
			playlistMenu.Items.Add(playlistMenuSave);

			playlistMenuRename = new MenuItem();
			playlistMenuRename.Header = U.T("MenuRenamePlaylist");
			playlistMenuRename.Click += RenamePlaylist_Click;
			playlistMenu.Items.Add(playlistMenuRename);

			playlistMenuRemove = new MenuItem();
			playlistMenuRemove.Header = U.T("MenuRemovePlaylist");
			playlistMenuRemove.Click += RemovePlaylist_Click;
			playlistMenu.Items.Add(playlistMenuRemove);

			playlistMenuShare = new MenuItem();
			playlistMenuShare.Header = U.T("MenuSharePlaylist");
			playlistMenuShare.Click += SharePlaylist_Click;
			playlistMenu.Items.Add(playlistMenuShare);

			base.OnInitialized(e);
		}

		#endregion

		#region Public

		/// <summary>
		/// Sets whether or not the visualizer should be visible depending on whether or
		/// not there are any installed visualizers.
		/// </summary>
		public void RefreshVisualizerVisibility()
		{
			bool anyVisualizers = false;
			foreach (var p in SettingsManager.Plugins)
				if (p.Type == Plugins.PluginType.Visualizer)
				{
					anyVisualizers = true;
					break;
				}
			Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
			{
				Visualizer.Visibility = SettingsManager.CurrentSelectedNavigation == "Visualizer" || anyVisualizers ? Visibility.Visible : Visibility.Collapsed;
			}));
		}

		/// <summary>
		/// Sets whether the search indicator for a given navigation item should
		/// be visible or not.
		/// </summary>
		/// <param name="navigation">The navigation item</param>
		/// <param name="config">The configuration specifying the filtering</param>
		public void SetSearchIndicator(string navigation, ListConfig config)
		{
			Image i = null;
			if (navigation == "Files")
				i = FileSearchIcon;
			else if (navigation == "YouTube")
				i = YoutubeSearchIcon;
			else if (navigation == "SoundCloud")
				i = SoundCloudSearchIcon;
			else if (navigation == "Radio")
				i = RadioSearchIcon;
			else if (navigation == "Jamendo")
				i = JamendoSearchIcon;
			else if (navigation == "Queue")
				i = QueueSearchIcon;
			else if (navigation == "History")
				i = HistorySearchIcon;
			else
			{
				foreach (TreeViewItem tvi in Playlists.Items)
				{
					var p = Tvi2Pl(tvi);
					if (p != null && navigation == "Playlist:" + p.Name)
					{
						DockPanel dp = tvi.Header as DockPanel;
						i = dp.Children[2] as Image;
						break;
					}
				}
			}

			if (i != null)
				RefreshSearchIndicator(i, config);
		}

		/// <summary>
		/// Refreshes all programatically set strings according to current language.
		/// </summary>
		public void RefreshStrings()
		{
			RefreshSearchIndicator(FileSearchIcon, SettingsManager.FileListConfig);
			RefreshSearchIndicator(RadioSearchIcon, SettingsManager.RadioListConfig);
			RefreshSearchIndicator(YoutubeSearchIcon, SettingsManager.YouTubeListConfig);
			RefreshSearchIndicator(SoundCloudSearchIcon, SettingsManager.SoundCloudListConfig);
			RefreshSearchIndicator(RadioSearchIcon, SettingsManager.RadioListConfig);
			RefreshSearchIndicator(JamendoSearchIcon, SettingsManager.JamendoListConfig);
			RefreshSearchIndicator(QueueSearchIcon, SettingsManager.QueueListConfig);
			RefreshSearchIndicator(HistorySearchIcon, SettingsManager.HistoryListConfig);

			foreach (TreeViewItem tvi in Playlists.Items)
			{
				Playlist p = Tvi2Pl(tvi);
				if (p != null)
				{
					DockPanel dp = tvi.Header as DockPanel;
					if (dp != null && dp.Children.Count > 2)
						RefreshSearchIndicator(dp.Children[2] as Image, p.ListConfig);
				}
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Updates the visibility and tooltip of the search indicator
		/// based on a given configuration.
		/// </summary>
		/// <param name="icon">The search indicator icon</param>
		/// <param name="config">The configuration containing the search filter</param>
		private void RefreshSearchIndicator(Image icon, ListConfig config)
		{
			if (config != null && !String.IsNullOrWhiteSpace(config.Filter))
			{
				icon.Visibility = Visibility.Visible;
				icon.ToolTip = String.Format(U.T("SearchTooltip"), config.Filter);
			}
			else
				icon.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Returns the playlist corresponding to a TreeViewItem if possible
		/// </summary>
		/// <param name="tvi">The playlist item in the TreeView</param>
		/// <returns>The corresponding playlist if such could be found, otherwise null</returns>
		private Playlist Tvi2Pl(TreeViewItem tvi)
		{
			EditableTextBlock etb = Tvi2Etb(tvi);
			if (etb == null) return null;
			return PlaylistManager.Get(etb.Text);
		}

		/// <summary>
		/// Fetches the underlying EditableTextBlock given a TreeViewItem.
		/// </summary>
		/// <param name="tvi">The TreeViewItem containing the EditableTextBlock</param>
		/// <returns>The EditableTextBlock inside tvi is such exists, otherwhise null</returns>
		private EditableTextBlock Tvi2Etb(TreeViewItem tvi)
		{
			if (tvi != null && tvi.Header is DockPanel)
			{
				DockPanel dp = tvi.Header as DockPanel;
				foreach (object child in dp.Children)
					if (child is EditableTextBlock)
						return child as EditableTextBlock;
			}
			return null;
		}

		/// <summary>
		/// Update the currently selected navigation and add the item to the history list
		/// </summary>
		/// <param name="name">The name of the item</param>
		/// <param name="tvi">The TreeViewItem that is selected</param>
		private void ToggleNavigation(string name, TreeViewItem tvi)
		{
			if (tvi.IsSelected)
			{
				historyList.Remove(tvi);
				historyList.Add(tvi);
			}
			SettingsManager.CurrentSelectedNavigation = name;
		}

		/// <summary>
		/// Selects the TreeViewItem of a playlist with a given name.
		/// </summary>
		/// <param name="name">The name of the playlist</param>
		private void SelectPlaylist(string name)
		{
			foreach (TreeViewItem tvi in Playlists.Items)
			{
				EditableTextBlock etb = Tvi2Etb(tvi);
				if (etb != null && etb.Text == name)
				{
					tvi.Focus();
					return;
				}
			}
		}

		/// <summary>
		/// Get the list of items that an operation (such as rename, remove, share)
		/// should affect.
		/// This will either be the item or selection directly under a recently
		/// opened context menu, or the current selection if no context menu
		/// has been opened.
		/// </summary>
		/// <returns>The list of TreeViewItems that should be operated on</returns>
		private List<TreeViewItem> GetCurrentItems()
		{
			List<TreeViewItem> list = new List<TreeViewItem>();
			if (rightClickedTvi != null)
				list.Add(rightClickedTvi);
			else
				foreach (TreeViewItem tvi in Playlists.Items)
					if (tvi.IsSelected)
						list.Add(tvi);
			return list;
		}

		/// <summary>
		/// Gets the item given its name.
		/// </summary>
		/// <param name="name">The name of the item</param>
		/// <returns>The item in the tree corresponding to the name</returns>
		private TreeViewItem GetItem(string name)
		{
			switch (name)
			{
				case "NowPlaying":
				case "Now playing":
					return NowPlaying;
				case "Video":
					return Video;
				case "Visualizer":
					return Visualizer;
				case "Music":
					return Music;
				case "Library":
				case "Files":
					return Files;
				case "YouTube":
					return Youtube;
				case "SoundCloud":
					return SoundCloud;
				case "Radio":
					return Radio;
				case "Jamendo":
					return Jamendo;
				case "History":
					return History;
				case "Queue":
					return Queue;
				case "Playlists":
					return Playlists;
				default:
					if (name.StartsWith("Playlist:"))
					{
						foreach (TreeViewItem tvi in Playlists.Items)
						{
							EditableTextBlock etb = Tvi2Etb(tvi);
							if (etb != null && name == "Playlist:" + etb.Text)
								return tvi;
						}
					}
					break;
			}
			return null;
		}

		/// <summary>
		/// Checks if a item or any of its children has focus.
		/// </summary>
		/// <param name="item">The item to check</param>
		/// <returns>True if the item or any child of it has focus, otherwise false</returns>
		private bool IsItemOrChildrenFocused(TreeViewItem item)
		{
			if (item.IsFocused)
				return true;

			foreach (TreeViewItem i in item.Items)
				if (IsItemOrChildrenFocused(i))
					return true;

			return false;
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Invoked when the control has been fully loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Navigation_Loaded(object sender, RoutedEventArgs e)
		{
			if (SettingsManager.CurrentSelectedNavigation.StartsWith("Playlist:"))
				if (PlaylistManager.Get(SettingsManager.CurrentSelectedNavigation.Split(new[] { ':' }, 2)[1]) == null)
					SettingsManager.CurrentSelectedNavigation = "Files";

			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
			{
				NavigationTree.ItemContainerStyle = (Style)TryFindResource("ClassicNavigationStyle");
				NowPlaying.ItemContainerStyle = (Style)TryFindResource("ClassicNavigationStyle");
				Music.ItemContainerStyle = (Style)TryFindResource("ClassicNavigationStyle");
				Playlists.ItemContainerStyle = (Style)TryFindResource("ClassicNavigationStyle");
			}
			else
			{
				NavigationTree.ItemContainerStyle = (Style)TryFindResource("AeroNavigationStyle");
				NowPlaying.ItemContainerStyle = (Style)TryFindResource("AeroNavigationStyle");
				Music.ItemContainerStyle = (Style)TryFindResource("AeroNavigationStyle");
				Playlists.ItemContainerStyle = (Style)TryFindResource("AeroNavigationStyle");
			}

			AddToPlaylistQueue = new List<object>();
		}

		/// <summary>
		/// Invoked when the "Favorites" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Favorites_Selected(object sender, RoutedEventArgs e)
		{
			//ToggleNavigation("Favorites", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Now playing" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void NowPlaying_Selected(object sender, RoutedEventArgs e)
		{
			if (NowPlaying.IsSelected)
				ToggleNavigation("Now playing", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Video" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Video_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("Video", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Visualizer" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Visualizer_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("Visualizer", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Music" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Music_Selected(object sender, RoutedEventArgs e)
		{
			if (Music.IsSelected)
				ToggleNavigation("Music", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Files" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Files_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("Files", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "YouTube" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Youtube_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("YouTube", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "SoundCloud" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SoundCloud_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("SoundCloud", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Radio" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Radio_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("Radio", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Jamendo" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Jamendo_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("Jamendo", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Disc" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Disc_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("Disc", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Queue" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Queue_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("Queue", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "History" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void History_Selected(object sender, RoutedEventArgs e)
		{
			ToggleNavigation("History", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when the "Playlists" item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Playlists_Selected(object sender, RoutedEventArgs e)
		{
			if (Playlists.IsSelected)
				ToggleNavigation("Playlists", sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when a playlist item is selected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void Playlist_Selected(object sender, RoutedEventArgs e)
		{
			Playlist playlist = Tvi2Pl(sender as TreeViewItem);
			if (playlist == null) return;
			ToggleNavigation("Playlist:" + playlist.Name, sender as TreeViewItem);
		}

		/// <summary>
		/// Invoked when a playlist's name has been edited.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void Playlist_Edited(object sender, EditableTextBlockEventArgs e)
		{
			PlaylistManager.Rename(e.OldText, e.NewText);
			SelectPlaylist(e.NewText);
		}

		/// <summary>
		/// Invoked when a playlist is being canceled from editing it's name.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void Playlist_Canceled(object sender, EventArgs e)
		{
			EditableTextBlock etb = sender as EditableTextBlock;
			if (etb != null)
				SelectPlaylist(etb.Text);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void CreateNewPlaylist_Selected(object sender, RoutedEventArgs e)
		{
			TreeViewItem item = sender as TreeViewItem;
			DockPanel dp = item.Header as DockPanel;
			EditableTextBlock etb = dp.Children[1] as EditableTextBlock;

			etb.IsInEditMode = etb.IsEditable;
		}

		/// <summary>
		/// Invoked when the "New playlist" item has been edited.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void CreateNewPlaylist_Edited(object sender, EditableTextBlockEventArgs e)
		{
			if (e.NewText != U.T("NavigationCreateNew") && e.NewText != "")
			{
				if (String.IsNullOrWhiteSpace(AddToPlaylistFilter))
				{
					PlaylistManager.Create(e.NewText, AddToPlaylistQueue.Count == 0);
					if (AddToPlaylistQueue.Count > 0)
						PlaylistManager.AddToPlaylist(AddToPlaylistQueue, e.NewText);
					AddToPlaylistQueue.Clear();
				}	
				else
				{
					PlaylistManager.CreateDynamic(e.NewText, AddToPlaylistFilter);
					AddToPlaylistFilter = null;
				}
			}
			else
				CreateNewPlaylist_Canceled(sender, e);
		}

		/// <summary>
		/// Invoked when the "New playlist" item was being edited by was canceled.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void CreateNewPlaylist_Canceled(object sender, EventArgs e)
		{
			if (historyList.Count > 0)
				historyList.Last<TreeViewItem>().Focus();

			else if (Playlists.Items.Count > 1)
				((TreeViewItem)Playlists.Items[0]).Focus();

			else
				Music.Focus();
		}

		/// <summary>
		/// Invoked when the users selects to remove a playlist item.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void RemovePlaylist_Click(object sender, RoutedEventArgs e)
		{
			foreach (TreeViewItem tvi in GetCurrentItems())
			{
				Playlist pld = Tvi2Pl(tvi);
				if (pld != null)
				{
					string title = U.T("MessageDeletePlaylist", "Title");
					string message = U.T("MessageDeletePlaylist", "Message");
					message = message.Replace("%name", pld.Name);
					if (pld.Tracks.Count == 0 ||
						MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
						PlaylistManager.Remove(pld.Name);
				}
			}
		}

		/// <summary>
		/// Invoked when the user clicks "Save" in playlist context menu
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void SavePlaylist_Click(object sender, RoutedEventArgs e)
		{
			foreach (TreeViewItem tvi in GetCurrentItems())
			{
				Playlist pld = Tvi2Pl(tvi);
				try
				{
					Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
					dialog.Title = "Save Playlist";
					dialog.DefaultExt = ".pls";
					dialog.Filter = "Playlist (*.pls)|*.pls|Playlist (*.m3u)|*.m3u";
					dialog.FileName = pld.Name;
					bool result = (bool)dialog.ShowDialog();
					if (result == true)
					{
						PlaylistManager.Save(dialog.FileName, pld.Name);
					}
				}
				catch (Exception exc)
				{
					MessageBox.Show(U.T("MessageSavingPlaylist", "Message") + ":\n" + exc.Message,
									U.T("MessageSavingPlaylist", "Title"),
									MessageBoxButton.OK,
									MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Invoked when the user clicks "Share" in playlist context menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void SharePlaylist_Click(object sender, RoutedEventArgs e)
		{
			foreach (TreeViewItem tvi in GetCurrentItems())
			{
				Playlist pld = Tvi2Pl(tvi);
				if (pld != null)
					ServiceManager.SharePlaylist(pld);
			}
		}

		/// <summary>
		/// Invoked when the user selects to rename a playlist item.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void RenamePlaylist_Click(object sender, RoutedEventArgs e)
		{
			EditableTextBlock etb = sender as EditableTextBlock;
			if (etb == null && sender is TreeViewItem)
				etb = Tvi2Etb(sender as TreeViewItem);
			if (etb == null)
				etb = Tvi2Etb(NavigationTree.SelectedItem as TreeViewItem);
			if (etb == null || !etb.IsEditable)
				return;
			var playlist = PlaylistManager.Get(etb.Text);

			// do not rename playlists we don't own
			if (playlist.IsSomeoneElses)
				return;

			etb.IsInEditMode = true;
		}

		/// <summary>
		/// Invoked when a drop occurs on a playlist item.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void Playlist_Drop(object sender, DragEventArgs e)
		{
			List<object> DraggedItems = e.Data.GetData(typeof(List<object>)) as List<object>;
			TreeViewItem tvi = sender as TreeViewItem;
			DockPanel dp = tvi.Header as DockPanel;
			EditableTextBlock etb = dp.Children[1] as EditableTextBlock;

			if (etb.Text == U.T("NavigationCreateNew"))
			{
				AddToPlaylistQueue.Clear();
				foreach (Track track in DraggedItems)
					AddToPlaylistQueue.Add(track);
				CreateNewPlaylistETB.IsInEditMode = true;
			}
			else
			{
				PlaylistManager.AddToPlaylist(DraggedItems, etb.Text);
			}
		}

		/// <summary>
		/// Invoked when the user releases a key.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void NavigationTree_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
				RemovePlaylist_Click(sender, e);
		}

		/// <summary>
		/// Invoked when the user right-clicks on a playlist
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void Playlist_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			TreeViewItem tvi = sender as TreeViewItem;

			if (tvi == null) return;
			if (!tvi.IsSelected)
			{
				rightClickedTvi = tvi;
				tvi.IsSelected = true;
			}

			playlistMenuRename.Visibility = Visibility.Visible;

			var dp = tvi.Header as DockPanel;
			if (dp == null) return;
			var etb = dp.Children[1] as EditableTextBlock;
			if (etb == null) return;
			var pl = PlaylistManager.Get(etb.Text);
			if (pl == null) return;

			// do not rename playlists we don't own
			if (pl.IsSomeoneElses)
				playlistMenuRename.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Invoked when the context menu of a playlist item closes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void Playlist_ContextMenuClosing(object sender, ContextMenuEventArgs e)
		{
			rightClickedTvi = null;
		}

		/// <summary>
		/// Invoked when a property of SettingsManager is changed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "CurrentSelectedNavigation":
					TreeViewItem tvi = GetItem(SettingsManager.CurrentSelectedNavigation);
					if (tvi != null && !tvi.IsSelected)
					{
						tvi.IsSelected = true;
						tvi.Focus();
					}
					RefreshVisualizerVisibility();
					break;
			}
		}

		/// <summary>
		/// Invoked when a plugin has been installed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PluginManager_Installed(object sender, PluginEventArgs e)
		{
			RefreshVisualizerVisibility();
		}

		/// <summary>
		/// Invoked when a plugin has been uninstalled.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PluginManager_Uninstalled(object sender, PluginEventArgs e)
		{
			RefreshVisualizerVisibility();
		}

		#endregion

		#endregion
	}
}
