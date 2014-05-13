/***
 * ExportImport.cs
 * 
 * Serializes the settings for exporting to a file and
 * importing files.
 * 
 * Can be used to migrate settings to another instance,
 * or backing up settings.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

using Stoffi.Core.Media;
using Stoffi.Core.Sources;

namespace Stoffi.Core.Settings
{
	/// <summary>
	/// Represents a manager that takes care of all
	/// application 
	/// </summary>
	public static partial class Manager
	{
		#region Methods

		#region Public

		/// <summary>
		/// Export the current configuration to an xml file
		/// </summary>
		/// <param name="filename">The path to the file</param>
		public static void Export(string filename)
		{
			Config c = new Config();

			// tracks
			c.Collection = ExportTracks(FileTracks);
			c.History = ExportTracks(HistoryTracks);
			c.Queue = ExportTracks(QueueTracks);
			c.Playlists = new List<ConfigPlaylist>();
			foreach (var p in Playlists)
			{
				ConfigPlaylist cp = new ConfigPlaylist();
				cp.Name = p.Name;
				cp.Tracks = ExportTracks(p.Tracks);
				cp.ListConfig = p.ListConfig;
				c.Playlists.Add(cp);
			}

			c.CurrentActiveNavigation = CurrentActiveNavigation;
			c.CurrentSelectedNavigation = CurrentSelectedNavigation;
			c.CurrentShortcutProfile = CurrentShortcutProfile;
			c.CurrentTrack = ExportTrack(CurrentTrack);
			c.DetailsPaneVisible = DetailsPaneVisible;
			c.FileListConfig = FileListConfig;
			c.HistoryIndex = HistoryIndex;
			c.HistoryListConfig = HistoryListConfig;
			c.Language = Language;
			c.MenuBarVisible = MenuBarVisible;
			c.FastStart = FastStart;
			c.OpenAddPolicy = OpenAddPolicy;
			c.OpenPlayPolicy = OpenPlayPolicy;
			c.QueueListConfig = QueueListConfig;
			c.Repeat = Repeat;
			c.SearchPolicy = SearchPolicy;
			c.Seek = Seek;
			c.Volume = Volume;
			c.ShortcutProfiles = ShortcutProfiles;
			c.ShowOSD = ShowOSD;
			c.Shuffle = Shuffle;
			c.SourceListConfig = SourceListConfig;
			c.UpgradePolicy = UpgradePolicy;
			c.YouTubeListConfig = YouTubeListConfig;

			// serialize to xml
			XmlSerializer ser = new XmlSerializer(typeof(Config));
			TextWriter w = new StreamWriter(filename);
			ser.Serialize(w, c);
			w.Close();
		}

		/// <summary>
		/// Import and apply a configuration from an xml file
		/// </summary>
		/// <param name="filename">The path of the file</param>
		public static void Import(string filename)
		{
			U.L(LogLevel.Debug, "SETTINGS", "Start parsing configuration XML");
			XmlSerializer ser = new XmlSerializer(typeof(Config));
			FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
			Config c = (Config)ser.Deserialize(fs);
			fs.Close();
			U.L(LogLevel.Debug, "SETTINGS", "Parsing finished");

			// tracks
			FileTracks.Clear();
			HistoryTracks.Clear();
			QueueTracks.Clear();
			ImportTracks(c.Collection, FileTracks, false);
			ImportTracks(c.History, HistoryTracks);
			ImportTracks(c.Queue, QueueTracks);
			Playlists.Clear();
			foreach (var p in c.Playlists)
			{
				var pl = Core.Playlists.Manager.Get(p.Name);
				if (pl == null)
					pl = Core.Playlists.Manager.Create(p.Name);
				ImportTracks(p.Tracks, pl.Tracks);
				pl.ListConfig = p.ListConfig;
			}

			CurrentActiveNavigation = c.CurrentActiveNavigation;
			CurrentSelectedNavigation = c.CurrentSelectedNavigation;
			CurrentShortcutProfile = c.CurrentShortcutProfile;
			CurrentTrack = ImportTrack(c.CurrentTrack);
			DetailsPaneVisible = c.DetailsPaneVisible;
			FileListConfig = c.FileListConfig;
			HistoryIndex = c.HistoryIndex;
			HistoryListConfig = c.HistoryListConfig;
			Language = c.Language;
			MenuBarVisible = c.MenuBarVisible;
			FastStart = c.FastStart;
			OpenAddPolicy = c.OpenAddPolicy;
			OpenPlayPolicy = c.OpenPlayPolicy;
			QueueListConfig = c.QueueListConfig;
			Repeat = c.Repeat;
			SearchPolicy = c.SearchPolicy;
			Seek = c.Seek;
			Volume = c.Volume;
			ShortcutProfiles = c.ShortcutProfiles;
			ShowOSD = c.ShowOSD;
			Shuffle = c.Shuffle;
			SourceListConfig = c.SourceListConfig;
			UpgradePolicy = c.UpgradePolicy;
			YouTubeListConfig = c.YouTubeListConfig;
		}

		#endregion

		#region Private

		/// <summary>
		/// Copies an observable collection of Track to a list of ConfigTrack
		/// </summary>
		/// <param name="tracks">The tracks to be copied</param>
		/// <returns>The tracks as configuration structures</returns>
		private static List<ConfigTrack> ExportTracks(ObservableCollection<Track> tracks)
		{
			List<ConfigTrack> l = new List<ConfigTrack>();
			foreach (Track t in tracks)
				l.Add(ExportTrack(t));
			return l;
		}

		/// <summary>
		/// Copies a Track into a ConfigTrack
		/// </summary>
		/// <param name="track">The track to be copied</param>
		/// <returns>The track as a configuration structure</returns>
		private static ConfigTrack ExportTrack(Track track)
		{
			if (track == null) return null;
			return new ConfigTrack() { Path = track.Path, PlayCount = track.PlayCount, LastPlayed = track.LastPlayed };
		}

		/// <summary>
		/// Copies a list of ConfigTrack to an observable collection of Track
		/// </summary>
		/// <param name="tracks">The tracks to be copied</param>
		/// <returns>The tracks as an observable collection of Track</returns>
		private static ObservableCollection<Track> ImportTracks(List<ConfigTrack> tracks)
		{
			ObservableCollection<Track> c = new ObservableCollection<Track>();
			foreach (ConfigTrack t in tracks)
			{
				Track track = ImportTrack(t);
				if (track != null)
					c.Add(track);
			}
			return c;
		}

		/// <summary>
		/// Reads from a confguration structure of tracks and imports the tracks into 
		/// an observable collection.
		/// </summary>
		/// <param name="source">The tracks to be copied</param>
		/// <param name="destination">The collection to copy the tracks into</param>
		/// <param name="addToList">Whether or not to also add the track to the destination (this may be done elsewhere)</param>
		private static void ImportTracks(List<ConfigTrack> source, ObservableCollection<Track> destination, bool addToList = true)
		{
			foreach (ConfigTrack t in source)
			{
				Track dest = null;
				foreach (Track track in destination)
				{
					if (track.Path == t.Path)
					{
						dest = track;
						break;
					}
				}
				if (dest == null)
				{
					Track track = ImportTrack(t, addToList);
					if (track != null && addToList)
						destination.Add(track);
				}
				else
				{
					dest.LastPlayed = t.LastPlayed;
					dest.PlayCount = t.PlayCount;
				}
			}
		}

		/// <summary>
		/// Copies a ConfigTrack into a Track
		/// </summary>
		/// <param name="track">The track to be copied</param>
		/// <param name="addManually">Whether or not the track has to be added manually (this may be done elsewhere)</param>
		/// <returns>The track as a Track</returns>
		private static Track ImportTrack(ConfigTrack track, bool addManually = true)
		{
			if (track == null) return null;
			if (Sources.Manager.YouTube.IsFromHere(track.Path))
			{
				return Sources.Manager.YouTube.CreateTrack(track.Path);
			}
			else if (File.Exists(track.Path))
			{
				Track t = Files.CreateTrack(track.Path, !addManually);
				if (addManually)
					Files.UpdateTrack(t);
				t.LastPlayed = track.LastPlayed;
				t.PlayCount = track.PlayCount;
				return t;
			}
			return null;
		}

		#endregion

		#endregion
	}

	/// <summary>
	/// Describes the configuration of Stoffi
	/// </summary>
	[Serializable()]
	public class Config
	{
		#region Properties

		/// <summary>
		/// Gets or sets the playlists of the configuration
		/// </summary>
		public List<ConfigPlaylist> Playlists { get; set; }

		/// <summary>
		/// Gets or sets the history of the configuration
		/// </summary>
		public List<ConfigTrack> History { get; set; }

		/// <summary>
		/// Gets or sets the queue of the configuration
		/// </summary>
		public List<ConfigTrack> Queue { get; set; }

		/// <summary>
		/// Gets or sets the track collection of the configuration
		/// </summary>
		public List<ConfigTrack> Collection { get; set; }

		/// <summary>
		/// Gets or sets language of Stoffi
		/// </summary>
		public string Language { get; set; }

		/// <summary>
		/// Gets or sets the repeat state
		/// </summary>
		public RepeatState Repeat { get; set; }

		/// <summary>
		/// Gets or sets whether to shuffle tracks or not
		/// </summary>
		public bool Shuffle { get; set; }

		/// <summary>
		/// Gets or sets the volume
		/// </summary>
		public double Volume { get; set; }

		/// <summary>
		/// Gets or sets the currently playing track
		/// </summary>
		public ConfigTrack CurrentTrack { get; set; }

		/// <summary>
		/// Gets or sets the current position of the seek
		/// </summary>
		public double Seek { get; set; }

		/// <summary>
		/// Gets or sets the index inside the history list
		/// </summary>
		public int HistoryIndex { get; set; }

		/// <summary>
		/// Gets or sets the navigation that is selected
		/// </summary>
		public string CurrentSelectedNavigation { get; set; }

		/// <summary>
		/// Gets or sets the navigation in which playback occurs
		/// </summary>
		public string CurrentActiveNavigation { get; set; }

		/// <summary>
		/// Gets or sets whether the menu bar is visible or not
		/// </summary>
		public bool MenuBarVisible { get; set; }

		/// <summary>
		/// Gets or sets whether the details pane is visible or not
		/// </summary>
		public bool DetailsPaneVisible { get; set; }

		/// <summary>
		/// Gets or sets the configuration for the source list
		/// </summary>
		public ListConfig SourceListConfig { get; set; }

		/// <summary>
		/// Gets or sets configuration for the file list
		/// </summary>
		public ListConfig FileListConfig { get; set; }

		/// <summary>
		/// Gets or sets configuration for the history list
		/// </summary>
		public ListConfig HistoryListConfig { get; set; }

		/// <summary>
		/// Gets or sets configuration for the queue list
		/// </summary>
		public ListConfig QueueListConfig { get; set; }

		/// <summary>
		/// Gets or sets configuration for the youtube list
		/// </summary>
		public ListConfig YouTubeListConfig { get; set; }

		/// <summary>
		/// Gets or sets the policy for adding files when opening them with Stoffi
		/// </summary>
		public OpenAddPolicy OpenAddPolicy { get; set; }

		/// <summary>
		/// Gets or sets the policy for playing files when opening them with Stoffi
		/// </summary>
		public OpenPlayPolicy OpenPlayPolicy { get; set; }

		/// <summary>
		/// Gets or sets how to perform upgrades of Stoffi
		/// </summary>
		public UpgradePolicy UpgradePolicy { get; set; }

		/// <summary>
		/// Gets or sets how different lists share search filters
		/// </summary>
		public SearchPolicy SearchPolicy { get; set; }

		/// <summary>
		/// Gets or sets whether to show a notification when a new track is played
		/// </summary>
		public bool ShowOSD { get; set; }

		/// <summary>
		/// Gets or sets whether to hide Stoffi from the taskbar when it's minimized
		/// </summary>
		public bool FastStart { get; set; }

		/// <summary>
		/// Gets or sets the collection of keyboard shortcuts
		/// </summary>
		public ObservableCollection<KeyboardShortcutProfile> ShortcutProfiles { get; set; }

		/// <summary>
		/// Gets or sets the profile of keyboard shortcuts that are currently in use
		/// </summary>
		public KeyboardShortcutProfile CurrentShortcutProfile { get; set; }

		#endregion
	}

	/// <summary>
	/// Describes a playlist configuration
	/// </summary>
	[Serializable()]
	public class ConfigPlaylist
	{
		#region Properties

		/// <summary>
		/// Gets or sets the name of the playlist
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the tracks of the playlist
		/// </summary>
		public List<ConfigTrack> Tracks { get; set; }

		/// <summary>
		/// Gets or sets configuration for the list
		/// </summary>
		public ListConfig ListConfig { get; set; }

		#endregion
	}

	/// <summary>
	/// Describes a track configuration
	/// </summary>
	[Serializable()]
	public class ConfigTrack
	{
		#region Properties

		/// <summary>
		/// Gets or sets the path of the track
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// Gets or sets the number of times the track has been played
		/// </summary>
		public uint PlayCount { get; set; }

		/// <summary>
		/// Gets or sets when the track was last played
		/// </summary>
		public DateTime LastPlayed { get; set; }

		#endregion
	}
}

