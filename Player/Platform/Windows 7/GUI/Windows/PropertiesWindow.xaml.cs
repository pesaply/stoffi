/**
 * PropertiesWindow.xaml.cs
 * 
 * The dialog showing information about one or several
 * tracks.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for PropertiesWindow.xaml
	/// </summary>
	public partial class PropertiesWindow : Window
	{
		#region Members

		/// <summary>
		/// Used to hold the data before writing it to file
		/// </summary>
		private TrackData tempTrack = new TrackData();

		/// <summary>
		/// Holds the list of all properties inside DetailsList
		/// </summary>
		ObservableCollection<PropertyData> properties = new ObservableCollection<PropertyData>();

		#endregion

		#region Properties

		/// <summary>
		/// Gets the tracks that are being viewed
		/// </summary>
		public List<TrackData> Tracks { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the PropertiesWindow window.
		/// </summary>
		/// <param name="tracks">The tracks to load</param>
		public PropertiesWindow(List<TrackData> tracks)
		{
			U.L(LogLevel.Debug, "PROPERTIES", "Initialize");
			InitializeComponent();
			U.L(LogLevel.Debug, "PROPERTIES", "Initialized");
			int size = 16;
			PrevImage.Source = Utilities.GetIcoImage("pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/LeftArrow.ico", size, size);
			NextImage.Source = Utilities.GetIcoImage("pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/RightArrow.ico", size, size);
			PrevImage.Height = size;
			NextImage.Height = size;
			PrevImage.Width = size;
			NextImage.Width = size;
			OK.Visibility = System.Windows.Visibility.Collapsed;
			Apply.Visibility = System.Windows.Visibility.Collapsed;
			Cancel.Content = U.T("ButtonClose", "Content");
			Tracks = new List<TrackData>();
			AddTracks(tracks);

			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName != "")
			{
				Tabs.Background = Brushes.White;
				Background = Brushes.WhiteSmoke;
			}
		}

		#endregion

		#region Methods

		#region Public
		
		/// <summary>
		/// Loads a number of tracks into the window
		/// </summary>
		/// <param name="tracksToAdd">The tracks to load</param>
		public void AddTracks(List<TrackData> tracksToAdd)
		{
			Tracks.Clear();
			Tracks.AddRange(tracksToAdd);

			if (Tracks.Count == 1)
			{
				// load values
				string p = Tracks[0].Path;
				string e = Path.GetExtension(p).Substring(1);
				FileInfo fInfo = new FileInfo(p);
				TagLib.File file = TagLib.File.Create(p, TagLib.ReadStyle.Average);

				// set temp track
				tempTrack.Path = p;

				// set visibilities
				LastPlayed.Visibility = System.Windows.Visibility.Visible;
				CreatedAt.Visibility = System.Windows.Visibility.Visible;
				ModifiedAt.Visibility = System.Windows.Visibility.Visible;
				AccessedAt.Visibility = System.Windows.Visibility.Visible;
				LastPlayedLabel.Visibility = System.Windows.Visibility.Visible;
				CreatedAtLabel.Visibility = System.Windows.Visibility.Visible;
				ModifiedAtLabel.Visibility = System.Windows.Visibility.Visible;
				AccessedAtLabel.Visibility = System.Windows.Visibility.Visible;
				Filename.Visibility = System.Windows.Visibility.Visible;
				Filecount.Visibility = System.Windows.Visibility.Collapsed;
				ArtBackgroundMultiple.Visibility = System.Windows.Visibility.Collapsed;
				ArtBackgroundSingle.Visibility = System.Windows.Visibility.Collapsed;

				// set textblocks
				Filename.Text = Path.GetFileName(p);
				Filetype.Text = String.Format(U.T("PropertiesGeneralTypeFormat"), e.ToUpper(), e);
				Filepath.Text = Path.GetDirectoryName(p);
				Filesize.Text = U.HumanSize(fInfo.Length);
				Codec.Text = Tracks[0].Codecs;
				Bitrate.Text = String.Format(U.T("KilobitsPerSecond"), Tracks[0].Bitrate);
				Channels.Text = Tracks[0].Channels.ToString();
				LastPlayed.Text = Tracks[0].LastPlayed.ToString("g");
				Length.Text = new DurationConverter().Convert(Tracks[0].Length, null, null, null) as string;
				PlayCount.Text = U.T(Tracks[0].PlayCount);
				SampleRate.Text = String.Format(U.T("KiloHertz"), Math.Round(Tracks[0].SampleRate/1000.0, 1));
				CreatedAt.Text = fInfo.CreationTime.ToString();
				ModifiedAt.Text = fInfo.LastWriteTime.ToString();
				AccessedAt.Text = fInfo.LastAccessTime.ToString();

				// set art
				AlbumArt.Source = Utilities.GetImageTag(Tracks[0]);
			}
			else if (Tracks.Count > 1)
			{
				Filename.Visibility = System.Windows.Visibility.Collapsed;
				Filecount.Visibility = System.Windows.Visibility.Visible;

				LastPlayed.Visibility = System.Windows.Visibility.Collapsed;
				CreatedAt.Visibility = System.Windows.Visibility.Collapsed;
				ModifiedAt.Visibility = System.Windows.Visibility.Collapsed;
				AccessedAt.Visibility = System.Windows.Visibility.Collapsed;
				LastPlayedLabel.Visibility = System.Windows.Visibility.Collapsed;
				CreatedAtLabel.Visibility = System.Windows.Visibility.Collapsed;
				ModifiedAtLabel.Visibility = System.Windows.Visibility.Collapsed;
				AccessedAtLabel.Visibility = System.Windows.Visibility.Collapsed;

				TrackData t = Tracks[0];
				string e = Path.GetExtension(t.Path).Substring(1);
				string location = Path.GetDirectoryName(t.Path);
				long size = 0;
				string bitrate = String.Format(U.T("KilobitsPerSecond"), t.Bitrate);
				string channels = t.Channels.ToString();
				double length = 0;
				uint playcount = 0;
				string samplerate = String.Format(U.T("KiloHertz"), Math.Round(t.SampleRate/1000.0, 1));
				string codec = t.Codecs;


				foreach (TrackData track in Tracks)
				{
					FileInfo f = new FileInfo(track.Path);

					size += f.Length;
					length += track.Length;
					playcount += track.PlayCount;

					if (Path.GetExtension(track.Path).Substring(1) != e)
						e = U.T("PropertiesGeneralVariousFormats");

					if (Path.GetDirectoryName(track.Path) != location)
						location = U.T("PropertiesGeneralVariousLocations");

					if (track.Bitrate.ToString() != bitrate)
						bitrate = U.T("PropertiesGeneralVariousBitrates");

					if (track.Channels.ToString() != channels)
						channels = U.T("PropertiesGeneralVariousChannels");

					if (track.SampleRate.ToString() != samplerate)
						samplerate = U.T("PropertiesGeneralVariousSamplingrates");

					if (track.Codecs.ToString() != codec)
						codec = U.T("PropertiesGeneralVariousCodecs");
				}

				if (e != U.T("PropertiesGeneralVariousFormats"))
					Filetype.Text = String.Format(U.T("PropertiesGeneralTypeFormat"), e.ToUpper(), e);
				else
					Filetype.Text = e;

				Filecount.Text = String.Format(U.T("PropertiesMultipleFiles"), Tracks.Count);
				Filepath.Text = location;
				Filesize.Text = U.HumanSize(size);
				Codec.Text = codec;
				Bitrate.Text = bitrate;
				Channels.Text = channels;
				Length.Text = U.TimeSpanToString(new TimeSpan(0, 0, Convert.ToInt32(length)));
				PlayCount.Text = playcount.ToString();
				SampleRate.Text = samplerate;

				AlbumArt.Source = Utilities.GetImageTag(Tracks[0]);
				ArtBackgroundMultiple.Visibility = System.Windows.Visibility.Collapsed;
				ArtBackgroundSingle.Visibility = System.Windows.Visibility.Collapsed;
			}

			FillProperties();
		}

		#endregion

		#region Private

		/// <summary>
		/// Checks if two strings are equal.
		/// An empty string and null are considered equal.
		/// </summary>
		/// <param name="str1">The first string</param>
		/// <param name="str2">The second string</param>
		/// <returns>True if the strings are equal, otherwise false</returns>
		private bool FieldsEqual(string str1, string str2)
		{
			return (str1 == str2 || (str1 == "" && str2 == null) || (str2 == "" && str1 == null));
		}

		/// <summary>
		/// Gets the value of a specific metadata tag given its name
		/// </summary>
		/// <param name="track">The track to retrieve the tag from</param>
		/// <param name="tag">The name of the tag</param>
		/// <returns>The value of the tag</returns>
		private object GetTag(TrackData track, string tag)
		{
			switch (tag)
			{
				case "Artist":
					return track.Artist as object;

				case "Title":
					return track.Title as object;

				case "Album":
					return track.Album as object;

				case "Genre":
					return track.Genre as object;

				case "Track":
					return track.Track as object;

				case "Year":
					if (track.Year == null)
						return null;
					object y = track.Year as object;
					return track.Year as object;

				default:
					return "" as object;
			}
		}

		/// <summary>
		/// Gets the property given its name
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <returns>The PropertyData in properties with the name <paramref name="name"/>.
		/// null if none found.</returns>
		private PropertyData GetProperty(string name)
		{
			foreach (PropertyData p in properties)
				if (name == p.Name)
					return p;
			return null;
		}

		/// <summary>
		/// Gets the property value given its name
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <returns>The value of the PropertyData in properties with the name <paramref name="name"/>.
		/// null if none found.</returns>
		private string GetPropertyValue(string name)
		{
			foreach (PropertyData p in properties)
				if (name == p.Name)
					return p.Value;
			return null;
		}

		/// <summary>
		/// Checks if the tracks have the same value for a given tag
		/// </summary>
		/// <param name="tag">The name of the metadata tag</param>
		/// <returns>Either the string for all tracks value of the field,
		/// or a string describing that the values are different</returns>
		private string CheckIfSame(string tag)
		{
			if (Tracks.Count < 1) return null;

			bool same = true;
			object o = GetTag(Tracks[0], tag);

			foreach (TrackData t in Tracks)
			{
				object p = GetTag(t, tag);
				if ((o != p && o == null) || (o.ToString() != p.ToString()))
				{
					same = false;
					break;
				}
			}

			string s = o == null ? "" : o.ToString();

			return same ? s : U.T("PropertiesDetailsMultipleValues");
		}

		/// <summary>
		/// Loads data into DetailsList
		/// </summary>
		private void FillProperties()
		{
			string title = "";
			string artist = "";
			string album = "";
			string year = "";
			string track = "";
			string genre = "";
			if (Tracks.Count == 1)
			{
				title = Tracks[0].Title;
				artist = Tracks[0].Artist;
				album = Tracks[0].Album;
				year = Tracks[0].Year.ToString();
				track = Tracks[0].Track.ToString();
				genre = Tracks[0].Genre;
			}
			else
			{
				title = CheckIfSame("Title");
				artist = CheckIfSame("Artist");
				album = CheckIfSame("Album");
				year = CheckIfSame("Year");
				track = CheckIfSame("Track");
				genre = CheckIfSame("Genre");
			}

			properties.Clear();
			properties.Add(new PropertyData() { Edited = false, Name = U.T("ColumnTitle"), Value = title });
			properties.Add(new PropertyData() { Edited = false, Name = U.T("ColumnArtist"), Value = artist });
			properties.Add(new PropertyData() { Edited = false, Name = U.T("ColumnAlbum"), Value = album });
			properties.Add(new PropertyData() { Edited = false, Name = U.T("ColumnYear"), Value = year as string });
			properties.Add(new PropertyData() { Edited = false, Name = U.T("ColumnTrack"), Value = track as string });
			properties.Add(new PropertyData() { Edited = false, Name = U.T("ColumnGenre"), Value = genre });
			DetailsList.ItemsSource = properties;

			// load into temporary holder
			tempTrack.Title = title;
			tempTrack.Artist = artist;
			tempTrack.Album = album;
			if (track != U.T("PropertiesDetailsMultipleValues") && track != "")
				tempTrack.Track = Convert.ToUInt32(track);
			else if (track == U.T("PropertiesDetailsMultipleValues"))
				tempTrack.Track = 881102; // this means "multiple values" for integers :)
			if (year != U.T("PropertiesDetailsMultipleValues") && year != "")
				tempTrack.Year = Convert.ToUInt32(year);
			else if (year == U.T("PropertiesDetailsMultipleValues"))
				tempTrack.Year = 881102; // this means "multiple values" for integers :)
			tempTrack.Genre = genre;
		}

		/// <summary>
		/// Saves data from DetailsList into tempTrack
		/// </summary>
		private void SaveProperties()
		{
			foreach (PropertyData p in properties)
			{
				if (p.Edited)
					switch (p.Name)
					{
						case "Artist":
							tempTrack.Artist = p.Value;
							break;

						case "Title":
							tempTrack.Title = p.Value;
							break;

						case "Album":
							tempTrack.Album = p.Value;
							break;

						case "Genre":
							tempTrack.Genre = p.Value;
							break;

						case "Track":
							try
							{
								tempTrack.Track = Convert.ToUInt32(p.Value);
							}
							catch (Exception e)
							{
								MessageBox.Show(e.Message, "Could Not Save Value", MessageBoxButton.OK, MessageBoxImage.Error);
							}
							break;

						case "Year":
							try
							{
								tempTrack.Year = Convert.ToUInt32(p.Value);
							}
							catch (Exception e)
							{
								MessageBox.Show(e.Message, "Could Not Save Value", MessageBoxButton.OK, MessageBoxImage.Error);
							}
							break;
					}
			}
		}

		/// <summary>
		/// Saves all data of tempTrack to the tracks
		/// </summary>
		private void Save()
		{
			SaveProperties();

			foreach (TrackData t in Tracks)
			{
				foreach (PropertyData p in properties)
				{
					if (p.Edited)
					{
						switch (p.Name)
						{
							case "Title":
								t.Title = tempTrack.Title;
								break;

							case "Artist":
								t.Artist = tempTrack.Artist;
								break;

							case "Album":
								t.Album = tempTrack.Album;
								break;

							case "Genre":
								t.Genre = tempTrack.Genre;
								break;

							case "Track":
								t.Track = tempTrack.Track;
								break;
						}
						p.Edited = false;
					}
				}
				try
				{
					FilesystemManager.SaveTrack(t);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, U.T("MessageErrorUpdating", "Title"), MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			if (Tracks.Count == 1)
			{
				string oldPath = Tracks[0].Path;
				string newPath = Path.Combine(Path.GetDirectoryName(oldPath), Filename.Text);
				if (oldPath != newPath)
				{
					try
					{
						File.Move(oldPath, newPath);
						tempTrack.Path = newPath;
					}
					catch (Exception e)
					{
						MessageBox.Show(e.Message, U.T("MessageErrorRenaming", "Title"), MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}

			ToggleButtons();
		}

		/// <summary>
		/// Will show a set of buttons depending on
		/// whether any value has changed.
		/// 
		/// changed:
		/// OK, Cancel, Apply
		/// 
		/// not changed:
		/// Close
		/// </summary>
		private void ToggleButtons()
		{
			bool anyChanged = false;

			// check properties
			foreach (PropertyData p in properties)
			{
				if (p.Edited)
				{
					anyChanged = true;
					break;
				}
			}

			// check filename
			string filename = Path.GetFileName(tempTrack.Path);
			if (Tracks.Count == 1 && filename != Filename.Text)
				anyChanged = true;

			if (anyChanged)
			{
				OK.Visibility = System.Windows.Visibility.Visible;
				Apply.Visibility = System.Windows.Visibility.Visible;
				Cancel.Content = U.T("ButtonCancel", "Content");
			}
			else
			{
				OK.Visibility = System.Windows.Visibility.Collapsed;
				Apply.Visibility = System.Windows.Visibility.Collapsed;
				Cancel.Content = U.T("ButtonClose", "Content");
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user clicks "Next"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>Will load the next track</remarks>
		private void Next_Click(object sender, RoutedEventArgs e)
		{
			OnNext();
		}

		/// <summary>
		/// Invoked when the user clicks "Previous"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>Will load the previous track</remarks>
		private void Previous_Click(object sender, RoutedEventArgs e)
		{
			OnPrev();
		}

		/// <summary>
		/// Invoked when the user clicks "OK"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>Will close the window after saving</remarks>
		private void OK_Click(object sender, RoutedEventArgs e)
		{
			Save();
			Close();
		}

		/// <summary>
		/// Invoked when the user clicks "Apply"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>Will save all data</remarks>
		private void Apply_Click(object sender, RoutedEventArgs e)
		{
			Save();
		}

		/// <summary>
		/// Invoked when the user clicks "Close" or "Cancel"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>Will close the window without saving</remarks>
		private void CloseCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		/// <summary>
		/// Invoked when the user edits the value of a property
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>Will save the text to the temporary structure</remarks>
		private void EditableTextBlock_Edited(object sender, EditableTextBlockEventArgs e)
		{
			EditableTextBlock etb = sender as EditableTextBlock;
			string prop = etb.Tag as string;
			PropertyData p = null;
			foreach (PropertyData pd in properties)
			{
				if (pd.Name == prop)
				{
					p = pd;
					break;
				}
			}

			if (p != null)
			{
				p.Value = e.NewText;
				bool multi = p.Value == U.T("PropertiesDetailsMultipleValues");
				object curObj = GetTag(tempTrack, p.Name);
				string cur = curObj as string;
				p.Edited = 
					curObj == null ||
					(p.Value != curObj.ToString() &&
					!(p.Name == "Track" && tempTrack.Track == 881102 && multi) &&
					!(p.Name == "Year" && tempTrack.Year == 881102 && multi));
			}

			ToggleButtons();
		}

		/// <summary>
		/// Invoked when the user is typing inside the Filename box.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>Will refresh the button set</remarks>
		private void Filename_KeyUp(object sender, KeyEventArgs e)
		{
			ToggleButtons();
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the NextClick event
		/// </summary>
		private void OnNext()
		{
			if (NextClick != null)
				NextClick(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the PreviousClick event
		/// </summary>
		private void OnPrev()
		{
			if (PreviousClick != null)
				PreviousClick(this, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks Next
		/// </summary>
		public event EventHandler NextClick;

		/// <summary>
		/// Occurs when the user clicks Previous
		/// </summary>
		public event EventHandler PreviousClick;

		#endregion
	}

	#region Datastructures

	/// <summary>
	/// Describes a source
	/// </summary>
	public class PropertyData : DependencyObject
	{
		#region Properties

		/// <summary>
		/// Identifies the Name dependency property
		/// </summary>
		public static readonly DependencyProperty NameProperty =
			DependencyProperty.Register("Name", typeof(string),
			typeof(PropertyData), new UIPropertyMetadata(null));

		/// <summary>
		/// Gets or sets the property name
		/// </summary>
		public string Name
		{
			get { return (string)GetValue(NameProperty); }
			set { SetValue(NameProperty, value); }
		}

		/// <summary>
		/// Identifies the Value dependency property
		/// </summary>
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register("Value", typeof(string),
			typeof(PropertyData), new UIPropertyMetadata(null));

		/// <summary>
		/// Gets or sets the property value
		/// </summary>
		public string Value
		{
			get { return (string)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		/// <summary>
		/// Gets or sets whether the property has been edited
		/// </summary>
		public bool Edited { get; set; }

		#endregion
	}

	#endregion

	#region Converters

	/// <summary>
	/// A converter between bool and visibility (visible or hidden)
	/// </summary>
	public class BoolToVisibilityConverter : IValueConverter
	{
		/// <summary>
		/// Converts two bool values (<paramref name="value"/> and <paramref name="parameter"/>)
		/// to a visibility value. If the two bool values are equal it will return Visible,
		/// if they are not it will return Hidden.
		/// </summary>
		/// <param name="value">The bool value</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">The string representation of a bool</param>
		/// <param name="culture">The culture (not used)</param>
		/// <returns>Visible if value and parameter are equal, otherwise Hidden</returns>
		public object Convert(object value, Type targetType,
			object parameter, System.Globalization.CultureInfo culture)
		{
			bool param = bool.Parse(parameter as string);
			bool val = (bool)value;

			return val == param ?
			  Visibility.Visible : Visibility.Hidden;
		}

		/// <summary>
		/// This function is not implemented and will throw an exception if used.
		/// </summary>
		/// <param name="value">(not used)</param>
		/// <param name="targetType">(not used)</param>
		/// <param name="parameter">(not used)</param>
		/// <param name="culture">(not used)</param>
		/// <returns>(nothing)</returns>
		public object ConvertBack(object value, Type targetType,
			object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	#endregion
}
