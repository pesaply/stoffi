/**
 * PlaylistManager.cs
 * 
 * Takes care of managing the playlists.
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

using Newtonsoft.Json.Linq;

namespace Stoffi
{
	/// <summary>
	/// Represents a manager that takes care of the playlist logic
	/// </summary>
	public static class PlaylistManager
	{
		#region Members

		private static String supportedFileFormats = ".m3u;.pls";
		private static String pathPrefix = "stoffi:playlist:";

		#endregion

		#region Properties

		/// <summary>
		/// The currently active playlist that is being played.
		/// An empty string if no playlist is active.
		/// </summary>
		public static String CurrentPlaylist { get; set; }

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initializes the playlist manager
		/// </summary>
		public static void Initialize()
		{
			CurrentPlaylist = "";

			ThreadStart GUIThread = delegate()
			{
				foreach (PlaylistData playlist in SettingsManager.Playlists)
				{
					playlist.Tracks.CollectionChanged += TracksChanged;
					DispatchPlaylistModified(playlist, ModifyType.Created);
					ServiceManager.RefreshPlaylist(playlist);
				}
			};

			if (SettingsManager.CurrentActiveNavigation.StartsWith("Playlist:"))
				CurrentPlaylist = SettingsManager.CurrentActiveNavigation.Split(new[]{':'},2)[1];

			Thread thread = new Thread(GUIThread);
			thread.Name = "Playlist Thread";
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Start();
		}

		/// <summary>
		/// Checks if a playlist belongs to someone else.
		/// </summary>
		/// <param name="playlist">The playlist to check</param>
		/// <returns>true if someone else is the owner of the playlist, otherwise false</returns>
		public static bool IsSomeoneElses(PlaylistData playlist)
		{
			if (playlist == null) return false;
			var id = ServiceManager.Identity;
			return playlist.Owner > 0 && (id == null || id.UserID != playlist.Owner);
		}

		/// <summary>
		/// Add tracks to a playlist
		/// </summary>
		/// <param name="tracks">The list of tracks to be added</param>
		/// <param name="playlistName">The name of the playlist to add the tracks to</param>
		/// <param name="pos">The position to insert the track at (-1 means at the end)</param>
		public static void AddToPlaylist(List<object> tracks, String playlistName, int pos = -1)
		{
			PlaylistData playlist = FindPlaylist(playlistName);
			if (playlist == null) return;
			if (IsSomeoneElses(playlist)) return;
			foreach (TrackData track in tracks)
			{
				if (!playlist.Tracks.Contains(track))
				{
					if (pos < 0 || pos >= playlist.Tracks.Count)
						playlist.Tracks.Add(track);
					else
						playlist.Tracks.Insert(pos, track);
					track.Source = "Playlist:" + playlist.Name;
				}
			}
		}

		/// <summary>
		/// Add tracks to a playlist
		/// </summary>
		/// <param name="tracks">The list of tracks to be added</param>
		/// <param name="playlistName">The name of the playlist to add the tracks to</param>
		/// <param name="pos">The position to insert the track at (-1 means at the end)</param>
		public static void AddToPlaylist(ObservableCollection<TrackData> tracks, String playlistName, int pos = -1)
		{
			PlaylistData playlist = FindPlaylist(playlistName);
			if (playlist == null) return;
			if (IsSomeoneElses(playlist)) return;
			foreach (TrackData track in tracks)
			{
				if (!playlist.Tracks.Contains(track))
				{
					if (pos < 0 || pos >= playlist.Tracks.Count)
						playlist.Tracks.Add(track);
					else
						playlist.Tracks.Insert(pos, track);
					track.Source = "Playlist:" + playlist.Name;
				}
			}
		}

		/// <summary>
		/// Add tracks to a playlist
		/// </summary>
		/// <param name="tracks">The list of tracks to be added</param>
		/// <param name="playlistName">The name of the playlist to add the tracks to</param>
		/// <param name="pos">The position to insert the track at (-1 means at the end)</param>
		public static void AddToPlaylist(List<TrackData> tracks, String playlistName, int pos = -1)
		{
			PlaylistData playlist = FindPlaylist(playlistName);
			if (playlist == null) return;
			if (IsSomeoneElses(playlist)) return;
			foreach (TrackData track in tracks)
			{
				// insert
				if (!playlist.Tracks.Contains(track))
				{
					if (pos < 0 || pos >= playlist.Tracks.Count)
						playlist.Tracks.Add(track);
					else
						playlist.Tracks.Insert(pos, track);
					track.Source = "Playlist:" + playlist.Name;
				}

				// move
				else if (pos != playlist.Tracks.IndexOf(track))
				{
					if (pos < 0 || pos >= playlist.Tracks.Count)
					{
						playlist.Tracks.Remove(track);
						playlist.Tracks.Add(track);
					}
					else if (pos < playlist.Tracks.IndexOf(track))
					{
						playlist.Tracks.Remove(track);
						playlist.Tracks.Insert(pos, track);
					}
					else
					{
						playlist.Tracks.Remove(track);
						playlist.Tracks.Insert(pos-1, track);
					}
				}
			}
		}

		/// <summary>
		/// Check to see if a file is a supported playlist file
		/// </summary>
		/// <param name="path">The filename to check</param>
		/// <returns>true of the file can be opened by Stoffi, otherwise false</returns>
		public static bool IsSupported(String path)
		{
			if (path.StartsWith(pathPrefix)) return true;
			string ext = Path.GetExtension(path).ToLower();
			return supportedFileFormats.Split(';').Contains<string>(ext);
		}

		/// <summary>
		/// Remove tracks from a playlist if they are found inside the playlist
		/// </summary>
		/// <param name="tracks">The list of t to be removed</param>
		/// <param name="playlistName">The name of the playlist to remove the t from</param>
		public static void RemoveFromPlaylist(List<TrackData> tracks, String playlistName)
		{
			PlaylistData playlist = FindPlaylist(playlistName);
			if (IsSomeoneElses(playlist)) return;
			if (playlist != null)
			{
				foreach (TrackData track in tracks)
					foreach (TrackData trackInPlaylist in playlist.Tracks)
						if (trackInPlaylist.Path == track.Path)
						{
							playlist.Tracks.Remove(trackInPlaylist);
							break;
						}
			}
		}

		/// <summary>
		/// Creates a new playlist
		/// </summary>
		/// <param name="name">The name of the new playlist (this will be appended with a number if neccessary)</param>
		/// <param name="interactive">Whether the action was performed by the user directly</param>
		/// <returns>The newly created PlaylistData for the playlist</returns>
		public static PlaylistData CreatePlaylist(String name, bool interactive)
		{
			var ownerID = ServiceManager.Identity == null ? 0 : ServiceManager.Identity.UserID;
			return CreatePlaylist(name, 0, ownerID, interactive);
		}

		/// <summary>
		/// Creates a new playlist
		/// </summary>
		/// <param name="name">The name of the new playlist (this will be appended with a number if neccessary)</param>
		/// <param name="id">The ID of the playlist in the cloud</param>
		/// <param name="owner">The ID of the user who owns the playlist</param>
		/// <param name="interactive">Whether the action was performed by the user directly</param>
		/// <returns>The newly created PlaylistData for the playlist</returns>
		public static PlaylistData CreatePlaylist(String name, uint id = 0, uint owner = 0, bool interactive = false)
		{
			name = U.CleanXMLString(name);

			if (FindPlaylist(name) != null)
			{
				int pExt = 1;
				while (FindPlaylist(name + pExt) != null)
					pExt++;
				name = name + pExt;
			}

			PlaylistData playlist = new PlaylistData();
			playlist.Name = name;
			playlist.ID = id;
			playlist.Owner = owner;
			playlist.Time = 0;
			playlist.Tracks = new ObservableCollection<TrackData>();
			playlist.Tracks.CollectionChanged += TracksChanged;
			SettingsManager.Playlists.Add(playlist);

			DispatchPlaylistModified(playlist, ModifyType.Created, interactive);

			return playlist;
		}

		/// <summary>
		/// Renames a playlist. If a playlist with the new name already exist or the new name is either "Create new" or "" it will do nothing.
		/// </summary>
		/// <param name="oldName">The current name of the playlist to be renamed</param>
		/// <param name="newName">The new name of the playlist</param>
		public static void RenamePlaylist(String oldName, String newName)
		{
			newName = U.CleanXMLString(newName);
			PlaylistData pl = FindPlaylist(oldName);
			RenamePlaylist(pl, newName);
		}
		
		/// <summary>
		/// Renames a playlist. If a playlist with the new name already exist or the new name is either "Create new" or "" it will do nothing.
		/// </summary>
		/// <param name="id">The current cloud ID of the playlist to be renamed</param>
		/// <param name="newName">The new name of the playlist</param>
		public static void RenamePlaylist(uint id, String newName)
		{
			PlaylistData pl = FindPlaylist(id);
			RenamePlaylist(pl, newName);
		}

		/// <summary>
		/// Renames a playlist. If a playlist with the new name already exist or the new name is either "Create new" or "" it will do nothing.
		/// </summary>
		/// <param name="playlist">The playlist to be renamed</param>
		/// <param name="newName">The new name of the playlist</param>
		public static void RenamePlaylist(PlaylistData playlist, String newName)
		{
			if (playlist != null && playlist.Name != newName)
			{
				string oldName = playlist.Name;
				if (FindPlaylist(newName) != null)
				{
					int pExt = 1;
					while (FindPlaylist(newName + pExt) != null)
						pExt++;
					newName = newName + pExt;
				}

				if (playlist != null && newName != "" && newName.ToLower() != U.T("NavigationCreateNew").ToLower())
					DispatchPlaylistRenamed(playlist, oldName, newName);
			}
		}

		/// <summary>
		/// Saves a playlist as a file
		/// </summary>
		/// <param name="path">The path of the saved playlist</param>
		/// <param name="name">The name of the playlist to save</param>
		public static void SavePlaylist(String path, String name)
		{
			PlaylistData pl = FindPlaylist(name);
			if (pl != null)
			{
				string ext = Path.GetExtension(path);
				System.IO.StreamWriter sw = System.IO.File.AppendText(path);

				if (ext == ".pls")
				{
					sw.WriteLine("[playlist]");
					sw.WriteLine("");
					int i = 0;
					foreach (TrackData track in pl.Tracks)
					{
						i++;
						sw.WriteLine(String.Format("File{0}={1}", i, track.Path));
						sw.WriteLine(String.Format("Title{0}={1}", i, track.Title));
						sw.WriteLine(String.Format("Length{0}={1}", i, (int)track.Length));
						sw.WriteLine("");
					}
					sw.WriteLine("NumberOfEntries=" + i);
					sw.WriteLine("Version=2");
				}
				else if (ext == ".m3u")
				{
					sw.WriteLine("#EXTM3U");
					sw.WriteLine("");
					foreach (TrackData track in pl.Tracks)
					{
						sw.WriteLine(String.Format("#EXTINF:{0},{1} - {2}", (int)track.Length, track.Artist, track.Title));
						sw.WriteLine(track.Path);
						sw.WriteLine("");
					}
				}
				sw.Close();
			}
		}

		/// <summary>
		/// Gets the playlist type of a path.
		/// </summary>
		/// <param name="path">The path of the playlist</param>
		/// <returns>The type of the given playlist path</returns>
		public static PlaylistType GetType(string path)
		{
			string ext = Path.GetExtension(path).ToLower();
			if (ext == ".pls")
				return PlaylistType.PLS;

			else if (ext == ".m3u")
				return PlaylistType.M3U;

			else if (ext == ".m3u8")
				return PlaylistType.M3U8;

			else if (ext == ".xspf")
				return PlaylistType.XSPF;

			else if (ext == ".asx")
				return PlaylistType.ASX;

			else
				throw new Exception("Unsupported playlist extension: " + ext);
		}

		/// <summary>
		/// Parses a playlist and returns a collection of tracks.
		/// </summary>
		/// <param name="reader">The data stream representing the playlist</param>
		/// <param name="type">The format of the playlist</param>
		/// <param name="path">The relative path of the location of the tracks in the playlist</param>
		/// <returns>A collection of Tracks representing the tracks of the playlist</returns>
		public static ObservableCollection<TrackData> ParsePlaylist(StreamReader reader, PlaylistType type, string path = "")
		{
			switch (type)
			{
				case PlaylistType.ASX:
					return ParseASX(reader, path);

				case PlaylistType.M3U:
					return ParseM3U(reader, path);

				//case PlaylistType.M3U8:
				//    return ParseM3U8(reader);

				case PlaylistType.PLS:
					return ParsePLS(reader, path);

				case PlaylistType.XSPF:
					return ParseXSPF(reader, path);

				default:
					throw new Exception("Unsupported playlist type: " + type);
			}
		}

		/// <summary>
		/// Convers an extension into a playlist type.
		/// </summary>
		/// <param name="extension">The filename extension of the playlist</param>
		/// <returns>The corresponding playlist type</returns>
		public static PlaylistType ExtensionToType(string extension)
		{
			if (extension.Length > 0 && extension[0] == '.')
				extension = extension.Substring(1);
			switch (extension.ToLower())
			{
				case "pls":
					return PlaylistType.PLS;
				case "m3u":
					return PlaylistType.M3U;
				case "m3u8":
					return PlaylistType.M3U8;
				case "asx":
					return PlaylistType.ASX;
				case "xspf":
					return PlaylistType.XSPF;
				default:
					throw new Exception("Unknown playlist extension: " + extension);
			}
		}

		/// <summary>
		/// Reads a file and creates a playlist using the name of the file.
		/// </summary>
		/// <param name="filename">The file to read</param>
		/// <returns>The PlaylistData of the newly created playlist</returns>
		public static PlaylistData LoadPlaylist(String filename)
		{
			if (filename.StartsWith(pathPrefix))
			{
				try
				{
					var id = Convert.ToUInt32(filename.Substring(pathPrefix.Length));
					return ServiceManager.FollowPlaylist(id);
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "PLAYLIST", "Error fetching playlist: " + e.Message);
				}
			}
			else
			{
				try
				{
					string pName = Path.GetFileNameWithoutExtension(filename);
					string path = Path.GetDirectoryName(filename);
					if (FindPlaylist(pName) != null)
					{
						int pExt = 1;
						while (FindPlaylist(pName + pExt) != null)
							pExt++;
						pName = pName + pExt;
					}
					PlaylistData pl = CreatePlaylist(pName);

					StreamReader sr = new StreamReader(filename);
					pl.Tracks = ParsePlaylist(sr, ExtensionToType(Path.GetExtension(filename)), path);
					sr.Close();
					return pl;
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "PLAYLIST", "Error parsing playlist: " + e.Message);
				}
			}
			return null;
		}

		/// <summary>
		/// Deletes a playlist
		/// </summary>
		/// <param name="name">The name of the playlist to delete</param>
		public static void RemovePlaylist(String name)
		{
			PlaylistData pl = FindPlaylist(name);
			if (pl != null)
			{
				DispatchPlaylistModified(pl, ModifyType.Removed);

				// and finally remove the playlist altogther (undo?)
				SettingsManager.Playlists.Remove(pl);
			}
		}

		/// <summary>
		/// Deletes a playlist
		/// </summary>
		/// <param name="id">The cloud ID of the playlist to delete</param>
		public static void RemovePlaylist(uint id)
		{
			PlaylistData pl = FindPlaylist(id);
			if (pl != null)
			{
				DispatchPlaylistModified(pl, ModifyType.Removed);

				// and finally remove the playlist altogther (undo?)
				SettingsManager.Playlists.Remove(pl);
			}
		}

		/// <summary>
		/// Tries to find a playlist with a given name
		/// </summary>
		/// <param name="name">The name of the playlist to look for</param>
		/// <returns>The PlaylistData of the playlist with the name <paramref name="name"/> of such a playlist could be found, otherwise null.</returns>
		public static PlaylistData FindPlaylist(String name)
		{
			foreach (PlaylistData p in SettingsManager.Playlists)
				if (p.Name == name) return p;
			return null;
		}

		/// <summary>
		/// Tries to find a playlist with a given cloud ID
		/// </summary>
		/// <param name="id">The cloud ID of the playlist to look for</param>
		/// <returns>The PlaylistData of the playlist with the cloud ID <paramref name="id"/> of such a playlist could be found, otherwise null.</returns>
		public static PlaylistData FindPlaylist(uint id)
		{
			if (id != 0)
				foreach (PlaylistData p in SettingsManager.Playlists)
					if (p.ID == id) return p;
			return null;
		}

		/// <summary>
		/// Update the "Last Played" and "Play Count" information of a given track
		/// </summary>
		/// <param name="RefTrack">The track that was just played</param>
		public static void TrackWasPlayed(TrackData RefTrack)
		{
			if (RefTrack == null) return;

			uint pc = RefTrack.PlayCount + 1;
			foreach (TrackData track in SettingsManager.FileTracks)
			{
				if (track.Path == RefTrack.Path)
				{
					track.PlayCount = pc;
					track.LastPlayed = DateTime.Now;
				}
			}

			foreach (TrackData track in SettingsManager.QueueTracks)
			{
				if (track.Path == RefTrack.Path)
				{
					track.PlayCount = pc;
					track.LastPlayed = DateTime.Now;
				}
			}

			foreach (TrackData track in SettingsManager.HistoryTracks)
				if (track.Path == RefTrack.Path)
					track.PlayCount = pc;

			foreach (PlaylistData playlist in SettingsManager.Playlists)
			{
				if (playlist.Name == CurrentPlaylist)
				{
					foreach (TrackData track in playlist.Tracks)
					{
						if (track.Path == RefTrack.Path)
						{
							track.PlayCount = pc;
							track.LastPlayed = DateTime.Now;
						}
					}
				}
			}
		}

        /// <summary>
        /// Finds all playlists that contains a given track.
        /// </summary>
        /// <param name="track">The track to look for</param>
        /// <returns>All playlists containing <paramref name="track"/></returns>
		public static List<PlaylistData> Has(TrackData track)
		{
			List<PlaylistData> has = new List<PlaylistData>();
			foreach (PlaylistData p in SettingsManager.Playlists)
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
        public static bool Contains(PlaylistData playlist, TrackData track)
        {
            foreach (TrackData t in playlist.Tracks)
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
        public static bool ContainsAny(PlaylistData playlist, List<TrackData> tracks)
        {
            foreach (TrackData t1 in playlist.Tracks)
                foreach (TrackData t2 in tracks)
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
        public static bool ContainsAll(PlaylistData playlist, List<TrackData> tracks)
        {
            foreach (TrackData t1 in playlist.Tracks)
                foreach (TrackData t2 in tracks)
                    if (t1.Path != t2.Path)
                        return false;
            return playlist.Tracks.Count != 0;
        }

		#endregion

		#region Private

		/// <summary>
		/// Parses a PLS playlist and returns the tracks of it.
		/// </summary>
		/// <param name="reader">The data stream of the playlist</param>
		/// <param name="path">The relative path of the tracks in the playlist</param>
		/// <returns>A collection of tracks represented by the playlist</returns>
		private static ObservableCollection<TrackData> ParsePLS(StreamReader reader, string path = "")
		{
			ObservableCollection<TrackData> ret = new ObservableCollection<TrackData>();
			bool hdr = false;
			string version = "";
			int noe = 0;
			int nr = 0;
			string line;

			List<string> lines = new List<string>();

			while ((line = reader.ReadLine()) != null)
			{
				lines.Add(line);
				nr++;
				if (line == "[playlist]")
					hdr = true;
				else if (!hdr)
					U.L(LogLevel.Warning, "PLAYLIST", "Bad format on line "
						+ nr + ": expecting '[playlist]'");
				else if (line.StartsWith("NumberOfEntries="))
					noe = Convert.ToInt32(line.Split('=')[1]);
				else if (line.StartsWith("Version="))
					version = line.Split('=')[1];
			}

			if (!hdr)
				U.L(LogLevel.Warning, "PLAYLIST", "No header found");


			// It seems there's many Internet radios that doesn't specify a version,
			// so we can't be too picky about this one.
			//else if (version != "2")
			//    U.L(LogLevel.Warning, "PLAYLIST", "Unsupported version '" +
			//        version + "'");

			else
			{
				string[,] tracks = new string[noe, 3];
				nr = 0;
				foreach (string l in lines)
				{
					if (l.StartsWith("File") || l.StartsWith("Title") || l.StartsWith("Length"))
					{
						int tmp = 4;
						int index = 0;
						if (l.StartsWith("Title")) { tmp = 5; index = 1; }
						else if (l.StartsWith("Length")) { tmp = 6; index = 2; }

						string[] split = l.Split('=');
						int number = Convert.ToInt32(split[0].Substring(tmp));

						if (number > noe)
							U.L(LogLevel.Warning, "PLAYLIST", "Bad format on line "
								+ nr + ": entry number is '" + number + "' but NumberOfEntries is '" + noe + "'");
						else
							tracks[number - 1, index] = split[1];
					}
					else if (!l.StartsWith("NumberOfEntries") && l != "[playlist]" && !l.StartsWith("Version="))
					{
						U.L(LogLevel.Warning, "PLAYLIST", "Bad format on line "
							+ nr + ": unexpected '" + l + "'");
					}
				}
				for (int i = 0; i < noe; i++)
				{
					string p = tracks[i, 0];

					TrackType type = MediaManager.GetType(p);
					TrackData track;

					switch (type)
					{
						case TrackType.File:
							if (!File.Exists(p) && File.Exists(Path.Combine(path, p)))
								p = Path.Combine(path, p);

							if (File.Exists(p))
							{
								if (!FilesystemManager.PathIsAdded(p))
									FilesystemManager.AddSource(p);
								foreach (TrackData t in SettingsManager.FileTracks)
									if (t.Path == p)
									{
										if (!ret.Contains(t))
											ret.Add(t);
										break;
									}
							}
							break;

						case TrackType.WebRadio:
							track = MediaManager.ParseURL(p);
							if (track != null && !ret.Contains(track))
							{
								if (String.IsNullOrWhiteSpace(track.Title))
									track.Title = tracks[i, 1];
								if (String.IsNullOrWhiteSpace(track.URL))
									track.URL = p;
								ret.Add(track);
							}
							break;

						case TrackType.YouTube:
							track = YouTubeManager.CreateTrack(p);
							if (track != null && !ret.Contains(track))
								ret.Add(track);
							break;

						case TrackType.SoundCloud:
							track = SoundCloudManager.CreateTrack(p);
							if (track != null && !ret.Contains(track))
								ret.Add(track);
							break;
					}
				}
			}
			return ret;
		}

		/// <summary>
		/// Parses an M3U playlist and returns the tracks of it.
		/// </summary>
		/// <param name="reader">The data stream of the playlist</param>
		/// <param name="path">The relative path of the tracks in the playlist</param>
		/// <returns>A collection of tracks represented by the playlist</returns>
		private static ObservableCollection<TrackData> ParseM3U(StreamReader reader, string path = "")
		{
			ObservableCollection<TrackData> ret = new ObservableCollection<TrackData>();
			string line;
			bool ext = false;
			string inf = "";
			int nr = 0;
			while ((line = reader.ReadLine()) != null)
			{
				nr++;
				if (line == "#EXTM3U")
					ext = true;
				else if (ext && line.StartsWith("#EXTINF:"))
					inf = line.Substring(8);
				else if (line.StartsWith("#") || line == "")
					continue;
				else
				{
					string p = line;
					TrackType type = MediaManager.GetType(p);
					TrackData track;

					switch (type)
					{
						case TrackType.File:

							if (!File.Exists(p) && File.Exists(Path.Combine(path, p)))
								p = Path.Combine(path, p);

							if (File.Exists(path))
							{
								string length = "";
								string artist = "";
								string title = "";
								if (inf != "")
								{
									if (!inf.Contains(','))
									{
										U.L(LogLevel.Warning, "PLAYLIST", "Bad format on line "
											+ nr + ": expecting ','");
										continue;
									}
									string[] split = inf.Split(',');
									length = split[0];
									if (split[1].Contains('-'))
									{
										artist = split[1].Split('-')[0];
										title = split[1].Split('-')[1];
									}
									else
										title = split[1];
								}
								if (!FilesystemManager.PathIsAdded(path))
									FilesystemManager.AddSource(p);
								foreach (TrackData t in SettingsManager.FileTracks)
									if (t.Path == path)
									{
										if (!ret.Contains(t))
											ret.Add(t);
										break;
									}
								inf = "";
							}
							break;

						case TrackType.WebRadio:
							track = MediaManager.ParseURL(p);
							if (track != null && !ret.Contains(track))
								ret.Add(track);
							break;

						case TrackType.YouTube:
							track = YouTubeManager.CreateTrack(p);
							if (track != null && !ret.Contains(track))
								ret.Add(track);
							break;

						case TrackType.SoundCloud:
							track = SoundCloudManager.CreateTrack(p);
							if (track != null && !ret.Contains(track))
								ret.Add(track);
							break;
					}
				}
			}
			return ret;
		}

		/// <summary>
		/// Parses an XSPF playlist and returns the tracks of it.
		/// </summary>
		/// <param name="reader">The data stream of the playlist</param>
		/// <param name="path">The relative path of the tracks in the playlist</param>
		/// <returns>A collection of tracks represented by the playlist</returns>
		private static ObservableCollection<TrackData> ParseXSPF(StreamReader reader, string path = "")
		{
			ObservableCollection<TrackData> ret = new ObservableCollection<TrackData>();
			return ret;
		}

		/// <summary>
		/// Parses an ASX playlist and returns the tracks of it.
		/// </summary>
		/// <param name="reader">The data stream of the playlist</param>
		/// <param name="path">The relative path of the tracks in the playlist</param>
		/// <returns>A collection of tracks represented by the playlist</returns>
		private static ObservableCollection<TrackData> ParseASX(StreamReader reader, string path = "")
		{
			ObservableCollection<TrackData> ret = new ObservableCollection<TrackData>();
			return ret;
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
			ObservableCollection<TrackData> tracks = sender as ObservableCollection<TrackData>;

			// find the playlist containing the track that sent the event
			PlaylistData pl = null;
			foreach (PlaylistData p in SettingsManager.Playlists)
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
			foreach (TrackData t in pl.Tracks)
				pl.Time += t.Length;

			if (pl.Time < 0) pl.Time = 0;
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// The dispatcher of the <see cref="PlaylistRenamed"/> event
		/// </summary>
		/// <param name="playlist">The playlist that was renamed</param>
		/// <param name="oldName">The name of the playlist before the change</param>
		/// <param name="newName">The name of the playlist after the change</param>
		private static void DispatchPlaylistRenamed(PlaylistData playlist, string oldName, string newName)
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
		private static void DispatchPlaylistModified(PlaylistData playlist, ModifyType type, bool interactive = false)
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

	/// <summary>
	/// Represent the type of a playlist file
	/// </summary>
	public enum PlaylistType
	{
		/// <summary>
		/// A plain text file with the .m3u or .M3U extension.
		/// </summary>
		M3U,

		/// <summary>
		/// A plain text file with the .m3u8 or .M3U8 extension.
		/// Following the same scheme as M3U but with UTF-8 encoding
		/// instead of Latin-1.
		/// </summary>
		M3U8,

		/// <summary>
		/// A plain text file with the .pls or .PLS extension, more
		/// expressive than M3U.
		/// </summary>
		PLS,

		/// <summary>
		/// XML Shareable Playlist Format.
		/// An XML playlist with content resolution.
		/// </summary>
		XSPF,

		/// <summary>
		/// Advanced Stream Redirector. An XML playlist for Windows Media
		/// files.
		/// </summary>
		ASX
	}

	#endregion
}
