/**
 * GeneratePlaylist.xaml.cs
 * 
 * Contains the logic for the dialog allowing the user to
 * generate a playlist.
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for CreateRandomPlaylist.xaml
	/// </summary>
	public partial class GeneratePlaylist : Window
	{
		#region Members

		private ViewDetailsSearchDelegate filterMatches = null;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a dialog for generating a random playlist.
		/// </summary>
		/// <param name="filterFunc">The function which determines if a track matches a filter.</param>
		public GeneratePlaylist(ViewDetailsSearchDelegate filterFunc)
		{
			filterMatches = filterFunc;
			InitializeComponent();

			foreach (PlaylistData p in SettingsManager.Playlists)
			{
				Lists.Items.Add(new ComboBoxItem() { Content = p.Name });
			}

			if (SettingsManager.CurrentSelectedNavigation.StartsWith("Playlist:"))
			{
				string name = SettingsManager.CurrentSelectedNavigation.Split(new[] { ':' }, 2)[1];
				foreach (ComboBoxItem cbi in Lists.Items)
					if ((string)cbi.Content == name)
					{
						cbi.IsSelected = true;
						break;
					}
			}
			else
			{
				Lists.SelectedIndex = 0;
				Lists_SelectionChanged(null, null);
			}
		}

		#endregion

		#region Methods

		#region Private

		/// <summary>
		/// Gets the corresponding collection of tracks given the selected list.
		/// </summary>
		/// <returns>The collection of tracks corresponding to the selected list</returns>
		private ObservableCollection<TrackData> GetTracks()
		{
			if (Lists.SelectedIndex > 0)
			{
				ComboBoxItem cbi = Lists.SelectedItem as ComboBoxItem;
				PlaylistData p = PlaylistManager.FindPlaylist((string)cbi.Content);
				if (p != null)
					return p.Tracks;
				else
					return new ObservableCollection<TrackData>();
			}
			else
				return SettingsManager.FileTracks;
		}

		/// <summary>
		/// Gets the corresponding filter given the selected list.
		/// </summary>
		/// <returns>The filter corresponding to the selected list</returns>
		private string GetFilter()
		{
			if (Lists.SelectedIndex > 0)
			{
				ComboBoxItem cbi = Lists.SelectedItem as ComboBoxItem;
				PlaylistData p = PlaylistManager.FindPlaylist((string)cbi.Content);
				if (p != null)
					return p.ListConfig.Filter;
				else
					return "";
			}
			else
				return SettingsManager.FileListConfig.Filter;
		}

		/// <summary>
		/// Tries to turn the number into an integer.
		/// If conversion fails or number is too large or too
		/// small -1 is returned and the proper error message is
		/// displayed.
		/// </summary>
		private int GetNumber()
		{
			string txt = Number.Text;
			try
			{
				int n = Convert.ToInt32(txt);
				int m = GetTracks().Count;
				if (n < 1)
				{
					ErrorMessage.Text = String.Format(U.T("DialogNumberTooSmall"), 0);
					ErrorMessage.Visibility = System.Windows.Visibility.Visible;
					return -1;
				}
				if (n > m)
				{
					ErrorMessage.Text = String.Format(U.T("DialogNumberTooLarge"), m+1);
					ErrorMessage.Visibility = System.Windows.Visibility.Visible;
					return -1;
				}
				else
					return n;
			}
			catch
			{
				ErrorMessage.Text = U.T("DialogNumberInvalid");
				ErrorMessage.Visibility = System.Windows.Visibility.Visible;
				return -1;
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user clicks "Generate".
		/// </summary>
		/// <remarks>
		/// Will verify the name and generate a playlist.
		/// </remarks>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Generate_Click(object sender, RoutedEventArgs e)
		{
			Regex alphaNumPattern = new Regex("[^a-zA-Z0-9 ]");
			if (alphaNumPattern.IsMatch(ListName.Text))
			{
				ErrorMessage.Text = U.T("DialogNameInvalidError");
				ErrorMessage.Visibility = System.Windows.Visibility.Visible;
			}
			else if (ListName.Text == "")
			{
				ErrorMessage.Text = U.T("DialogNameEmptyError");
				ErrorMessage.Visibility = System.Windows.Visibility.Visible;
			}
			else if (PlaylistManager.FindPlaylist(ListName.Text) != null)
			{
				ErrorMessage.Text = U.T("DialogNameExistsError");
				ErrorMessage.Visibility = System.Windows.Visibility.Visible;
			}
			else
			{
				// copy tracks to temporary list
				List<TrackData> tracks = new List<TrackData>();
				string filter = GetFilter();
				foreach (TrackData t in GetTracks())
					if (!(bool)DoFilter.IsChecked || filterMatches(t, filter))
						tracks.Add(t);

				int n = GetNumber();
				if (n < 0) return;

				if (tracks.Count > 0)
				{
					// create empty playlist
					PlaylistData p = PlaylistManager.CreatePlaylist(ListName.Text);
					if (p != null)
					{
						// move tracks from temporary list into playlist
						for (int i = 0; i < n && tracks.Count > 0; i++)
						{
							Random r = new Random();
							int x = r.Next(tracks.Count - 1);
							TrackData t = tracks[x];
							p.Tracks.Add(t);
							tracks.RemoveAt(x);
						}
					}
				}

				Close();
			}
		}

		/// <summary>
		/// Invoked when the user selects a list.
		/// </summary>
		/// <remarks>
		/// Will fill the Number control with 1 through "number of tracks in list"
		/// </remarks>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Lists_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Number == null) return;
			Number.Text = GetTracks().Count.ToString();
		}

		#endregion

		#endregion
	}
}
