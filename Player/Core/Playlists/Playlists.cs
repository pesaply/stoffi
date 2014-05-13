/**
 * Playlist.cs
 * 
 * Takes care of managing the playlists.
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

using Stoffi.Core.Media;
using Stoffi.Core.Settings;
using Stoffi.Core.Sources;

using Newtonsoft.Json.Linq;

namespace Stoffi.Core.Playlists
{
	/// <summary>
	/// Represents a manager that takes care of the playlist logic
	/// </summary>
	public static class Manager
	{
		#region Members
		private static List<Parser> parsers = new List<Parser>();
		private static string defaultName = "Playlist";
		#endregion

		#region Properties

		/// <summary>
		/// The currently active playlist that is being played.
		/// An empty string if no playlist is active.
		/// </summary>
		public static String CurrentPlaylist { get; set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes the <see cref="Stoffi.Core.Playlists.Manager"/> class.
		/// </summary>
		static Manager()
		{
			parsers.Add (new Parsers.PLS ());
			parsers.Add (new Parsers.M3U ());
			parsers.Add (new Parsers.WindowsMediaMetafile ());
			parsers.Add (new Parsers.iTunes ());
			parsers.Add (new Parsers.XSPF ());
			parsers.Add (new Parsers.WPL ());
			parsers.Add (new Parsers.B4S ());
			parsers.Add (new Parsers.RAM ());
			parsers.Add (new Parsers.Cloud ());
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initializes the playlist manager
		/// </summary>
		public static void Initialize()
		{
			CurrentPlaylist = "";

			bool allTracksNull = true;
			foreach (Playlist playlist in Settings.Manager.Playlists)
			{
				playlist.Tracks.CollectionChanged += TracksChanged;
				playlist.PropertyChanged += new PropertyChangedEventHandler(Playlist_PropertyChanged);
				DispatchPlaylistModified(playlist, ModifyType.Created);
				Services.Manager.RefreshPlaylist(playlist);

				allTracksNull = true;
				for (int i = 0; i < playlist.Tracks.Count && allTracksNull; i++)
					allTracksNull = playlist.Tracks[i] == null;
				if (allTracksNull)
					playlist.Tracks.Clear();
			}

			if (Settings.Manager.CurrentActiveNavigationIsPlaylist) {
				var p = GetActive ();

				// fallback if playlist doesn't exist
				if (p == null)
					Settings.Manager.CurrentActiveNavigation = Settings.Manager.DefaultNavigation;
				else
					CurrentPlaylist = p.Name;
			}
			
			// fallback if playlist doesn't exist
			if (Settings.Manager.CurrentSelectedNavigationIsPlaylist && GetSelected () == null)
				Settings.Manager.CurrentSelectedNavigation = Settings.Manager.DefaultNavigation;

			Settings.Manager.FileTracks.CollectionChanged += new NotifyCollectionChangedEventHandler(FileTracks_CollectionChanged);
			Settings.Manager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(Settings_PropertyChanged);
		}

		/// <summary>
		/// Gets the playlist currently selected in the navigation pane.
		/// </summary>
		/// <returns>The selected playlist if found, otherwise null</returns>
		public static Playlist GetSelected()
		{
			return GetFromNavigation (Settings.Manager.CurrentSelectedNavigation);
		}

		/// <summary>
		/// Gets the playlist currently active in the navigation pane.
		/// </summary>
		/// <returns>The selected playlist if found, otherwise null</returns>
		public static Playlist GetActive()
		{
			return GetFromNavigation (Settings.Manager.CurrentActiveNavigation);
		}

		/// <summary>
		/// Get the playlist specified by the navigation identifier.
		/// </summary>
		/// <returns>The playlist if found, otherwise null.</returns>
		/// <param name="navigation">The identifier of the navigation item</param>
		public static Playlist GetFromNavigation(string navigation)
		{
			foreach (var playlist in Settings.Manager.Playlists)
				if (playlist.NavigationID == navigation)
					return playlist;
			return null;
		}

		/// <summary>
		/// Add tracks to a playlist
		/// </summary>
		/// <param name="tracks">The list of tracks to be added</param>
		/// <param name="playlistName">The name of the playlist to add the tracks to</param>
		/// <param name="pos">The position to insert the track at (-1 means at the end)</param>
		public static void AddToPlaylist(IEnumerable<object> tracks, string playlistName, int pos = -1)
		{
			Playlist playlist = Get(playlistName);
			if (playlist != null)
				playlist.Add (tracks, pos);
		}

		/// <summary>
		/// Check to see if a file is a supported playlist file
		/// </summary>
		/// <param name="path">The filename to check</param>
		/// <returns>true of the file can be opened by Stoffi, otherwise false</returns>
		public static bool IsSupported(string path)
		{
			foreach (var parser in parsers)
				if (parser.Supports (path))
					return true;
			return false;
		}

		/// <summary>
		/// Remove tracks from a playlist if they are found inside the playlist
		/// </summary>
		/// <param name="tracks">The list of tracks to be removed</param>
		/// <param name="playlistName">The name of the playlist to remove the tracks from</param>
		public static void Remove(IEnumerable<Track> tracks, string playlistName)
		{
			Playlist playlist = Get(playlistName);
			if (playlist != null)
				playlist.Remove (tracks);
		}

		/// <summary>
		/// Creates a new playlist
		/// </summary>
		/// <param name="name">The name of the new playlist (this will be appended with a number if neccessary)</param>
		/// <param name="interactive">Whether the action was performed by the user directly</param>
		/// <returns>The newly created Playlist for the playlist</returns>
		public static Playlist Create(string name, bool interactive)
		{
			var ownerID = Services.Manager.Identity == null ? 0 : Services.Manager.Identity.UserID;
			return Create(name, 0, ownerID, interactive);
		}

		/// <summary>
		/// Creates a new playlist
		/// </summary>
		/// <param name="name">The name of the new playlist (this will be appended with a number if neccessary)</param>
		/// <param name="id">The ID of the playlist in the cloud</param>
		/// <param name="owner">The ID of the user who owns the playlist</param>
		/// <param name="interactive">Whether the action was performed by the user directly</param>
		/// <param name="dispatchEvent">Whether or not to dispatch the PlaylistModified after creation</param>
		/// <returns>The newly created Playlist for the playlist</returns>
		public static Playlist Create(string name, uint id = 0, uint owner = 0, bool interactive = false, bool dispatchEvent = true)
		{
			Playlist playlist = new Playlist();
			playlist.Name = name;
			Init (playlist);
			playlist.ID = id;
			playlist.OwnerID = owner;

			if (dispatchEvent)
				DispatchPlaylistModified(playlist, ModifyType.Created, interactive);

			return playlist;
		}

		/// <summary>
		/// Initialize a newly created playlist.
		/// </summary>
		/// <param name="playlist">The playlist to initialize</param>
		public static void Init(Playlist playlist)
		{
			playlist.Name = GenerateName (playlist.Name);

			var config = ListConfig.Create ();
			config.Initialize ();
			if (Settings.Manager.SearchPolicy == SearchPolicy.Global)
				config.Filter = Settings.Manager.FileListConfig.Filter;
			else if (Settings.Manager.SearchPolicy == SearchPolicy.Partial && Settings.Manager.Playlists.Count > 0)
				config.Filter = Settings.Manager.Playlists[0].ListConfig.Filter;

			playlist.ListConfig = config;
			playlist.Tracks.CollectionChanged += TracksChanged;
			playlist.PropertyChanged += new PropertyChangedEventHandler(Playlist_PropertyChanged);
			Settings.Manager.Playlists.Add(playlist);
		}

		/// <summary>
		/// Create an auto updating playlist based on a search query.
		/// </summary>
		/// <param name="name">The name of the playlist</param>
		/// <param name="filter">The search query</param>
		/// <returns>The newly created playlist</returns>
		public static Playlist CreateDynamic(string name, string filter)
		{
			var playlist = Create(name, 0, 0, true, false);
			playlist.Filter = filter;
			playlist.Refresh ();
			DispatchPlaylistModified(playlist, ModifyType.Created, true);
			return playlist;
		}

		/// <summary>
		/// Renames a playlist. If a playlist with the new name already exist or the new name is either "Create new" or "" it will do nothing.
		/// </summary>
		/// <param name="oldName">The current name of the playlist to be renamed</param>
		/// <param name="newName">The new name of the playlist</param>
		public static void Rename(String oldName, string newName)
		{
			newName = U.CleanXMLString(newName);
			Playlist pl = Get(oldName);
			Rename(pl, newName);
		}
		
		/// <summary>
		/// Renames a playlist. If a playlist with the new name already exist or the new name is either "Create new" or "" it will do nothing.
		/// </summary>
		/// <param name="id">The current cloud ID of the playlist to be renamed</param>
		/// <param name="newName">The new name of the playlist</param>
		public static void Rename(uint id, string newName)
		{
			Playlist pl = Get(id);
			Rename(pl, newName);
		}

		/// <summary>
		/// Renames a playlist. If a playlist with the new name already exist or the new name is either "Create new" or "" it will do nothing.
		/// </summary>
		/// <param name="playlist">The playlist to be renamed</param>
		/// <param name="newName">The new name of the playlist</param>
		public static void Rename(Playlist playlist, string newName)
		{
			if (playlist != null && playlist.Name != newName)
			{
				string oldName = playlist.Name;
				newName = GenerateName (newName);

				if (playlist != null && newName != "" && newName.ToLower () != U.T ("NavigationCreateNew").ToLower ()) {
					playlist.Name = newName;
					DispatchPlaylistRenamed (playlist, oldName, newName);
				}
			}
		}

		/// <summary>
		/// Saves a playlist as a file
		/// </summary>
		/// <param name="path">The path of the saved playlist</param>
		/// <param name="name">The name of the playlist to save</param>
		public static void Save(String path, String name)
		{
			Playlist pl = Get(name);
			if (pl != null)
				pl.Save (path);
		}

		/// <summary>
		/// Find parsers which can parse a given path.
		/// </summary>
		/// <returns>The parsers.</returns>
		/// <param name="path">Path.</param>
		public static List<Parser> GetParsers(string path)
		{
			var list = new List<Parser> ();
			foreach (var parser in parsers)
				if (parser.Supports (path))
					list.Add (parser);
			return list;
		}

		/// <summary>
		/// Parses a path of one or several playlists.
		/// </summary>
		/// <param name="path">The path of the playlist object</param>
		/// <param name="resolveMetaData">If true and the playlist contains stream URLs, then a connection will be made to load meta data.</param>
		/// <returns>A list of playlists found at the path</returns>
		public static List<Playlist> Parse(string path, bool resolveMetaData = true)
		{
			U.L (LogLevel.Debug, "Playlist", "Looking for a parser for " + path);
			foreach (var parser in GetParsers (path))
			{
				try {
					var playlists = parser.Read (path, resolveMetaData);
					U.L (LogLevel.Debug, "Playlist", "Found parser: " + parser.GetType().Name);
					return playlists;
				}
				catch (Exception e) {
					U.L (LogLevel.Warning, "Playlist", String.Format("Parser {0} failed on {1}: {2}",
						parser.GetType().Name, path, e.Message));
				}
			}

			U.L (LogLevel.Warning, "Playlist", "Could not find appropriate parser for " + path);
			return new List<Playlist>();
		}

		/// <summary>
		/// Parses a path of one or several playlists and adds to collection.
		/// </summary>
		/// <param name="path">The path of the playlist object</param>
		/// <returns>A list of playlists found at the path</returns>
		public static List<Playlist> Load(string path)
		{
			var playlists = Parse (path);
			if (playlists != null)
				foreach (var playlist in playlists)
					Init (playlist);
			return playlists;
		}

		/// <summary>
		/// Generate a name for a playlist that is not yet taken by adding a number to the end and increasing it until
		/// a non-taken name is found.
		/// </summary>
		/// <returns>The name.</returns>
		/// <param name="name">Name.</param>
		public static string GenerateName(string name)
		{
			if (String.IsNullOrWhiteSpace(name))
				name = defaultName;
			name = U.CleanXMLString(name);
			if (Get(name) != null)
			{
				int ext = 1;
				while (Get(String.Format("{0} {1}", name, ext)) != null)
					ext++;
				name = String.Format("{0} {1}", name, ext);
			}
			return name;
		}

		/// <summary>
		/// Deletes a playlist
		/// </summary>
		/// <param name="name">The name of the playlist to delete</param>
		public static void Remove(String name)
		{
			Playlist pl = Get(name);
			if (pl != null)
			{
				DispatchPlaylistModified(pl, ModifyType.Removed);

				// and finally remove the playlist altogther (undo?)
				Settings.Manager.Playlists.Remove(pl);
			}
		}

		/// <summary>
		/// Deletes a playlist
		/// </summary>
		/// <param name="id">The cloud ID of the playlist to delete</param>
		public static void Remove(uint id)
		{
			Playlist pl = Get(id);
			if (pl != null)
			{
				DispatchPlaylistModified(pl, ModifyType.Removed);

				// and finally remove the playlist altogther (undo?)
				Settings.Manager.Playlists.Remove(pl);
			}
		}

		/// <summary>
		/// Tries to find a playlist with a given name
		/// </summary>
		/// <param name="name">The name of the playlist to look for</param>
		/// <returns>The Playlist of the playlist with the name <paramref name="name"/> of such a playlist could be found, otherwise null.</returns>
		public static Playlist Get(String name)
		{
			foreach (Playlist p in Settings.Manager.Playlists)
				if (p.Name == name) return p;
			return null;
		}

		/// <summary>
		/// Tries to find a playlist with a given cloud ID
		/// </summary>
		/// <param name="id">The cloud ID of the playlist to look for</param>
		/// <returns>The Playlist of the playlist with the cloud ID <paramref name="id"/> of such a playlist could be found, otherwise null.</returns>
		public static Playlist Get(uint id)
		{
			if (id != 0)
				foreach (Playlist p in Settings.Manager.Playlists)
					if (p.ID == id) return p;
			return null;
		}

		/// <summary>
		/// Update the "Last Played" and "Play Count" information of a given track
		/// </summary>
		/// <param name="RefTrack">The track that was just played</param>
		public static void TrackWasPlayed(Track RefTrack)
		{
			if (RefTrack == null) return;

			uint pc = RefTrack.PlayCount + 1;
			var tracks = U.GetTracks(RefTrack.Path);
			foreach (var track in tracks)
			{
				track.PlayCount = pc;
				track.LastPlayed = DateTime.Now;
			}
		}

        /// <summary>
        /// Finds all playlists that contains a given track.
        /// </summary>
        /// <param name="track">The track to look for</param>
        /// <returns>All playlists containing <paramref name="track"/></returns>
		public static List<Playlist> Has(Track track)
		{
			List<Playlist> has = new List<Playlist>();
			foreach (Playlist p in Settings.Manager.Playlists)
                if (Contains(p, track))
                    has.Add(p);
			return has;
		}

        /// <summary>
        /// Checks whether a given playlist contains a given track.
        /// </summary>
        /// <param name="playlist">The playlist to search in</param>
        /// <param name="track">The track to search for</param>
        /// <returns>True of <paramref name="playlist"/> contains <paramref name="track"/>, otherwise false</returns>
        public static bool Contains(Playlist playlist, Track track)
        {
            foreach (Track t in playlist.Tracks)
                if (t.Path == track.Path)
                    return true;
            return false;
        }

        /// <summary>
        /// Checks whether a given playlist contains any of a given list of track.
        /// </summary>
        /// <param name="playlist">The playlist to search in</param>
        /// <param name="tracks">The tracks to search for</param>
        /// <returns>True of <paramref name="playlist"/> contains any of <paramref name="tracks"/>, otherwise false</returns>
        public static bool ContainsAny(Playlist playlist, List<Track> tracks)
        {
            foreach (Track t1 in playlist.Tracks)
                foreach (Track t2 in tracks)
                    if (t1.Path == t2.Path)
                        return true;
            return false;
        }

        /// <summary>
        /// Checks whether a given playlist contains all of a given list of track.
        /// </summary>
        /// <param name="playlist">The playlist to search in</param>
        /// <param name="tracks">The tracks to search for</param>
        /// <returns>True of <paramref name="playlist"/> contains all of <paramref name="tracks"/>, otherwise false</returns>
        public static bool ContainsAll(Playlist playlist, List<Track> tracks)
        {
            foreach (Track t1 in playlist.Tracks)
                foreach (Track t2 in tracks)
                    if (t1.Path != t2.Path)
                        return false;
            return playlist.Tracks.Count != 0;
        }

		/// <summary>
		/// Get the index of a given playlist
		/// </summary>
		/// <returns>The index of the playlist.</returns>
		/// <param name="playlist">Playlist.</param>
		public static int IndexOf(Playlist playlist)
		{
			return Settings.Manager.Playlists.IndexOf (playlist);
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Updates the total time of all tracks of a playlist.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void TracksChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			ObservableCollection<Track> tracks = sender as ObservableCollection<Track>;

			// find the playlist containing the track that sent the event
			Playlist pl = null;
			foreach (Playlist p in Settings.Manager.Playlists)
			{
				if (p.Tracks == tracks)
				{
					pl = p;
					break;
				}
			}

			// no playlist found (weird!)
			if (pl == null) return;

			pl.Time = 0;
			foreach (Track t in pl.Tracks)
				pl.Time += t.Length;

			if (pl.Time < 0) pl.Time = 0;
		}

		/// <summary>
		/// Invoked when the track collection changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void FileTracks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (var playlist in Settings.Manager.Playlists)
				{
					try
					{
						if (!String.IsNullOrWhiteSpace(playlist.Filter))
							playlist.Refresh(e.NewItems);
					}
					catch (Exception exc)
					{
						U.L(LogLevel.Warning, "Playlist", "Could not add tracks dynamically to playlist: " + exc.Message);
					}
				}
			}
		}

		/// <summary>
		/// Invoked when a property of a playlist changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Playlist_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			try
			{
				if (e.PropertyName == "Filter")
				{
					var playlist = sender as Playlist;
					if (!String.IsNullOrWhiteSpace(playlist.Filter))
						playlist.Refresh();
				}
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "Playlist", "Could not handle change of property " + e.PropertyName + ": " + exc.Message);
			}
		}
		/// <summary>
		/// Invoked when a property of the settings manager changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			try
			{
				switch (e.PropertyName)
				{
					case "FileTracks":
						Settings.Manager.FileTracks.CollectionChanged += new NotifyCollectionChangedEventHandler(FileTracks_CollectionChanged);
						foreach (var playlist in Settings.Manager.Playlists)
						if (playlist.Type == PlaylistType.Dynamic)
							playlist.Refresh();
						break;
				}
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "Playlist", "Could not handle update of settings manager property: " + exc.Message);
			}
		}


		#endregion

		#region Dispatchers

		/// <summary>
		/// The dispatcher of the <see cref="PlaylistRenamed"/> event
		/// </summary>
		/// <param name="playlist">The playlist that was renamed</param>
		/// <param name="oldName">The name of the playlist before the change</param>
		/// <param name="newName">The name of the playlist after the change</param>
		private static void DispatchPlaylistRenamed(Playlist playlist, string oldName, string newName)
		{
			if (PlaylistRenamed != null)
				PlaylistRenamed(playlist, new RenamedEventArgs(WatcherChangeTypes.Renamed, "playlist", newName, oldName));
		}

		/// <summary>
		/// The dispatcher of the <see cref="PlaylistModified"/> event
		/// </summary>
		/// <param name="playlist">The playlist that was modified</param>
		/// <param name="type">The type of modification that occured</param>
		/// <param name="interactive">Whether the action was performed by the user directly</param>
		private static void DispatchPlaylistModified(Playlist playlist, ModifyType type, bool interactive = false)
		{
			if (PlaylistModified != null)
				PlaylistModified(playlist, new ModifiedEventArgs(type, null, interactive));
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when a playlist has been renamed
		/// </summary>
		public static event RenamedEventHandler PlaylistRenamed;

		/// <summary>
		/// Occurs when a playlist has been created, removed or changed
		/// </summary>
		public static event ModifiedEventHandler PlaylistModified;

		#endregion
	}

	#region Delegates

	public delegate void ModifiedEventHandler(object sender, ModifiedEventArgs e);

	#endregion

	#region Event arguments

	/// <summary>
	/// Provides data for the events where something has been modified
	/// </summary>
	public class ModifiedEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the type of modification that occured.
		/// </summary>
		public ModifyType Type { get; private set; }

		/// <summary>
		/// Gets the data of the modification.
		/// </summary>
		public object Data { get; private set; }

		/// <summary>
		/// Gets whether the action was performed by the user directly.
		/// </summary>
		public bool Interactive { get; private set; }

		/// <summary>
		/// Creates an instance of the ModifiedEventArgs class
		/// </summary>
		/// <param name="type">The type of modification that occured</param>
		/// <param name="data">The data of the modification</param>
		/// <param name="interactive">Whether the action was performed by the user directly</param>
		public ModifiedEventArgs(ModifyType type, object data, bool interactive = false)
		{
			Type = type;
			Data = data;
			Interactive = interactive;
		}
	}

	#endregion

	#region Enums

	/// <summary>
	/// Represents the type of modification that can occur
	/// </summary>
	public enum ModifyType
	{
		/// <summary>
		/// The object was created
		/// </summary>
		Created,

		/// <summary>
		/// The object was removed
		/// </summary>
		Removed,

		/// <summary>
		/// The object was changed
		/// </summary>
		Changed,

		/// <summary>
		/// The object was added
		/// </summary>
		Added
	}

	#endregion
}
