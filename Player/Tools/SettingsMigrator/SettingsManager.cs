/**
 * SettingsManager.cs
 * 
 * Takes care of initializing settings during first run.
 * The file also contains all data structures used to store
 * data of Stoffi.
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
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace Stoffi.Tools.Migrator
{
	/// <summary>
	/// Represents a manager that takes care of all
	/// application settings.
	/// </summary>
	public static partial class SettingsManager
	{
		#region Members

		private static double bufferSize = 0;
		private static bool isInitialized = false;

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initializes the settings manager.
		/// </summary>
		public static void Initialize()
		{
		}

		public static void InitializeShortcutProfiles(List<KeyboardShortcutProfile> profiles)
		{
			// Stoffi
			KeyboardShortcutProfile shortStoffi = new KeyboardShortcutProfile();
			InitShortcutProfile(shortStoffi, "Stoffi", true);
			profiles.Add(shortStoffi);

			// Stoffi (laptop)
			KeyboardShortcutProfile shortLaptop = new KeyboardShortcutProfile();
			InitShortcutProfile(shortLaptop, "Stoffi (laptop)", true);
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Previous", "Ctrl+1");
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Play or pause", "Ctrl+2");
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Next", "Ctrl+3");
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Decrease volume", "Ctrl+4");
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Increase volume", "Ctrl+5");
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Seek backward", "Ctrl+6");
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Seek forward", "Ctrl+7");
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Toggle shuffle", "Ctrl+8");
			SetKeyboardShortcut(shortLaptop, "MediaCommands", "Toggle repeat", "Ctrl+9");
			profiles.Add(shortLaptop);

			// Amarok
			KeyboardShortcutProfile shortAmarok = new KeyboardShortcutProfile();
			InitShortcutProfile(shortAmarok, "Amarok", true);
			SetKeyboardShortcut(shortAmarok, "Application", "Add track", "Win+A");
			SetKeyboardShortcut(shortAmarok, "Application", "Add playlist", "Space");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Play or pause", "Win+X");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Next", "Win+B");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Previous", "Win+Z");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Toggle shuffle", "Ctrl+H");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Increase volume", "Win++ (numpad)");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Decrease volume", "Win+- (numpad)");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Seek forward", "Win+Shift++ (numpad)");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Seek backward", "Win+Shift+- (numpad)");
			SetKeyboardShortcut(shortAmarok, "MediaCommands", "Jump to current track", "Ctrl+Enter");
			SetKeyboardShortcut(shortAmarok, "MainWindow", "Toggle menu bar", "Ctrl+M");
			profiles.Add(shortAmarok);

			// Banshee
			KeyboardShortcutProfile shortBanshee = new KeyboardShortcutProfile();
			InitShortcutProfile(shortBanshee, "Banshee", true);
			SetKeyboardShortcut(shortBanshee, "Application", "Add track", "Ctrl+I");
			SetKeyboardShortcut(shortBanshee, "Application", "Close", "Ctrl+Q");
			SetKeyboardShortcut(shortBanshee, "MediaCommands", "Play or pause", "Space");
			SetKeyboardShortcut(shortBanshee, "MediaCommands", "Next", "N");
			SetKeyboardShortcut(shortBanshee, "MediaCommands", "Previous", "B");
			profiles.Add(shortBanshee);

			// Foobar2000
			KeyboardShortcutProfile shortFoobar2000 = new KeyboardShortcutProfile();
			InitShortcutProfile(shortFoobar2000, "Foobar2000", true);
			SetKeyboardShortcut(shortFoobar2000, "Application", "Add track", "Ctrl+O");
			SetKeyboardShortcut(shortFoobar2000, "Application", "Add folder", "Ctrl+A");
			SetKeyboardShortcut(shortFoobar2000, "Application", "Add playlist", "Ctrl+L");
			SetKeyboardShortcut(shortFoobar2000, "Application", "Add radio station", "Ctrl+U");
			SetKeyboardShortcut(shortFoobar2000, "MainWindow", "General preferences", "Ctrl+P");
			SetKeyboardShortcut(shortFoobar2000, "Track", "View information", "Alt+Enter");
			profiles.Add(shortFoobar2000);

			// iTunes
			KeyboardShortcutProfile shortiTunes = new KeyboardShortcutProfile();
			InitShortcutProfile(shortiTunes, "iTunes", true);
			SetKeyboardShortcut(shortiTunes, "Application", "Add track", "Ctrl+O");
			SetKeyboardShortcut(shortiTunes, "Application", "Add playlist", "Ctrl+P");
			SetKeyboardShortcut(shortiTunes, "Application", "Add radio station", "Ctrl+U");
			SetKeyboardShortcut(shortiTunes, "MainWindow", "General preferences", "Ctrl+,");
			SetKeyboardShortcut(shortiTunes, "MainWindow", "Search", "Ctrl+Alt+F");
			SetKeyboardShortcut(shortiTunes, "MediaCommands", "Play or pause", "Space");
			SetKeyboardShortcut(shortiTunes, "MediaCommands", "Increase volume", "Ctrl+Up");
			SetKeyboardShortcut(shortiTunes, "MediaCommands", "Decrease volume", "Ctrl+Down");
			SetKeyboardShortcut(shortiTunes, "MediaCommands", "Seek forward", "Ctrl+Alt+Right");
			SetKeyboardShortcut(shortiTunes, "MediaCommands", "Seek backward", "Ctrl+Alt+Left");
			SetKeyboardShortcut(shortiTunes, "MediaCommands", "Previous", "Left");
			SetKeyboardShortcut(shortiTunes, "MediaCommands", "Next", "Right");
			SetKeyboardShortcut(shortiTunes, "MediaCommands", "Jump to current track", "Ctrl+L");
			SetKeyboardShortcut(shortiTunes, "Track", "Open folder", "Ctrl+R");
			profiles.Add(shortiTunes);

			// MusicBee
			KeyboardShortcutProfile shortMusicBee = new KeyboardShortcutProfile();
			InitShortcutProfile(shortMusicBee, "MusicBee", true);
			SetKeyboardShortcut(shortMusicBee, "MainWindow", "General preferences", "Ctrl+O");
			SetKeyboardShortcut(shortMusicBee, "MainWindow", "Create playlist", "Ctrl+Shift+N");
			SetKeyboardShortcut(shortMusicBee, "MediaCommands", "Play or pause", "Ctrl+P");
			SetKeyboardShortcut(shortMusicBee, "MediaCommands", "Next", "Ctrl+N");
			SetKeyboardShortcut(shortMusicBee, "MediaCommands", "Previous", "Ctrl+B");
			SetKeyboardShortcut(shortMusicBee, "MediaCommands", "Increase volume", "Ctrl+Up");
			SetKeyboardShortcut(shortMusicBee, "MediaCommands", "Decrease volume", "Ctrl+Down");
			SetKeyboardShortcut(shortMusicBee, "Track", "Queue and dequeue", "Ctrl+Enter");
			SetKeyboardShortcut(shortMusicBee, "Track", "View information", "Alt+E");
			profiles.Add(shortMusicBee);

			// Rythmbox
			KeyboardShortcutProfile shortRythmbox = new KeyboardShortcutProfile();
			InitShortcutProfile(shortRythmbox, "Rythmbox", true);
			SetKeyboardShortcut(shortRythmbox, "Application", "Close", "Ctrl+Q");
			SetKeyboardShortcut(shortRythmbox, "Application", "Add folder", "Ctrl+O");
			SetKeyboardShortcut(shortRythmbox, "Application", "Add radio station", "Ctrl+I");
			SetKeyboardShortcut(shortRythmbox, "MainWindow", "Search", "Alt+S");
			SetKeyboardShortcut(shortRythmbox, "MediaCommands", "Play or pause", "Ctrl+Space");
			SetKeyboardShortcut(shortRythmbox, "MediaCommands", "Previous", "Alt+Left");
			SetKeyboardShortcut(shortRythmbox, "MediaCommands", "Next", "Alt+Right");
			SetKeyboardShortcut(shortRythmbox, "MediaCommands", "Toggle shuffle", "Ctrl+U");
			SetKeyboardShortcut(shortRythmbox, "MediaCommands", "Toggle repeat", "Ctrl+R");
			SetKeyboardShortcut(shortRythmbox, "MediaCommands", "Jump to current track", "Ctrl+J");
			SetKeyboardShortcut(shortRythmbox, "Track", "View information", "Alt+Enter");

			SetKeyboardShortcut(shortRythmbox, "MediaCommands", "Jump to previous bookmark", "Alt+,");
			SetKeyboardShortcut(shortRythmbox, "MediaCommands", "Jump to next bookmark", "Alt+.");
			profiles.Add(shortRythmbox);

			// Spotify
			KeyboardShortcutProfile shortSpotify = new KeyboardShortcutProfile();
			InitShortcutProfile(shortSpotify, "Spotify", true);
			SetKeyboardShortcut(shortSpotify, "MediaCommands", "Play or pause", "Space");
			SetKeyboardShortcut(shortSpotify, "MediaCommands", "Next", "Ctrl+Right");
			SetKeyboardShortcut(shortSpotify, "MediaCommands", "Previous", "Ctrl+Left");
			SetKeyboardShortcut(shortSpotify, "MediaCommands", "Increase volume", "Ctrl+Up");
			SetKeyboardShortcut(shortSpotify, "MediaCommands", "Decrease volume", "Ctrl+Down");
			SetKeyboardShortcut(shortSpotify, "MainWindow", "Search", "Ctrl+L");
			SetKeyboardShortcut(shortSpotify, "MainWindow", "General preferences", "Ctrl+P");

			SetKeyboardShortcut(shortSpotify, "Track", "Open folder", "Alt+L");
			profiles.Add(shortSpotify);

			// VLC
			KeyboardShortcutProfile shortVLC = new KeyboardShortcutProfile();
			InitShortcutProfile(shortVLC, "VLC", true);
			SetKeyboardShortcut(shortVLC, "Application", "Add track", "Ctrl+O");
			SetKeyboardShortcut(shortVLC, "Application", "Add folder", "Ctrl+F");
			SetKeyboardShortcut(shortVLC, "Application", "Add radio station", "Ctrl+N");
			SetKeyboardShortcut(shortVLC, "Application", "Close", "Ctrl+Q");
			SetKeyboardShortcut(shortVLC, "MainWindow", "General preferences", "Ctrl+P");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Play or pause", "Space");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Next", "N");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Previous", "P");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Seek backward", "Ctrl+Left");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Seek forward", "Ctrl+Right");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Increase volume", "Ctrl+Up");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Decrease volume", "Ctrl+Down");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Toggle shuffle", "R");
			SetKeyboardShortcut(shortVLC, "MediaCommands", "Toggle repeat", "L");

			SetKeyboardShortcut(shortVLC, "MainWindow", "Search", "F3");
			SetKeyboardShortcut(shortVLC, "MainWindow", "Create playlist", "Ctrl+Alt+P");
			profiles.Add(shortVLC);

			// Winamp
			KeyboardShortcutProfile shortWinamp = new KeyboardShortcutProfile();
			InitShortcutProfile(shortWinamp, "Winamp", true);
			SetKeyboardShortcut(shortWinamp, "Application", "Minimize", "Alt+M");
			SetKeyboardShortcut(shortWinamp, "Application", "Add track", "Ctrl+Alt+L");
			SetKeyboardShortcut(shortWinamp, "MainWindow", "History", "Ctrl+H");
			SetKeyboardShortcut(shortWinamp, "MainWindow", "General preferences", "Ctrl+P");
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Play or pause", "Ctrl+Alt+Insert", true);
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Next", "Ctrl+Alt+PageDown", true);
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Previous", "Ctrl+Alt+PageUp", true);
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Increase volume", "Ctrl+Alt+Up", true);
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Decrease volume", "Ctrl+Alt+Down", true);
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Seek forward", "Ctrl+Alt+Right", true);
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Seek backward", "Ctrl+Alt+Left", true);
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Toggle shuffle", "S");
			SetKeyboardShortcut(shortWinamp, "MediaCommands", "Toggle repeat", "R");
			SetKeyboardShortcut(shortWinamp, "Track", "View information", "Alt+3");

			SetKeyboardShortcut(shortWinamp, "MainWindow", "Toggle menu bar", "Ctrl+Alt+M");
			profiles.Add(shortWinamp);

			// Windows Media Player
			KeyboardShortcutProfile shortWMP = new KeyboardShortcutProfile();
			InitShortcutProfile(shortWMP, "Windows Media Player", true);
			SetKeyboardShortcut(shortWMP, "Application", "Add track", "Ctrl+O");
			SetKeyboardShortcut(shortWMP, "Application", "Add radio station", "Ctrl+U");
			SetKeyboardShortcut(shortWMP, "MainWindow", "Search", "Ctrl+E");
			SetKeyboardShortcut(shortWMP, "MainWindow", "Toggle menu bar", "F10");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Play or pause", "Ctrl+P");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Next", "Ctrl+F");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Previous", "Ctrl+B");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Seek forward", "Ctrl+Shift+F");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Seek backward", "Ctrl+Shift+B");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Toggle shuffle", "Ctrl+H");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Toggle repeat", "Ctrl+T");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Increase volume", "F9");
			SetKeyboardShortcut(shortWMP, "MediaCommands", "Decrease volume", "F8");
			SetKeyboardShortcut(shortWMP, "Track", "View information", "F2");

			SetKeyboardShortcut(shortWMP, "MainWindow", "Tracklist", "Ctrl+1");
			profiles.Add(shortWMP);
		}

		public static void InitializeEqualizerProfiles(List<EqualizerProfile> profiles)
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

		/// <summary>
		/// Find a keyboard shortcut inside a profile by looking for its key combination.
		/// </summary>
		/// <param name="profile">The keyboard shortcut profile to look in</param>
		/// <param name="keysAsText">The key combination of the shortcut</param>
		/// <returns>A corresponding shortcut if found, otherwise null</returns>
		public static KeyboardShortcut GetKeyboardShortcut(KeyboardShortcutProfile profile, String keysAsText)
		{
			if (profile != null)
				foreach (KeyboardShortcut s in profile.Shortcuts)
					if (s.Keys == keysAsText)
						return s;
			return null;
		}

		/// <summary>
		/// Find a keyboard shortcut inside a profile by looking for its name.
		/// </summary>
		/// <param name="profile">The keyboard shortcut profile to look in</param>
		/// <param name="category">The name of the category of the shortcut</param>
		/// <param name="name">The name of the shortcut</param>
		/// <returns>The keyboard shortcut corresponding to the category and name inside the profile</returns>
		public static KeyboardShortcut GetKeyboardShortcut(KeyboardShortcutProfile profile, String category, String name)
		{
			foreach (KeyboardShortcut s in profile.Shortcuts)
				if (s.Name == name && s.Category == category)
					return s;
			return null;
		}

		/// <summary>
		/// Initializes a keyboard shortcut profile.
		/// </summary>
		/// <param name="profile">The keyboard shortcut profile</param>
		/// <param name="name">The name of the profile</param>
		/// <param name="isprotected">Whether or not the profile is protected from changes by user</param>
		public static void InitShortcutProfile(KeyboardShortcutProfile profile, String name, Boolean isprotected)
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

			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Video", IsGlobal = false, Keys = "Ctrl+F1" }); // index 10
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Visualizer", IsGlobal = false, Keys = "Ctrl+F2" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Files", IsGlobal = false, Keys = "Ctrl+F3" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "YouTube", IsGlobal = false, Keys = "Ctrl+F4" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "SoundCloud", IsGlobal = false, Keys = "Ctrl+F5" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Radio", IsGlobal = false, Keys = "Ctrl+F6" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Queue", IsGlobal = false, Keys = "Ctrl+F7" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "History", IsGlobal = false, Keys = "Ctrl+F8" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Playlists", IsGlobal = false, Keys = "Ctrl+F9" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Tracklist", IsGlobal = false, Keys = "Ctrl+T" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Search", IsGlobal = false, Keys = "Ctrl+F" }); // index 20
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "General preferences", IsGlobal = false, Keys = "Alt+F1" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Music sources", IsGlobal = false, Keys = "Alt+F2" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Services", IsGlobal = false, Keys = "Alt+F3" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Apps", IsGlobal = false, Keys = "Alt+F5" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Keyboard shortcuts", IsGlobal = false, Keys = "Alt+F6" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "About", IsGlobal = false, Keys = "Alt+F7" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Toggle details pane", IsGlobal = false, Keys = "Alt+D" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Toggle menu bar", IsGlobal = false, Keys = "Alt+M" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Create playlist", IsGlobal = false, Keys = "Ctrl+N" });

			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Play or pause", IsGlobal = false, Keys = "Alt+5 (numpad)" }); // index 30
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Next", IsGlobal = false, Keys = "Alt+6 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Previous", IsGlobal = false, Keys = "Alt+4 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Toggle shuffle", IsGlobal = false, Keys = "Alt+9 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Toggle repeat", IsGlobal = false, Keys = "Alt+7 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Increase volume", IsGlobal = false, Keys = "Alt+8 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Decrease volume", IsGlobal = false, Keys = "Alt+2 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Seek forward", IsGlobal = false, Keys = "Alt+3 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Seek backward", IsGlobal = false, Keys = "Alt+1 (numpad)" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Add bookmark", IsGlobal = false, Keys = "Alt+B" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to previous bookmark", IsGlobal = false, Keys = "Alt+Left" }); // index 40
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to next bookmark", IsGlobal = false, Keys = "Alt+Right" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to first bookmark", IsGlobal = false, Keys = "Alt+Home" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to last bookmark", IsGlobal = false, Keys = "Alt+End" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 1", IsGlobal = false, Keys = "Alt+1" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 2", IsGlobal = false, Keys = "Alt+2" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 3", IsGlobal = false, Keys = "Alt+3" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 4", IsGlobal = false, Keys = "Alt+4" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 5", IsGlobal = false, Keys = "Alt+5" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 6", IsGlobal = false, Keys = "Alt+6" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 7", IsGlobal = false, Keys = "Alt+7" }); // index 50
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 8", IsGlobal = false, Keys = "Alt+8" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 9", IsGlobal = false, Keys = "Alt+9" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 10", IsGlobal = false, Keys = "Alt+0" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to current track", IsGlobal = false, Keys = "Alt+C" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to selected track", IsGlobal = false, Keys = "Alt+X" });

			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Play track", IsGlobal = false, Keys = "Enter" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Queue and dequeue", IsGlobal = false, Keys = "Shift+Q" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Open folder", IsGlobal = false, Keys = "Ctrl+L" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Remove", IsGlobal = false, Keys = "Delete" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Remove from harddrive", IsGlobal = false, Keys = "Shift+Delete" }); // index 60
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Copy", IsGlobal = false, Keys = "Ctrl+C" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Move", IsGlobal = false, Keys = "Ctrl+X" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "View information", IsGlobal = false, Keys = "Ctrl+I" });
			profile.Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Share", IsGlobal = false, Keys = "Shift+S" });
		}

		/// <summary>
		/// Creates an EqualizerLevel with some given values.
		/// Works as a constructor for the data structure.
		/// </summary>
		/// <param name="name">The name of the profile</param>
		/// <param name="levels">The 10 levels (ranging from 0 to 10)</param>
		/// <param name="echo">The echo level (ranging from 0 to 10)</param>
		/// <param name="isProtected">Whether or not the user can edit the profile</param>
		public static EqualizerProfile CreateEqualizerLevel(string name, float[] levels, float echo, bool isProtected = false)
		{
			return new EqualizerProfile() { EchoLevel = echo, IsProtected = isProtected, Levels = levels, Name = name };
		}

		/// <summary>
		/// Creates a ViewDetailsConfig with default values
		/// </summary>
		/// <returns>The newly created config</returns>
		public static ViewDetailsConfig CreateListConfig()
		{
			var config = new ViewDetailsConfig();
			config.HasNumber = true;
			config.IsNumberVisible = false;
			config.Filter = "";
			config.IsClickSortable = true;
			config.IsDragSortable = true;
			config.LockSortOnNumber = false;
			config.SelectedIndices = new ObservableCollection<uint>();
			config.UseIcons = true;
			config.AcceptFileDrops = true;
			config.Columns = new ObservableCollection<ViewDetailsColumn>();
			config.NumberColumn = CreateListColumn("#", "#", "Number", "Number", 60, Alignment.Right, false);
			config.Sorts = new ObservableCollection<string>();
			return config;
		}

		/// <summary>
		/// Creates a ViewDetailsColumn
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
		public static ViewDetailsColumn CreateListColumn(string name, string text, int width, string converter,
														 Alignment alignment = Alignment.Left,
														 bool isVisible = true,
														 bool isAlwaysVisible = false,
														 bool isSortable = true)
		{
			return CreateListColumn(name, text, name, name, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Creates a ViewDetailsColumn
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
		public static ViewDetailsColumn CreateListColumn(string name, string text, int width,
														 Alignment alignment = Alignment.Left,
														 bool isVisible = true,
														 bool isAlwaysVisible = false,
														 bool isSortable = true,
		                                                 string converter = null)
		{
			return CreateListColumn(name, text, name, name, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Creates a ViewDetailsColumn
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
		public static ViewDetailsColumn CreateListColumn(string name, string text, string binding, int width,
														 Alignment alignment = Alignment.Left,
														 bool isVisible = true,
														 bool isAlwaysVisible = false,
														 bool isSortable = true,
														 string converter = null)
		{
			return CreateListColumn(name, text, binding, binding, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Creates a ViewDetailsColumn
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
		public static ViewDetailsColumn CreateListColumn(string name, string text, string binding, string sortField, int width,
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

		#region Private

		/// <summary>
		/// Copies an observable collection of TrackData to a list of ConfigTrack
		/// </summary>
		/// <param name="tracks">The tracks to be copied</param>
		/// <returns>The tracks as configuration structures</returns>
		private static List<ConfigTrack> ExportTracks(ObservableCollection<TrackData> tracks)
		{
			List<ConfigTrack> l = new List<ConfigTrack>();
			foreach (TrackData t in tracks)
				l.Add(ExportTrack(t));
			return l;
		}

		/// <summary>
		/// Copies a TrackData into a ConfigTrack
		/// </summary>
		/// <param name="track">The track to be copied</param>
		/// <returns>The track as a configuration structure</returns>
		private static ConfigTrack ExportTrack(TrackData track)
		{
			if (track == null) return null;
			return new ConfigTrack() { Path = track.Path, PlayCount = track.PlayCount, LastPlayed = track.LastPlayed };
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="profiles"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private static KeyboardShortcutProfile GetKeyboardShortcutProfile(List<KeyboardShortcutProfile> profiles, String name)
		{
			foreach (KeyboardShortcutProfile profile in profiles)
				if (profile.Name == name)
					return profile;
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sc"></param>
		/// <param name="keysAsText"></param>
		/// <param name="isGlobal"></param>
		private static void SetKeyboardShortcut(KeyboardShortcut sc, String keysAsText, bool isGlobal = false)
		{
			sc.Keys = keysAsText;
			sc.IsGlobal = isGlobal;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="profile"></param>
		/// <param name="category"></param>
		/// <param name="name"></param>
		/// <param name="keysAsText"></param>
		/// <param name="isGlobal"></param>
		private static void SetKeyboardShortcut(KeyboardShortcutProfile profile, String category, String name, String keysAsText, bool isGlobal = false)
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

		#endregion

		#region Dispatchers

		/// <summary>
		/// Trigger the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		/// <param name="oldValue">The value of the property before the change</param>
		/// <param name="newValue">The value of the property after the change</param>
		public static void DispatchPropertyChanged(string name, object oldValue, object newValue)
		{
			//Save();
			if (PropertyChanged != null && oldValue != newValue)
			{
				PropertyChanged(null, new PropertyChangedWithValuesEventArgs(name, oldValue, newValue));
			}
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when a property has been changed
		/// </summary>
		public static event PropertyChangedWithValuesEventHandler PropertyChanged;

		/// <summary>
		/// Occurs when the manager has been fully initialized
		/// </summary>
		public static event EventHandler Initialized;

		#endregion
	}

	#region Delegates

	/// <summary>
	/// Represents the method that will be called with the PropertyChanged event occurs
	/// </summary>
	/// <param name="sender">The sender of the event</param>
	/// <param name="e">The event data</param>
	public delegate void PropertyChangedWithValuesEventHandler(object sender, PropertyChangedWithValuesEventArgs e);

	#endregion

	#region Enums

	/// <summary>
	/// The policy for adding file opened with the application
	/// </summary>
	public enum OpenAddPolicy
	{
		/// <summary>
		/// Add the file to the library only
		/// </summary>
		Library,

		/// <summary>
		/// Do not add the file
		/// </summary>
		DoNotAdd,
		
		/// <summary>
		/// Add the file both to the library and the currently selected playlist
		/// (if such exists)
		/// </summary>
		LibraryAndPlaylist
	}

	/// <summary>
	/// The policy for playing files opened with the application
	/// </summary>
	public enum OpenPlayPolicy
	{
		/// <summary>
		/// Play the file
		/// </summary>
		Play,

		/// <summary>
		/// Add the file to the end of the queue
		/// </summary>
		BackOfQueue,

		/// <summary>
		/// Add the file to the front of the queue
		/// </summary>
		FrontOfQueue,

		/// <summary>
		/// Do not play the file
		/// </summary>
		DoNotPlay
	}

	/// <summary>
	/// The policy for upgrading the application
	/// </summary>
	public enum UpgradePolicy
	{
		/// <summary>
		/// Check, download and apply upgrades automatically
		/// </summary>
		Automatic,

		/// <summary>
		/// Check for upgrade automatically and notify the user when
		/// an upgrade is found.
		/// </summary>
		Notify,

		/// <summary>
		/// Let the user manually check for upgrades
		/// </summary>
		Manual
	}

	/// <summary>
	/// The policy for letting different lists share a search filter
	/// </summary>
	public enum SearchPolicy
	{
		/// <summary>
		/// All lists share the same search filter
		/// </summary>
		Global,

		/// <summary>
		/// All playlists share one search filter, every other
		/// list has their own filter
		/// </summary>
		Partial,

		/// <summary>
		/// Each and every list has their own filter
		/// </summary>
		Individual
	}

	/// <summary>
	/// The state of the media player
	/// </summary>
	public enum MediaState
	{
		/// <summary>
		/// The media player is playing
		/// </summary>
		Playing,

		/// <summary>
		/// The media player is paused
		/// </summary>
		Paused,

		/// <summary>
		/// The media player is stopped
		/// </summary>
		Stopped,

		/// <summary>
		/// The current track has ended
		/// </summary>
		Ended
	}

	/// <summary>
	/// The state of repeating
	/// </summary>
	public enum RepeatState
	{
		/// <summary>
		/// Do not repeat any tracks
		/// </summary>
		NoRepeat,

		/// <summary>
		/// Repeat all tracks
		/// </summary>
		RepeatAll,

		/// <summary>
		/// Repeat the current track
		/// </summary>
		RepeatOne
	}

	/// <summary>
	/// The type of a source
	/// </summary>
	public enum SourceType
	{
		/// <summary>
		/// A single file
		/// </summary>
		File,

		/// <summary>
		/// A folder
		/// </summary>
		Folder,

		/// <summary>
		/// A Windows 7 Library
		/// </summary>
		Library
	}

	/// <summary>
	/// Alignment of an object relative to a container.
	/// </summary>
	public enum Alignment
	{
		/// <summary>
		/// Horizontally to the left.
		/// </summary>
		Left,

		/// <summary>
		/// Horizontally to the right.
		/// </summary>
		Right,

		/// <summary>
		/// Horizontally in the center.
		/// </summary>
		Center,

		/// <summary>
		/// Vertically at the top.
		/// </summary>
		Top,

		/// <summary>
		/// Vertically at the bottom.
		/// </summary>
		Bottom,

		/// <summary>
		/// Vertically in the middle.
		/// </summary>
		Middle
	}

	#endregion

	#region Data structures

	/// <summary>
	/// Describes a list of mappings between keys and values.
	/// </summary>
	[Serializable()]
	public class CloudIdentity
	{
		#region Properties

		/// <summary>
		/// Gets or sets the ID of the owner.
		/// </summary>
		public uint UserID { get; set; }

		/// <summary>
		/// Gets or sets the ID of the configuration.
		/// </summary>
		/// <remarks>
		/// We have deprecated the usage of multiple configurations so we will
		/// always use the first one returned by the server, or create one
		/// named "Default" if no configuration exists.
		/// </remarks>
		public uint ConfigurationID { get; set; }

		/// <summary>
		/// Gets or sets whether or not to synchronize with the cloud.
		/// </summary>
		public bool Synchronize { get; set; }

		/// <summary>
		/// Gets or sets whether or not to synchronize the playlists.
		/// </summary>
		public bool SynchronizePlaylists { get; set; }

		/// <summary>
		/// Gets or sets whether or not to synchronize the configuration.
		/// </summary>
		public bool SynchronizeConfig { get; set; }

		/// <summary>
		/// Gets or sets whether or not to synchronize the play queue.
		/// </summary>
		public bool SynchronizeQueue { get; set; }

		/// <summary>
		/// Gets or sets whether or not to synchronize the files.
		/// </summary>
		public bool SynchronizeFiles { get; set; }

		/// <summary>
		/// Gets or sets the ID of the device.
		/// </summary>
		public uint DeviceID { get; set; }

		/// <summary>
		/// Gets or sets the links to third party accounts.
		/// </summary>
		public List<Link> Links { get; set; }

		#endregion
	}

	/// <summary>
	/// Describes a link to an account at a third party provider.
	/// </summary>
	[Serializable()]
	public class Link : INotifyPropertyChanged
	{
		#region Members

		string error = null;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the name of the provider.
		/// </summary>
		public string Provider { get; set; }

		/// <summary>
		/// Gets or sets whether the link is connected.
		/// </summary>
		public bool Connected { get; set; }

		/// <summary>
		/// Gets or sets whether it's possible to share stuff on the account.
		/// </summary>
		public bool CanShare { get; set; }

		/// <summary>
		/// Gets or sets whether the user allows share stuff on the account.
		/// </summary>
		public bool DoShare { get; set; }

		/// <summary>
		/// Gets or sets whether it's possible to submit plays to the account.
		/// </summary>
		public bool CanListen { get; set; }

		/// <summary>
		/// Gets or sets whether the user allows sending plays to the account.
		/// </summary>
		public bool DoListen { get; set; }

		/// <summary>
		/// Gets or sets whether it's possible to share donations on the account.
		/// </summary>
		public bool CanDonate { get; set; }

		/// <summary>
		/// Gets or sets whether the user allows sharing donations on the account.
		/// </summary>
		public bool DoDonate { get; set; }

		/// <summary>
		/// Gets or sets whether it's possible to share newly created playlists on the account.
		/// </summary>
		public bool CanCreatePlaylist { get; set; }

		/// <summary>
		/// Gets or sets whether the user allows sharing newly created playlists on the account.
		/// </summary>
		public bool DoCreatePlaylist { get; set; }

		/// <summary>
		/// Gets or sets the user's profile picture.
		/// </summary>
		public string Picture { get; set; }

		/// <summary>
		/// Gets or sets the user's names.
		/// </summary>
		public List<string> Names { get; set; }

		/// <summary>
		/// Gets or sets the URL for the link (either to the object or for creating a connection).
		/// </summary>
		public string URL { get; set; }

		/// <summary>
		/// Gets or sets the URL for the creating a connection.
		/// </summary>
		public string ConnectURL { get; set; }

		/// <summary>
		/// Gets or sets the ID of the link.
		/// </summary>
		public uint ID { get; set; }

		/// <summary>
		/// The last error while communicating over the link. If null then the last attempt at
		/// communication was successful.
		/// </summary>
		public string Error
		{
			get { return error; }
			set { error = value; OnPropertyChanged("Error"); }
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
		public ViewDetailsConfig SourceListConfig { get; set; }

		/// <summary>
		/// Gets or sets configuration for the file list
		/// </summary>
		public ViewDetailsConfig FileListConfig { get; set; }

		/// <summary>
		/// Gets or sets configuration for the history list
		/// </summary>
		public ViewDetailsConfig HistoryListConfig { get; set; }

		/// <summary>
		/// Gets or sets configuration for the queue list
		/// </summary>
		public ViewDetailsConfig QueueListConfig { get; set; }

		/// <summary>
		/// Gets or sets configuration for the youtube list
		/// </summary>
		public ViewDetailsConfig YouTubeListConfig { get; set; }

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
		public bool MinimizeToTray { get; set; }

		/// <summary>
		/// Gets or sets the collection of keyboard shortcuts
		/// </summary>
		public List<KeyboardShortcutProfile> ShortcutProfiles { get; set; }

		/// <summary>
		/// Gets or sets the profile of keyboard shortcuts that are currently in use
		/// </summary>
		public string CurrentShortcutProfile { get; set; }

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
		public ViewDetailsConfig ListConfig { get; set; }

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

	/// <summary>
	/// Describes a keyboard shortcut.
	/// </summary>
	public class KeyboardShortcut
	{
		#region Properties

		/// <summary>
		/// Gets or sets the name of the shortcut.
		/// </summary>
		public String Name { get; set; }

		/// <summary>
		/// Gets or sets the category of the shortcut.
		/// </summary>
		public String Category { get; set; }

		/// <summary>
		/// Get or sets the keys that will trigger the shortcut.
		/// </summary>
		public String Keys { get; set; }

		/// <summary>
		/// Gets or sets whether the shortcut should be accessible
		/// when the application doesn't have focus.
		/// </summary>
		public Boolean IsGlobal { get; set; }

		#endregion
	}

	/// <summary>
	/// Describes a profile of keyboard shortcuts.
	/// </summary>
	public class KeyboardShortcutProfile
	{
		#region Properties

		/// <summary>
		/// Get or sets the name of the profile.
		/// </summary>
		public String Name { get; set; }

		/// <summary>
		/// Get or sets whether the user can modify the profile.
		/// </summary>
		public Boolean IsProtected { get; set; }

		/// <summary>
		/// Get or sets the shortcuts of the profile.
		/// </summary>
		public List<KeyboardShortcut> Shortcuts { get; set; }

		#endregion
	}

	/// <summary>
	/// Describes an equalizer profile.
	/// </summary>
	public class EqualizerProfile
	{
		#region Properties

		/// <summary>
		/// Get or sets the name of the profile.
		/// </summary>
		public String Name { get; set; }

		/// <summary>
		/// Get or sets whether the user can modify the profile.
		/// </summary>
		public Boolean IsProtected { get; set; }

		/// <summary>
		/// Get or sets the levels (between -10 and 10).
		/// </summary>
		/// <remarks>
		/// Is a list with 10 floats between -10 and 10,
		/// where each float represents the maximum level
		/// on a frequency band going from lower to higher.
		/// </remarks>
		public float[] Levels { get; set; }

		/// <summary>
		/// Get or sets the echo level.
		/// A float ranging from 0 to 10 going from
		/// dry to wet.
		/// </summary>
		public float EchoLevel { get; set; }

		#endregion
	}

	/// <summary>
	/// Describes a playlist
	/// </summary>
	public class PlaylistData : INotifyPropertyChanged
	{
		#region Members

		private string name;
		private ObservableCollection<TrackData> tracks;
		private ViewDetailsConfig listConfig;

		#endregion

		#region Properties

		/// <summary>
		/// Get or sets the name of the playlist
		/// </summary>
		public string Name
		{
			get { return name; }
			set { name = value; OnPropertyChanged("Name"); }
		}

		/// <summary>
		/// Gets or sets the ID of the playlist in the cloud.
		/// </summary>
		public uint ID { get; set; }

		/// <summary>
		/// Get or sets the combined time of all tracks
		/// </summary>
		public double Time { get; set; }

		/// <summary>
		/// Get or sets the ID of the user who owns the playlist in the cloud.
		/// </summary>
		public uint Owner { get; set; }

		/// <summary>
		/// Get or sets the ID of the user who owns the playlist in the cloud.
		/// </summary>
		public uint OwnerID { get; set; }

		/// <summary>
		/// Get or sets the name of the user who owns the playlist in the cloud.
		/// </summary>
		public string OwnerName { get; set; }

		/// <summary>
		/// Get or sets the time when OwnerName was checked and cached.
		/// </summary>
		public DateTime OwnerCacheTime { get; set; }

		/// <summary>
		/// Get or sets the filter for automatic adding/removing of songs.
		/// </summary>
		/// <remarks>
		/// If null then the playlist is not dynamic.
		/// </remarks>
		public string Filter { get; set; }

		/// <summary>
		/// Gets or sets the collection of tracks of the playlist
		/// </summary>
		public ObservableCollection<TrackData> Tracks
		{
			get { return tracks; }
			set { tracks = value; OnPropertyChanged("Tracks"); }
		}

		/// <summary>
		/// Get or sets the configuration of the list view
		/// </summary>
		public ViewDetailsConfig ListConfig
		{
			get { return listConfig; }
			set { listConfig = value; OnPropertyChanged("ListConfig"); }
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
	/// Describes a track.
	/// </summary>
	public class TrackData : ViewDetailsItemData
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
		private ObservableCollection<Tuple<string, double>> bookmarks = new ObservableCollection<Tuple<string, double>>();

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
			set { SetProp<string>(ref artist, value, "Artist"); }
		}

		/// <summary>
		/// Gets or sets the title of the track.
		/// </summary>
		public string Title
		{
			get { return title; }
			set { SetProp<string>(ref title, value, "Title"); }
		}

		/// <summary>
		/// Gets or sets the album of the track.
		/// </summary>
		public string Album
		{
			get { return album; }
			set { SetProp<string>(ref album, value, "Album"); }
		}

		/// <summary>
		/// Gets or sets the genre of the track.
		/// </summary>
		public string Genre
		{
			get { return genre; }
			set { SetProp<string>(ref genre, value, "Genre"); }
		}

		/// <summary>
		/// Gets or sets the number of the track on the album.
		/// </summary>
		public uint TrackNumber
		{
			get { return track; }
			set { SetProp<uint>(ref track, value, "TrackNumber"); }
		}

		/// <summary>
		/// Gets or sets the year the track was made.
		/// </summary>
		public uint Year
		{
			get { return year; }
			set { SetProp<uint>(ref year, value, "Year"); }
		}

		/// <summary>
		/// Gets or sets the length of the track in seconds.
		/// </summary>
		public double Length
		{
			get { return length; }
			set { diff = (int)(value - length); SetProp<double>(ref length, value, "Length"); diff = 0; }
		}

		/// <summary>
		/// Gets or sets the path to the track.
		/// </summary>
		public string Path
		{
			get { return path; }
			set { SetProp<string>(ref path, value, "Path"); }
		}

		/// <summary>
		/// Gets or sets the number of times that the track has been played.
		/// </summary>
		public uint PlayCount
		{
			get { return userPlayCount; }
			set { SetProp<uint>(ref userPlayCount, value, "PlayCount"); }
		}

		/// <summary>
		/// Gets or sets the URL of the track.
		/// Only applicable on streamable tracks.
		/// </summary>
		public string URL
		{
			get { return url; }
			set { SetProp<string>(ref url, value, "URL"); }
		}

		/// <summary>
		/// Gets or sets the amount of views on YouTube.
		/// </summary>
		public ulong Views
		{
			get { return globalPlayCount; }
			set { SetProp<ulong>(ref globalPlayCount, value, "Views"); }
		}

		/// <summary>
		/// Gets or sets the time the track was last played (in epoch time).
		/// </summary>
		public DateTime LastPlayed
		{
			get { return lastPlayed; }
			set { SetProp<DateTime>(ref lastPlayed, value, "LastPlayed"); }
		}

		/// <summary>
		/// Gets or sets the URL/path to the album art.
		/// </summary>
		public string ArtURL
		{
			get { return image; }
			set { SetProp<string>(ref image, value, "ArtURL"); }
		}

		/// <summary>
		/// Gets or sets the URL to the album art at the original host instead of a potentially cached version.
		/// </summary>
		public string OriginalArtURL
		{
			get { return originalArtURL; }
			set { SetProp<string>(ref originalArtURL, value, "OriginalArtURL"); }
		}

		/// <summary>
		/// Gets or sets whether the track has been scanned for meta data.
		/// </summary>
		public bool Processed
		{
			get { return processed; }
			set { SetProp<bool>(ref processed, value, "Processed"); }
		}

		/// <summary>
		/// Gets or sets the time that the file was last written/updated.
		/// </summary>
		public long LastWrite
		{
			get { return lastWrite; }
			set { SetProp<long>(ref lastWrite, value, "LastWrite"); }
		}

		/// <summary>
		/// Gets or sets the bitrate of the track.
		/// </summary>
		public int Bitrate
		{
			get { return bitrate; }
			set { SetProp<int>(ref bitrate, value, "Bitrate"); }
		}

		/// <summary>
		/// Gets or sets the number of channels of the track.
		/// </summary>
		public int Channels
		{
			get { return channels; }
			set { SetProp<int>(ref channels, value, "Channels"); }
		}

		/// <summary>
		/// Gets or sets the sample rate of the track.
		/// </summary>
		public int SampleRate
		{
			get { return sampleRate; }
			set { SetProp<int>(ref sampleRate, value, "SampleRate"); }
		}

		/// <summary>
		/// Gets or sets the codecs of the track.
		/// </summary>
		public string Codecs
		{
			get { return codecs; }
			set { SetProp<string>(ref codecs, value, "Codecs"); }
		}

		/// <summary>
		/// Gets or sets where the track belongs to ("Files", "Playlist:Name").
		/// </summary>
		public string Source
		{
			get { return source; }
			set { SetProp<string>(ref source, value, "Source"); }
		}

		/// <summary>
		/// Gets or sets the bookmarks of the track (percentage).
		/// </summary>
		public ObservableCollection<Tuple<string, double>> Bookmarks
		{
			get { return bookmarks; }
			set
			{
				if (bookmarks != null)
					bookmarks.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<Tuple<string, double>>>(ref bookmarks, value, "Bookmarks");
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
				return TrackData.GetType(Path);
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
		public TrackData()
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
			if ((ObservableCollection<Tuple<string, double>>)sender == bookmarks && bookmarks != null)
				OnPropertyChanged("Bookmarks");
		}

		/// <summary>
		/// Gets the track type of a path.
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>The type of the given track path</returns>
		public static TrackType GetType(string path)
		{
			if (String.IsNullOrEmpty(path))
				return TrackType.Unknown;

			else if (Regex.IsMatch(path, @"^https?://", RegexOptions.IgnoreCase))
			{
				if (Regex.IsMatch(path, @"https?://[^\?]+/[^\?]+\.\w{1,5}(\?.*)?$", RegexOptions.IgnoreCase))
					return TrackType.File;
				return TrackType.WebRadio;
			}

			else if (path.ToLower().StartsWith("stoffi:youtube:track:"))
				return TrackType.YouTube;

			else if (path.ToLower().StartsWith("stoffi:soundcloud:track:"))
				return TrackType.SoundCloud;

			else if (path.ToLower().StartsWith("stoffi:jamendo:track:"))
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
		public TrackData Track { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Create a new instance of the TarckChangedEventArgs class
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <param name="track">The track that changed</param>
		public TrackChangedEventArgs(string name, TrackData track)
		{
			PropertyName = name;
			Track = track;
		}

		#endregion
	}

	/// <summary>
	/// Describes a source.
	/// </summary>
	public class SourceData : ViewDetailsItemData
	{
		#region Members

		private SourceType type;
		private String data;
		private bool ignore;
		private bool automatic = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets whether the source was added automatically or not.
		/// </summary>
		public bool Automatic { get { return automatic; } set { automatic = value; } }

		/// <summary>
		/// Gets or sets whether the source should be striked through.
		/// </summary>
		public bool Strike
		{
			get { return ignore; }
			set { Ignore = value; }
		}

		/// <summary>
		/// Gets or sets the type of the source.
		/// </summary>
		public SourceType Type
		{
			get { return type; }
			set { type = value; OnPropertyChanged("Type"); }
		}

		/// <summary>
		/// Gets or sets whether the files inside the source should be ignored.
		/// </summary>
		public bool Ignore
		{
			get { return ignore; }
			set { ignore = value; OnPropertyChanged("Ignore"); OnPropertyChanged("Strike"); }
		}

		/// <summary>
		/// Gets or sets whether the files inside the source should be included.
		/// </summary>
		public bool Include
		{
			get { return !Ignore; }
			set { Ignore = !value; OnPropertyChanged("Include"); }
		}

		/// <summary>
		/// Gets or sets the name (if type is "Library") or path (if type is "File" or "Folder")
		/// of the source.
		/// </summary>
		public String Data
		{ 
			get { return data; }
			set { data = value; OnPropertyChanged("Data"); } 
		} 

		#endregion
	}

	/// <summary>
	/// Describes a plugin's settings.
	/// </summary>
	/// <remarks>
	/// Used to store the settings in an XML file.
	/// </remarks>
	public class PluginSettings
	{
		/// <summary>
		/// Gets or sets the ID of the plugin.
		/// </summary>
		public String PluginID { get; set; }

		/// <summary>
		/// Gets or sets the list of settings.
		/// </summary>
		public List<Plugins.Setting> Settings { get; set; }

        /// <summary>
        /// Gets or sets whether or not the plugin is activated.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the date that the plugin was installed.
        /// </summary>
        public DateTime Installed { get; set; }
	}

	/// <summary>
	/// Base class for classes which sends out PropertyChanged events.
	/// </summary>
	public abstract class PropertyChangedBase : INotifyPropertyChanged
	{

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		protected void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>
		/// Set the value of a property's underlying variable.
		/// </summary>
		/// <param name="variable">Variable.</param>
		/// <param name="value">Value.</param>
		/// <param name="name">Name of the property.</param>
		/// <returns>true if the value was changed, otherwise false.</returns>
		protected bool SetProp<T>(ref T variable, T value, string name)
		{
			if (!EqualityComparer<T>.Default.Equals(variable, value))
			{
				variable = value;
				OnPropertyChanged(name);
				return true;
			}
			return false;
		}
	}

	#endregion

    #region Event arguments

    /// <summary>
	/// Holds the data for the PropertyChanged event with 
	/// the addition of the values before and after the change
	/// </summary>
	public class PropertyChangedWithValuesEventArgs : PropertyChangedEventArgs
	{
		#region Properties

		/// <summary>
		/// Gets the value of the property before the change
		/// </summary>
		public object OldValue { get; private set; }

		/// <summary>
		/// Gets the value of the property after the change
		/// </summary>
		public object NewValue { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Create a new instance of the PropertyChangedWithValuesEventArgs class
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <param name="oldValue">The value of the property before the change</param>
		/// <param name="newValue">The value of the property after the change</param>
		public PropertyChangedWithValuesEventArgs(string name, object oldValue, object newValue) :
			base(name)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}

		#endregion
	}

	#endregion
}