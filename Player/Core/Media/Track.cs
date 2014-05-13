/***
 * Track.cs
 * 
 * Represents a track, either a file or a stream, which can be played.
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
 ***/

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;

using Stoffi.Core.Settings;

namespace Stoffi.Core.Media
{
	/// <summary>
	/// Describes a track.
	/// </summary>
	public class Track : ListItem
	{
		#region Members

		private string artist;
		private string album;
		private string title;
		private string genre;
		private string path;
		private uint track;
		private uint year;
		private double length;
		private uint userPlayCount;
		private ulong globalPlayCount;
		private DateTime lastPlayed;
		private string url;
		private string originalArtURL;
		private bool processed = false;
		private long lastWrite = 0;
		private string codecs;
		private int channels;
		private int bitrate;
		private int sampleRate;
		private string source;
		private ObservableCollection<Tuple<string,double>> bookmarks = new ObservableCollection<Tuple<string,double>>();

		/// <summary>
		/// The difference in time when Length is changed
		/// </summary>
		public int diff = 0;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the artist of the track.
		/// </summary>
		public string Artist
		{
			get { return artist; }
			set { SetProp<string> (ref artist, value, "Artist"); }
		}

		/// <summary>
		/// Gets or sets the title of the track.
		/// </summary>
		public string Title
		{
			get { return title; }
			set { SetProp<string> (ref title, value, "Title"); }
		}

		/// <summary>
		/// Gets or sets the album of the track.
		/// </summary>
		public string Album
		{
			get { return album; }
			set { SetProp<string> (ref album, value, "Album"); }
		}

		/// <summary>
		/// Gets or sets the genre of the track.
		/// </summary>
		public string Genre
		{
			get { return genre; }
			set { SetProp<string> (ref genre, value, "Genre"); }
		}

		/// <summary>
		/// Gets or sets the number of the track on the album.
		/// </summary>
		public uint TrackNumber
		{
			get { return track; }
			set { SetProp<uint> (ref track, value, "TrackNumber"); }
		}

		/// <summary>
		/// Gets or sets the year the track was made.
		/// </summary>
		public uint Year
		{
			get { return year; }
			set { SetProp<uint> (ref year, value, "Year"); }
		}

		/// <summary>
		/// Gets or sets the length of the track in seconds.
		/// </summary>
		public double Length
		{
			get { return length; }
			set { diff = (int)(value - length); SetProp<double> (ref length, value, "Length"); diff = 0; }
		}

		/// <summary>
		/// Gets or sets the path to the track.
		/// </summary>
		public string Path
		{
			get { return path; }
			set { SetProp<string> (ref path, value, "Path"); }
		}

		/// <summary>
		/// Gets or sets the number of times that the track has been played.
		/// </summary>
		public uint PlayCount
		{
			get { return userPlayCount; }
			set { SetProp<uint> (ref userPlayCount, value, "PlayCount"); }
		}

		/// <summary>
		/// Gets or sets the URL of the track.
		/// Only applicable on streamable tracks.
		/// </summary>
		public string URL
		{
			get { return url; }
			set { SetProp<string> (ref url, value, "URL"); }
		}

		/// <summary>
		/// Gets or sets the amount of views on YouTube.
		/// </summary>
		public ulong Views
		{
			get { return globalPlayCount; }
			set { SetProp<ulong> (ref globalPlayCount, value, "Views"); }
		}

		/// <summary>
		/// Gets or sets the time the track was last played (in epoch time).
		/// </summary>
		public DateTime LastPlayed
		{
			get { return lastPlayed; }
			set { SetProp<DateTime> (ref lastPlayed, value, "LastPlayed"); }
		}

		/// <summary>
		/// Gets or sets the URL/path to the album art.
		/// </summary>
		public string ArtURL
		{
			get { return image; }
			set { SetProp<string> (ref image, value, "ArtURL"); }
		}

		/// <summary>
		/// Gets or sets the URL to the album art at the original host instead of a potentially cached version.
		/// </summary>
		public string OriginalArtURL
		{
			get { return originalArtURL; }
			set { SetProp<string> (ref originalArtURL, value, "OriginalArtURL"); }
		}

		/// <summary>
		/// Gets or sets whether the track has been scanned for meta data.
		/// </summary>
		public bool Processed
		{
			get { return processed; }
			set { SetProp<bool> (ref processed, value, "Processed"); }
		}

		/// <summary>
		/// Gets or sets the time that the file was last written/updated.
		/// </summary>
		public long LastWrite
		{
			get { return lastWrite; }
			set { SetProp<long> (ref lastWrite, value, "LastWrite"); }
		}

		/// <summary>
		/// Gets or sets the bitrate of the track.
		/// </summary>
		public int Bitrate
		{
			get { return bitrate; }
			set { SetProp<int> (ref bitrate, value, "Bitrate"); }
		}

		/// <summary>
		/// Gets or sets the number of channels of the track.
		/// </summary>
		public int Channels
		{
			get { return channels; }
			set { SetProp<int> (ref channels, value, "Channels"); }
		}

		/// <summary>
		/// Gets or sets the sample rate of the track.
		/// </summary>
		public int SampleRate
		{
			get { return sampleRate; }
			set { SetProp<int> (ref sampleRate, value, "SampleRate"); }
		}

		/// <summary>
		/// Gets or sets the codecs of the track.
		/// </summary>
		public string Codecs
		{
			get { return codecs; }
			set { SetProp<string> (ref codecs, value, "Codecs"); }
		}

		/// <summary>
		/// Gets or sets where the track belongs to ("Files", "Playlist:Name").
		/// </summary>
		public string Source
		{
			get { return source; }
			set { SetProp<string> (ref source, value, "Source"); }
		}

		/// <summary>
		/// Gets or sets the bookmarks of the track (percentage).
		/// </summary>
		public ObservableCollection<Tuple<string,double>> Bookmarks
		{
			get { return bookmarks; }
			set
			{
				if (bookmarks != null)
					bookmarks.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<Tuple<string,double>>> (ref bookmarks, value, "Bookmarks");
				if (bookmarks != null)
					bookmarks.CollectionChanged += CollectionChanged;
			}
		}

		/// <summary>
		/// Gets the type of track.
		/// </summary>
		/// <value>The type.</value>
		public TrackType Type
		{
			get
			{
				return Track.GetType (Path);
			}
		}

		/// <summary>
		/// Gets the icon of the track.
		/// </summary>
		/// <value>The icon.</value>
		public new string Icon
		{
			get
			{
				switch (Type)
				{
					case TrackType.File:
						return "fileaudio";

					case TrackType.Jamendo:
						return "jamendo";

					case TrackType.SoundCloud:
						return "soundcloud";

					case TrackType.Unknown:
						return "unknown";

					case TrackType.WebRadio:
						return "radio";

					case TrackType.YouTube:
						return "youtube";
				}
				return "unsupported";
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.Track"/> class.
		/// </summary>
		public Track()
		{
			bookmarks.CollectionChanged += CollectionChanged;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Invoked when a collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if ((ObservableCollection<Tuple<string,double>>)sender == bookmarks && bookmarks != null)
				OnPropertyChanged ("Bookmarks");
		}

		/// <summary>
		/// Gets the track type of a path.
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>The type of the given track path</returns>
		public static TrackType GetType(string path)
		{
			if (String.IsNullOrEmpty (path))
				return TrackType.Unknown;

			else if (Regex.IsMatch (path, @"^https?://", RegexOptions.IgnoreCase))
			{
				if (Regex.IsMatch (path, @"https?://[^\?]+/[^\?]+\.\w{1,5}(\?.*)?$", RegexOptions.IgnoreCase))
					return TrackType.File;
				return TrackType.WebRadio;
			}

			else if (Sources.Manager.YouTube.IsFromHere(path))
				return TrackType.YouTube;

			else if (Sources.Manager.SoundCloud.IsFromHere(path))
				return TrackType.SoundCloud;

			else if (Sources.Manager.Jamendo.IsFromHere(path))
				return TrackType.Jamendo;

			else
				return TrackType.File;
		}

		#endregion
	}

	/// <summary>
	/// Represents the type of a track.
	/// </summary>
	public enum TrackType
	{
		/// <summary>
		/// A local or remote audio file.
		/// </summary>
		File,

		/// <summary>
		/// A radio stream over the web.
		/// </summary>
		WebRadio,

		/// <summary>
		/// A YouTube video clip.
		/// </summary>
		YouTube,

		/// <summary>
		/// A SoundCloud track.
		/// </summary>
		SoundCloud,

		/// <summary>
		/// A Jamendo track.
		/// </summary>
		Jamendo,

		/// <summary>
		/// An unknown track type
		/// </summary>
		Unknown
	}

	/// <summary>
	/// Holds the data for the TrackChanged event.
	/// </summary>
	public class TrackChangedEventArgs : EventArgs
	{
		#region Properties

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string PropertyName { get; private set; }

		/// <summary>
		/// Gets the track that changed
		/// </summary>
		public Track Track { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Create a new instance of the TarckChangedEventArgs class
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <param name="track">The track that changed</param>
		public TrackChangedEventArgs(string name, Track track)
		{
			PropertyName = name;
			Track = track;
		}

		#endregion
	}
}

