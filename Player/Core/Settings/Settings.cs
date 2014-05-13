/***
 * Settings.cs
 * 
 * Takes care of initializing settings during first run.
 * The file also contains all data structures used to store
 * data of Stoffi.
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
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Plugins;
using Stoffi.Core.Settings;
using Stoffi.Core.Services;
using Stoffi.Core.Sources;

namespace Stoffi.Core.Settings
{
	/// <summary>
	/// Represents a manager that takes care of all
	/// application 
	/// </summary>
	public static partial class Manager
	{
		#region Members

		private static bool isInitialized = false;
		private static double bufferSize = 0;

		#region GUI
		private static double winWidth = -1;
		private static double winHeight = -1;
		private static double winLeft = -1;
		private static double winTop = -1;
		private static string winState = "Normal";
		private static double equalizerWidth;
		private static double equalizerHeight;
		private static double equalizerLeft;
		private static double equalizerTop;
		private static string currentSelectedNavigation = "Files";
		private static double navigationPaneWidth = 150;
		private static double detailsPaneHeight = 80;
		private static bool detailsPaneVisible = true;
		private static bool menuBarVisible = false;
		private static string language;
		#endregion

		#region Lists
		private static ListConfig sourceListConfig;
		private static ObservableCollection<Location> scanSources = new ObservableCollection<Location>();
		private static ListConfig pluginListConfig;
		private static ObservableCollection<PluginItem> plugins = new ObservableCollection<PluginItem>();
		private static ListConfig historyListConfig;
		private static ObservableCollection<Track> history = new ObservableCollection<Track>();
		private static ListConfig queueListConfig;
		private static ObservableCollection<Track> queue = new ObservableCollection<Track>();
		private static ListConfig fileListConfig;
		private static ObservableCollection<Track> files = new ObservableCollection<Track>();
		private static ListConfig radioListConfig;
		private static ObservableCollection<Track> radio = new ObservableCollection<Track>();
		private static ListConfig discListConfig;
		#endregion

		#region Application params
		private static int id;
		#endregion

		#region Settings
		private static UpgradePolicy upgradePolicy = UpgradePolicy.Automatic;
		private static SearchPolicy searchPolicy = SearchPolicy.Individual;
		private static OpenAddPolicy openAddPolicy = OpenAddPolicy.Library;
		private static OpenPlayPolicy openPlayPolicy = OpenPlayPolicy.BackOfQueue;
		private static bool fastStart;
		private static bool showNotifications = true;
		private static bool pauseWhenLocked = false;
		private static bool pauseWhenSongEnds = false;
		private static KeyboardShortcutProfile currentShortcutProfile;
		private static ObservableCollection<KeyboardShortcutProfile> shortcutProfiles = new ObservableCollection<KeyboardShortcutProfile>();
		private static ObservableCollection<Metadata> pluginSettings = new ObservableCollection<Core.Plugins.Metadata>();
		private static string youTubeFilter = "Music";
		private static string youTubeQuality = "Automatic";
		#endregion

		#region Playback
		private static string currentActiveNavigation = "Files";
		private static Track currentTrack;
		private static EqualizerProfile currentEqualizerProfile;
		private static ObservableCollection<EqualizerProfile> equalizerProfiles = new ObservableCollection<EqualizerProfile>();
		private static int historyIndex = 0;
		private static bool shuffle = false;
		private static RepeatState repeat = RepeatState.NoRepeat;
		private static double volume = 50;
		private static double seek = 0;
		private static MediaState mediaState = MediaState.Paused;
		#endregion

		#region Cloud
		private static bool downloadAlbumArt = false;
		private static bool downloadAlbumArtSmall = true;
		private static bool downloadAlbumArtMedium = true;
		private static bool downloadAlbumArtLarge = true;
		private static bool downloadAlbumArtHuge = false;
		private static ObservableCollection<Identity> cloudIdentities = new ObservableCollection<Identity>();
		private static bool submitSongs = true;
		private static Dictionary<string,Tuple<string,string>> listenBuffer = new Dictionary<string, Tuple<string, string>>();
		#endregion

		#region Misc
		private static bool firstRun = true;
		private static long lastUpgradeCheck = 0;
		private static ObservableCollection<Playlist> playlists = new ObservableCollection<Playlist>();
		private static string oauthSecret;
		private static string oauthToken;
		private static string currentVisualizer;
		#endregion

		#endregion

		#region Properties

		/// <summary>
		/// Gets whether the manager has been initialized or not.
		/// </summary>
		public static bool IsInitialized { get { return isInitialized; } private set { isInitialized = value; } }

		/// <summary>
		/// Gets the culture corresponding to Language.
		/// </summary>
		public static CultureInfo Culture
		{
			get { return CultureInfo.GetCultureInfo(Language); }
		}

		/// <summary>
		/// Gets a value indicating whether data is currently queued for writing to database.
		/// </summary>
		/// <value><c>true</c> if this database writing is queued; otherwise, <c>false</c>.</value>
		public static bool IsWriting
		{
			get
			{
				var coll = collectionChangedBuffer.Count > 0 && collectionChangedTimer != null;
				var prop = propertyChangedBuffer.Count > 0 && propertyChangedTimer != null;
				return coll || prop;
			}
		}

		#region GUI

		/// <summary>
		/// Gets or sets the width of the main window
		/// </summary>
		public static double WinWidth
		{
			get { return winWidth; }
			set
			{
				object old = winWidth;
				winWidth = value;
				OnPropertyChanged("WinWidth", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the height of the main window
		/// </summary>
		public static double WinHeight
		{
			get { return winHeight; }
			set
			{
				object old = winHeight;
				winHeight = value;
				OnPropertyChanged("WinHeight", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the left position of the main window
		/// </summary>
		public static double WinLeft
		{
			get { return winLeft; }
			set
			{
				object old = winLeft;
				winLeft = value;
				OnPropertyChanged("WinLeft", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the top position of the main window
		/// </summary>
		public static double WinTop
		{
			get { return winTop; }
			set
			{
				object old = winTop;
				winTop = value;
				OnPropertyChanged("WinTop", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the state of the main window
		/// </summary>
		public static string WinState
		{
			get { return winState; }
			set
			{
				object old = winState;
				winState = value;
				OnPropertyChanged("WinState", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the height of the equalizer window
		/// </summary>
		public static double EqualizerHeight
		{
			get { return equalizerHeight; }
			set
			{
				object old = equalizerHeight;
				equalizerHeight = value;
				OnPropertyChanged("EqualizerHeight", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the width of the equalizer window
		/// </summary>
		public static double EqualizerWidth
		{
			get { return equalizerWidth; }
			set
			{
				object old = equalizerWidth;
				equalizerWidth = value;
				OnPropertyChanged("EqualizerWidth", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the left position of the equalizer window
		/// </summary>
		public static double EqualizerLeft
		{
			get { return equalizerLeft; }
			set
			{
				object old = equalizerLeft;
				equalizerLeft = value;
				OnPropertyChanged("EqualizerLeft", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the top position of the equalizer window
		/// </summary>
		public static double EqualizerTop
		{
			get { return equalizerTop; }
			set
			{
				object old = equalizerTop;
				equalizerTop = value;
				OnPropertyChanged("EqualizerTop", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the currently selected navigation
		/// </summary>
		public static string CurrentSelectedNavigation
		{
			get { return currentSelectedNavigation; }
			set
			{
				object old = currentSelectedNavigation;
				currentSelectedNavigation = value;
				OnPropertyChanged("CurrentSelectedNavigation", old, value);
			}
		}

		/// <summary>
		/// Gets the default selected navigation
		/// </summary>
		public static string DefaultNavigation { get { return "Files"; } }

		/// <summary>
		/// Gets a value indicating whether the current selected navigation is a playlist.
		/// </summary>
		/// <value><c>true</c> if the current selected navigation is a playlist; otherwise, <c>false</c>.</value>
		public static bool CurrentSelectedNavigationIsPlaylist
		{
			get { return NavigationIsPlaylist(CurrentSelectedNavigation); }
		}

		/// <summary>
		/// Gets or sets the width of the navigation pane
		/// </summary>
		public static double NavigationPaneWidth
		{
			get { return navigationPaneWidth; }
			set
			{
				object old = navigationPaneWidth;
				navigationPaneWidth = value;
				OnPropertyChanged("NavigationPaneWidth", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the height of the details pane
		/// </summary>
		public static double DetailsPaneHeight
		{
			get { return detailsPaneHeight; }
			set
			{
				object old = detailsPaneHeight;
				detailsPaneHeight = value;
				OnPropertyChanged("DetailsPaneHeight", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether the details pane is visible
		/// </summary>
		public static bool DetailsPaneVisible
		{
			get { return detailsPaneVisible; }
			set
			{
				var old = detailsPaneVisible;
				detailsPaneVisible = value;
				OnPropertyChanged("DetailsPaneVisible", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether the menu bar is visible
		/// </summary>
		public static bool MenuBarVisible
		{
			get { return menuBarVisible; }
			set
			{
				var old = menuBarVisible;
				menuBarVisible = value;
				OnPropertyChanged("MenuBarVisible", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the language of the application
		/// </summary>
		public static string Language
		{
			get
			{
				string l = language;
				if (l != null) return l;
				return Thread.CurrentThread.CurrentUICulture.IetfLanguageTag;
			}
			set
			{
				object old = language;
				language = value;
				OnPropertyChanged("Language", old, value);
			}
		}

		#endregion

		#region Lists

        /// <summary>
        /// Gets or sets the configuration of the source list
        /// </summary>
        public static ListConfig SourceListConfig
        {
            get { return sourceListConfig; }
            set
            {
                var old = sourceListConfig;
				sourceListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
                OnPropertyChanged("SourceListConfig", old, value);
            }
        }

        /// <summary>
        /// Gets or sets the sources where Stoffi looks for music
        /// </summary>
		public static ObservableCollection<Location> ScanSources
        {
            get { return scanSources; }
            set
            {
                var old = scanSources;
				scanSources = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
                OnPropertyChanged("Sources", old, value);
            }
        }

        /// <summary>
        /// Gets or sets the configuration of the plugins list
        /// </summary>
        public static ListConfig PluginListConfig
        {
            get { return pluginListConfig; }
            set
            {
                var old = pluginListConfig;
				pluginListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
                OnPropertyChanged("PluginListConfig", old, value);
            }
        }

        /// <summary>
        /// Gets or sets the plugins
        /// </summary>
		public static ObservableCollection<PluginItem> Plugins
        {
            get { return plugins; }
            set
            {
                var old = plugins;
				plugins = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
                OnPropertyChanged("Plugins", old, value);
            }
        }

		/// <summary>
		/// Gets or sets the configuration of the history list
		/// </summary>
		public static ListConfig HistoryListConfig
		{
			get { return historyListConfig; }
			set
			{
				var old = historyListConfig;
				historyListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
				OnPropertyChanged("HistoryListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the history tracks
		/// </summary>
		public static ObservableCollection<Track> HistoryTracks
		{
			get { return history; }
			set
			{
				var old = history;
				history = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("HistoryTracks", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the file list
		/// </summary>
		public static ListConfig FileListConfig
		{
			get { return fileListConfig; }
			set
			{
				var old = fileListConfig;
				fileListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
				OnPropertyChanged("FileListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the file tracks
		/// </summary>
		public static ObservableCollection<Track> FileTracks
		{
			get { return files; }
			set
			{
				var old = files;
				files = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("FileTracks", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the radio list
		/// </summary>
		public static ListConfig RadioListConfig
		{
			get { return radioListConfig; }
			set
			{
				var old = radioListConfig;
				radioListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
				OnPropertyChanged("RadioListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the radio tracks
		/// </summary>
		public static ObservableCollection<Track> RadioTracks
		{
			get { return radio; }
			set
			{
				var old = radio;
				radio = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("RadioTracks", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the disc list
		/// </summary>
		public static ListConfig DiscListConfig
		{
			get { return discListConfig; }
			set
			{
				var old = discListConfig;
				discListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
				OnPropertyChanged("DiscListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the queue list
		/// </summary>
		public static ListConfig QueueListConfig
		{
			get { return queueListConfig; }
			set
			{
				var old = queueListConfig;
				queueListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
				OnPropertyChanged("QueueListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the queue tracks
		/// </summary>
		public static ObservableCollection<Track> QueueTracks
		{
			get { return queue; }
			set
			{
				var old = queue;
				queue = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("QueueTracks", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the YouTube list
		/// </summary>
		public static ListConfig YouTubeListConfig
		{
			get { return Sources.Manager.YouTube.ListConfig; }
			set
			{
				var old = Sources.Manager.YouTube.ListConfig;
				Sources.Manager.YouTube.ListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
				OnPropertyChanged("YouTubeListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the SoundCloud list
		/// </summary>
		public static ListConfig SoundCloudListConfig
		{
			get { return Sources.Manager.SoundCloud.ListConfig; }
			set
			{
				var old = Sources.Manager.SoundCloud.ListConfig;
				Sources.Manager.SoundCloud.ListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
				OnPropertyChanged("SoundCloudListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the Jamendo list
		/// </summary>
		public static ListConfig JamendoListConfig
		{
			get { return Sources.Manager.Jamendo.ListConfig; }
			set
			{
				var old = Sources.Manager.Jamendo.ListConfig;
				Sources.Manager.Jamendo.ListConfig = value;
				if (old != null)
					old.PropertyChanged -= Object_PropertyChanged;
				OnPropertyChanged("JamendoListConfig", old, value);
			}
		}

		#endregion

		#region Application parameters

		/// <summary>
		/// Gets the architecture of the application.
		/// </summary>
		public static string Architecture
		{
			get { return "32"; }
		}

		/// <summary>
		/// Gets the channel of the application (Alpha, Beta or Stable).
		/// </summary>
		public static string Channel
		{
			get { return "beta"; } // build-marker channel DO NOT TOUCH
		}

		/// <summary>
		/// Gets or sets the ID of the application
		/// </summary>
		public static int ID
		{
			get { return id; }
			set
			{
				// it may be null, although C# say int can't be null... :)
				object old = null;
				try
				{
					old = id;
				}
				catch { }

				id = value;
				OnPropertyChanged("ID", old, value);
			}
		}

		/// <summary>
		/// Gets the version of the application
		/// </summary>
		public static long Version
		{
			get { return 1371193904; } // build-marker version DO NOT TOUCH
		}

		/// <summary>
		/// Gets the release of the application
		/// </summary>
		public static string Release
		{
			get { return "Han Beta One"; }
		}

		#endregion

		#region Settings

		/// <summary>
		/// Gets or sets how the upgrades of the application should be performed
		/// </summary>
		public static UpgradePolicy UpgradePolicy
		{
			get { return upgradePolicy; }
			set
			{
				object old = upgradePolicy;
				upgradePolicy = value;
				OnPropertyChanged("UpgradePolicy", old, value);
			}
		}

		/// <summary>
		/// Gets or sets how different list should share search filters
		/// </summary>
		public static SearchPolicy SearchPolicy
		{
			get { return searchPolicy; }
			set
			{
				object old = searchPolicy;
				searchPolicy = value;
				OnPropertyChanged("SearchPolicy", old, value);
			}
		}

		/// <summary>
		/// Gets or sets how to add a track when it's opened with the application
		/// </summary>
		public static OpenAddPolicy OpenAddPolicy
		{
			get { return openAddPolicy; }
			set
			{
				object old = openAddPolicy;
				openAddPolicy = value;
				OnPropertyChanged("OpenAddPolicy", old, value);
			}
		}

		/// <summary>
		/// Gets or sets how to play a track when it's opened with the application
		/// </summary>
		public static OpenPlayPolicy OpenPlayPolicy
		{
			get { return openPlayPolicy; }
			set
			{
				object old = openPlayPolicy;
				openPlayPolicy = value;
				OnPropertyChanged("OpenPlayPolicy", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether the application should stay visible in the taskbar when it's minimized
		/// </summary>
		public static bool FastStart
		{
			get { return fastStart; }
			set
			{
				bool old = fastStart;
				fastStart = value;
#if Windows
				if (!same)
				{
					string path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
					string appPath = U.FullPath;
					string icon = Path.ChangeExtension(appPath, ".ico");

#if WIN7
					path = Path.Combine(path, Path.GetFileName(appPath));
					path = Path.ChangeExtension(path, "lnk");
					if (value)
					{
						try
						{
							if (!File.Exists(path))
							{
								IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
								IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(path);
								shortcut.TargetPath = appPath;
								shortcut.IconLocation = icon;
								shortcut.Arguments = "-minimized";
								shortcut.Description = U.T("MinimizedShortcutDescription");
								shortcut.WorkingDirectory = Path.GetDirectoryName(appPath);
								shortcut.Save();
							}
						}
						catch (Exception e)
						{
							U.L(LogLevel.Error, "SETTINGS", "Could not add autostart link: " + e.Message);
						}
					}
					else
					{
						try
						{
							if (File.Exists(path))
								File.Delete(path);
						}
						catch (Exception e)
						{
							U.L(LogLevel.Error, "SETTINGS", "Could not remove autostart link: " + e.Message);
						}
					}
#endif
				}
#endif
				OnPropertyChanged("FastStart", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to show a notification when a new track is played
		/// </summary>
		public static bool ShowOSD
		{
			get { return showNotifications; }
			set
			{
				bool old = showNotifications;
				showNotifications = value;
				OnPropertyChanged("ShowOSD", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to pause playback while computer is locked
		/// </summary>
		public static bool PauseWhenLocked
		{
			get { return pauseWhenLocked; }
			set
			{
				var old = pauseWhenLocked;
				pauseWhenLocked = value;
				OnPropertyChanged("PauseWhenLocked", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to pause playback when the currently playing song reaches the end.
		/// </summary>
		public static bool PauseWhenSongEnds
		{
			get { return pauseWhenSongEnds; }
			set
			{
				var old = pauseWhenSongEnds;
				pauseWhenSongEnds = value;
				OnPropertyChanged("PauseWhenSongEnds", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the currently used orofile of keyboard shortcuts
		/// </summary>
		public static KeyboardShortcutProfile CurrentShortcutProfile
		{
			get { return currentShortcutProfile; }
			set
			{
				object old = currentShortcutProfile;
				currentShortcutProfile = value;
				OnPropertyChanged("CurrentShortcutProfile", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the keyboard shortcut profiles
		/// </summary>
		public static ObservableCollection<KeyboardShortcutProfile> ShortcutProfiles
		{
			get { return shortcutProfiles; }
			set
			{
				var old = shortcutProfiles;
				shortcutProfiles = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("ShortcutProfiles", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the settings of plugins
		/// </summary>
		public static ObservableCollection<Metadata> PluginSettings
		{
			get { return pluginSettings; }
			set
			{
				var old = pluginSettings;
				pluginSettings = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("PluginSettings", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the filter to use for YouTube searching.
		/// </summary>
		public static string YouTubeFilter
		{
			get { return youTubeFilter; }
			set
			{
				object old = youTubeFilter;
				youTubeFilter = value;
				OnPropertyChanged("YouTubeFilter", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the desired quality of YouTube streaming.
		/// </summary>
		public static string YouTubeQuality
		{
			get { return youTubeQuality; }
			set
			{
				object old = youTubeQuality;
				youTubeQuality = value;
				OnPropertyChanged("YouTubeQuality", old, value);
			}
		}

		#endregion

		#region Playback

		/// <summary>
		/// Gets or sets the navigation that the currently playing track belongs to
		/// </summary>
		public static string CurrentActiveNavigation
		{
			get { return currentActiveNavigation; }
			set
			{
				var old = currentActiveNavigation;
				currentActiveNavigation = value;
				OnPropertyChanged("CurrentActiveNavigation", old, value);
			}
		}

		/// <summary>
		/// Gets a value indicating whether the active selected navigation is a playlist.
		/// </summary>
		/// <value><c>true</c> if the current active navigation is a playlist; otherwise, <c>false</c>.</value>
		public static bool CurrentActiveNavigationIsPlaylist
		{
			get { return NavigationIsPlaylist(CurrentActiveNavigation); }
		}

		/// <summary>
		/// Gets or sets the currently playing track
		/// </summary>
		public static Track CurrentTrack
		{
			get { return currentTrack; }
			set
			{
				var old = currentTrack;
				currentTrack = value;
				OnPropertyChanged("CurrentTrack", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the current equalizer profile
		/// </summary>
		public static EqualizerProfile CurrentEqualizerProfile
		{
			get { return currentEqualizerProfile; }
			set
			{
				var old = currentEqualizerProfile;
				currentEqualizerProfile = value;
				OnPropertyChanged("CurrentEqualizerProfile", old, value);
			}
		}

		/// <summary>
		/// Gets or sets equalizer levels
		/// </summary>
		public static ObservableCollection<EqualizerProfile> EqualizerProfiles
		{
			get { return equalizerProfiles; }
			set
			{
				var old = equalizerProfiles;
				equalizerProfiles = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("EqualizerProfiles", old, value);
			}
		}

		/// <summary>
		/// Gets or sets where in history the current playback is
		/// </summary>
		public static int HistoryIndex
		{
			get { return historyIndex; }
			set
			{
				object old = historyIndex;
				historyIndex = value;
				OnPropertyChanged("HistoryIndex", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to use shuffle when selecting the next track
		/// </summary>
		public static bool Shuffle
		{
			get { return shuffle; }
			set
			{
				bool old = shuffle;
				shuffle = value;
				OnPropertyChanged("Shuffle", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to repeat the tracks or not
		/// </summary>
		public static RepeatState Repeat
		{
			get { return repeat; }
			set
			{
				object old = repeat;
				repeat = value;
				OnPropertyChanged("Repeat", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the volume in percent.
		/// </summary>
		public static double Volume
		{
			get { return volume; }
			set
			{
				object old = volume;
				volume = value;
				OnPropertyChanged("Volume", old, value);
			}
		}

		/// <summary>
		/// Gets or sets current position of the currently playing
		/// track as a value between 0 and 10
		/// </summary>
		public static double Seek
		{
			get { return seek; }
			set
			{
				object old = seek;
				seek = value;
				OnPropertyChanged("Seek", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the buffer size as a value between 0 and 10
		/// </summary>
		public static double BufferSize
		{
			get { return bufferSize; }
			set
			{
				object old = bufferSize;
				bufferSize = value;
				OnPropertyChanged("BufferSize", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the state of the media player
		/// </summary>
		public static MediaState MediaState
		{
			get { return mediaState; }
			set
			{
				if (mediaState != value)
				{
					object old = seek;
					mediaState = value;
					OnPropertyChanged("MediaState", old, value);
				}
			}
		}

		#endregion

		#region Cloud

		/// <summary>
		/// Gets or sets the whether or not to download album art.
		/// </summary>
		public static bool DownloadAlbumArt
		{
			get { return downloadAlbumArt; }
			set
			{
				var old = downloadAlbumArt;
				downloadAlbumArt = value;
				OnPropertyChanged("DownloadAlbumArt", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the whether or not to download small sized album art images.
		/// </summary>
		/// <remarks>Only has effect if DownloadAlbumArt is true.</remarks>
		public static bool DownloadAlbumArtSmall
		{
			get { return downloadAlbumArtSmall; }
			set
			{
				var old = downloadAlbumArtSmall;
				downloadAlbumArtSmall = value;
				OnPropertyChanged("DownloadAlbumArtSmall", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the whether or not to download medium sized album art images.
		/// </summary>
		/// <remarks>Only has effect if DownloadAlbumArt is true.</remarks>
		public static bool DownloadAlbumArtMedium
		{
			get { return downloadAlbumArtMedium; }
			set
			{
				var old = downloadAlbumArtMedium;
				downloadAlbumArtMedium = value;
				OnPropertyChanged("DownloadAlbumArtMedium", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the whether or not to download large sized album art images.
		/// </summary>
		/// <remarks>Only has effect if DownloadAlbumArt is true.</remarks>
		public static bool DownloadAlbumArtLarge
		{
			get { return downloadAlbumArtLarge; }
			set
			{
				var old = downloadAlbumArtLarge;
				downloadAlbumArtLarge = value;
				OnPropertyChanged("DownloadAlbumArtLarge", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the whether or not to download huge sized album art images.
		/// </summary>
		/// <remarks>Only has effect if DownloadAlbumArt is true.</remarks>
		public static bool DownloadAlbumArtHuge
		{
			get { return downloadAlbumArtHuge; }
			set
			{
				var old = downloadAlbumArtHuge;
				downloadAlbumArtHuge = value;
				OnPropertyChanged("DownloadAlbumArtHuge", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the Synchronization to an account
		/// </summary>
		public static ObservableCollection<Identity> CloudIdentities
		{
			get
			{
				return cloudIdentities;
			}
			set
			{
				var old = cloudIdentities;
				cloudIdentities = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("CloudIdentities", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the whether or not tracks should be submitted to the cloud.
		/// </summary>
		public static bool SubmitSongs
		{
			get { return submitSongs; }
			set
			{
				var old = submitSongs;
				submitSongs = value;
				OnPropertyChanged("SubmitSongs", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the buffer used to cache listen submissions which failed, for retransmission later.
		/// </summary>
		/// <remarks>
		/// Key is URL. Value is tuple of method and query parameters.
		/// </remarks>
		public static Dictionary<string,Tuple<string,string>> ListenBuffer
		{
			get { return listenBuffer; }
			set
			{
				var old = listenBuffer;
				listenBuffer = value;
				OnPropertyChanged("ListenBuffer", old, value);
			}
		}
		
		#endregion

		#region Misc

		/// <summary>
		/// Gets or sets whether the application has never been run before
		/// </summary>
		public static bool FirstRun
		{
			get { return firstRun; }
			set
			{
				var old = firstRun;
				firstRun = value;
				OnPropertyChanged("FirstRun", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the last time the application checked for an upgrade
		/// </summary>
		public static long UpgradeCheck
		{
			get { return lastUpgradeCheck; }
			set
			{
				var old = lastUpgradeCheck;
				lastUpgradeCheck = value;
				OnPropertyChanged("UpgradeCheck", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the playlists
		/// </summary>
		public static ObservableCollection<Playlist> Playlists
		{
			get { return playlists; }
			set
			{
				var old = playlists;
				playlists = value;
				if (old != null)
					old.CollectionChanged -= ObservableCollection_CollectionChanged;
				OnPropertyChanged("Playlists", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the OAuth secret
		/// </summary>
		public static string OAuthSecret
		{
			get { return oauthSecret; }
			set
			{
				object old = oauthSecret;
				oauthSecret = value;
				OnPropertyChanged("OAuthSecret", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the OAuth token
		/// </summary>
		public static string OAuthToken
		{
			get { return oauthToken; }
			set
			{
				object old = oauthToken;
				oauthToken = value;
				OnPropertyChanged("OAuthToken", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the navigation that the currently playing track belongs to
		/// </summary>
		public static string CurrentVisualizer
		{
			get { return currentVisualizer; }
			set
			{
				object old = currentVisualizer;
				currentVisualizer = value;
				OnPropertyChanged("CurrentVisualizer", old, value);
			}
		}

		#endregion

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initializes the settings manager.
		/// </summary>
		public static void Initialize()
		{
			#region Equalizer

			int defaultLevels = 0;
			foreach (EqualizerProfile ep in EqualizerProfiles)
				if (ep.IsProtected)
					defaultLevels++;

			if (defaultLevels < 2)
			{
				EqualizerProfiles.Clear();
				EqualizerProfiles.Add(new EqualizerProfile()
				{
					Name = "Flat",
					IsProtected = true
				});
				EqualizerProfiles.Add(new EqualizerProfile()
				{
					Name = "Bass",
					IsProtected = true,
					Levels = new ObservableCollection<float> { 9.2f, 6.6f, 4.2f, 1.5f, -0.5f, -1.5f, 0.5f, 3.0f, 3.5f, 4.8f }
				});
				EqualizerProfiles.Add(new EqualizerProfile()
				{
					Name = "Jazz",
					IsProtected = true,
					Levels = new ObservableCollection<float> { 4.0f, 3.0f, 1.6f, 2.0f, -1.6f, -1.6f, 0f, 1.6f, 3.3f, 4.0f }
				});
				EqualizerProfiles.Add(new EqualizerProfile()
				{
					Name = "Dance",
					IsProtected = true,
					Levels = new ObservableCollection<float> { 7f, 4.7f, 2.5f, 0f, 0f, -3.5f, -5f, -5f, 0f, 0f }
				});
				EqualizerProfiles.Add(new EqualizerProfile()
				{
					Name = "RnB",
					IsProtected = true,
					Levels = new ObservableCollection<float> { 3f, 7f, 5.5f, 1.5f, -3f, -1.5f, 1.8f, 2.4f, 3f, 3.5f }
				});
				EqualizerProfiles.Add(new EqualizerProfile()
				{
					Name = "Speech",
					IsProtected = true,
					Levels = new ObservableCollection<float> { -3.3f, -0.5f, 0f, 0.6f, 3.5f, 5.0f, 5.0f, 4.5f, 3.0f, 0f }
				});
				EqualizerProfiles.Add(new EqualizerProfile()
				{
					Name = "Loud",
					IsProtected = true,
					Levels = new ObservableCollection<float> { 6.6f, 5.2f, 0f, 0f, -1f, 0f, 0f, -5f, 6f, 1f }
				});
				EqualizerProfiles.Add(new EqualizerProfile()
				{
					Name = "Headphones",
					IsProtected = true,
					Levels = new ObservableCollection<float> { 5.5f, 4.5f, 3.5f, 1.5f, -1f, -1.5f, 0.5f, 3.5f, 6.5f, 9f }
				});
			}

			if (CurrentEqualizerProfile == null)
				CurrentEqualizerProfile = EqualizerProfiles[0];

			#endregion

			#region Shortcuts

			if (ShortcutProfiles.Count == 0)
			{
				// Stoffi
				KeyboardShortcutProfile shortStoffi = new KeyboardShortcutProfile();
				shortStoffi.Initialize("Stoffi", true);
				ShortcutProfiles.Add(shortStoffi);

				// Stoffi (laptop)
				KeyboardShortcutProfile shortLaptop = new KeyboardShortcutProfile();
				shortLaptop.Initialize("Stoffi (laptop)", true);
				shortLaptop.SetShortcut("MediaCommands", "Previous", "Ctrl+1");
				shortLaptop.SetShortcut("MediaCommands", "Play or pause", "Ctrl+2");
				shortLaptop.SetShortcut("MediaCommands", "Next", "Ctrl+3");
				shortLaptop.SetShortcut("MediaCommands", "Decrease volume", "Ctrl+4");
				shortLaptop.SetShortcut("MediaCommands", "Increase volume", "Ctrl+5");
				shortLaptop.SetShortcut("MediaCommands", "Seek backward", "Ctrl+6");
				shortLaptop.SetShortcut("MediaCommands", "Seek forward", "Ctrl+7");
				shortLaptop.SetShortcut("MediaCommands", "Toggle shuffle", "Ctrl+8");
				shortLaptop.SetShortcut("MediaCommands", "Toggle repeat", "Ctrl+9");
				ShortcutProfiles.Add(shortLaptop);

				// Amarok
				KeyboardShortcutProfile shortAmarok = new KeyboardShortcutProfile();
				shortAmarok.Initialize("Amarok", true);
				shortAmarok.SetShortcut("Application", "Add track", "Win+A");
				shortAmarok.SetShortcut("Application", "Add playlist", "Space");
				shortAmarok.SetShortcut("MediaCommands", "Play or pause", "Win+X");
				shortAmarok.SetShortcut("MediaCommands", "Next", "Win+B");
				shortAmarok.SetShortcut("MediaCommands", "Previous", "Win+Z");
				shortAmarok.SetShortcut("MediaCommands", "Toggle shuffle", "Ctrl+H");
				shortAmarok.SetShortcut("MediaCommands", "Increase volume", "Win++ (numpad)");
				shortAmarok.SetShortcut("MediaCommands", "Decrease volume", "Win+- (numpad)");
				shortAmarok.SetShortcut("MediaCommands", "Seek forward", "Win+Shift++ (numpad)");
				shortAmarok.SetShortcut("MediaCommands", "Seek backward", "Win+Shift+- (numpad)");
				shortAmarok.SetShortcut("MediaCommands", "Jump to current track", "Ctrl+Enter");
				shortAmarok.SetShortcut("MainWindow", "Toggle menu bar", "Ctrl+M");
				ShortcutProfiles.Add(shortAmarok);

				// Banshee
				KeyboardShortcutProfile shortBanshee = new KeyboardShortcutProfile();
				shortBanshee.Initialize("Banshee", true);
				shortBanshee.SetShortcut("Application", "Add track", "Ctrl+I");
				shortBanshee.SetShortcut("Application", "Close", "Ctrl+Q");
				shortBanshee.SetShortcut("MediaCommands", "Play or pause", "Space");
				shortBanshee.SetShortcut("MediaCommands", "Next", "N");
				shortBanshee.SetShortcut("MediaCommands", "Previous", "B");
				ShortcutProfiles.Add(shortBanshee);

				// Foobar2000
				KeyboardShortcutProfile shortFoobar2000 = new KeyboardShortcutProfile();
				shortFoobar2000.Initialize("Foobar2000", true);
				shortFoobar2000.SetShortcut("Application", "Add track", "Ctrl+O");
				shortFoobar2000.SetShortcut("Application", "Add folder", "Ctrl+A");
				shortFoobar2000.SetShortcut("Application", "Add playlist", "Ctrl+L");
				shortFoobar2000.SetShortcut("Application", "Add radio station", "Ctrl+U");
				shortFoobar2000.SetShortcut("MainWindow", "General preferences", "Ctrl+P");
				shortFoobar2000.SetShortcut("Track", "View information", "Alt+Enter");
				ShortcutProfiles.Add(shortFoobar2000);

				// iTunes
				KeyboardShortcutProfile shortiTunes = new KeyboardShortcutProfile();
				shortiTunes.Initialize("iTunes", true);
				shortiTunes.SetShortcut("Application", "Add track", "Ctrl+O");
				shortiTunes.SetShortcut("Application", "Add playlist", "Ctrl+P");
				shortiTunes.SetShortcut("Application", "Add radio station", "Ctrl+U");
				shortiTunes.SetShortcut("MainWindow", "General preferences", "Ctrl+,");
				shortiTunes.SetShortcut("MainWindow", "Search", "Ctrl+Alt+F");
				shortiTunes.SetShortcut("MediaCommands", "Play or pause", "Space");
				shortiTunes.SetShortcut("MediaCommands", "Increase volume", "Ctrl+Up");
				shortiTunes.SetShortcut("MediaCommands", "Decrease volume", "Ctrl+Down");
				shortiTunes.SetShortcut("MediaCommands", "Seek forward", "Ctrl+Alt+Right");
				shortiTunes.SetShortcut("MediaCommands", "Seek backward", "Ctrl+Alt+Left");
				shortiTunes.SetShortcut("MediaCommands", "Previous", "Left");
				shortiTunes.SetShortcut("MediaCommands", "Next", "Right");
				shortiTunes.SetShortcut("MediaCommands", "Jump to current track", "Ctrl+L");
				shortiTunes.SetShortcut("Track", "Open folder", "Ctrl+R");
				ShortcutProfiles.Add(shortiTunes);

				// MusicBee
				KeyboardShortcutProfile shortMusicBee = new KeyboardShortcutProfile();
				shortMusicBee.Initialize("MusicBee", true);
				shortMusicBee.SetShortcut("MainWindow", "General preferences", "Ctrl+O");
				shortMusicBee.SetShortcut("MainWindow", "Create playlist", "Ctrl+Shift+N");
				shortMusicBee.SetShortcut("MediaCommands", "Play or pause", "Ctrl+P");
				shortMusicBee.SetShortcut("MediaCommands", "Next", "Ctrl+N");
				shortMusicBee.SetShortcut("MediaCommands", "Previous", "Ctrl+B");
				shortMusicBee.SetShortcut("MediaCommands", "Increase volume", "Ctrl+Up");
				shortMusicBee.SetShortcut("MediaCommands", "Decrease volume", "Ctrl+Down");
				shortMusicBee.SetShortcut("Track", "Queue and dequeue", "Ctrl+Enter");
				shortMusicBee.SetShortcut("Track", "View information", "Alt+E");
				ShortcutProfiles.Add(shortMusicBee);

				// Rythmbox
				KeyboardShortcutProfile shortRythmbox = new KeyboardShortcutProfile();
				shortRythmbox.Initialize("Rythmbox", true);
				shortRythmbox.SetShortcut("Application", "Close", "Ctrl+Q");
				shortRythmbox.SetShortcut("Application", "Add folder", "Ctrl+O");
				shortRythmbox.SetShortcut("Application", "Add radio station", "Ctrl+I");
				shortRythmbox.SetShortcut("MainWindow", "Search", "Alt+S");
				shortRythmbox.SetShortcut("MediaCommands", "Play or pause", "Ctrl+Space");
				shortRythmbox.SetShortcut("MediaCommands", "Previous", "Alt+Left");
				shortRythmbox.SetShortcut("MediaCommands", "Next", "Alt+Right");
				shortRythmbox.SetShortcut("MediaCommands", "Toggle shuffle", "Ctrl+U");
				shortRythmbox.SetShortcut("MediaCommands", "Toggle repeat", "Ctrl+R");
				shortRythmbox.SetShortcut("MediaCommands", "Jump to current track", "Ctrl+J");
				shortRythmbox.SetShortcut("Track", "View information", "Alt+Enter");

				shortRythmbox.SetShortcut("MediaCommands", "Jump to previous bookmark", "Alt+,");
				shortRythmbox.SetShortcut("MediaCommands", "Jump to next bookmark", "Alt+.");
				ShortcutProfiles.Add(shortRythmbox);

				// Spotify
				KeyboardShortcutProfile shortSpotify = new KeyboardShortcutProfile();
				shortSpotify.Initialize("Spotify", true);
				shortSpotify.SetShortcut("MediaCommands", "Play or pause", "Space");
				shortSpotify.SetShortcut("MediaCommands", "Next", "Ctrl+Right");
				shortSpotify.SetShortcut("MediaCommands", "Previous", "Ctrl+Left");
				shortSpotify.SetShortcut("MediaCommands", "Increase volume", "Ctrl+Up");
				shortSpotify.SetShortcut("MediaCommands", "Decrease volume", "Ctrl+Down");
				shortSpotify.SetShortcut("MainWindow", "Search", "Ctrl+L");
				shortSpotify.SetShortcut("MainWindow", "General preferences", "Ctrl+P");

				shortSpotify.SetShortcut("Track", "Open folder", "Alt+L");
				ShortcutProfiles.Add(shortSpotify);

				// VLC
				KeyboardShortcutProfile shortVLC = new KeyboardShortcutProfile();
				shortVLC.Initialize("VLC", true);
				shortVLC.SetShortcut("Application", "Add track", "Ctrl+O");
				shortVLC.SetShortcut("Application", "Add folder", "Ctrl+F");
				shortVLC.SetShortcut("Application", "Add radio station", "Ctrl+N");
				shortVLC.SetShortcut("Application", "Close", "Ctrl+Q");
				shortVLC.SetShortcut("MainWindow", "General preferences", "Ctrl+P");
				shortVLC.SetShortcut("MediaCommands", "Play or pause", "Space");
				shortVLC.SetShortcut("MediaCommands", "Next", "N");
				shortVLC.SetShortcut("MediaCommands", "Previous", "P");
				shortVLC.SetShortcut("MediaCommands", "Seek backward", "Ctrl+Left");
				shortVLC.SetShortcut("MediaCommands", "Seek forward", "Ctrl+Right");
				shortVLC.SetShortcut("MediaCommands", "Increase volume", "Ctrl+Up");
				shortVLC.SetShortcut("MediaCommands", "Decrease volume", "Ctrl+Down");
				shortVLC.SetShortcut("MediaCommands", "Toggle shuffle", "R");
				shortVLC.SetShortcut("MediaCommands", "Toggle repeat", "L");

				shortVLC.SetShortcut("MainWindow", "Search", "F3");
				shortVLC.SetShortcut("MainWindow", "Create playlist", "Ctrl+Alt+P");
				ShortcutProfiles.Add(shortVLC);

				// Winamp
				KeyboardShortcutProfile shortWinamp = new KeyboardShortcutProfile();
				shortWinamp.Initialize("Winamp", true);
				shortWinamp.SetShortcut("Application", "Minimize", "Alt+M");
				shortWinamp.SetShortcut("Application", "Add track", "Ctrl+Alt+L");
				shortWinamp.SetShortcut("MainWindow", "History", "Ctrl+H");
				shortWinamp.SetShortcut("MainWindow", "General preferences", "Ctrl+P");
				shortWinamp.SetShortcut("MediaCommands", "Play or pause", "Ctrl+Alt+Insert", true);
				shortWinamp.SetShortcut("MediaCommands", "Next", "Ctrl+Alt+PageDown", true);
				shortWinamp.SetShortcut("MediaCommands", "Previous", "Ctrl+Alt+PageUp", true);
				shortWinamp.SetShortcut("MediaCommands", "Increase volume", "Ctrl+Alt+Up", true);
				shortWinamp.SetShortcut("MediaCommands", "Decrease volume", "Ctrl+Alt+Down", true);
				shortWinamp.SetShortcut("MediaCommands", "Seek forward", "Ctrl+Alt+Right", true);
				shortWinamp.SetShortcut("MediaCommands", "Seek backward", "Ctrl+Alt+Left", true);
				shortWinamp.SetShortcut("MediaCommands", "Toggle shuffle", "S");
				shortWinamp.SetShortcut("MediaCommands", "Toggle repeat", "R");
				shortWinamp.SetShortcut("Track", "View information", "Alt+3");

				shortWinamp.SetShortcut("MainWindow", "Toggle menu bar", "Ctrl+Alt+M");
				ShortcutProfiles.Add(shortWinamp);

				// Windows Media Player
				KeyboardShortcutProfile shortWMP = new KeyboardShortcutProfile();
				shortWMP.Initialize("Windows Media Player", true);
				shortWMP.SetShortcut("Application", "Add track", "Ctrl+O");
				shortWMP.SetShortcut("Application", "Add radio station", "Ctrl+U");
				shortWMP.SetShortcut("MainWindow", "Search", "Ctrl+E");
				shortWMP.SetShortcut("MainWindow", "Toggle menu bar", "F10");
				shortWMP.SetShortcut("MediaCommands", "Play or pause", "Ctrl+P");
				shortWMP.SetShortcut("MediaCommands", "Next", "Ctrl+F");
				shortWMP.SetShortcut("MediaCommands", "Previous", "Ctrl+B");
				shortWMP.SetShortcut("MediaCommands", "Seek forward", "Ctrl+Shift+F");
				shortWMP.SetShortcut("MediaCommands", "Seek backward", "Ctrl+Shift+B");
				shortWMP.SetShortcut("MediaCommands", "Toggle shuffle", "Ctrl+H");
				shortWMP.SetShortcut("MediaCommands", "Toggle repeat", "Ctrl+T");
				shortWMP.SetShortcut("MediaCommands", "Increase volume", "F9");
				shortWMP.SetShortcut("MediaCommands", "Decrease volume", "F8");
				shortWMP.SetShortcut("Track", "View information", "F2");

				shortWMP.SetShortcut("MainWindow", "Tracklist", "Ctrl+1");
				ShortcutProfiles.Add(shortWMP);
			}

			#endregion

			#region List configurations

			// sources list
			if (sourceListConfig == null)
			{
				sourceListConfig = ListConfig.Create();
				sourceListConfig.Columns.Add(ListColumn.Create("Data", U.T("ColumnLocation"), 200));
				sourceListConfig.Columns.Add(ListColumn.Create("Type", U.T("ColumnType"), 150, "SourceType"));
				sourceListConfig.Sorts.Add("asc:Type");
				sourceListConfig.Sorts.Add("asc:Data");
				SaveListConfiguration(sourceListConfig, "listConfigurations");
				SaveConfig("config", "sourceListConfig", DBEncode(db.LastID("listConfigurations")));
			}
			sourceListConfig.PropertyChanged += Object_PropertyChanged;

			if (firstRun)
			{
				SaveListConfiguration(YouTubeListConfig, "listConfigurations");
				SaveConfig("config", "youTubeListConfig", DBEncode(db.LastID("listConfigurations")));

				SaveListConfiguration(SoundCloudListConfig, "listConfigurations");
				SaveConfig("config", "soundCloudListConfig", DBEncode(db.LastID("listConfigurations")));

				SaveListConfiguration(JamendoListConfig, "listConfigurations");
				SaveConfig("config", "jamendoListConfig", DBEncode(db.LastID("listConfigurations")));

				Sources.Radio.Manager.PopulateDefaults();
			}
			YouTubeListConfig.PropertyChanged += Object_PropertyChanged;
			SoundCloudListConfig.PropertyChanged += Object_PropertyChanged;
			JamendoListConfig.PropertyChanged += Object_PropertyChanged;

			// file list
			if (fileListConfig == null)
			{
				fileListConfig = ListConfig.Create();
				fileListConfig.Initialize();
				fileListConfig.Sorts.Add("asc:Title");
				fileListConfig.Sorts.Add("asc:Track");
				fileListConfig.Sorts.Add("asc:Album");
				fileListConfig.Sorts.Add("asc:Artist");
				SaveListConfiguration(fileListConfig, "listConfigurations");
				SaveConfig("config", "fileListConfig", DBEncode(db.LastID("listConfigurations")));
			}
			fileListConfig.PropertyChanged += Object_PropertyChanged;

			// radio list
			if (radioListConfig == null)
			{
				radioListConfig = ListConfig.Create();
				radioListConfig.Columns.Add(ListColumn.Create("Album", U.T("ColumnGroup"), 150));
				radioListConfig.Columns.Add(ListColumn.Create("Title", U.T("ColumnTitle"), 200));
				radioListConfig.Columns.Add(ListColumn.Create("Genre", U.T("ColumnGenre"), 100));
				radioListConfig.Columns.Add(ListColumn.Create("Artist", U.T("ColumnDJ"), 150));
				radioListConfig.Columns.Add(ListColumn.Create("Views", U.T("ColumnListeners"), 100));
				radioListConfig.Columns.Add(ListColumn.Create("URL", U.T("ColumnURL"), 300));
				radioListConfig.Columns.Add(ListColumn.Create("LastPlayed", U.T("ColumnLastPlayed"), 200, "DateTime"));
				radioListConfig.Columns.Add(ListColumn.Create("PlayCount", U.T("ColumnPlayCount"), 100, "Number", Alignment.Right, false));
				radioListConfig.Sorts.Add("desc:Views");
				radioListConfig.Sorts.Add("asc:Group");
				SaveListConfiguration(radioListConfig, "listConfigurations");
				SaveConfig("config", "radioListConfig", DBEncode(db.LastID("listConfigurations")));
			}
			radioListConfig.PropertyChanged += Object_PropertyChanged;

			// disc list
			if (discListConfig == null)
			{
				discListConfig = ListConfig.Create();
				discListConfig.Initialize();
				SaveListConfiguration(discListConfig, "listConfigurations");
				SaveConfig("config", "discListConfig", DBEncode(db.LastID("listConfigurations")));
			}
			discListConfig.PropertyChanged += Object_PropertyChanged;

			// queue list
			if (queueListConfig == null)
			{
				queueListConfig = ListConfig.Create();
				queueListConfig.IsNumberVisible = true;
				queueListConfig.Initialize();
				queueListConfig.Sorts.Add("asc:Title");
				queueListConfig.Sorts.Add("asc:Track");
				queueListConfig.Sorts.Add("asc:Album");
				queueListConfig.Sorts.Add("asc:Artist");
				SaveListConfiguration(queueListConfig, "listConfigurations");
				SaveConfig("config", "queueListConfig", DBEncode(db.LastID("listConfigurations")));
			}
			queueListConfig.PropertyChanged += Object_PropertyChanged;

			// history list
			if (historyListConfig == null)
			{
				historyListConfig = ListConfig.Create();
				historyListConfig.Columns.Add(ListColumn.Create("LastPlayed", U.T("ColumnPlayed"), 200, "DateTime"));
				historyListConfig.Columns.Add(ListColumn.Create("Artist", U.T("ColumnArtist"), 100));
				historyListConfig.Columns.Add(ListColumn.Create("Album", U.T("ColumnAlbum"), 100));
				historyListConfig.Columns.Add(ListColumn.Create("Title", U.T("ColumnTitle"), 100));
				historyListConfig.Columns.Add(ListColumn.Create("Genre", U.T("ColumnGenre"), 100));
				historyListConfig.Columns.Add(ListColumn.Create("Length", U.T("ColumnLength"), 60, "Duration", Alignment.Right));
				historyListConfig.Columns.Add(ListColumn.Create("Year", U.T("ColumnYear"), 100, Alignment.Right, false));
				historyListConfig.Columns.Add(ListColumn.Create("PlayCount", U.T("ColumnPlayCount"), 100, "Number", Alignment.Right, false));
				historyListConfig.Columns.Add(ListColumn.Create("Path", U.T("ColumnPath"), 200, Alignment.Left, false));
				historyListConfig.Columns.Add(ListColumn.Create("Track", U.T("ColumnTrack"), "TrackNumber", 100, Alignment.Right, false));
				historyListConfig.Sorts.Add("dsc:LastPlayed");
				SaveListConfiguration(historyListConfig, "listConfigurations");
				SaveConfig("config", "historyListConfig", DBEncode(db.LastID("listConfigurations")));
			}
			historyListConfig.PropertyChanged += Object_PropertyChanged;

			if (pluginListConfig == null)
			{
				pluginListConfig = ListConfig.Create();
				pluginListConfig.AcceptFileDrops = false;
				pluginListConfig.IsDragSortable = false;
				pluginListConfig.Columns.Add(ListColumn.Create("Name", U.T("ColumnName"), 150));
				pluginListConfig.Columns.Add(ListColumn.Create("Author", U.T("ColumnAuthor"), 100));
				pluginListConfig.Columns.Add(ListColumn.Create("Type", U.T("ColumnType"), 100, "PluginType"));
				pluginListConfig.Columns.Add(ListColumn.Create("Installed", U.T("ColumnInstalled"), 150, "DateTime"));
				pluginListConfig.Columns.Add(ListColumn.Create("Version", U.T("ColumnVersion"), 80));
				//pluginListConfig.Columns.Add(ListColumn.Create("Enabled", U.T("ColumnEnabled"), "Enabled", 20));
				pluginListConfig.Sorts.Add("asc:Name");
				SaveListConfiguration(pluginListConfig, "listConfigurations");
				SaveConfig("config", "pluginListConfig", DBEncode(db.LastID("listConfigurations")));
			}
			pluginListConfig.PropertyChanged += Object_PropertyChanged;

			#endregion

			#region Adjustments

			//Save();

			//foreach (Track t in FileTracks)
			//    if (String.IsNullOrWhiteSpace(t.ArtURL))
			//        t.ArtURL = "/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg";

			//foreach (Track t in HistoryTracks)
			//    if (String.IsNullOrWhiteSpace(t.ArtURL))
			//        t.ArtURL = "/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg";

			//foreach (Track t in QueueTracks)
			//    if (String.IsNullOrWhiteSpace(t.ArtURL))
			//        t.ArtURL = "/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg";

			//foreach (Playlist p in Playlists)
			//    foreach (Track t in p.Tracks)
			//        if (String.IsNullOrWhiteSpace(t.ArtURL))
			//            t.ArtURL = "/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg";

			//Thread aa_thread = new Thread(delegate()
			//{
			//    foreach (Track t in HistoryTracks)
			//    {
			//        if (U.IsClosing) break;
			//        if (t.ArtURL == "/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg")
			//            ServiceManager.SetArt(t);
			//    }

			//    foreach (Track t in QueueTracks)
			//    {
			//        if (U.IsClosing) break;
			//        if (t.ArtURL == "/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg")
			//            ServiceManager.SetArt(t);
			//    }

			//    foreach (Playlist p in Playlists)
			//        foreach (Track t in p.Tracks)
			//        {
			//            if (U.IsClosing) break;
			//            if (t.ArtURL == "/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg")
			//                ServiceManager.SetArt(t);
			//        }
			//    foreach (Track t in FileTracks)
			//    {
			//        if (U.IsClosing) break;
			//        if (t.ArtURL == "/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg")
			//            ServiceManager.SetArt(t);
			//    }
			//});
			//aa_thread.Name = "Fetch album art";
			//aa_thread.Priority = ThreadPriority.BelowNormal;
			//aa_thread.Start();

			// fix tracks
			//foreach (Track t in HistoryTracks)
			//{
			//    if (t.Path.StartsWith("youtube://"))
			//        t.Path = "stoffi:track:youtube:" + t.Path.Substring(10);
			//    else if (t.Path.StartsWith("soundcloud://"))
			//        t.Path = "stoffi:track:soundcloud:" + t.Path.Substring(13);
			//}
			//foreach (Track t in QueueTracks)
			//{
			//    if (t.Path.StartsWith("youtube://"))
			//        t.Path = "stoffi:track:youtube:" + t.Path.Substring(10);
			//    else if (t.Path.StartsWith("soundcloud://"))
			//        t.Path = "stoffi:track:soundcloud:" + t.Path.Substring(13);
			//}

			//foreach (Playlist p in Playlists)
			//    foreach (Track t in p.Tracks)
			//    {
			//        if (t.Path.StartsWith("youtube://"))
			//            t.Path = "stoffi:track:youtube:" + t.Path.Substring(10);
			//        else if (t.Path.StartsWith("soundcloud://"))
			//            t.Path = "stoffi:track:soundcloud:" + t.Path.Substring(13);
			//    }

			//foreach (KeyboardShortcutProfile p in ShortcutProfiles)
			//    if (p.Shortcuts.ElementAt<KeyboardShortcut>(17).Name != "Plugins")
			//        p.Shortcuts.Insert(17, new KeyboardShortcut { Category = "MainWindow", Name = "Plugins", IsGlobal = false, 
			//            Keys = "Alt+X" });

			//foreach (KeyboardShortcutProfile p in ShortcutProfiles)
			//    foreach (KeyboardShortcut s in p.Shortcuts)
			//        s.Name = s.Name.Replace("Plugin", "App").Replace("plugin", "app");

			FixListViewConfig(SourceListConfig);
			FixListViewConfig(FileListConfig);
			FixListViewConfig(QueueListConfig);
			FixListViewConfig(HistoryListConfig);
			FixListViewConfig(YouTubeListConfig);
			foreach (Playlist p in Playlists)
			{
				if (p.ListConfig == null)
					p.ListConfig = ListConfig.Create();
				else
					FixListViewConfig(p.ListConfig);
			}

			//ServiceManager.SetArt();

			if (String.IsNullOrWhiteSpace(CurrentSelectedNavigation))
				CurrentSelectedNavigation = "Files";

			#endregion

			#region Event handlers
			foreach (var x in radio)
			{
				x.PropertyChanged -= Object_PropertyChanged;
				x.PropertyChanged += Object_PropertyChanged;
			}
			foreach (var x in scanSources)
			{
				x.PropertyChanged -= Object_PropertyChanged;
				x.PropertyChanged += Object_PropertyChanged;
			}
			foreach (var x in equalizerProfiles)
			{
				x.PropertyChanged -= Object_PropertyChanged;
				x.PropertyChanged += Object_PropertyChanged;
			}
			foreach (var x in shortcutProfiles)
			{
				x.PropertyChanged -= Object_PropertyChanged;
				x.PropertyChanged += Object_PropertyChanged;
			}
			foreach (var x in playlists)
			{
				x.PropertyChanged -= Object_PropertyChanged;
				x.PropertyChanged += Object_PropertyChanged;
			}
			foreach (var x in pluginSettings)
			{
				x.PropertyChanged -= Object_PropertyChanged;
				x.PropertyChanged += Object_PropertyChanged;
			}
			#endregion

			if (firstRun)
				Sources.Radio.Manager.PopulateDefaults ();
			DispatchInitialized();
		}

		/// <summary>
		/// Fixes the list view config.
		/// </summary>
		/// <param name="vdc">Vdc.</param>
		public static void FixListViewConfig(ListConfig vdc)
		{
			//if (vdc.Columns.Count == 2 && vdc.Sorts.Count == 0)
			//{
			//  vdc.Sorts.Add("asc:Type");
			//  vdc.Sorts.Add("asc:Data");
			//}
			//if (vdc.Columns.Count == 10 && vdc.Sorts.Count == 0)
			//{
			//  vdc.Sorts.Add("asc:Title");
			//  vdc.Sorts.Add("asc:Track");
			//  vdc.Sorts.Add("asc:Album");
			//  vdc.Sorts.Add("asc:Artist");
			//}

			for (int i=0; i < vdc.Sorts.Count; i++)
				if (vdc.Sorts [i].EndsWith (":Track"))
					vdc.Sorts [i] = vdc.Sorts [i].Replace (":Track", ":TrackNumber");

			foreach (ListColumn column in vdc.Columns)
			{
				if (column.Binding == "Track")
					column.Binding = "TrackNumber";
				//switch (column.Name)
				//{
				//  case "Length":
				//    column.SortField = "Length";
				//    column.Converter = "Duration";
				//    break;

				//  case "LastPlayed":
				//    column.SortField = "LastPlayed";
				//    column.Converter = "DateTime";
				//    break;

				//  case "Views":
				//    column.SortField = "Views";
				//    column.Converter = "Number";
				//    break;

				//  case "PlayCount":
				//    column.Converter = "Number";
				//    break;

				//  case "Type":
				//    column.Converter = "SourceType";
				//    break;
				//}
			}
		}

		/// <summary>
		/// Checks if a given navigation is a playlist.
		/// </summary>
		/// <returns><c>true</c>, the navigation is a playlist, <c>false</c> otherwise.</returns>
		/// <param name="navigation">Navigation item.</param>
		public static bool NavigationIsPlaylist(string navigation)
		{
			return !String.IsNullOrWhiteSpace (navigation) && navigation.StartsWith ("Playlist:");
		}

		/// <summary>
		/// Gets the list configuration for a given navigation.
		/// </summary>
		/// <returns>The list configuration.</returns>
		/// <param name="navigation">The navigation item.</param>
		public static ListConfig GetListConfiguration(string navigation)
		{
			switch (navigation) {
				case "Files":
				return FileListConfig;

				case "YouTube":
				return YouTubeListConfig;

				case "SoundCloud":
				return SoundCloudListConfig;

				case "Radio":
				return RadioListConfig;

				case "Jamendo":
				return JamendoListConfig;

				case "Queue":
				return QueueListConfig;

				case "History":
				return HistoryListConfig;

				default:
				var playlist = Core.Playlists.Manager.GetSelected ();
				if (playlist != null)
					return playlist.ListConfig;
				break;
			}
			return null;
		}

		/// <summary>
		/// Gets the list configuration for the selected navigation item.
		/// </summary>
		/// <returns>The selected list configuration.</returns>
		public static ListConfig GetSelectedListConfiguration ()
		{
			return GetListConfiguration (CurrentSelectedNavigation);
		}

		/// <summary>
		/// Gets the track collection for a given navigation.
		/// </summary>
		/// <returns>The track collection.</returns>
		/// <param name="navigation">The navigation item.</param>
		public static ObservableCollection<Track> GetTrackCollection(string navigation)
		{
			switch (navigation) {
			case "Files":
				return FileTracks;

			case "YouTube":
				return Sources.Manager.YouTube.Tracks;

			case "SoundCloud":
				return Sources.Manager.SoundCloud.Tracks;

			case "Radio":
				return RadioTracks;

			case "Jamendo":
				return Sources.Manager.Jamendo.Tracks;

			case "Queue":
				return QueueTracks;

			case "History":
				return HistoryTracks;

			default:
				var playlist = Core.Playlists.Manager.GetSelected ();
				if (playlist != null)
					return playlist.Tracks;
				break;
			}
			return null;
		}

		/// <summary>
		/// Gets a number of bools indicating whether the track collection contains any or only
		/// tracks of a specific type and whether the tracks are queued or playing.
		/// </summary>
		/// <returns>The collection state.</returns>
		/// <param name="tracks">Track collection.</param>
		public static Dictionary<string,bool> GetCollectionState(IEnumerable<Track> tracks)
		{
			var r = new Dictionary<string,bool> ();
			r["anyYouTube"] = false;
			r["anyFiles"] = false;
			r["anyJamendo"] = false;
			r["anyRadio"] = false;
			r["anySoundCloud"] = false;

			r["onlyYouTube"] = false;
			r["onlyFiles"] = false;
			r["onlyJamendo"] = false;
			r["onlyRadio"] = false;
			r["onlySoundCloud"] = false;

			r["isPlaying"] = false;
			r["isQueued"] = false;

			foreach (var t in tracks) {
				switch (t.Type) {
				case TrackType.File:
					r["anyFiles"] = true;
					break;

				case TrackType.Jamendo:
					r["anyJamendo"] = true;
					break;

				case TrackType.SoundCloud:
					r["anySoundCloud"] = true;
					break;

				case TrackType.WebRadio:
					r["anyRadio"] = true;
					break;

				case TrackType.YouTube:
					r["anyYouTube"] = true;
					break;
				}

				if (!r["isPlaying"] && CurrentTrack != null && CurrentTrack.Path == t.Path && MediaState == MediaState.Playing)
					r["isPlaying"] = true;

				if (!r["isQueued"]) {
					foreach (var q in QueueTracks) {
						if (t.Path == q.Path) {
							r["isQueued"] = true;
							break;
						}
					}
				}
			}

			r ["onlyFiles"] = !(r ["anyJamendo"] || r ["anySoundCloud"] || r ["anyRadio"] || r ["anyYouTube"]);
			r ["onlyJamendo"] = !(r ["anyFiles"] || r ["anySoundCloud"] || r ["anyRadio"] || r ["anyYouTube"]);
			r ["onlySoundCloud"] = !(r ["anyFiles"] || r ["anyJamendo"] || r ["anyRadio"] || r ["anyYouTube"]);
			r ["onlyRadio"] = !(r ["anyFiles"] || r ["anyJamendo"] || r ["anySoundCloud"] || r ["anyYouTube"]);
			r ["onlyYouTube"] = !(r ["anyFiles"] || r ["anyJamendo"] || r ["anySoundCloud"] || r ["anyRadio"]);
			r ["onlySharable"] = !r["anyFiles"];

			return r;
		}

		/// <summary>
		/// Generates a name for an equalizer profile given an initial seed.
		/// </summary>
		/// <returns>The equalizer name to either create or add a prefix to.</returns>
		/// <param name="name">The first available name of Name, Name 1, Name 2, etc.</param>
		public static string GenerateEqualizerName(string name)
		{
			int n = 0;
			while (true)
			{
				bool exists = false;
				var checkName = String.Format ("{0} {1}", name, n);
				if (n == 0)
					checkName = name;
				foreach (var p in EqualizerProfiles)
				{
					if (p.Name == checkName)
					{
						exists = true;
						break;
					}
				}
				if (!exists)
					return checkName;
				n++;
			}
		}

		/// <summary>
		/// Gets the track collection for the selected navigation item.
		/// </summary>
		/// <returns>The selected track collection.</returns>
		public static ObservableCollection<Track> GetSelectedTrackCollection ()
		{
			return GetTrackCollection (CurrentSelectedNavigation);
		}

		/// <summary>
		/// Gets the track collection for the navigation from which the currently playing track is.
		/// </summary>
		/// <returns>The active track collection.</returns>
		public static List<Track> GetActiveTrackCollection()
		{
			var collection = GetTrackCollection (CurrentActiveNavigation);
			if (collection != null)
				return collection.ToList ();
			return new List<Track>();
		}

		/// <summary>
		/// Determines if the specified track is the current track.
		/// </summary>
		/// <returns><c>true</c> if it is current; otherwise, <c>false</c>.</returns>
		/// <param name="track">Track to check.</param>
		public static bool IsCurrent(Track track)
		{
			return currentTrack != null && track != null && track.Path == currentTrack.Path;
		}

		/// <summary>
		/// Retrieves the an equalizer profile given its name
		/// </summary>
		/// <param name="name">The name of the profile</param>
		/// <returns>The equalizer profile with the given name</returns>
		public static EqualizerProfile GetEqualizerProfile(string name)
		{
			foreach (EqualizerProfile p in EqualizerProfiles)
				if (p.IsProtected && U.T("EqualizerProfile" + p.Name) == name || p.Name == name)
					return p;
			if (EqualizerProfiles.Count > 0)
				return EqualizerProfiles[0];
			return null;
		}
	
		/// <summary>
		/// Looks for the cloud identity with a given user ID.
		/// </summary>
		/// <param name="userID">The user ID to look for.</param>
		/// <returns>The corresponding cloud identity or null if not found.</returns>
		public static Identity GetCloudIdentity(uint userID)
		{
			foreach (Identity i in CloudIdentities)
			{
				if (i.UserID == userID)
					return i;
			}
			return null;
		}

		/// <summary>
		/// Checks if there exists a cloud identity with a given user ID.
		/// </summary>
		/// <param name="userID">The user ID to look for.</param>
		/// <returns>True if an identity was found, otherwise false.</returns>
		public static bool HasCloudIdentity(uint userID)
		{
			return GetCloudIdentity(userID) != null;
		}

		/// <summary>
		/// Removes a set of tracks from the currently selected track list.
		/// </summary>
		/// <param name="tracks">Tracks.</param>
		public static void RemoveTracks(IEnumerable<Track> tracks)
		{
			bool inQueue = CurrentSelectedNavigation == "Queue";
			bool inHistory = CurrentSelectedNavigation == "History";
			bool inPlaylist = CurrentSelectedNavigationIsPlaylist;

			tracks = from track in tracks
			         where track.Type == TrackType.File || track.Type == TrackType.WebRadio || inQueue || inHistory || inPlaylist
			         select track;

			foreach (var track in tracks) {
				if (MediaState == MediaState.Playing && track.IsActive)
				{
					Media.Manager.Stop ();
					break;
				}
			}

			switch (CurrentSelectedNavigation) {
			case "Files":
				Files.Remove (tracks);
				break;

			case "Radio":
				foreach (var track in tracks) {
					RadioTracks.Remove (track);
				}
				break;

			case "Queue":
				Media.Manager.Dequeue (tracks);
				break;

			case "History":
				foreach (var track in tracks) {
					foreach (var t in U.GetTracks (HistoryTracks, track.Path))
						if (HistoryTracks.IndexOf (t) <= HistoryIndex)
							HistoryIndex--;
					U.RemovePath (HistoryTracks, track.Path);
				}
				if (HistoryIndex < 0)
					HistoryIndex = 0;
				else if (HistoryIndex > HistoryTracks.Count - 1)
					HistoryIndex = HistoryTracks.Count - 1;
				break;

			default:
				if (CurrentSelectedNavigationIsPlaylist) {
					var p = Core.Playlists.Manager.GetSelected ();
					if (p != null)
						p.Remove (tracks);
				}
				break;
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Gets a keyboard shortcut profile based on its name.
		/// </summary>
		/// <returns>The keyboard shortcut profile.</returns>
		/// <param name="profiles">Profiles.</param>
		/// <param name="name">Name.</param>
		private static KeyboardShortcutProfile GetKeyboardShortcutProfile(IEnumerable<KeyboardShortcutProfile> profiles, String name)
		{
			foreach (KeyboardShortcutProfile profile in profiles)
				if (profile.Name == name)
					return profile;
			return null;
		}

		/// <summary>
		/// Checks whether or not a given shortcut profile exists or not.
		/// </summary>
		/// <param name="profileName">The name of the profile to look for</param>
		/// <returns>True if the profile exists, otherwise false</returns>
		private static bool KeyboardProfileExists(string profileName)
		{
			foreach (KeyboardShortcutProfile p in ShortcutProfiles)
				if (p.Name == profileName)
					return true;
			return false;
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Trigger the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		/// <param name="oldValue">The value of the property before the change</param>
		/// <param name="newValue">The value of the property after the change</param>
		public static void OnPropertyChanged(string name, bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
				OnPropertyChanged (name, (object)oldValue, (object)newValue);
		}

		/// <summary>
		/// Trigger the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		/// <param name="oldValue">The value of the property before the change</param>
		/// <param name="newValue">The value of the property after the change</param>
		public static void OnPropertyChanged(string name, object oldValue, object newValue)
		{
			if (oldValue != newValue)
			{
				if (PropertyChanged != null)
					PropertyChanged(null, new PropertyChangedWithValuesEventArgs(name, oldValue, newValue));
				
				var t = new Thread (delegate() { Save (name); });
				t.Name = "Save property to database";
				t.Priority = ThreadPriority.BelowNormal;
				t.Start ();
			}
		}

		/// <summary>
		/// Trigger the Initialized event
		/// </summary>
		public static void DispatchInitialized()
		{
			U.L(LogLevel.Debug, "SETTINGS", "Dispatching initialized");
			IsInitialized = true;
			firstRun = true;
			if (Initialized != null)
				Initialized(null, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when a property has been changed
		/// </summary>
		public static event EventHandler<PropertyChangedWithValuesEventArgs> PropertyChanged;

		/// <summary>
		/// Occurs when the manager has been fully initialized
		/// </summary>
		public static event EventHandler Initialized;

		#endregion
	}

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