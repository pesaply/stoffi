/***
 * Playlist.cs
 * 
 * Describe a playlist.
 * Both static playlists with a fixed set of tracks, or a dynamic playlist
 * based on a filter which is used to decide which tracks should or shouldn't
 * be added to the playlist.
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

using Stoffi.Core.Media;
using Stoffi.Core.Settings;

namespace Stoffi.Core.Playlists
{
	/// <summary>
	/// Describes a playlist.
	/// </summary>
	public class Playlist : PropertyChangedBase
	{
		#region Members

		private string name;
		private uint id;
		private double time = 0;
		private uint ownerID;
		private string ownerName;
		private DateTime ownerCacheTime;
		private string filter;
		private ObservableCollection<Track> tracks = new ObservableCollection<Track>();
		private ListConfig listConfig = new ListConfig();

		#endregion

		#region Properties

		/// <summary>
		/// Gets the identifier for this playlist in the navigation tree.
		/// </summary>
		/// <value>The navigation I.</value>
		public string NavigationID
		{
			get { return String.Format ("Playlist:{0}:{1}", Name, ID); }
		}

		/// <summary>
		/// Get or sets the name of the playlist
		/// </summary>
		public string Name
		{
			get { return name; }
			set { SetProp<string> (ref name, value, "Name"); }
		}

		/// <summary>
		/// Gets or sets the ID of the playlist in the cloud.
		/// </summary>
		public uint ID
		{
			get { return id; }
			set { SetProp<uint> (ref id, value, "ID"); }
		}

		/// <summary>
		/// Get or sets the combined time of all tracks
		/// </summary>
		public double Time
		{
			get { return time; }
			set { SetProp<double> (ref time, value, "Time"); }
		}

		/// <summary>
		/// Get or sets the ID of the user who owns the playlist in the cloud.
		/// </summary>
		public uint OwnerID
		{
			get { return ownerID; }
			set { SetProp<uint> (ref ownerID, value, "OwnerID"); }
		}

		/// <summary>
		/// Get or sets the name of the user who owns the playlist in the cloud.
		/// </summary>
		public string OwnerName
		{
			get { return ownerName; }
			set { SetProp<string> (ref ownerName, value, "OwnerName"); }
		}

		/// <summary>
		/// Get or sets the time when OwnerName was checked and cached.
		/// </summary>
		public DateTime OwnerCacheTime
		{
			get { return ownerCacheTime; }
			set { SetProp<DateTime> (ref ownerCacheTime, value, "OwnerCacheTime"); }
		}

		/// <summary>
		/// Get or sets the filter for automatic adding/removing of songs.
		/// </summary>
		/// <remarks>
		/// If null then the playlist is not dynamic.
		/// </remarks>
		public string Filter
		{
			get { return filter; }
			set { SetProp<string> (ref filter, value, "Filter"); }
		}

		/// <summary>
		/// Gets or sets the collection of tracks of the playlist
		/// </summary>
		public ObservableCollection<Track> Tracks
		{
			get { return tracks; }
			set
			{
				if (tracks != null)
					tracks.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<Track>> (ref tracks, value, "Tracks");
				if (tracks != null) {
					foreach (var track in tracks) {
						track.PropertyChanged -= Track_PropertyChanged;
						track.PropertyChanged += Track_PropertyChanged;
					}
					tracks.CollectionChanged += CollectionChanged;
				}
			}
		}

		/// <summary>
		/// Get or sets the configuration of the list view
		/// </summary>
		public ListConfig ListConfig
		{
			get { return listConfig; }
			set
			{
				if (listConfig != null)
					listConfig.PropertyChanged -= ListConfig_PropertyChanged;
				SetProp<ListConfig> (ref listConfig, value, "ListConfig");
				if (listConfig != null)
					listConfig.PropertyChanged += ListConfig_PropertyChanged;
			}
		}

		/// <summary>
		/// Gets the type.
		/// </summary>
		/// <value>The type.</value>
		public PlaylistType Type
		{
			get
			{
				if (String.IsNullOrWhiteSpace(Filter))
					return PlaylistType.Standard;
				else
					return PlaylistType.Dynamic;
			}
		}

		/// <summary>
		/// Gets if a playlist belongs to someone else.
		/// </summary>
		/// <returns>true if someone else is the owner of the playlist, otherwise false</returns>
		public bool IsSomeoneElses
		{
			get
			{
				var id = Services.Manager.Identity;
				return OwnerID > 0 && (id == null || id.UserID != OwnerID);
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.Playlist"/> class.
		/// </summary>
		public Playlist()
		{
			tracks.CollectionChanged += CollectionChanged;
			listConfig.PropertyChanged += ListConfig_PropertyChanged;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Invoked when a property of a setting changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Track_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnTrackChanged (e.PropertyName, sender as Track);
		}

		/// <summary>
		/// Invoked when a property of the list config changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void ListConfig_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged ("ListConfig");
		}

		/// <summary>
		/// Invoked when a collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if ((ObservableCollection<Track>)sender == tracks && tracks != null)
			{
				if (e.Action == NotifyCollectionChangedAction.Remove)
					foreach (Track t in e.OldItems)
						t.PropertyChanged -= Track_PropertyChanged;
				else if (e.Action == NotifyCollectionChangedAction.Add)
					foreach (Track t in e.NewItems)
						t.PropertyChanged += Track_PropertyChanged;
				OnPropertyChanged ("Tracks");
			}
		}

		/// <summary>
		/// Raises the TrackChanged event.
		/// </summary>
		/// <param name="propertyName">Property name.</param>
		/// <param name="track">Track.</param>
		private void OnTrackChanged(string propertyName, Track track)
		{
			if (TrackChanged != null)
				TrackChanged (this, new TrackChangedEventArgs (propertyName, track));
		}

		/// <summary>
		/// Add tracks to the playlist.
		/// </summary>
		/// <param name="tracks">The list of tracks to be added</param>
		/// <param name="pos">The position to insert the track at (-1 means at the end)</param>
		public void Add(IEnumerable<object> tracks, int pos = -1)
		{
			if (IsSomeoneElses) return;
			if (pos < 0)
				pos = Tracks.Count;
			try
			{
				foreach (Track track in tracks)
				{
					// check if track is already added
					var index = -1;
					foreach (var t in Tracks)
					{
						if (U.Equal(t.Path, track.Path))
						{
							index = Tracks.IndexOf(t);
							break;
						}
					}

					// not added: insert
					if (index < 0)
					{
						if (pos < 0 || pos >= Tracks.Count)
							Tracks.Add(track);
						else
							Tracks.Insert(pos, track);
						track.Source = "Playlist:" + Name;
					}

					// already added: move
					else if (pos != index)
					{
						if (pos < 0 || pos >= Tracks.Count)
						{
							Tracks.RemoveAt(index);
							Tracks.Add(track);
						}
						else
							Tracks.Move(index, pos);
					}
					pos++;
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "Playlist", "Could not add tracks to playlist: " + e.Message);
			}
		}

		/// <summary>
		/// Remove tracks from a playlist if they are found inside the playlist
		/// </summary>
		/// <param name="tracks">The list of tracks to be removed</param>
		public void Remove(IEnumerable<Track> tracks)
		{
			if (IsSomeoneElses) return;
			try
			{
				foreach (Track track in tracks)
					foreach (Track trackInPlaylist in Tracks)
						if (trackInPlaylist.Path == track.Path)
						{
							Tracks.Remove(trackInPlaylist);
							break;
						}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "Playlist", "Could not remove tracks from playlist: " + e.Message);
			}
		}

		/// <summary>
		/// Saves the playlist as a file
		/// </summary>
		/// <param name="path">The path of the saved playlist</param>
		/// <param name="name">The name of the playlist to save</param>
		public void Save(string path)
		{
			foreach (var parser in Playlists.Manager.GetParsers (path))
			{
				try
				{
					if (File.Exists (path))
						File.Delete (path);
					parser.Write (this, path);
					break;
				}
				catch (Exception e)
				{
					U.L (LogLevel.Debug, "Playlist", String.Format("Parser {0} is not appropriate because {1}", parser.GetType().ToString(), e.Message));
				}
			}
			U.L (LogLevel.Warning, "Playlist", "Could not find appropriate parser for " + path);
		}

		/// <summary>
		/// Updates the tracks of a dynamic playlist.
		/// </summary>
		/// <param name="playlist">The playlist to refresh</param>
		/// <param name="tracks">The tracks to search in (default the whole file collection)</param>
		public void Refresh(IEnumerable tracks = null)
		{
			try
			{
				if (String.IsNullOrWhiteSpace(Filter))
					return;

				// remove non-matching tracks from playlist
				var tracksToRemove = new List<Track>();
				foreach (var track in Tracks)
					if (!U.TrackMatchesQuery(track, Filter))
						tracksToRemove.Add(track);
				Remove(tracksToRemove);

				// add matching tracks from collection
				if (tracks == null)
					tracks = Settings.Manager.FileTracks;

				var tracksToAdd = new List<Track>();
				foreach (Track track in tracks)
					if (U.TrackMatchesQuery(track, Filter))
						tracksToAdd.Add(track);
				Add(tracksToAdd);

			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "Playlist", "Could not refresh dynamic playlist: " + e.Message);
			}
		}

		#endregion

		#region Events

		public event EventHandler<TrackChangedEventArgs> TrackChanged;

		#endregion
	}

	/// <summary>
	/// Represents the type of a playlist.
	/// </summary>
	public enum PlaylistType
	{
		/// <summary>
		/// A normal playlist with manually added songs.
		/// </summary>
		Standard,

		/// <summary>
		/// An auto updating playlist based on a search filter.
		/// </summary>
		Dynamic,

		/// <summary>
		/// Unknown type (for example if the playlist is null).
		/// </summary>
		Unknown
	}
}

