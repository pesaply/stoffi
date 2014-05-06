/**
 * Associations.xaml.cs
 * 
 * Contains the logic for a dialog allowing the user to select
 * which files the application should associate itself with.
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
using System.Windows.Shapes;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for Associations.xaml
	/// </summary>
	public partial class Associations : Window
	{
		#region Members

		#endregion

		#region Properties

		/// <summary>
		/// Gets the song assocation checkboxes.
		/// </summary>
		public SortedList<string, CheckBox> SongAssociations { get; private set; }

		/// <summary>
		/// Gets the playlist association checkboxes.
		/// </summary>
		public SortedList<string, CheckBox> PlaylistAssociations { get; private set; }

		/// <summary>
		/// Gets the "Other" assocation checkboxes.
		/// </summary>
		public SortedList<string, CheckBox> MiscAssociations { get; private set; }

		/// <summary>
		/// Gets the list of file type associations that has been checked.
		/// </summary>
		public List<string> FileList
		{
			get
			{
				List<string> l = new List<string>();
				foreach (KeyValuePair<string,CheckBox> item in SongAssociations)
					if ((bool)item.Value.IsChecked)
						l.Add((string)item.Value.Tag);
				foreach (KeyValuePair<string, CheckBox> item in PlaylistAssociations)
					if ((bool)item.Value.IsChecked)
						l.Add((string)item.Value.Tag);
				foreach (KeyValuePair<string, CheckBox> item in MiscAssociations)
					if ((bool)item.Value.IsChecked)
						l.Add((string)item.Value.Tag);
				return l;
			}
		}

		/// <summary>
		/// Gets the list of all possible file type associations.
		/// </summary>
		public List<string> FullFileList
		{
			get
			{
				List<string> l = new List<string>();
				foreach (KeyValuePair<string, CheckBox> item in SongAssociations)
					l.Add((string)item.Value.Tag);
				foreach (KeyValuePair<string, CheckBox> item in PlaylistAssociations)
					l.Add((string)item.Value.Tag);
				foreach (KeyValuePair<string, CheckBox> item in MiscAssociations)
					l.Add((string)item.Value.Tag);
				return l;
			}
		}

		#endregion

		/// <summary>
		/// Creates an instance of this class.
		/// </summary>
		public Associations()
		{
			InitializeComponent();
			SongAssociations = new SortedList<string, CheckBox>();
			SongAssociations.Add("AAC", AAC);
			SongAssociations.Add("AC3", AC3);
			SongAssociations.Add("AIF", AIF);
			SongAssociations.Add("AIFF", AIFF);
			SongAssociations.Add("APE", APE);
			SongAssociations.Add("APL", APL);
			SongAssociations.Add("BWF", BWF);
			SongAssociations.Add("FLAC", FLAC);
			SongAssociations.Add("M1A", M1A);
			SongAssociations.Add("M2A", M2A);
			SongAssociations.Add("MPPlus", MPPlus);
			SongAssociations.Add("MP1", MP1);
			SongAssociations.Add("MP2", MP2);
			SongAssociations.Add("MP3", MP3);
			SongAssociations.Add("MP3Pro", MP3Pro);
			SongAssociations.Add("MPA", MPA);
			SongAssociations.Add("MPC", MPC);
			SongAssociations.Add("MPP", MPP);
			SongAssociations.Add("MUS", MUS);
			SongAssociations.Add("OFR", OFR);
			SongAssociations.Add("OFS", OFS);
			SongAssociations.Add("OGG", OGG);
			SongAssociations.Add("SPX", SPX);
			SongAssociations.Add("TTA", TTA);
			SongAssociations.Add("WAV", WAV);
			SongAssociations.Add("WV", WV);

			PlaylistAssociations = new SortedList<string, CheckBox>();
			PlaylistAssociations.Add("PLS", PLS);
			PlaylistAssociations.Add("M3U", M3U);

			MiscAssociations = new SortedList<string, CheckBox>();
			MiscAssociations.Add("SPP", SPP);
			MiscAssociations.Add("SCX", SCX);
		}

		/// <summary>
		/// Invoked when the user checks/unchecks the Songs box.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Songs_Click(object sender, RoutedEventArgs e)
		{
			foreach (CheckBox cb in SongAssociations.Values)
				cb.IsChecked = Songs.IsChecked;
		}

		/// <summary>
		/// Invoked when the user checks/unchecks a song.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Song_Click(object sender, RoutedEventArgs e)
		{
			bool? check = AAC.IsChecked;
			foreach (CheckBox cb in SongAssociations.Values)
				if (check != cb.IsChecked)
					return;
			Songs.IsChecked = check;
		}

		/// <summary>
		/// Invoked when the user checks/unchecks the Playlists box.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Playlists_Click(object sender, RoutedEventArgs e)
		{
			foreach (CheckBox cb in PlaylistAssociations.Values)
				cb.IsChecked = Playlists.IsChecked;
		}

		/// <summary>
		/// Invoked when the user checks/unchecks a playlist.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Playlist_Click(object sender, RoutedEventArgs e)
		{
			bool? check = PLS.IsChecked;
			foreach (CheckBox cb in PlaylistAssociations.Values)
				if (check != cb.IsChecked)
					return;
			Playlists.IsChecked = check;
		}

		/// <summary>
		/// Invoked when the user checks/unchecks the Other box.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Others_Click(object sender, RoutedEventArgs e)
		{
			foreach (CheckBox cb in MiscAssociations.Values)
				cb.IsChecked = Other.IsChecked;
		}

		/// <summary>
		/// Invoked when the user checks/unchecks a box under Other.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Other_Click(object sender, RoutedEventArgs e)
		{
			bool? check = SPP.IsChecked;
			foreach (CheckBox cb in MiscAssociations.Values)
				if (check != cb.IsChecked)
					return;
			Other.IsChecked = check;
		}

		/// <summary>
		/// Invoked when the user clicks the Learn more link.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void LearnMore_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			System.Diagnostics.Process.Start("http://dev.stoffiplayer.com/wiki/Formats");
		}

		/// <summary>
		/// Invoked when the user clicks the OK button.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void OK_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
