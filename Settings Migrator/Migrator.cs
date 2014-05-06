/**
 * Migrator.cs
 * 
 * Creates a modified version of a settings file for migration
 * between two versions of Stoffi Music Player.
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
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Stoffi
{
    /// <summary>
    /// Migrates settings between two versions of Stoffi.
    /// </summary>
    public class SettingsMigrator : IMigrator
    {
        #region Members

        #endregion

        #region Methods

        #region Public

        /// <summary>
        /// Migrates a settings file.
        /// </summary>
        /// <param name="fromFile">The original file</param>
        /// <param name="toFile">The output file</param>
        public void Migrate(String fromFile, String toFile)
        {
			NewSettings settings = new NewSettings();

			U.L(LogLevel.Information, "Migrator", "Reading configuration");
			ReadConfig(settings, fromFile);

			U.L(LogLevel.Information, "Migrator", "Modifying configuration");

			#region Modifications

			FixTracks(settings.FileTracks);
			FixTracks(settings.QueueTracks);
			FixTracks(settings.HistoryTracks);
			FixTracks(settings.RadioTracks);
			foreach (var playlist in settings.Playlists)
				FixTracks(playlist.Tracks);
			FixSources(settings.Sources);

			var customProfiles = new List<KeyboardShortcutProfile>();
			foreach (var p in settings.ShortcutProfiles)
				if (!p.IsProtected)
					customProfiles.Add(p);

			settings.ShortcutProfiles.Clear();
			SettingsManager.InitializeShortcutProfiles(settings.ShortcutProfiles);

			// adjust custom shortcut profiles
			foreach (var profile in customProfiles)
			{
				KeyboardShortcut ks, check;
				string txt;

				// adjust untouched defaults
				ks = GetKeyboardShortcut(profile, "Application", "Add track");
				check = GetKeyboardShortcut(profile, "Alt+T");
				if ((check == null || (check.Category == "MainWindow" && check.Name == "Tracklist" && ks.Keys == "Ctrl+T")) && (ks.Keys == "Ctrl+T" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+T";

				ks = GetKeyboardShortcut(profile, "Application", "Add folder");
				check = GetKeyboardShortcut(profile, "Alt+F");
				if ((check == null || (check.Category == "MainWindow" && check.Name == "Search" && ks.Keys == "Ctrl+F")) && (ks.Keys == "Ctrl+F" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+F";

				ks = GetKeyboardShortcut(profile, "Application", "Minimize");
				if (GetKeyboardShortcut(profile, "Ctrl+Shift+M") == null && (ks.Keys == "Ctrl+M" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+Shift+N";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Now playing");
				ks.Name = "Video";
				if (GetKeyboardShortcut(profile, "Ctrl+F1") == null && (ks.Keys == "Alt+W" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+F1";
				profile.Shortcuts.Remove(ks);
				profile.Shortcuts.Insert(7, ks);

				ks = GetKeyboardShortcut(profile, "MainWindow", "Library");
				ks.Name = "Files";
				if (GetKeyboardShortcut(profile, "Ctrl+F3") == null && (ks.Keys == "Alt+L" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+F3";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Queue");
				if (GetKeyboardShortcut(profile, "Ctrl+F7") == null && (ks.Keys == "Alt+Q" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+F7";

				ks = GetKeyboardShortcut(profile, "MainWindow", "History");
				if (GetKeyboardShortcut(profile, "Ctrl+F8") == null && (ks.Keys == "Alt+H" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+F8";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Playlists");
				if (GetKeyboardShortcut(profile, "Ctrl+F9") == null && (ks.Keys == "Alt+P" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+F9";

				ks = GetKeyboardShortcut(profile, "Application", "Open playlist");
				ks.Name = "Add playlist";
				if (GetKeyboardShortcut(profile, "Alt+P") == null && (ks.Keys == "Ctrl+O" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+P";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Tracklist");
				if (GetKeyboardShortcut(profile, "Ctrl+T") == null && (ks.Keys == "Alt+T" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+T";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Search");
				if (GetKeyboardShortcut(profile, "Ctrl+F") == null && (ks.Keys == "Alt+F" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+F";

				ks = GetKeyboardShortcut(profile, "MainWindow", "General preferences");
				if (GetKeyboardShortcut(profile, "Alt+F1") == null && (ks.Keys == "Alt+G" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+F1";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Library sources");
				ks.Name = "Music sources";
				if (GetKeyboardShortcut(profile, "Alt+F2") == null && (ks.Keys == "Alt+S" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+F2";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Services");
				if (GetKeyboardShortcut(profile, "Alt+F3") == null && (ks.Keys == "Alt+V" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+F3";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Plugins");
				ks.Name = "Apps";
				if (GetKeyboardShortcut(profile, "Alt+F5") == null && (ks.Keys == "Alt+X" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+F5";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Keyboard shortcuts");
				if (GetKeyboardShortcut(profile, "Alt+F6") == null && (ks.Keys == "Alt+K" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+F6";

				ks = GetKeyboardShortcut(profile, "MainWindow", "About");
				if (GetKeyboardShortcut(profile, "Alt+F7") == null && (ks.Keys == "Alt+A" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Alt+F7";

				ks = GetKeyboardShortcut(profile, "MainWindow", "Create playlist");
				if (GetKeyboardShortcut(profile, "Ctrl+N") == null && (ks.Keys == "Alt+N" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+N";

				ks = GetKeyboardShortcut(profile, "Track", "Remove track");
				ks.Name = "Remove";
				ks = GetKeyboardShortcut(profile, "Track", "View information");
				if (GetKeyboardShortcut(profile, "Ctrl+I") == null && (ks.Keys == "Shift+I" || String.IsNullOrWhiteSpace(ks.Keys)))
					ks.Keys = "Ctrl+I";

				check = GetKeyboardShortcut(profile, "Alt+R");
				txt = check != null ? "" : "Alt+R";
				profile.Shortcuts.Insert(3, new KeyboardShortcut { Category = "Application", Name = "Add radio station", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Alt+A");
				txt = check != null ? "" : "Alt+A";
				profile.Shortcuts.Insert(4, new KeyboardShortcut { Category = "Application", Name = "Add app", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Alt+G");
				txt = check != null ? "" : "Alt+G";
				profile.Shortcuts.Insert(5, new KeyboardShortcut { Category = "Application", Name = "Generate playlist", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Ctrl+F2");
				txt = check != null ? "" : "Ctrl+F2";
				profile.Shortcuts.Insert(11, new KeyboardShortcut { Category = "MainWindow", Name = "Visualizer", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Ctrl+F4");
				txt = check != null ? "" : "Ctrl+F4";
				profile.Shortcuts.Insert(13, new KeyboardShortcut { Category = "MainWindow", Name = "YouTube", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Ctrl+F5");
				txt = check != null ? "" : "Ctrl+F5";
				profile.Shortcuts.Insert(14, new KeyboardShortcut { Category = "MainWindow", Name = "SoundCloud", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Ctrl+F6");
				txt = check != null ? "" : "Ctrl+F6";
				profile.Shortcuts.Insert(15, new KeyboardShortcut { Category = "MainWindow", Name = "Radio", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Ctrl+L");
				txt = check != null ? "" : "Ctrl+L";
				profile.Shortcuts.Insert(58, new KeyboardShortcut { Category = "Track", Name = "Open folder", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Shift+Delete");
				txt = check != null ? "" : "Shift+Delete";
				profile.Shortcuts.Insert(60, new KeyboardShortcut { Category = "Track", Name = "Remove from harddrive", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Ctrl+C");
				txt = check != null ? "" : "Ctrl+C";
				profile.Shortcuts.Insert(61, new KeyboardShortcut { Category = "Track", Name = "Copy", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Ctrl+X");
				txt = check != null ? "" : "Ctrl+X";
				profile.Shortcuts.Insert(62, new KeyboardShortcut { Category = "Track", Name = "Move", IsGlobal = false, Keys = txt });

				check = GetKeyboardShortcut(profile, "Ctrl+S");
				txt = check != null ? "" : "Ctrl+S";
				profile.Shortcuts.Insert(64, new KeyboardShortcut { Category = "Track", Name = "Share", IsGlobal = false, Keys = txt });
			}

			foreach (KeyboardShortcutProfile p in customProfiles)
				settings.ShortcutProfiles.Add(p);

			FixViewDetailsConfig(settings.SourceListConfig);
			FixViewDetailsConfig(settings.FileListConfig);
			FixViewDetailsConfig(settings.QueueListConfig);
			FixViewDetailsConfig(settings.HistoryListConfig);
			FixViewDetailsConfig(settings.YouTubeListConfig);
			foreach (PlaylistData p in settings.Playlists)
				FixViewDetailsConfig(p.ListConfig);
			FixViewDetailsConfig(settings.PluginListConfig);

			settings.CurrentEqualizerProfile = settings.EqualizerProfiles[0];

			#endregion

			U.L(LogLevel.Information, "Migrator", "Writing configuration");
			WriteConfig(settings, toFile);
		}

        #endregion

		#region Private

		private void FixSources(IEnumerable<SourceData> items)
		{
			if (items == null) return;
			foreach (var item in items)
				FixSource(item);
		}

		private void FixTracks(IEnumerable<TrackData> tracks)
		{
			if (tracks == null) return;
			foreach (TrackData track in tracks)
				FixTrack(track);
		}

		private void FixTrack(TrackData track)
		{
			if (track == null) return;
			if (track.Source == "Library")
				track.Source = "Files";
			if (track.Path.StartsWith("youtube://"))
				track.Path = "stoffi:track:youtube:" + track.Path.Substring(10);
			else if (track.Path.StartsWith("soundcloud://"))
				track.Path = "stoffi:track:soundcloud:" + track.Path.Substring(13);
			//if (File.Exists(track.Path))
			//{
			//    TagLib.File file = TagLib.File.Create(track.Path, TagLib.ReadStyle.Average);
			//    track.Bitrate = file.Properties.AudioBitrate;
			//    track.Channels = file.Properties.AudioChannels;
			//    track.SampleRate = file.Properties.AudioSampleRate;
			//    track.Codecs = "";
			//    foreach (TagLib.ICodec c in file.Properties.Codecs)
			//        if (c != null)
			//            track.Codecs += c.Description + ", ";
			//    if (track.Codecs.Length > 2)
			//        track.Codecs = track.Codecs.Substring(0, track.Codecs.Length - 2);
			//    track.Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/FileAudio.ico";
			//}
			//else if (track.Path.StartsWith("youtube://"))
			//{
			//    track.Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/YouTube.ico";
			//}
			track.Icon = track.Icon.Replace(
				"pack://application:,,,/GUI/Images/Icons/",
				"pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/"
			);
		}

		public static void FixViewDetailsConfig(ViewDetailsConfig vdc)
		{
			if (vdc == null) return;
			if (vdc.Columns.Count == 2 && vdc.Sorts.Count == 0)
			{
				vdc.Sorts.Add("asc:Type");
				vdc.Sorts.Add("asc:Data");
			}
			if (vdc.Columns.Count == 10 && vdc.Sorts.Count == 0)
			{
				vdc.Sorts.Add("asc:Title");
				vdc.Sorts.Add("asc:Track");
				vdc.Sorts.Add("asc:Album");
				vdc.Sorts.Add("asc:Artist");
			}

			foreach (ViewDetailsColumn column in vdc.Columns)
			{
				switch (column.Name)
				{
					case "Length":
						column.SortField = "Length";
						column.Converter = "Duration";
						break;

					case "LastPlayed":
						column.SortField = "LastPlayed";
						column.Converter = "DateTime";
						break;

					case "Views":
						column.SortField = "Views";
						column.Converter = "Number";
						break;

					case "PlayCount":
						column.Converter = "Number";
						break;

					case "Type":
						column.SortField = "Type";
						column.Binding = "Type";
						column.Converter = "SourceType";
						break;

					case "Installed":
						column.Converter = "DateTime";
						break;
				}
			}
		}

		private void FixSource(SourceData item)
		{
			if (item == null) return;
			item.Icon = item.Icon.Replace(
				"pack://application:,,,/GUI/Images/Icons/",
				"pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/"
			);
		}

		private KeyboardShortcut GetKeyboardShortcut(KeyboardShortcutProfile profile, String keysAsText)
		{
			foreach (KeyboardShortcut s in profile.Shortcuts)
				if (s.Keys == keysAsText)
					return s;
			return null;
		}

		private KeyboardShortcut GetKeyboardShortcut(KeyboardShortcutProfile profile, String category, String name)
		{
			foreach (KeyboardShortcut s in profile.Shortcuts)
				if (s.Name == name && s.Category == category)
					return s;
			return null;
		}

		private void SetKeyboardShortcut(KeyboardShortcut sc, String keysAsText, bool isGlobal = false)
		{
			sc.Keys = keysAsText;
			sc.IsGlobal = isGlobal;
		}

		private void SetKeyboardShortcut(KeyboardShortcutProfile profile, String category, String name, String keysAsText, bool isGlobal = false)
		{
			KeyboardShortcut sc = GetKeyboardShortcut(profile, category, name);
			if (sc == null)
			{
				sc = new KeyboardShortcut();
				sc.Category = category;
				sc.Name = name;
				profile.Shortcuts.Add(sc);
			}
			SetKeyboardShortcut(sc, keysAsText, isGlobal);
		}

		private void InitShortcutProfile(KeyboardShortcutProfile profile, String name, bool isprotected)
		{
			profile.Name = name;
			profile.IsProtected = isprotected;
			profile.Shortcuts = new List<KeyboardShortcut>();

			// set the default shortcuts
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add track", IsGlobal = false, Keys = "Alt+T" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add folder", IsGlobal = false, Keys = "Alt+F" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add playlist", IsGlobal = false, Keys = "Alt+P" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add radio station", IsGlobal = false, Keys = "Alt+R" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add app", IsGlobal = false, Keys = "Alt+A" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Generate playlist", IsGlobal = false, Keys = "Alt+G" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Minimize", IsGlobal = false, Keys = "Ctrl+Shift+M" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Restore", IsGlobal = true, Keys = "Ctrl+Shift+R" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Help", IsGlobal = false, Keys = "F1" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Close", IsGlobal = false, Keys = "Ctrl+W" });

			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Video", IsGlobal = false, Keys = "Ctrl+F1" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Visualizer", IsGlobal = false, Keys = "Ctrl+F2" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Files", IsGlobal = false, Keys = "Ctrl+F3" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "YouTube", IsGlobal = false, Keys = "Ctrl+F4" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "SoundCloud", IsGlobal = false, Keys = "Ctrl+F5" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Radio", IsGlobal = false, Keys = "Ctrl+F6" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Queue", IsGlobal = false, Keys = "Ctrl+F7" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "History", IsGlobal = false, Keys = "Ctrl+F8" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Playlists", IsGlobal = false, Keys = "Ctrl+F9" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Tracklist", IsGlobal = false, Keys = "Ctrl+T" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Search", IsGlobal = false, Keys = "Ctrl+F" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "General preferences", IsGlobal = false, Keys = "Alt+F1" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Music sources", IsGlobal = false, Keys = "Alt+F2" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Services", IsGlobal = false, Keys = "Alt+F3" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Apps", IsGlobal = false, Keys = "Alt+F5" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Keyboard shortcuts", IsGlobal = false, Keys = "Alt+F6" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "About", IsGlobal = false, Keys = "Alt+F7" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Toggle details pane", IsGlobal = false, Keys = "Alt+D" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Toggle menu bar", IsGlobal = false, Keys = "Alt+M" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Create playlist", IsGlobal = false, Keys = "Ctrl+N" });

			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Play or pause", IsGlobal = false, Keys = "Alt+5 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Next", IsGlobal = false, Keys = "Alt+6 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Previous", IsGlobal = false, Keys = "Alt+4 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Toggle shuffle", IsGlobal = false, Keys = "Alt+9 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Toggle repeat", IsGlobal = false, Keys = "Alt+7 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Increase volume", IsGlobal = false, Keys = "Alt+8 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Decrease volume", IsGlobal = false, Keys = "Alt+2 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Seek forward", IsGlobal = false, Keys = "Alt+3 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Seek backward", IsGlobal = false, Keys = "Alt+1 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Add bookmark", IsGlobal = false, Keys = "Alt+B" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to previous bookmark", IsGlobal = false, Keys = "Alt+Left" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to next bookmark", IsGlobal = false, Keys = "Alt+Right" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to first bookmark", IsGlobal = false, Keys = "Alt+Home" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to last bookmark", IsGlobal = false, Keys = "Alt+End" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 1", IsGlobal = false, Keys = "Alt+1" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 2", IsGlobal = false, Keys = "Alt+2" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 3", IsGlobal = false, Keys = "Alt+3" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 4", IsGlobal = false, Keys = "Alt+4" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 5", IsGlobal = false, Keys = "Alt+5" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 6", IsGlobal = false, Keys = "Alt+6" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 7", IsGlobal = false, Keys = "Alt+7" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 8", IsGlobal = false, Keys = "Alt+8" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 9", IsGlobal = false, Keys = "Alt+9" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 10", IsGlobal = false, Keys = "Alt+0" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to current track", IsGlobal = false, Keys = "Alt+C" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to selected track", IsGlobal = false, Keys = "Alt+X" });

			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Play track", IsGlobal = false, Keys = "Enter" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Queue and dequeue", IsGlobal = false, Keys = "Shift+Q" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Open folder", IsGlobal = false, Keys = "Ctrl+L" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Remove", IsGlobal = false, Keys = "Delete" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Remove from harddrive", IsGlobal = false, Keys = "Shift+Delete" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Copy", IsGlobal = false, Keys = "Ctrl+C" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Move", IsGlobal = false, Keys = "Ctrl+X" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "View information", IsGlobal = false, Keys = "Ctrl+I" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Share", IsGlobal = false, Keys = "Shift+S" });
		}

		private void ReadConfig(NewSettings settings, String file)
		{
			U.L(LogLevel.Debug, "Migrator", "Reading config");
			if (File.Exists(file) == false)
			{
				U.L(LogLevel.Error, "Migrator", "Could not find data file " + file);
				return;
			}

			XmlTextReader xmlReader = new XmlTextReader(file);
			xmlReader.WhitespaceHandling = WhitespaceHandling.None;
			xmlReader.Read();

			while (xmlReader.Read())
			{
				if (xmlReader.NodeType == XmlNodeType.Element)
				{
					if (xmlReader.Name == "setting")
					{
						String name = "";
						for (int i = 0; i < xmlReader.AttributeCount; xmlReader.MoveToAttribute(i), i++)
							if (xmlReader.Name == "name") name = xmlReader.Value;

						xmlReader.Read();
						if (!xmlReader.IsEmptyElement)
							xmlReader.Read();

						U.L(LogLevel.Debug, "Migrator", "Parsing attribute '" + name + "'");

						if (name == "WinWidth")
							settings.WinWidth = xmlReader.Value;

						else if (name == "WinHeight")
							settings.WinHeight = xmlReader.Value;

						else if (name == "WinLeft")
							settings.WinLeft = xmlReader.Value;

						else if (name == "WinTop")
							settings.WinTop = xmlReader.Value;

						else if (name == "WinState")
							settings.WinState = xmlReader.Value;

						else if (name == "EqualizerHeight")
							settings.EqualizerHeight = xmlReader.Value;

						else if (name == "EqualizerWidth")
							settings.EqualizerWidth = xmlReader.Value;

						else if (name == "EqualizerLeft")
							settings.EqualizerLeft = xmlReader.Value;

						else if (name == "EqualizerTop")
							settings.EqualizerTop = xmlReader.Value;

						else if (name == "CurrentSelectedNavigation")
							settings.CurrentSelectedNavigation = xmlReader.Value;

						else if (name == "NavigationPaneWidth")
							settings.NavigationPaneWidth = xmlReader.Value;

						else if (name == "DetailsPaneHeight")
							settings.DetailsPaneHeight = xmlReader.Value;

						else if (name == "MenuBarVisible")
							settings.MenuBarVisible = xmlReader.Value;

						else if (name == "DetailsPaneVisible")
							settings.DetailsPaneVisible = xmlReader.Value;

						else if (name == "Language")
							settings.Language = xmlReader.Value;

						else if (name == "SourceListConfig")
							settings.SourceListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "Sources")
							settings.Sources = ReadSetting<List<SourceData>>(xmlReader);

						else if (name == "PluginListConfig")
							settings.PluginListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "HistoryListConfig")
							settings.HistoryListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "HistoryTracks")
						{
							//settings.HistoryTracks = ReadSetting<List<TrackData>>(xmlReader);
							List<Old.TrackData> tracks = ReadSetting<List<Old.TrackData>>(xmlReader);
							settings.HistoryTracks = new List<TrackData>();
							foreach (Old.TrackData t in tracks)
								settings.HistoryTracks.Add(ParseOldTrack(t));
						}

						else if (name == "FileListConfig" || name == "LibraryListConfig")
							settings.FileListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "FileTracks" || name == "LibraryTracks")
						{
							//settings.FileTracks = ReadSetting<List<TrackData>>(xmlReader);
							List<Old.TrackData> tracks = ReadSetting<List<Old.TrackData>>(xmlReader);
							settings.FileTracks = new List<TrackData>();
							foreach (Old.TrackData t in tracks)
								settings.FileTracks.Add(ParseOldTrack(t));
						}

						else if (name == "RadioListConfig")
							settings.RadioListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "RadioTracks")
						{
							//settings.RadioTracks = ReadSetting<List<TrackData>>(xmlReader);
							List<Old.TrackData> tracks = ReadSetting<List<Old.TrackData>>(xmlReader);
							settings.RadioTracks = new List<TrackData>();
							foreach (Old.TrackData t in tracks)
								settings.RadioTracks.Add(ParseOldTrack(t));
						}

						else if (name == "DiscListConfig")
							settings.DiscListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "QueueListConfig")
							settings.QueueListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "QueueTracks")
						{
							//settings.QueueTracks = ReadSetting<List<TrackData>>(xmlReader);
							List<Old.TrackData> tracks = ReadSetting<List<Old.TrackData>>(xmlReader);
							settings.QueueTracks = new List<TrackData>();
							foreach (Old.TrackData t in tracks)
								settings.QueueTracks.Add(ParseOldTrack(t));
						}

						else if (name == "YouTubeListConfig")
							settings.YouTubeListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "SoundCloudListConfig")
							settings.SoundCloudListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "ID")
							settings.ID = xmlReader.Value;

						else if (name == "UpgradePolicy")
							settings.UpgradePolicy = xmlReader.Value;

						else if (name == "SearchPolicy")
							settings.SearchPolicy = xmlReader.Value;

						else if (name == "OpenAddPolicy")
							settings.OpenAddPolicy = xmlReader.Value;

						else if (name == "OpenPlayPolicy")
							settings.OpenPlayPolicy = xmlReader.Value;

						else if (name == "MinimizeToTray")
							settings.MinimizeToTray = xmlReader.Value;

						else if (name == "ShowOSD")
							settings.ShowOSD = xmlReader.Value;

						else if (name == "PauseWhenLocked")
							settings.PauseWhenLocked = xmlReader.Value;

						else if (name == "PauseWhenSongEnds")
							settings.PauseWhenSongEnds = xmlReader.Value;

						else if (name == "CurrentShortcutProfile")
							settings.CurrentShortcutProfile = xmlReader.Value;

						else if (name == "ShortcutProfiles")
							settings.ShortcutProfiles = ReadSetting<List<KeyboardShortcutProfile>>(xmlReader);

						else if (name == "PluginSettings")
							settings.PluginSettings = ReadSetting<List<PluginSettings>>(xmlReader);

						else if (name == "YouTubeFilter")
							settings.YouTubeFilter = xmlReader.Value;

						else if (name == "YouTubeQuality")
							settings.YouTubeQuality = xmlReader.Value;

						else if (name == "CurrentActiveNavigation")
							settings.CurrentActiveNavigation = xmlReader.Value;

						else if (name == "CurrentTrack")
						{
							//settings.CurrentTrack = ReadSetting<TrackData>(xmlReader);
							settings.CurrentTrack = ParseOldTrack(ReadSetting<Old.TrackData>(xmlReader));
						}

						else if (name == "CurrentEqualizerProfile")
							settings.CurrentEqualizerProfile = ReadSetting<EqualizerProfile>(xmlReader);

						else if (name == "EqualizerProfiles")
							settings.EqualizerProfiles = ReadSetting<List<EqualizerProfile>>(xmlReader);

						else if (name == "HistoryIndex")
							settings.HistoryIndex = xmlReader.Value;

						else if (name == "Shuffle")
							settings.Shuffle = xmlReader.Value;

						else if (name == "Repeat")
							settings.Repeat = xmlReader.Value;

						else if (name == "Volume")
							settings.Volume = xmlReader.Value;

						else if (name == "Seek")
							settings.Seek = xmlReader.Value;

						else if (name == "MediaState")
							settings.MediaState = xmlReader.Value;

						else if (name == "CloudIdentities")
							settings.CloudIdentities = ReadSetting<List<CloudIdentity>>(xmlReader);

						else if (name == "SubmitSongs")
							settings.SubmitSongs = xmlReader.Value;

						else if (name == "ListenBuffer")
							settings.ListenBuffer = ReadSetting<System.Collections.Generic.Dictionary<string, System.Tuple<string, string>>>(xmlReader);

						else if (name == "FirstRun")
							settings.FirstRun = xmlReader.Value;

						else if (name == "UpgradeCheck")
							settings.UpgradeCheck = xmlReader.Value;

						else if (name == "Playlists")
						{
							//settings.Playlists = ReadSetting<List<PlaylistData>>(xmlReader);
							List<Old.PlaylistData> playlists = ReadSetting<List<Old.PlaylistData>>(xmlReader);
							settings.Playlists = new List<PlaylistData>();
							foreach (Old.PlaylistData p in playlists)
							{
								PlaylistData pl = new PlaylistData();
								pl.Name = p.Name;
								pl.Time = double.Parse(p.Time, CultureInfo.GetCultureInfo("en-US"));
								pl.ListConfig = p.ListConfig;
								pl.Tracks = new ObservableCollection<TrackData>();
								foreach (Old.TrackData t in p.Tracks)
									pl.Tracks.Add(ParseOldTrack(t));
								settings.Playlists.Add(pl);
							}
						}

						else if (name == "OAuthToken")
							settings.OAuthToken = xmlReader.Value;

						else if (name == "OAuthSecret")
							settings.OAuthSecret = xmlReader.Value;

						else if (name == "CurrentVisualizer")
							settings.CurrentVisualizer = xmlReader.Value;

						U.L(LogLevel.Debug, "Migrator", "Done");
					}
				}
			}

			xmlReader.Close();
		}

		private TrackData ParseOldTrack(Old.TrackData track)
		{
			if (track == null) return null;
			TrackData t = new TrackData();
			t.Album = track.Album;
			t.Artist = track.Artist;
			t.ArtURL = track.ArtURL;
			t.Bitrate = track.Bitrate;
			t.Bookmarks = track.Bookmarks;
			t.Channels = track.Channels;
			t.Codecs = track.Codecs;
			t.Genre = track.Genre;
			t.Icon = track.Icon;
			t.IsActive = track.IsActive;
			if (!String.IsNullOrWhiteSpace(track.LastPlayed))
				t.LastPlayed = DateTime.Parse(track.LastPlayed);
			t.LastWrite = track.LastWrite;
			t.Length = track.RawLength;
			t.Number = track.Number;
			t.Path = track.Path;
			t.PlayCount = track.PlayCount;
			t.Processed = track.Processed;
			t.SampleRate = track.SampleRate;
			t.Source = track.Source;
			t.Strike = track.Strike;
			t.Title = track.Title;
			t.Track = track.Track;
			t.URL = track.URL;
			t.Views = track.RawViews;
			t.Year = track.Year;
			return t;
		}

		private void WriteConfig(NewSettings settings, String file)
		{
			U.L(LogLevel.Debug, "Migrator", "Writing config");
			XmlTextWriter xmlWriter = new XmlTextWriter(file, Encoding.UTF8);
			xmlWriter.WriteStartDocument();
			xmlWriter.WriteWhitespace("\n");
			xmlWriter.WriteStartElement("configuration");
			xmlWriter.WriteWhitespace("\n    ");

			xmlWriter.WriteStartElement("configSections");
			xmlWriter.WriteWhitespace("\n        ");
			xmlWriter.WriteStartElement("sectionGroup");
			xmlWriter.WriteStartAttribute("name");
			xmlWriter.WriteString("userSettings");
			xmlWriter.WriteEndAttribute();

			xmlWriter.WriteWhitespace("\n            ");
			xmlWriter.WriteStartElement("section");
			xmlWriter.WriteStartAttribute("name");
			xmlWriter.WriteString("Stoffi.Properties.Settings");
			xmlWriter.WriteEndAttribute();
			xmlWriter.WriteStartAttribute("type");
			xmlWriter.WriteString("System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			xmlWriter.WriteEndAttribute();
			xmlWriter.WriteStartAttribute("allowExeDefinition");
			xmlWriter.WriteString("MachineToLocalUser");
			xmlWriter.WriteEndAttribute();
			xmlWriter.WriteStartAttribute("requirePermission");
			xmlWriter.WriteString("false");
			xmlWriter.WriteEndAttribute();
			xmlWriter.WriteEndElement();

			xmlWriter.WriteWhitespace("\n        ");
			xmlWriter.WriteEndElement();

			xmlWriter.WriteWhitespace("\n    ");
			xmlWriter.WriteEndElement();


			xmlWriter.WriteWhitespace("\n    ");
			xmlWriter.WriteStartElement("userSettings");
			xmlWriter.WriteWhitespace("\n        ");
			xmlWriter.WriteStartElement("Stoffi.Properties.Settings");

			WriteSetting(xmlWriter, "WinWidth", 3, settings.WinWidth);
			WriteSetting(xmlWriter, "WinHeight", 3, settings.WinHeight);
			WriteSetting(xmlWriter, "WinTop", 3, settings.WinTop);
			WriteSetting(xmlWriter, "WinLeft", 3, settings.WinLeft);
			WriteSetting(xmlWriter, "WinState", 3, settings.WinState);
			WriteSetting(xmlWriter, "EqualizerHeight", 3, settings.EqualizerHeight);
			WriteSetting(xmlWriter, "EqualizerWidth", 3, settings.EqualizerWidth);
			WriteSetting(xmlWriter, "EqualizerLeft", 3, settings.EqualizerLeft);
			WriteSetting(xmlWriter, "EqualizerTop", 3, settings.EqualizerTop);
			WriteSetting(xmlWriter, "CurrentSelectedNavigation", 3, settings.CurrentSelectedNavigation);
			WriteSetting(xmlWriter, "NavigationPaneWidth", 3, settings.NavigationPaneWidth);
			WriteSetting(xmlWriter, "DetailsPaneHeight", 3, settings.DetailsPaneHeight);
			WriteSetting(xmlWriter, "DetailsPaneVisibile", 3, settings.DetailsPaneVisible);
			WriteSetting(xmlWriter, "MenuBarVisible", 3, settings.MenuBarVisible);
			WriteSetting(xmlWriter, "Language", 3, settings.Language);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "SourceListConfig", "Xml", 3, settings.SourceListConfig);
			WriteSetting<List<SourceData>>(xmlWriter, "Sources", "Xml", 3, settings.Sources);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "PluginListConfig", "Xml", 3, settings.PluginListConfig);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "HistoryListConfig", "Xml", 3, settings.HistoryListConfig);
			WriteSetting<List<TrackData>>(xmlWriter, "HistoryTracks", "Xml", 3, settings.HistoryTracks);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "FileListConfig", "Xml", 3, settings.FileListConfig);
			WriteSetting<List<TrackData>>(xmlWriter, "FileTracks", "Xml", 3, settings.FileTracks);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "RadioListConfig", "Xml", 3, settings.RadioListConfig);
			WriteSetting<List<TrackData>>(xmlWriter, "RadioTracks", "Xml", 3, settings.RadioTracks);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "DiscListConfig", "Xml", 3, settings.DiscListConfig);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "QueueListConfig", "Xml", 3, settings.QueueListConfig);
			WriteSetting<List<TrackData>>(xmlWriter, "QueueTracks", "Xml", 3, settings.QueueTracks);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "YouTubeListConfig", "Xml", 3, settings.YouTubeListConfig);
			WriteSetting<ViewDetailsConfig>(xmlWriter, "SoundCloudListConfig", "Xml", 3, settings.SoundCloudListConfig);
			WriteSetting(xmlWriter, "ID", 3, settings.ID);
			WriteSetting(xmlWriter, "UpgradePolicy", 3, settings.UpgradePolicy);
			WriteSetting(xmlWriter, "SearchPolicy", 3, settings.SearchPolicy);
			WriteSetting(xmlWriter, "OpenAddPolicy", 3, settings.OpenAddPolicy);
			WriteSetting(xmlWriter, "OpenPlayPolicy", 3, settings.OpenPlayPolicy);
			WriteSetting(xmlWriter, "MinimizeToTray", 3, settings.MinimizeToTray);
			WriteSetting(xmlWriter, "ShowOSD", 3, settings.ShowOSD);
			WriteSetting(xmlWriter, "PauseWhenLocked", 3, settings.PauseWhenLocked);
			WriteSetting(xmlWriter, "PauseWhenSongEnds", 3, settings.PauseWhenSongEnds);
			WriteSetting(xmlWriter, "CurrentShortcutProfile", 3, settings.CurrentShortcutProfile);
			WriteSetting<List<KeyboardShortcutProfile>>(xmlWriter, "ShortcutProfiles", "Xml", 3, settings.ShortcutProfiles);
			WriteSetting<List<PluginSettings>>(xmlWriter, "PluginSettings", "Xml", 3, settings.PluginSettings);
			WriteSetting(xmlWriter, "YouTubeFilter", 3, settings.YouTubeFilter);
			WriteSetting(xmlWriter, "YouTubeQuality", 3, settings.YouTubeQuality);
			WriteSetting(xmlWriter, "CurrentActiveNavigation", 3, settings.CurrentActiveNavigation);
			WriteSetting<TrackData>(xmlWriter, "CurrentTrack", "Xml", 3, settings.CurrentTrack);
			WriteSetting<EqualizerProfile>(xmlWriter, "CurrentEqualizerProfile", "Xml", 3, settings.CurrentEqualizerProfile);
			WriteSetting<List<EqualizerProfile>>(xmlWriter, "EqualizerProfiles", "Xml", 3, settings.EqualizerProfiles);
			WriteSetting(xmlWriter, "HistoryIndex", 3, settings.HistoryIndex);
			WriteSetting(xmlWriter, "Shuffle", 3, settings.Shuffle);
			WriteSetting(xmlWriter, "Repeat", 3, settings.Repeat);
			WriteSetting(xmlWriter, "Volume", 3, settings.Volume);
			WriteSetting(xmlWriter, "Seek", 3, settings.Seek);
			WriteSetting(xmlWriter, "MediaState", 3, settings.MediaState);
			WriteSetting<List<CloudIdentity>>(xmlWriter, "CloudIdentities", "Xml", 3, settings.CloudIdentities);
			WriteSetting(xmlWriter, "SubmitSongs", 3, settings.SubmitSongs);
			WriteSetting<Dictionary<string, Tuple<string, string>>>(xmlWriter, "ListenBuffer", "Xml", 3, settings.ListenBuffer);
			WriteSetting(xmlWriter, "FirstRun", 3, settings.FirstRun);
			WriteSetting(xmlWriter, "UpgradeCheck", 3, settings.UpgradeCheck);
			WriteSetting<List<PlaylistData>>(xmlWriter, "Playlists", "Xml", 3, settings.Playlists);
			WriteSetting(xmlWriter, "OAuthToken", 3, settings.OAuthToken);
			WriteSetting(xmlWriter, "OAuthSecret", 3, settings.OAuthSecret);
			WriteSetting(xmlWriter, "CurrentVisualizer", 3, settings.CurrentVisualizer);

			xmlWriter.WriteWhitespace("\n        ");
			xmlWriter.WriteEndElement();
			xmlWriter.WriteWhitespace("\n    ");
			xmlWriter.WriteEndElement();
			xmlWriter.WriteWhitespace("\n");
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndDocument();
			U.L(LogLevel.Debug, "Migrator", "Write done");
			xmlWriter.Close();
		}

		/// <summary>
		/// Writes a settings to the XML settings file
		/// </summary>
		/// <typeparam name="T">The type of the settings</typeparam>
		/// <param name="xmlWriter">The XmlWriter</param>
		/// <param name="setting">The name of the setting</param>
		/// <param name="serializeAs">A string describing how the setting is serialized</param>
		/// <param name="indent">The number of spaces used for indentation</param>
		/// <param name="value">The value</param>
		private void WriteSetting<T>(XmlWriter xmlWriter, String setting, String serializeAs, int indent, T value)
		{
			String indentString = "";
			for (int i = 0; i < indent; i++)
				indentString += "    ";

			xmlWriter.WriteWhitespace("\n" + indentString);
			xmlWriter.WriteStartElement("setting");
			xmlWriter.WriteStartAttribute("name");
			xmlWriter.WriteString(setting);
			xmlWriter.WriteEndAttribute();
			xmlWriter.WriteStartAttribute("serializeAs");
			xmlWriter.WriteString(serializeAs);
			xmlWriter.WriteEndAttribute();

			xmlWriter.WriteWhitespace("\n" + indentString + "    ");
			xmlWriter.WriteStartElement("value");

			if (value != null)
			{
				XmlSerializer ser = new XmlSerializer(typeof(T));
				ser.Serialize(xmlWriter, value);
			}

			xmlWriter.WriteEndElement();

			xmlWriter.WriteWhitespace("\n" + indentString);
			xmlWriter.WriteEndElement();
		}

		/// <summary>
		/// Reads a setting from the XML settings file
		/// </summary>
		/// <typeparam name="T">The type of the setting</typeparam>
		/// <param name="xmlReader">The xml reader</param>
		/// <returns>The deserialized setting</returns>
		private T ReadSetting<T>(XmlTextReader xmlReader)
		{
			try
			{
				XmlSerializer ser = new XmlSerializer(typeof(T));
				return (T)ser.Deserialize(xmlReader);
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "Migrator", "Could not read "+typeof(T)+": "+e.Message);
				return (T)(null as object);
			}
		}

		private void WriteSetting(XmlWriter xmlWriter, String setting, int indent, String value)
		{
			if (value == "" || value == null) return;
			String indentString = "";
			for (int i = 0; i < indent; i++)
				indentString += "    ";

			xmlWriter.WriteWhitespace("\n" + indentString);
			xmlWriter.WriteStartElement("setting");
			xmlWriter.WriteStartAttribute("name");
			xmlWriter.WriteString(setting);
			xmlWriter.WriteEndAttribute();
			xmlWriter.WriteStartAttribute("serializeAs");
			xmlWriter.WriteString("String");
			xmlWriter.WriteEndAttribute();

			xmlWriter.WriteWhitespace("\n" + indentString + "    ");
			xmlWriter.WriteStartElement("value");
			xmlWriter.WriteString(value);
			xmlWriter.WriteEndElement();

			xmlWriter.WriteWhitespace("\n" + indentString);
			xmlWriter.WriteEndElement();
		}

        #endregion

        #endregion
	}

	/// <summary>
	/// Describes a configuration for the ViewDetails class
	/// </summary>
	public class ViewDetailsConfig : INotifyPropertyChanged
	{
		#region Members

		string filter = "";

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the columns
		/// </summary>
		public ObservableCollection<ViewDetailsColumn> Columns { get; set; }

		/// <summary>
		/// Gets or sets the number column configuration
		/// </summary>
		public ViewDetailsColumn NumberColumn { get; set; }

		/// <summary>
		/// Gets or sets the indices of the selected items
		/// </summary>
		public List<int> SelectedIndices { get; set; }

		/// <summary>
		/// Gets or sets the the sort orders
		/// Each sort is represented as a string on the format
		/// "asc/dsc:ColumnName"
		/// </summary>
		public List<string> Sorts { get; set; }

		/// <summary>
		/// Gets or sets text used to filter the list
		/// </summary>
		public string Filter
		{
			get { return filter; }
			set
			{
				filter = value;
				OnPropertyChanged("Filter");
			}
		}

		/// <summary>
		/// Gets or sets whether the number column should be enabled
		/// </summary>
		public bool HasNumber { get; set; }

		/// <summary>
		/// Gets or sets whether the number column should be visible
		/// </summary>
		public bool IsNumberVisible { get; set; }

		/// <summary>
		/// Gets or sets the position of the number column
		/// </summary>
		public int NumberIndex { get; set; }

		/// <summary>
		/// Gets or sets whether to display icons or not
		/// </summary>
		public bool UseIcons { get; set; }

		/// <summary>
		/// Gets or sets whether files can be dropped onto the list
		/// </summary>
		public bool AcceptFileDrops { get; set; }

		/// <summary>
		/// Gets or sets whether the list can be resorted via drag and drop
		/// </summary>
		public bool IsDragSortable { get; set; }

		/// <summary>
		/// Gets or sets whether the list can be resorted by clicking on a column
		/// </summary>
		public bool IsClickSortable { get; set; }

		/// <summary>
		/// Gets or sets whether only the number column can be used to sort the list
		/// </summary>
		public bool LockSortOnNumber { get; set; }

		/// <summary>
		/// Gets or sets the vertical scroll offset
		/// </summary>
		public double VerticalScrollOffset { get; set; }

		/// <summary>
		/// Gets or sets the horizontal scroll offset
		/// </summary>
		public double HorizontalScrollOffset { get; set; }

		/// <summary>
		/// Gets or sets the vertical scroll offset when no search is active.
		/// </summary>
		public double VerticalScrollOffsetWithoutSearch { get; set; }

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion
	}

	/// <summary>
	/// Represents a column of a details list
	/// </summary>
	public class ViewDetailsColumn : INotifyPropertyChanged
	{
		#region Members

		private string text;
		private string binding;
		private string converter;
		private string sortField;
		private bool isAlwaysVisible = false;
		private bool isSortable = true;
		private double width = 50.0;
		private bool isVisible = true;
		private Alignment alignment = Alignment.Left;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the name of the column
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the displayed text
		/// </summary>
		public string Text
		{
			get { return text; }
			set
			{
				text = value;
				OnPropertyChanged("Text");
			}
		}

		/// <summary>
		/// Gets or sets the value to bind to
		/// </summary>
		public string Binding
		{
			get { return binding; }
			set
			{
				binding = value;
				OnPropertyChanged("Binding");
			}
		}

		/// <summary>
		/// Gets or sets the converter that should be used to present the value of the binding.
		/// </summary>
		public string Converter
		{
			get { return converter; }
			set
			{
				converter = value;
				OnPropertyChanged("Converter");
			}
		}

		/// <summary>
		/// Gets or sets the value to sort on
		/// </summary>
		public string SortField
		{
			get { return sortField; }
			set
			{
				sortField = value;
				OnPropertyChanged("SortField");
			}
		}

		/// <summary>
		/// Gets or sets whether the column is always visible
		/// </summary>
		public bool IsAlwaysVisible
		{
			get { return isAlwaysVisible; }
			set
			{
				isAlwaysVisible = value;
				OnPropertyChanged("IsAlwaysVisible");
			}
		}

		/// <summary>
		/// Gets or sets whether the column is sortable
		/// </summary>
		public bool IsSortable
		{
			get { return isSortable; }
			set
			{
				isSortable = value;
				OnPropertyChanged("IsSortable");
			}
		}

		/// <summary>
		/// Gets or sets the width of the column
		/// </summary>
		public double Width
		{
			get { return width; }
			set
			{
				width = value;
				OnPropertyChanged("Width");
			}
		}

		/// <summary>
		/// Gets or sets whether the column is visible (only effective if IsAlwaysVisible is false)
		/// </summary>
		public bool IsVisible
		{
			get { return isVisible; }
			set
			{
				isVisible = value;
				OnPropertyChanged("IsVisible");
			}
		}

		/// <summary>
		/// Gets or sets the text alignment of the displayed text
		/// </summary>
		public Alignment Alignment
		{
			get { return alignment; }
			set
			{
				alignment = value;
				OnPropertyChanged("Alignment");
			}
		}

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion
	}

	/// <summary>
	/// Describes the data source of an item inside the ViewDetails list
	/// </summary>
	public class ViewDetailsItemData : INotifyPropertyChanged
	{
		#region Members

		private int number;
		private bool isActive;
		private string icon;
		private bool strike;
		private bool disabled = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the index number of the item
		/// </summary>
		public int Number
		{
			get { return number; }
			set { number = value; OnPropertyChanged("Number"); }
		}

		/// <summary>
		/// Gets or sets whether the item is marked as active or not
		/// </summary>
		public bool IsActive
		{
			get { return isActive; }
			set { isActive = value; OnPropertyChanged("IsActive"); }
		}

		/// <summary>
		/// Gets or sets the icon of the item
		/// </summary>
		public string Icon
		{
			get { return icon; }
			set { icon = value; OnPropertyChanged("Icon"); }
		}

		/// <summary>
		/// Gets or sets whether the items should feature a strikethrough
		/// </summary>
		public bool Strike
		{
			get { return strike; }
			set { strike = value; OnPropertyChanged("Strike"); }
		}

		/// <summary>
		/// Gets or sets whether the items should be viewed as disabled (for example grayed out)
		/// </summary>
		public bool Disabled
		{
			get { return disabled; }
			set { disabled = value; OnPropertyChanged("Disabled"); }
		}

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion
	}

	namespace Old
	{
		/// <summary>
		/// Describes an old playlist
		/// </summary>
		public class PlaylistData
		{
			#region Properites

			/// <summary>
			/// 
			/// </summary>
			public String Name { get; set; }

			/// <summary>
			/// 
			/// </summary>
			public String Time { get; set; }

			/// <summary>
			/// 
			/// </summary>
			public List<TrackData> Tracks { get; set; }

			/// <summary>
			/// Get or sets the configuration of the list view
			/// </summary>
			public ViewDetailsConfig ListConfig { get; set; }

			#endregion
		}

		/// <summary>
		/// Describes an old track.
		/// </summary>
		public class TrackData : ViewDetailsItemData
		{
			#region Properties

			/// <summary>
			/// Gets or sets the artist of the track.
			/// </summary>
			public String Artist { get; set; }

			/// <summary>
			/// Gets or sets the title of the track.
			/// </summary>
			public String Title { get; set; }

			/// <summary>
			/// Gets or sets the album of the track.
			/// </summary>
			public String Album { get; set; }

			/// <summary>
			/// Gets or sets the genre of the track.
			/// </summary>
			public String Genre { get; set; }

			/// <summary>
			/// Gets or sets the number of the track on the album.
			/// </summary>
			public uint Track { get; set; }

			/// <summary>
			/// Gets or sets the year the track was made.
			/// </summary>
			public uint Year { get; set; }

			/// <summary>
			/// Gets or sets the length of the track in seconds.
			/// </summary>
			public String Length { get; set; }

			/// <summary>
			/// Gets or sets the length of the track in seconds.
			/// </summary>
			public double RawLength { get; set; }

			/// <summary>
			/// Gets or sets the path to the track.
			/// </summary>
			public String Path { get; set; }

			/// <summary>
			/// Gets or sets the url of the track.
			/// </summary>
			public String URL { get; set; }

			/// <summary>
			/// Gets or sets the art url of the track.
			/// </summary>
			public String ArtURL { get; set; }

			/// <summary>
			/// Gets or sets the number of times that the track has been played.
			/// </summary>
			public uint PlayCount { get; set; }

			/// <summary>
			/// Gets or sets the time the track was last played.
			/// </summary>
			public String LastPlayed { get; set; }

			/// <summary>
			/// Gets or sets the time the track was last played (in epoch time).
			/// </summary>
			public String RawLastPlayed { get; set; }

			/// <summary>
			/// Gets or sets the amount of views on YouTube.
			/// </summary>
			public int RawViews { get; set; }

			/// <summary>
			/// Gets or sets whether the track has been scanned for meta data.
			/// </summary>
			public bool Processed { get; set; }

			/// <summary>
			/// Gets or sets the time that the file was last written/updated.
			/// </summary>
			public long LastWrite { get; set; }

			/// <summary>
			/// Gets or sets the bitrate of the track.
			/// </summary>
			public int Bitrate { get; set; }

			/// <summary>
			/// Gets or sets the number of channels of the track.
			/// </summary>
			public int Channels { get; set; }

			/// <summary>
			/// Gets or sets the sample rate of the track.
			/// </summary>
			public int SampleRate { get; set; }

			/// <summary>
			/// Gets or sets the codecs of the track.
			/// </summary>
			public string Codecs { get; set; }

			/// <summary>
			/// Gets or sets where the track belongs to ("Files", "Playlist:Name").
			/// </summary>
			public string Source { get; set; }

			/// <summary>
			/// Gets or sets the bookmarks of the track (percentage).
			/// </summary>
			public List<double> Bookmarks { get; set; }

			#endregion
		}
	}

	/// <summary>
	/// The settings after the migration.
	/// </summary>
	public class NewSettings
	{
		#region Properties

		public String WinWidth { get; set; }
		public String WinHeight { get; set; }
		public String WinLeft { get; set; }
		public String WinTop { get; set; }
		public String WinState { get; set; }
		public String EqualizerHeight { get; set; }
		public String EqualizerWidth { get; set; }
		public String EqualizerLeft { get; set; }
		public String EqualizerTop { get; set; }
		public String CurrentSelectedNavigation { get; set; }
		public String NavigationPaneWidth { get; set; }
		public String DetailsPaneHeight { get; set; }
		public String DetailsPaneVisible { get; set; }
		public String MenuBarVisible { get; set; }
		public String Language { get; set; }
		public ViewDetailsConfig SourceListConfig { get; set; }
		public List<SourceData> Sources { get; set; }
		public ViewDetailsConfig PluginListConfig { get; set; }
		public ViewDetailsConfig HistoryListConfig { get; set; }
		public List<TrackData> HistoryTracks { get; set; }
		public ViewDetailsConfig FileListConfig { get; set; }
		public List<TrackData> FileTracks { get; set; }
		public ViewDetailsConfig RadioListConfig { get; set; }
		public List<TrackData> RadioTracks { get; set; }
		public ViewDetailsConfig DiscListConfig { get; set; }
		public ViewDetailsConfig QueueListConfig { get; set; }
		public List<TrackData> QueueTracks { get; set; }
		public ViewDetailsConfig YouTubeListConfig { get; set; }
		public ViewDetailsConfig SoundCloudListConfig { get; set; }
		public String ID { get; set; }
		public String UpgradePolicy { get; set; }
		public String SearchPolicy { get; set; }
		public String OpenAddPolicy { get; set; }
		public String OpenPlayPolicy { get; set; }
		public String MinimizeToTray { get; set; }
		public String ShowOSD { get; set; }
		public String PauseWhenLocked { get; set; }
		public String PauseWhenSongEnds { get; set; }
		public String CurrentShortcutProfile { get; set; }
		public List<KeyboardShortcutProfile> ShortcutProfiles { get; set; }
		public List<PluginSettings> PluginSettings { get; set; }
		public String YouTubeFilter { get; set; }
		public String YouTubeQuality { get; set; }
		public String CurrentActiveNavigation { get; set; }
		public TrackData CurrentTrack { get; set; }
		public EqualizerProfile CurrentEqualizerProfile { get; set; }
		public List<EqualizerProfile> EqualizerProfiles { get; set; }
		public String HistoryIndex { get; set; }
		public String Shuffle { get; set; }
		public String Repeat { get; set; }
		public String Volume { get; set; }
		public String Seek { get; set; }
		public String MediaState { get; set; }
		public List<CloudIdentity> CloudIdentities { get; set; }
		public String SubmitSongs { get; set; }
		public System.Collections.Generic.Dictionary<string, System.Tuple<string, string>> ListenBuffer { get; set; }
		public String FirstRun { get; set; }
		public String UpgradeCheck { get; set; }
		public List<PlaylistData> Playlists { get; set; }
		public String OAuthToken { get; set; }
		public String OAuthSecret { get; set; }
		public String CurrentVisualizer { get; set; }

		#endregion
	}

    /// <summary>
    /// The interface of the settings migrator library.
    /// </summary>
    interface IMigrator
    {
        #region Constructor

        /// <summary>
        /// Create a migrated settings file.
        /// </summary>
        /// <param name="fromFile">The file to read current settings from</param>
        /// <param name="toFile">The file to write the new settings to</param>
        void Migrate(String fromFile, String toFile);

        #endregion
    }
}
