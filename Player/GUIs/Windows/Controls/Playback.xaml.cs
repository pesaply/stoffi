/**
 * Playback.xaml.cs
 * 
 * All buttons and controls used to manage playback such as
 * play, pause, next and previous along with seek, volume and
 * search.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Shapes;

using Stoffi.Core;
using Stoffi.Core.Settings;
using Stoffi.Core.Media;

using MediaManager = Stoffi.Core.Media.Manager;
using SettingsManager = Stoffi.Core.Settings.Manager;

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// A playback control that contains all playback buttons, track information, volume and seek slides as well as a search box
	/// </summary>
	public partial class Playback : UserControl
	{
		#region Members

		private BookmarkLayer bookmarkLayer;
		private DispatcherTimer ticker = new DispatcherTimer();
		private uint tickFails = 0;
		private TimeSpan normalTickInterval = new TimeSpan(0, 0, 0, 0, 50);
		private TimeSpan failingTickInterval = new TimeSpan(0, 0, 0, 1);

		// compression constants
		private double startCompression = 800;
		private double volumeMaxWidth = 70;
		private double volumeMinWidth = 35;
		private double repshuMaxMargin = 10;
		private double repshuMinMargin = 0;
		private double searchMaxWidth = 200;
		private double searchMinWidth = 90;
		private double volumeMaxMargin = 10;
		private double volumeMinMargin = 0;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates the playback control
		/// </summary>
		public Playback()
		{
			//U.L(LogLevel.Debug, "PLAYBACK", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "PLAYBACK", "Initialized");
			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);
			VolumeSlide.AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft;

			SongProgress.Value = SettingsManager.Seek;
			SongProgress.ValueChanged += new RoutedPropertyChangedEventHandler<double>(SongProgress_ValueChanged);

			VolumeSlide.Value = SettingsManager.Volume;
			VolumeSlide.ValueChanged += new RoutedPropertyChangedEventHandler<double>(VolumeSlide_ValueChanged);
			//U.L(LogLevel.Debug, "PLAYBACK", "Created");
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Updates the information about the currently loaded track
		/// </summary>
		public void UpdateInfo()
		{
			Track t = SettingsManager.CurrentTrack;

			if (t == null || (t.Type == TrackType.File && !File.Exists(t.Path)))
			{
				InfoName.Text = U.T("PlaybackEmpty");
				InfoTimeMinus.Content = "N/A";
				InfoTimePlus.Content = "N/A";
				SongProgress.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(SongProgress_ValueChanged);
				SongProgress.Value = 0;
				SongProgress.SecondValueWidth = 0;
				SongProgress.ValueChanged += new RoutedPropertyChangedEventHandler<double>(SongProgress_ValueChanged);
			}
			else
			{
				if (t.Type == TrackType.WebRadio)
				{
					InfoName.Text = t.Title;
					InfoTimeMinus.Content = "N/A";
					InfoTimePlus.Content = "N/A";
					SongProgress.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(SongProgress_ValueChanged);
					SongProgress.Value = 0;
					SongProgress.SecondValueWidth = 0;
					SongProgress.ValueChanged += new RoutedPropertyChangedEventHandler<double>(SongProgress_ValueChanged);
				}
				else
				{
					InfoName.Text = t.Artist + " - " + t.Title;
					double pos = MediaManager.Position;
					double len = MediaManager.Length;

					if (pos < 0) pos = 0;
					if (len < 0) len = 0;

					TimeSpan timePlus = new TimeSpan(0, 0, (int)pos);
					TimeSpan timeMinus = new TimeSpan(0, 0, (int)(len - pos));

					if (timePlus.TotalSeconds < 0)
						InfoTimePlus.Content = "N/A";
					else if (timePlus.TotalSeconds >= 0)
						InfoTimePlus.Content = U.TimeSpanToString(timePlus);

					if (timeMinus.TotalSeconds < 0)
						InfoTimeMinus.Content = "N/A";
					else if (timeMinus.TotalSeconds > 0)
						InfoTimeMinus.Content = "-" + U.TimeSpanToString(timeMinus);

					double seek = SettingsManager.Seek * (SongProgress.Maximum / 10.0);
					if (timePlus.Seconds < 0 || Double.IsNaN(seek) || Double.IsInfinity(seek)) seek = 0;

					if (seek > 0 || SettingsManager.MediaState != Core.Settings.MediaState.Playing)
					{
						SongProgress.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(SongProgress_ValueChanged);
						SongProgress.Value = seek;
						SongProgress.ValueChanged += new RoutedPropertyChangedEventHandler<double>(SongProgress_ValueChanged);
					}
				}
			}
		}

		/// <summary>
		/// Compresses the controls inside the pane so they take up smaller size
		/// if neeeded.
		/// </summary>
		/// <param name="width">The width of the control</param>
		/// <param name="minWidth">The minimum width for which compressing is done.</param>
		public void Compress(double width, double minWidth)
		{
			if (width < startCompression)
			{
				if (width < minWidth)
					width = minWidth;
				double stretchFactor = (width - minWidth) / (startCompression - minWidth);

				double volumeWidth = volumeMinWidth + ((volumeMaxWidth - volumeMinWidth) * stretchFactor);
				double volumeMargin = volumeMinMargin + ((volumeMaxMargin - volumeMinMargin) * stretchFactor);
				double searchWidth = searchMinWidth + ((searchMaxWidth - searchMinWidth) * stretchFactor);
				double repshuMargin = repshuMinMargin + ((repshuMaxMargin - repshuMinMargin) * stretchFactor);

				VolumeSlide.Width = volumeWidth;
				VolumeSlide.Margin = new Thickness(volumeMargin, 0, volumeMargin, 0);
				Search.SearchContainer.Width = searchWidth;
				RepeatShuffleContainer.Margin = new Thickness(repshuMargin, 0, repshuMargin, 0);
			}
			else
			{
				VolumeSlide.Width = volumeMaxWidth;
				VolumeSlide.Margin = new Thickness(volumeMaxMargin, 0, volumeMaxMargin, 0);
				Search.SearchContainer.Width = searchMaxWidth;
				RepeatShuffleContainer.Margin = new Thickness(repshuMaxMargin, 0, repshuMaxMargin, 0);
			}
		}

		/// <summary>
		/// Adds a bookmark indicator.
		/// </summary>
		/// <param name="pos">The position where to add the bookmark</param>
		public void AddBookmark(double pos)
		{
			while (bookmarkLayer == null) ;
			bookmarkLayer.AddBookmark(pos);
		}

		/// <summary>
		/// Removes a bookmark indicator.
		/// </summary>
		/// <param name="pos">The position of the bookmark to remove</param>
		public void RemoveBookmark(double pos)
		{
			while (bookmarkLayer == null) ;
			bookmarkLayer.RemoveBookmark(pos);
		}

		/// <summary>
		/// Removes all bookmark indicators.
		/// </summary>
		public void ClearBookmarks()
		{
			while (bookmarkLayer == null) ;
			bookmarkLayer.ClearBookmarks();
		}

		#endregion

		#region Private

		/// <summary>
		/// Updates the shuffle button to reflect the current Shuffle state
		/// </summary>
		private void UpdateShuffle()
		{
			switch (SettingsManager.Shuffle)
			{
				case true:
					ShuffleButton.Style = (Style)FindResource("ShuffleButtonStyle");
					break;

				case false:
				default:
					ShuffleButton.Style = (Style)FindResource("ShuffleGrayButtonStyle");
					break;
			}
		}

		/// <summary>
		/// Updates the repeat button to reflect the current Repeat state
		/// </summary>
		private void UpdateRepeat()
		{
			switch (SettingsManager.Repeat)
			{
				case RepeatState.RepeatAll:
					RepeatButton.Style = (Style)FindResource("RepeatAllButtonStyle");
					break;

				case RepeatState.RepeatOne:
					RepeatButton.Style = (Style)FindResource("RepeatOneButtonStyle");
					break;

				case RepeatState.NoRepeat:
				default:
					RepeatButton.Style = (Style)FindResource("RepeatGrayButtonStyle");
					break;
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user clicks on pause/play.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PausePlay_Click(object sender, RoutedEventArgs e)
		{
			DispatchPausePlayClick();
		}

		/// <summary>
		/// Invoked when the user clicks on next.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Next_Click(object sender, RoutedEventArgs e)
		{
			MediaManager.Next(true);
		}

		/// <summary>
		/// Invoked when the user clicks on previous.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Previous_Click(object sender, RoutedEventArgs e)
		{
			MediaManager.Previous();
		}

		/// <summary>
		/// Invoked when the user clicks on repeat.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void Repeat_Click(object sender, RoutedEventArgs e)
		{
			switch (SettingsManager.Repeat)
			{
				case RepeatState.RepeatAll:
					SettingsManager.Repeat = RepeatState.RepeatOne;
					break;
				case RepeatState.RepeatOne:
					SettingsManager.Repeat = RepeatState.NoRepeat;
					break;
				case RepeatState.NoRepeat:
				default:
					SettingsManager.Repeat = RepeatState.RepeatAll;
					break;
			}
			UpdateRepeat();
		}

		/// <summary>
		/// Invoked when the user clicks on shuffle.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void Shuffle_Click(object sender, RoutedEventArgs e)
		{
			SettingsManager.Shuffle = !SettingsManager.Shuffle;
			UpdateShuffle();
		}

		/// <summary>
		/// Invoked when the user changes the volume slider.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void VolumeSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			SettingsManager.Volume = VolumeSlide.Value;
		}

		/// <summary>
		/// Invoked when the user scrolls over the volume slider.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void VolumeSlide_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
				VolumeSlide.Value+=5;
			else
				VolumeSlide.Value-=5;
		}

		/// <summary>
		/// Invoked when the user changes the seek slider.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void SongProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (MediaManager.IsInitialized)
				MediaManager.Seek = (SongProgress.Value * 10.0) / SongProgress.Maximum;
		}
		
		/// <summary>
		/// Invoked when the user modifies the text in the search box
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Search_TextChanged(object sender, EventArgs e)
		{
			DispatchSearchTextChanged();
		}

		/// <summary>
		/// Invoked when the user selects to add a search query to an existing playlist
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Search_AddClick(object sender, GenericEventArgs<string> e)
		{
			DispatchAddSearch(e.Value);
		}

		/// <summary>
		/// Invoked when the user selects to add a search query to a new playlist
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Search_AddToNewClick(object sender, EventArgs e)
		{
			DispatchAddSearchToNew();
		}

		/// <summary>
		/// Invoked when the user selects to add a search query to a new dynamic playlist
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Search_AddToNewDynamicClick(object sender, EventArgs e)
		{
			DispatchAddSearchToNewDynamic();
		}

		/// <summary>
		/// Invoked when the user selects to remove a search query from a playlist
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Search_RemoveClick(object sender, GenericEventArgs<string> e)
		{
			DispatchRemoveSearch(e.Value);
		}

		/// <summary>
		/// Invoked when the user clears the search box
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Search_Cleared(object sender, EventArgs e)
		{
			DispatchSearchCleared();
		}

		/// <summary>
		/// Invoked when the control is loded
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Playback_Loaded(object sender, RoutedEventArgs e)
		{
			bookmarkLayer = new BookmarkLayer(SongProgress);
			bookmarkLayer.RemoveClicked += new EventHandler(RemoveBookmark_Click);
			bookmarkLayer.Clicked += new BookmarkEventHandler(Bookmark_Click);
			AdornerLayer al = AdornerLayer.GetAdornerLayer(SongProgress);
			al.Add(bookmarkLayer);

			if (SettingsManager.CurrentTrack != null)
			{
				foreach (var b in MediaManager.GetLibrarySourceTrack(SettingsManager.CurrentTrack).Bookmarks)
				{
					bookmarkLayer.AddBookmark(b.Item2);
				}
			}

			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
			{
				InfoWindow.BorderThickness = new Thickness(1, 1, 0, 0);
				InfoWindow.BorderBrush = SystemColors.ControlDarkBrush;
				InfoWindow.CornerRadius = new CornerRadius(0);

				InfoWindowBorder1.BorderThickness = new Thickness(0, 0, 1, 1);
				InfoWindowBorder1.BorderBrush = SystemColors.ControlLightLightBrush;
				InfoWindowBorder1.CornerRadius = new CornerRadius(0);

				InfoWindowBorder2.BorderThickness = new Thickness(1, 1, 0, 0);
				InfoWindowBorder2.BorderBrush = null;
				InfoWindowBorder2.CornerRadius = new CornerRadius(0);

				InfoWindowBorder3.BorderThickness = new Thickness(0, 0, 1, 1);
				InfoWindowBorder3.BorderBrush = null;
				InfoWindowBorder3.CornerRadius = new CornerRadius(0);

				InfoWindowInner.Background = SystemColors.ControlBrush;

				InfoTimeMinus.Foreground = SystemColors.ControlTextBrush;
				InfoTimePlus.Foreground = SystemColors.ControlTextBrush;
				InfoName.Foreground = SystemColors.ControlTextBrush;
			}

			UpdateInfo();
			UpdateShuffle();
			UpdateRepeat();
		}

		/// <summary>
		/// Invoked when the user removes a bookmark
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void RemoveBookmark_Click(object sender, EventArgs e)
		{
			DispatchRemoveBookmarkClick(sender, e);
		}

		/// <summary>
		/// Invoked when the user clicks on a bookmark
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Bookmark_Click(object sender, BookmarkEventArgs e)
		{
			this.SongProgress.Value = e.Position / 10;
		}

		/// <summary>
		/// Invoked when a property of the settings manager changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				switch (e.PropertyName)
				{
					case "Seek":
						UpdateInfo();
						break;

					case "BufferSize":
						SongProgress.SecondValue = SettingsManager.BufferSize * (SongProgress.Maximum / 10.0);
						break;

					case "Volume":
						VolumeSlide.Value = SettingsManager.Volume;
						break;

					case "Shuffle":
						UpdateShuffle();
						break;

					case "Repeat":
						UpdateRepeat();
						break;

					case "CurrentTrack":
						UpdateInfo();
						ClearBookmarks();
						if (SettingsManager.CurrentTrack != null)
						{
							Track libraryTrack = MediaManager.GetLibrarySourceTrack(SettingsManager.CurrentTrack);
							if (libraryTrack.Bookmarks != null)
								foreach (var b in libraryTrack.Bookmarks)
									AddBookmark(b.Item2);
						}
						break;

					case "MediaState":
						switch (SettingsManager.MediaState)
						{
							case Core.Settings.MediaState.Playing:
								PausePlayButton.Style = (Style)FindResource("PauseButtonStyle");
								break;

							case Core.Settings.MediaState.Paused:
							case Core.Settings.MediaState.Stopped:
								PausePlayButton.Style = (Style)FindResource("PlayButtonStyle");
								break;
						}
						break;
				}
			}));
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the RemoveBookmarkClick event.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void DispatchRemoveBookmarkClick(object sender, EventArgs e)
		{
			if (RemoveBookmarkClick != null)
				RemoveBookmarkClick(sender, e);
		}

		/// <summary>
		/// Dispatches the SearchTextChanged event.
		/// </summary>
		private void DispatchSearchTextChanged()
		{
			if (SearchTextChanged != null)
				SearchTextChanged(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the SearchCleared event.
		/// </summary>
		private void DispatchSearchCleared()
		{
			if (SearchCleared != null)
				SearchCleared(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the AddSearchToNew event.
		/// </summary>
		private void DispatchAddSearchToNew()
		{
			if (AddSearchToNew != null)
				AddSearchToNew(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the AddSearchToNewDynamic event.
		/// </summary>
		private void DispatchAddSearchToNewDynamic()
		{
			if (AddSearchToNewDynamic != null)
				AddSearchToNewDynamic(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the AddSearch event.
		/// </summary>
		/// <param name="playlistName">The name of the playlist to add the search to</param>
		private void DispatchAddSearch(string playlistName)
		{
			if (AddSearch != null)
				AddSearch(this, new GenericEventArgs<string>(playlistName));
		}

		/// <summary>
		/// Dispatches the RemoveSearch event.
		/// </summary>
		/// <param name="playlistName">The name of the playlist to remove the search from</param>
		private void DispatchRemoveSearch(string playlistName)
		{
			if (RemoveSearch != null)
				RemoveSearch(this, new GenericEventArgs<string>(playlistName));
		}

		/// <summary>
		/// Dispatches the PausePlayClick event.
		/// </summary>
		private void DispatchPausePlayClick()
		{
			if (PausePlayClick != null)
				PausePlayClick(this, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user removes a bookmark.
		/// </summary>
		public event EventHandler RemoveBookmarkClick;

		/// <summary>
		/// Occurs when the text of the search box is changed.
		/// </summary>
		public event EventHandler SearchTextChanged;

		/// <summary>
		/// Occurs when the search box is cleared.
		/// </summary>
		public event EventHandler SearchCleared;

		/// <summary>
		/// Occurs when the user adds a search to a new playlist.
		/// </summary>
		public event EventHandler AddSearchToNew;

		/// <summary>
		/// Occurs when the user adds a search to a new dynamic playlist.
		/// </summary>
		public event EventHandler AddSearchToNewDynamic;

		/// <summary>
		/// Occurs when the user adds a search to an existing playlist.
		/// </summary>
		public event EventHandler<GenericEventArgs<string>> AddSearch;

		/// <summary>
		/// Occurs when the user removes a search from a playlist.
		/// </summary>
		public event EventHandler<GenericEventArgs<string>> RemoveSearch;

		/// <summary>
		/// Occurs when the user clicks on Pause/Play.
		/// </summary>
		public event EventHandler PausePlayClick;

		#endregion
	}

	/// <summary>
	/// Describes the layer above the seek bar which holds the bookmarks
	/// </summary>
	public class BookmarkLayer : Adorner
	{
		#region Members

		private VisualCollection visualChildren;
		private Grid grid;
		private List<ColumnDefinition> spacers = new List<ColumnDefinition>();
		public List<Bookmark> bookmarks = new List<Bookmark>();
		public event EventHandler RemoveClicked;
		public event BookmarkEventHandler Clicked;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a Bookmarklayer
		/// </summary>
		/// <param name="adornedElement"></param>
		public BookmarkLayer(UIElement adornedElement)
			: base(adornedElement)
		{
			visualChildren = new VisualCollection(this);
			grid = new Grid();
			grid.Margin = new Thickness(0, 1, 0, 0);
			visualChildren.Add(grid);
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pos"></param>
		public void AddBookmark(double pos)
		{
			double bmWidth = 4.0;
			double MyWidth = this.ActualWidth;
			if (MyWidth < 147) MyWidth = 147;
			double posInPixels = MyWidth * (pos / 100);
			//if (posInPixels < (bmWidth / 2) || posInPixels > this.ActualWidth - (bmWidth / 2)) return;

			double width = posInPixels;
			double i = 0;
			ColumnDefinition newCd;
			ColumnDefinition bmCd;
			Bookmark bm;

			foreach (ColumnDefinition cd in spacers)
			{
				width = posInPixels - i;
				i += cd.Width.Value;
				if (i - (bmWidth / 2) < posInPixels && posInPixels <= i + (bmWidth * 1.5)) return;

				if (posInPixels < i)
				{
					newCd = new ColumnDefinition();
					newCd.Width = new GridLength(width - (bmWidth/2));

					bmCd = new ColumnDefinition();
					bmCd.Width = new GridLength(bmWidth);

					int c = grid.ColumnDefinitions.IndexOf(cd);

					grid.ColumnDefinitions.Insert(c, bmCd);
					grid.ColumnDefinitions.Insert(c, newCd);

					cd.Width = new GridLength(cd.Width.Value - (width + (bmWidth * 1.5)));

					bm = new Bookmark();
					bm.Position = pos;
					bm.RemoveClicked += new EventHandler(Bookmark_RemoveClicked);
					bm.Clicked += new BookmarkEventHandler(Bookmark_Click);
					foreach (Bookmark b in bookmarks)
					{
						if (b.Position > bm.Position)
						{
							spacers.Insert(bookmarks.IndexOf(b), newCd);
							bookmarks.Insert(bookmarks.IndexOf(b), bm);
							break;
						}
					}

					int j = 1;
					foreach (Bookmark b in bookmarks)
					{
						j += 2;
						if (b.Position > bm.Position)
							Grid.SetColumn(b, j);
					}
					Grid.SetColumn(bm, c + 1);
					grid.Children.Add(bm);

					return;
				}
				i += 2.0;
			}

			width = posInPixels - i;

			newCd = new ColumnDefinition();
			newCd.Width = new GridLength(width - (bmWidth / 2));
			spacers.Add(newCd);


			bmCd = new ColumnDefinition();
			bmCd.Width = new GridLength(bmWidth);

			grid.ColumnDefinitions.Add(newCd);
			grid.ColumnDefinitions.Add(bmCd);

			bm = new Bookmark();
			bm.Position = pos;
			bm.RemoveClicked += new EventHandler(Bookmark_RemoveClicked);
			bm.Clicked += new BookmarkEventHandler(Bookmark_Click);
			bookmarks.Add(bm);

			Grid.SetColumn(bm, grid.ColumnDefinitions.Count - 1);
			grid.Children.Add(bm);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pos"></param>
		public void RemoveBookmark(double pos)
		{
			Bookmark bookmarkToRemove = null;
			int i = 0;
			foreach (Bookmark m in bookmarks)
			{
				if (m.Position == pos)
				{
					i = bookmarks.IndexOf(m);
					bookmarkToRemove = m;
					break;
				}
			}

			if (bookmarkToRemove != null)
			{
				if (i == bookmarks.Count - 1)
				{
					grid.ColumnDefinitions.Remove(grid.ColumnDefinitions.Last());
					grid.ColumnDefinitions.Remove(grid.ColumnDefinitions.Last());
					grid.Children.Remove(bookmarks.Last());
					spacers.Remove(spacers.Last());
					bookmarks.Remove(bookmarks.Last());
				}
				else
				{
					ColumnDefinition cd1 = grid.ColumnDefinitions[(2 * i)];
					ColumnDefinition cd2 = grid.ColumnDefinitions[(2 * i) + 1];
					ColumnDefinition cd3 = grid.ColumnDefinitions[(2 * i) + 2];
					cd3.Width = new GridLength(cd3.Width.Value + cd2.Width.Value + cd1.Width.Value);
										
					grid.ColumnDefinitions.Remove(cd1);
					grid.ColumnDefinitions.Remove(cd2);
					grid.Children.Remove(bookmarkToRemove);
					spacers.Remove(cd1);
					bookmarks.Remove(bookmarkToRemove);
					foreach (Bookmark b in bookmarks)
						Grid.SetColumn(b, bookmarks.IndexOf(b) * 2 + 1);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void ClearBookmarks()
		{
			bookmarks.Clear();
			spacers.Clear();
			grid.ColumnDefinitions.Clear();
			grid.Children.Clear();
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Bookmark_RemoveClicked(object sender, EventArgs e)
		{
			DispatchRemoveClicked(sender, e);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Bookmark_Click(object sender, BookmarkEventArgs e)
		{
			DispatchClicked(sender, e);
		}

		#endregion

		#endregion

		#region Overrides

		/// <summary>
		/// 
		/// </summary>
		/// <param name="finalSize"></param>
		/// <returns></returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			double bmWidth = 4.0;
			double x = 0;
			double y = 0;
			grid.Arrange(new Rect(x, y, finalSize.Width, finalSize.Height));

			double i = 0.0;
			foreach (Bookmark bm in bookmarks)
			{
				double posInPixels = finalSize.Width * (bm.Position / 100);
				
				ColumnDefinition cd = grid.ColumnDefinitions[(2 * bookmarks.IndexOf(bm))];
				grid.ColumnDefinitions[(2 * bookmarks.IndexOf(bm)) + 1].Width = new GridLength(bmWidth);

				Grid.SetColumn(bm, 1+ (bookmarks.IndexOf(bm) * 2));

				if (posInPixels - i - (bmWidth / 2) > 0)
					cd.Width = new GridLength(posInPixels - i - (bmWidth / 2));
				else
					cd.Width = new GridLength(0);

				i += cd.Width.Value + bmWidth;
			}

			return finalSize;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override int VisualChildrenCount { get { return visualChildren.Count; } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		protected override Visual GetVisualChild(int index) { return visualChildren[index]; }

		#endregion

		#region Events

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void DispatchRemoveClicked(object sender, EventArgs e)
		{
			if (RemoveClicked != null)
			{
				RemoveClicked(sender, e);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void DispatchClicked(object sender, BookmarkEventArgs e)
		{
			if (Clicked != null)
			{
				Clicked(sender, e);
			}
		}

		#endregion
	}
}
