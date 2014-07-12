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

namespace Stoffi.Tools.Migrator
{
    /// <summary>
    /// Migrates settings between two versions of Stoffi.
    /// </summary>
    public class SettingsMigrator : IMigrator
    {
		#region Members

		private static Database db;
		private static object saveToDatabaseLock = new object();

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

			#endregion

			U.L(LogLevel.Information, "Migrator", "Writing configuration");
			//WriteConfig(settings, toFile);
		}

        #endregion

		#region Private

		public void InitializeEqualizerProfiles(List<EqualizerProfile> profiles)
		{
			int defaultLevels = 0;
			foreach (var ep in profiles)
				if (ep.IsProtected)
					defaultLevels++;

			if (defaultLevels < 2)
			{
				profiles.Clear();
				profiles.Add(new EqualizerProfile()
				{
					Name = "Flat",
					IsProtected = true,
					Levels = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
					EchoLevel = 0
				});
				profiles.Add(new EqualizerProfile()
				{
					Name = "Bass",
					IsProtected = true,
					Levels = new float[] { 9.2f, 6.6f, 4.2f, 1.5f, -0.5f, -1.5f, 0.5f, 3.0f, 3.5f, 4.8f },
					EchoLevel = 0
				});
				profiles.Add(new EqualizerProfile()
				{
					Name = "Jazz",
					IsProtected = true,
					Levels = new float[] { 4.0f, 3.0f, 1.6f, 2.0f, -1.6f, -1.6f, 0f, 1.6f, 3.3f, 4.0f },
					EchoLevel = 0
				});
				profiles.Add(new EqualizerProfile()
				{
					Name = "Dance",
					IsProtected = true,
					Levels = new float[] { 7f, 4.7f, 2.5f, 0f, 0f, -3.5f, -5f, -5f, 0f, 0f },
					EchoLevel = 0
				});
				profiles.Add(new EqualizerProfile()
				{
					Name = "RnB",
					IsProtected = true,
					Levels = new float[] { 3f, 7f, 5.5f, 1.5f, -3f, -1.5f, 1.8f, 2.4f, 3f, 3.5f },
					EchoLevel = 0
				});
				profiles.Add(new EqualizerProfile()
				{
					Name = "Speech",
					IsProtected = true,
					Levels = new float[] { -3.3f, -0.5f, 0f, 0.6f, 3.5f, 5.0f, 5.0f, 4.5f, 3.0f, 0f },
					EchoLevel = 0
				});
				profiles.Add(new EqualizerProfile()
				{
					Name = "Loud",
					IsProtected = true,
					Levels = new float[] { 6.6f, 5.2f, 0f, 0f, -1f, 0f, 0f, -5f, 6f, 1f },
					EchoLevel = 0
				});
				profiles.Add(new EqualizerProfile()
				{
					Name = "Headphones",
					IsProtected = true,
					Levels = new float[] { 5.5f, 4.5f, 3.5f, 1.5f, -1f, -1.5f, 0.5f, 3.5f, 6.5f, 9f },
					EchoLevel = 0
				});
			}
		}

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
		}

		public static void FixViewDetailsConfig(ViewDetailsConfig vdc)
		{
		}

		private void FixSource(SourceData item)
		{
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
							settings.HistoryTracks = ReadSetting<List<TrackData>>(xmlReader);

						else if (name == "FileListConfig" || name == "LibraryListConfig")
							settings.FileListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "FileTracks" || name == "LibraryTracks")
							settings.FileTracks = ReadSetting<List<TrackData>>(xmlReader);

						else if (name == "RadioListConfig")
							settings.RadioListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "RadioTracks")
							settings.RadioTracks = ReadSetting<List<TrackData>>(xmlReader);

						else if (name == "DiscListConfig")
							settings.DiscListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "QueueListConfig")
							settings.QueueListConfig = ReadSetting<ViewDetailsConfig>(xmlReader);

						else if (name == "QueueTracks")
							settings.QueueTracks = ReadSetting<List<TrackData>>(xmlReader);

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
							settings.CurrentTrack = ReadSetting<TrackData>(xmlReader);

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
							settings.Playlists = ReadSetting<List<PlaylistData>>(xmlReader);

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
	public class ViewDetailsConfig : PropertyChangedBase
	{
		#region Members

		private ObservableCollection<ViewDetailsColumn> columns = new ObservableCollection<ViewDetailsColumn>();
		private ViewDetailsColumn numberColumn = new ViewDetailsColumn();
		private ObservableCollection<uint> selectedIndices = new ObservableCollection<uint>();
		private ObservableCollection<string> sorts = new ObservableCollection<string>();
		private string filter = "";
		private bool hasNumber = true;
		private bool isNumberVisible = true;
		private int numberIndex = 0;
		private bool useIcons = true;
		private bool acceptFileDrops = false;
		private bool isDragSortable = true;
		private bool isClickSortable = true;
		private bool lockSortOnNumber = false;
		private double verticalScrollOffset = 0;
		private double horizontalScrollOffset = 0;
		private double verticalScrollOffsetWithoutSearch = 0;
		private ViewMode mode = ViewMode.Details;
		private double iconSize = 64;
		private bool isLoading = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the columns
		/// </summary>
		public ObservableCollection<ViewDetailsColumn> Columns
		{
			get { return columns; }
			set
			{
				if (columns != null)
					columns.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<ViewDetailsColumn>> (ref columns, value, "Columns");
				if (columns != null)
				{
					foreach (var c in columns) {
						c.PropertyChanged -= Column_PropertyChanged;
						c.PropertyChanged += Column_PropertyChanged;
					}
					columns.CollectionChanged += CollectionChanged;
				}
			}
		}

		/// <summary>
		/// Gets or sets the number column configuration
		/// </summary>
		public ViewDetailsColumn NumberColumn
		{
			get { return numberColumn; }
			set { SetProp<ViewDetailsColumn> (ref numberColumn, value, "NumberColumn"); }
		}

		/// <summary>
		/// Gets or sets the indices of the selected items
		/// </summary>
		public ObservableCollection<uint> SelectedIndices
		{
			get { return selectedIndices; }
			set
			{ 
				if (selectedIndices != null)
					selectedIndices.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<uint>> (ref selectedIndices, value, "SelectedIndices");
				if (selectedIndices != null)
					selectedIndices.CollectionChanged += CollectionChanged;
			}
		}

		/// <summary>
		/// Gets or sets the the sort orders
		/// Each sort is represented as a string on the format
		/// "asc/dsc:ColumnName"
		/// </summary>
		public ObservableCollection<string> Sorts
		{
			get { return sorts; }
			set
			{
				if (sorts != null)
					sorts.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<string>> (ref sorts, value, "Sorts");
				if (sorts != null)
					sorts.CollectionChanged += CollectionChanged;
			}
		}

		/// <summary>
		/// Gets or sets text used to filter the list
		/// </summary>
		public string Filter
		{
			get { return filter; }
			set { SetProp<string> (ref filter, value, "Filter"); }
		}

		/// <summary>
		/// Gets or sets whether the number column should be enabled
		/// </summary>
		public bool HasNumber
		{
			get { return hasNumber; }
			set { SetProp<bool> (ref hasNumber, value, "HasNumber"); }
		}

		/// <summary>
		/// Gets or sets whether the number column should be visible
		/// </summary>
		public bool IsNumberVisible
		{
			get { return isNumberVisible; }
			set { SetProp<bool> (ref isNumberVisible, value, "IsNumberVisible"); }
		}

		/// <summary>
		/// Gets or sets the position of the number column
		/// </summary>
		public int NumberIndex
		{
			get { return numberIndex; }
			set { SetProp<int> (ref numberIndex, value, "NumberIndex"); }
		}

		/// <summary>
		/// Gets or sets whether to display icons or not
		/// </summary>
		public bool UseIcons
		{
			get { return useIcons; }
			set { SetProp<bool> (ref useIcons, value, "UseIcons"); }
		}

		/// <summary>
		/// Gets or sets whether files can be dropped onto the list
		/// </summary>
		public bool AcceptFileDrops
		{
			get { return acceptFileDrops; }
			set { SetProp<bool> (ref acceptFileDrops, value, "AcceptFileDrops"); }
		}

		/// <summary>
		/// Gets or sets whether the list can be resorted via drag and drop
		/// </summary>
		public bool IsDragSortable
		{
			get { return isDragSortable; }
			set { SetProp<bool> (ref isDragSortable, value, "IsDragSortable"); }
		}

		/// <summary>
		/// Gets or sets whether the list can be resorted by clicking on a column
		/// </summary>
		public bool IsClickSortable
		{
			get { return isClickSortable; }
			set { SetProp<bool> (ref isClickSortable, value, "IsClickSortable"); }
		}

		/// <summary>
		/// Gets or sets whether only the number column can be used to sort the list
		/// </summary>
		public bool LockSortOnNumber
		{
			get { return lockSortOnNumber; }
			set { SetProp<bool> (ref lockSortOnNumber, value, "LockSortOnNumber"); }
		}

		/// <summary>
		/// Gets or sets the vertical scroll offset
		/// </summary>
		public double VerticalScrollOffset
		{
			get { return verticalScrollOffset; }
			set { SetProp<double> (ref verticalScrollOffset, value, "VerticalScrollOffset"); }
		}

		/// <summary>
		/// Gets or sets the horizontal scroll offset
		/// </summary>
		public double HorizontalScrollOffset
		{
			get { return horizontalScrollOffset; }
			set { SetProp<double> (ref horizontalScrollOffset, value, "HorizontalScrollOffset"); }
		}

		/// <summary>
		/// Gets or sets the vertical scroll offset when no search is active.
		/// </summary>
		public double VerticalScrollOffsetWithoutSearch
		{
			get { return verticalScrollOffsetWithoutSearch; }
			set { SetProp<double> (ref verticalScrollOffsetWithoutSearch, value, "VerticalScrollOffsetWithoutSearch"); }
		}

		/// <summary>
		/// Gets or sets the view mode.
		/// </summary>
		public ViewMode Mode
		{
			get { return mode; }
			set { SetProp<ViewMode> (ref mode, value, "Mode"); }
		}

		/// <summary>
		/// Gets or sets the size of the icons.
		/// </summary>
		/// <remarks>
		/// Only applicable when Mode is Icons.
		/// </remarks>
		public double IconSize
		{
			get { return iconSize; }
			set { SetProp<double> (ref iconSize, value, "IconSize"); }
		}

		/// <summary>
		/// Gets or sets whether the list is currently loading the content.
		/// </summary>
		public bool IsLoading
		{
			get { return isLoading; }
			set { SetProp<bool> (ref isLoading, value, "IsLoading"); }
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.ListViewConfig"/> class.
		/// </summary>
		public ViewDetailsConfig()
		{
			columns.CollectionChanged += CollectionChanged;
			selectedIndices.CollectionChanged += CollectionChanged;
			sorts.CollectionChanged += CollectionChanged;
			numberColumn.PropertyChanged += Column_PropertyChanged;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Get a specific column.
		/// </summary>
		/// <returns>The column.</returns>
		/// <param name="name">The name of the column.</param>
		public ViewDetailsColumn GetColumn(string name)
		{
			if (name == "Number")
				return NumberColumn;
			else
				foreach (var c in Columns)
					if (c.Name == name)
						return c;
			return null;
		}

		/// <summary>
		/// Invoked when a property of a column changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Column_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged ("Columns");
		}

		/// <summary>
		/// Invoked when a collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (sender is ObservableCollection<ViewDetailsColumn> && (ObservableCollection<ViewDetailsColumn>)sender == columns) {
				foreach (var c in columns) {
					c.PropertyChanged -= Column_PropertyChanged;
					c.PropertyChanged += Column_PropertyChanged;
				}
				OnPropertyChanged ("Columns");
			}
			else if (sender is ObservableCollection<string> && (ObservableCollection<string>)sender == sorts)
				OnPropertyChanged ("Sorts");
			else if (sender is ObservableCollection<uint> && (ObservableCollection<uint>)sender == selectedIndices)
				OnPropertyChanged ("SelectedIndices");
		}
		/// <summary>
		/// Creates a config with default values
		/// </summary>
		/// <returns>The newly created config</returns>
		public static ViewDetailsConfig Create()
		{
			var config = new ViewDetailsConfig();
			config.HasNumber = true;
			config.IsNumberVisible = false;
			config.Filter = "";
			config.IsClickSortable = true;
			config.IsDragSortable = true;
			config.LockSortOnNumber = false;
			config.UseIcons = true;
			config.AcceptFileDrops = true;
			config.Columns = new ObservableCollection<ViewDetailsColumn>();
			config.NumberColumn = ViewDetailsColumn.Create("#", "#", "Number", "Number", 60, Alignment.Right, false);
			return config;
		}

		/// <summary>
		/// Initializes a configuration of a list.
		/// </summary>
		public void Initialize()
		{
			Columns.Add(ViewDetailsColumn.Create("Artist", U.T("ColumnArtist"), 180));
			Columns.Add(ViewDetailsColumn.Create("Album", U.T("ColumnAlbum"), 160));
			Columns.Add(ViewDetailsColumn.Create("Title", U.T("ColumnTitle"), 220));
			Columns.Add(ViewDetailsColumn.Create("Genre", U.T("ColumnGenre"), 90));
			Columns.Add(ViewDetailsColumn.Create("Length", U.T("ColumnLength"), 70, "Duration", Alignment.Right));
			Columns.Add(ViewDetailsColumn.Create("Year", U.T("ColumnYear"), 100, Alignment.Right, false));
			Columns.Add(ViewDetailsColumn.Create("LastPlayed", U.T("ColumnLastPlayed"), 150, "DateTime", Alignment.Left, false));
			Columns.Add(ViewDetailsColumn.Create("PlayCount", U.T("ColumnPlayCount"), 80, "Number", Alignment.Right));
			Columns.Add(ViewDetailsColumn.Create("Track", U.T("ColumnTrack"), "TrackNumber", 100, Alignment.Right, false));
			Columns.Add(ViewDetailsColumn.Create("Path", U.T("ColumnPath"), "Path", 300, Alignment.Left, false));
			Sorts.Add ("asc:Title");
			Sorts.Add ("asc:TrackNumber");
			Sorts.Add ("asc:Album");
			Sorts.Add ("asc:Artist");
		}

		#endregion
	}

	/// <summary>
	/// How the content can be displayed.
	/// </summary>
	public enum ViewMode
	{
		/// <summary>
		/// A columned list which scrolls vertically.
		/// </summary>
		Details,

		/// <summary>
		/// A grid of icons.
		/// </summary>
		Icons,

		/// <summary>
		/// A list which scrolls horizontally.
		/// </summary>
		List,

		/// <summary>
		/// A grid of medium sized icons with meta data.
		/// </summary>
		Tiles,

		/// <summary>
		/// A list of medium sized icons with meta data.
		/// </summary>
		Content
	}

	/// <summary>
	/// Represents a column of a details list
	/// </summary>
	public class ViewDetailsColumn : PropertyChangedBase
	{
		#region Members

		private string name;
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
		public string Name
		{
			get { return name; }
			set { SetProp<string> (ref name, value, "Name"); }
		}

		/// <summary>
		/// Gets or sets the displayed text
		/// </summary>
		public string Text
		{
			get { return text; }
			set { SetProp <string>(ref text, value, "Text"); }
		}

		/// <summary>
		/// Gets or sets the value to bind to
		/// </summary>
		public string Binding
		{
			get { return binding; }
			set { SetProp <string>(ref binding, value, "Binding"); }
		}

		/// <summary>
		/// Gets or sets the converter that should be used to present the value of the binding.
		/// </summary>
		public string Converter
		{
			get { return converter; }
			set { SetProp <string>(ref converter, value, "Converter"); }
		}

		/// <summary>
		/// Gets or sets the value to sort on
		/// </summary>
		public string SortField
		{
			get { return sortField; }
			set { SetProp <string>(ref sortField, value, "SortField"); }
		}

		/// <summary>
		/// Gets or sets whether the column is always visible
		/// </summary>
		public bool IsAlwaysVisible
		{
			get { return isAlwaysVisible; }
			set { SetProp <bool>(ref isAlwaysVisible, value, "IsAlwaysVisible"); }
		}

		/// <summary>
		/// Gets or sets whether the column is sortable
		/// </summary>
		public bool IsSortable
		{
			get { return isSortable; }
			set { SetProp <bool>(ref isSortable, value, "IsSortable"); }
		}

		/// <summary>
		/// Gets or sets the width of the column
		/// </summary>
		public double Width
		{
			get { return width; }
			set { SetProp <double>(ref width, value, "Width"); }
		}

		/// <summary>
		/// Gets or sets whether the column is visible (only effective if IsAlwaysVisible is false)
		/// </summary>
		public bool IsVisible
		{
			get { return isVisible; }
			set { SetProp <bool>(ref isVisible, value, "IsVisible"); }
		}

		/// <summary>
		/// Gets or sets the text alignment of the displayed text
		/// </summary>
		public Alignment Alignment
		{
			get { return alignment; }
			set { SetProp <Alignment>(ref alignment, value, "Alignment"); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Create a column.
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The displayed text</param>
		/// <param name="width">The width</param>
		/// <param name="converter">The converter used to convert the value of the binding</param>
		/// <param name="isVisible">Whether the column is visible</param>
		/// <param name="alignment">The alignment of the text</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <returns>The newly created column</returns>
		public static ViewDetailsColumn Create(string name, string text, int width, string converter,
			Alignment alignment = Alignment.Left,
			bool isVisible = true,
			bool isAlwaysVisible = false,
			bool isSortable = true)
		{
			return Create(name, text, name, name, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Create a column.
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The displayed text</param>
		/// <param name="width">The width</param>
		/// <param name="isVisible">Whether the column is visible</param>
		/// <param name="alignment">The alignment of the text</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <param name="converter">The converter used to convert the value of the binding</param>
		/// <returns>The newly created column</returns>
		public static ViewDetailsColumn Create(string name, string text, int width,
			Alignment alignment = Alignment.Left,
			bool isVisible = true,
			bool isAlwaysVisible = false,
			bool isSortable = true,
			string converter = null)
		{
			return Create(name, text, name, name, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Create a column.
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The displayed text</param>
		/// <param name="binding">The value to bind to</param>
		/// <param name="width">The width</param>
		/// <param name="isVisible">Whether the column is visible</param>
		/// <param name="alignment">The alignment of the text</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <param name="converter">The converter used to convert the value of the binding</param>
		/// <returns>The newly created column</returns>
		public static ViewDetailsColumn Create(string name, string text, string binding, int width,
			Alignment alignment = Alignment.Left,
			bool isVisible = true,
			bool isAlwaysVisible = false,
			bool isSortable = true,
			string converter = null)
		{
			return Create(name, text, binding, binding, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Create a column.
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The displayed text</param>
		/// <param name="binding">The value to bind to</param>
		/// <param name="sortField">The column to sort on</param>
		/// <param name="width">The width</param>
		/// <param name="isVisible">Whether the column is visible</param>
		/// <param name="alignment">The alignment of the text</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <param name="converter">The converter used to convert the value of the binding</param>
		/// <returns>The newly created column</returns>
		public static ViewDetailsColumn Create(string name, string text, string binding, string sortField, int width,
			Alignment alignment = Alignment.Left,
			bool isVisible = true,
			bool isAlwaysVisible = false,
			bool isSortable = true,
			string converter = null)
		{
			ViewDetailsColumn column = new ViewDetailsColumn();
			column.Name = name;
			column.Text = text;
			column.Binding = binding;
			column.Width = width;
			column.Alignment = alignment;
			column.IsAlwaysVisible = isAlwaysVisible;
			column.IsSortable = isSortable;
			column.IsVisible = isVisible;
			column.SortField = sortField;
			column.Converter = converter;
			return column;
		}

		#endregion
	}

	/// <summary>
	/// Describes the data source of an item inside the ViewDetails list
	/// </summary>
	public class ViewDetailsItemData : PropertyChangedBase
	{
		#region Members

		protected int number;
		protected bool isActive;
		protected string icon;
		protected string image;
		protected bool strike;
		protected bool disabled = false;
		protected bool isVisible = false;
		protected string group;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the index number of the item
		/// </summary>
		public int Number
		{
			get { return number; }
			set { SetProp<int>(ref number, value, "Number"); }
		}

		/// <summary>
		/// Gets or sets whether the item is marked as active or not
		/// </summary>
		public bool IsActive
		{
			get { return isActive; }
			set { SetProp<bool>(ref isActive, value, "IsActive"); }
		}

		/// <summary>
		/// Gets or sets the icon of the item
		/// </summary>
		public string Icon
		{
			get { return icon; }
			set { SetProp<string>(ref icon, value, "Icon"); }
		}

		/// <summary>
		/// Gets or sets the image of the item
		/// </summary>
		public string Image
		{
			get { return image; }
			set { SetProp<string>(ref image, value, "Image"); }
		}

		/// <summary>
		/// Gets or sets whether the items should feature a strikethrough
		/// </summary>
		public bool Strike
		{
			get { return strike; }
			set { SetProp<bool>(ref strike, value, "Strike"); }
		}

		/// <summary>
		/// Gets or sets whether the items should be viewed as disabled (for example grayed out)
		/// </summary>
		public bool Disabled
		{
			get { return disabled; }
			set { SetProp<bool>(ref disabled, value, "Disabled"); }
		}

		/// <summary>
		/// Gets or sets whether or not the item is visible
		/// and should be rendered.
		/// </summary>
		/// <remarks>
		/// Used to implement virtualization for controls that
		/// don't support it (like the WrapPanel).
		/// </remarks>
		public bool IsVisible
		{
			get { return isVisible; }
			set { SetProp<bool>(ref isVisible, value, "IsVisible"); }
		}

		/// <summary>
		/// Gets or sets the group of the item.
		/// </summary>
		/// <remarks>
		/// This is used when grouping is enabled.
		/// </remarks>
		public string Group
		{
			get { return group; }
			set { SetProp<string>(ref group, value, "Group"); }
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
