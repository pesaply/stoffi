/**
 * SearchBox.xaml.cs
 * 
 * The search box.
 * 
 * * * * * * * * *
 * 
 * Copyright 2012 Simplare
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
using System.Collections.Generic;
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

namespace Stoffi
{
	/// <summary>
	/// A Windows Explorer styled search box
	/// </summary>
	public partial class SearchBox : UserControl
	{
		#region Members

		private bool isActive = false;
		private bool changingActiveStatus = false;
		private DispatcherTimer delay = new DispatcherTimer();

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets whether the box will send events on changed content.
		/// </summary>
		public bool IsConnected { get; set; }

		/// <summary>
		/// Gets or sets whether the search box is enabled or not.
		/// </summary>
		public new bool IsEnabled
		{
			get { return Box.IsEnabled; }
			set
			{
				Box.IsEnabled = value;
				byte a = (byte)(value ? 192 : 255);
				byte v = (byte)(value ? 255 : 244);
				SearchBackground.Background = new SolidColorBrush(Color.FromArgb(a, v, v, v));
			}
		}

		/// <summary>
		/// Gets or sets the current state of the search box
		/// </summary>
		public bool IsActive
		{
			get { return isActive; }
			set
			{
				changingActiveStatus = true;
				if (value)
				{
					Box.Text = "";
					Box.FontStyle = System.Windows.FontStyles.Normal;
					Box.Foreground = System.Windows.Media.Brushes.Black;
					SearchBackground.Background = System.Windows.Media.Brushes.White;
					Button.Style = (Style)FindResource("SearchClearButtonStyle");
				}
				else
				{
					Box.Text = U.T("PlaybackSearch", "Text");
					Box.FontStyle = System.Windows.FontStyles.Italic;
					Box.Foreground = new SolidColorBrush(Color.FromRgb(0x79, 0x7a, 0x7a));
					SearchBackground.Background = new SolidColorBrush(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF));
					Button.Style = (Style)FindResource("SearchButtonStyle");
				}
				changingActiveStatus = false;
				isActive = value;
				DispatchActiveStateChanged();
			}
		}

		/// <summary>
		/// Get or sets the search text.
		/// An empty string if the search box is not active.
		/// </summary>
		public String Text
		{
			get
			{
				if (IsActive)
					return Box.Text;
				else
					return "";
			}
			set { Box.Text = value; }
		}

		/// <summary>
		/// Gets or sets the position of the cursor in the text box.
		/// </summary>
		public int Position
		{
			get { return Box.SelectionStart; }
			set { Box.SelectionStart = value; }
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Create a search box
		/// </summary>
		public SearchBox()
		{
			//U.L(LogLevel.Debug, "SEARCH BOX", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "SEARCH BOX", "Initialized");
			delay.Interval = new TimeSpan(0, 0, 0, 0, 500);
			delay.Tick += new EventHandler(Delay_Tick);
			IsConnected = true;
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Add a new playlist to the search box's context menu
		/// </summary>
		/// <param name="playlist">The playlist to be added</param>
		public void AddPlaylist(PlaylistData playlist)
		{
			// create the menu item in "Add to Playlist" in list
			MenuItem ListAddMenu = new MenuItem();
			ListAddMenu.Header = playlist.Name;
			ListAddMenu.Click += AddToPlaylist_Clicked;
			Menu_Add.Items.Insert(Menu_Add.Items.Count - 1, ListAddMenu);

			// create the menu item in "Remove from Playlist" in list
			MenuItem ListDelMenu = new MenuItem();
			ListDelMenu.Header = playlist.Name;
			ListDelMenu.Click += RemoveFromPlaylist_Clicked;
			Menu_Remove.Items.Insert(Menu_Remove.Items.Count, ListDelMenu);

			Menu_Remove.IsEnabled = true;
		}

		/// <summary>
		/// Remove a playlist from the search box's context menu
		/// </summary>
		/// <param name="playlist">The playlist to be removed</param>
		public void RemovePlaylist(PlaylistData playlist)
		{
			// remove from "add to playlist" menu in list
			List<MenuItem> menu_items_to_remove = new List<MenuItem>();
			foreach (MenuItem item in Menu_Add.Items)
			{
				if (item.Header.ToString() == playlist.Name)
					menu_items_to_remove.Add(item);
			}
			foreach (MenuItem item in menu_items_to_remove)
				Menu_Add.Items.Remove(item);

			// remove from "remove from playlist" menu in list
			menu_items_to_remove.Clear();
			foreach (MenuItem item in Menu_Remove.Items)
			{
				if (item.Header.ToString() == playlist.Name)
					menu_items_to_remove.Add(item);
			}
			foreach (MenuItem item in menu_items_to_remove)
				Menu_Remove.Items.Remove(item);


			if (SettingsManager.Playlists.Count <= 0)
				Menu_Remove.IsEnabled = false;
		}

		/// <summary>
		/// Rename a playlist in the search box's context menu
		/// </summary>
		/// <param name="oldName">The old name of the playlist</param>
		/// <param name="newName">The new name of the playlist</param>
		public void RenamePlaylist(String oldName, String newName)
		{
			foreach (MenuItem item in Menu_Add.Items)
				if (item.Header.ToString() == oldName)
					item.Header = newName;

			foreach (MenuItem item in Menu_Remove.Items)
				if (item.Header.ToString() == oldName)
					item.Header = newName;
		}

		/// <summary>
		/// Clear the current search.
		/// </summary>
		public void Clear()
		{
			Box.Text = "";
			IsActive = false;
			Box.Text = U.T("PlaybackSearch", "Text");
			Box.FontStyle = System.Windows.FontStyles.Italic;
			Box.Foreground = new SolidColorBrush(Color.FromRgb(0x79, 0x7a, 0x7a));
			SearchBackground.Background = new SolidColorBrush(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF));
			Box.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
			Button.Style = (Style)FindResource("SearchButtonStyle");
			DispatchCleared();
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user clicks the Clear button.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Button_Clicked(object sender, RoutedEventArgs e)
		{
			Clear();
		}

		/// <summary>
		/// Invoked when the user clicks AddToNew in the context menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void AddToNew_Clicked(object sender, RoutedEventArgs e)
		{
			DispatchAddToNew();
		}

		/// <summary>
		/// Invoked when the user clicks Add in the context menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void AddToPlaylist_Clicked(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem)
			{
				MenuItem item = sender as MenuItem;
				String name = item.Header.ToString();
				DispatchAdd(name);
			}
		}

		/// <summary>
		/// Invoked when the user clicks Remove in the context menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void RemoveFromPlaylist_Clicked(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem)
			{
				MenuItem item = sender as MenuItem;
				String name = item.Header.ToString();
				DispatchRemove(name);
			}
		}

		/// <summary>
		/// Invoked when the text box gets focus.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Box_GotFocus(object sender, RoutedEventArgs e)
		{
			U.ListenForShortcut = false;
			if (Box.Text == U.T("PlaybackSearch", "Text"))
				IsActive = true;
			else
				Box.SelectAll();
		}

		/// <summary>
		/// Invoked when the text box loses focus.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Box_LostFocus(object sender, RoutedEventArgs e)
		{
			U.ListenForShortcut = true;
			if (Box.Text == "")
				IsActive = false;
		}

		/// <summary>
		/// Invoked when the text of the text box is changed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Box_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (IsEnabled && IsActive && IsConnected && !changingActiveStatus)
			{
				delay.Stop();
				delay.Start();
			}
		}

		/// <summary>
		/// Called after a search has been performed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event arguments</param>
		private void Delay_Tick(object sender, EventArgs e)
		{
			delay.Stop();
			DispatchTextChanged();
		}
		
		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the ActiveStateChanged event.
		/// </summary>
		private void DispatchActiveStateChanged()
		{
			if (ActiveStateChanged != null)
				ActiveStateChanged(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the Cleared event.
		/// </summary>
		private void DispatchCleared()
		{
			if (Cleared != null)
				Cleared(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the AddToNew event.
		/// </summary>
		private void DispatchAddToNew()
		{
			if (AddToNew != null)
				AddToNew(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the Add event.
		/// </summary>
		private void DispatchAdd(string playlistName)
		{
			if (Add != null)
				Add(this, new GenericEventArgs<string>(playlistName));
		}

		/// <summary>
		/// Dispatches the Remove event.
		/// </summary>
		private void DispatchRemove(string playlistName)
		{
			if (Remove != null)
				Remove(this, new GenericEventArgs<string>(playlistName));
		}

		/// <summary>
		/// Dispatches the TextChanged event.
		/// </summary>
		private void DispatchTextChanged()
		{
			if (TextChanged != null)
				TextChanged(this, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Invoked when the active status of the search box changes
		/// For example when it receives focus
		/// </summary>
		public event EventHandler ActiveStateChanged;

		/// <summary>
		/// Occurs when the search box is cleared.
		/// </summary>
		public event EventHandler Cleared;

		/// <summary>
		/// Occurs when the current search is added to a new playlist.
		/// </summary>
		public event EventHandler AddToNew;

		/// <summary>
		/// Occurs when the current search is added to a playlist.
		/// </summary>
		public event EventHandler<GenericEventArgs<string>> Add;

		/// <summary>
		/// Occurs when the current search is or removed from a playlist.
		/// </summary>
		public event EventHandler<GenericEventArgs<string>> Remove;

		/// <summary>
		/// Occurs when the text of the search box is changed.
		/// </summary>
		public event EventHandler TextChanged;

		#endregion
	}
}
