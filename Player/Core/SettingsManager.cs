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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

using Stoffi.Core;

namespace Stoffi
{
	/// <summary>
	/// Represents a manager that takes care of all
	/// application settings.
	/// </summary>
	public static class SettingsManager
	{
		#region Members

		private static double bufferSize = 0;
		private static bool isInitialized = false;

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

		#region GUI

		/// <summary>
		/// Gets or sets the width of the main window
		/// </summary>
		public static double WinWidth
		{
			get { return Properties.Settings.Default.WinWidth; }
			set
			{
				object old = Properties.Settings.Default.WinWidth;
				Properties.Settings.Default.WinWidth = value;
				DispatchPropertyChanged("WinWidth", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the height of the main window
		/// </summary>
		public static double WinHeight
		{
			get { return Properties.Settings.Default.WinHeight; }
			set
			{
				object old = Properties.Settings.Default.WinHeight;
				Properties.Settings.Default.WinHeight = value;
				DispatchPropertyChanged("WinHeight", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the left position of the main window
		/// </summary>
		public static double WinLeft
		{
			get { return Properties.Settings.Default.WinLeft; }
			set
			{
				object old = Properties.Settings.Default.WinLeft;
				Properties.Settings.Default.WinLeft = value;
				DispatchPropertyChanged("WinLeft", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the top position of the main window
		/// </summary>
		public static double WinTop
		{
			get { return Properties.Settings.Default.WinTop; }
			set
			{
				object old = Properties.Settings.Default.WinTop;
				Properties.Settings.Default.WinTop = value;
				DispatchPropertyChanged("WinTop", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the state of the main window
		/// </summary>
		public static string WinState
		{
			get { return Properties.Settings.Default.WinState; }
			set
			{
				object old = Properties.Settings.Default.WinState;
				Properties.Settings.Default.WinState = value;
				DispatchPropertyChanged("WinState", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the height of the equalizer window
		/// </summary>
		public static double EqualizerHeight
		{
			get { return Properties.Settings.Default.EqualizerHeight; }
			set
			{
				object old = Properties.Settings.Default.EqualizerHeight;
				Properties.Settings.Default.EqualizerHeight = value;
				DispatchPropertyChanged("EqualizerHeight", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the width of the equalizer window
		/// </summary>
		public static double EqualizerWidth
		{
			get { return Properties.Settings.Default.EqualizerWidth; }
			set
			{
				object old = Properties.Settings.Default.EqualizerWidth;
				Properties.Settings.Default.EqualizerWidth = value;
				DispatchPropertyChanged("EqualizerWidth", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the left position of the equalizer window
		/// </summary>
		public static double EqualizerLeft
		{
			get { return Properties.Settings.Default.EqualizerLeft; }
			set
			{
				object old = Properties.Settings.Default.EqualizerLeft;
				Properties.Settings.Default.EqualizerLeft = value;
				DispatchPropertyChanged("EqualizerLeft", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the top position of the equalizer window
		/// </summary>
		public static double EqualizerTop
		{
			get { return Properties.Settings.Default.EqualizerTop; }
			set
			{
				object old = Properties.Settings.Default.EqualizerTop;
				Properties.Settings.Default.EqualizerTop = value;
				DispatchPropertyChanged("EqualizerTop", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the currently selected navigation
		/// </summary>
		public static string CurrentSelectedNavigation
		{
			get { return Properties.Settings.Default.CurrentSelectedNavigation; }
			set
			{
				object old = Properties.Settings.Default.CurrentSelectedNavigation;
				Properties.Settings.Default.CurrentSelectedNavigation = value;
				DispatchPropertyChanged("CurrentSelectedNavigation", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the width of the navigation pane
		/// </summary>
		public static double NavigationPaneWidth
		{
			get { return Properties.Settings.Default.NavigationPaneWidth; }
			set
			{
				object old = Properties.Settings.Default.NavigationPaneWidth;
				Properties.Settings.Default.NavigationPaneWidth = value;
				DispatchPropertyChanged("NavigationPaneWidth", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the height of the details pane
		/// </summary>
		public static double DetailsPaneHeight
		{
			get { return Properties.Settings.Default.DetailsPaneHeight; }
			set
			{
				object old = Properties.Settings.Default.DetailsPaneHeight;
				Properties.Settings.Default.DetailsPaneHeight = value;
				DispatchPropertyChanged("DetailsPaneHeight", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether the details pane is visible
		/// </summary>
		public static bool DetailsPaneVisible
		{
			get { return Properties.Settings.Default.DetailsPaneVisible; }
			set
			{
				object old = Properties.Settings.Default.DetailsPaneVisible;
				Properties.Settings.Default.DetailsPaneVisible = value;
				DispatchPropertyChanged("DetailsPaneVisible", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether the menu bar is visible
		/// </summary>
		public static bool MenuBarVisible
		{
			get { return Properties.Settings.Default.MenuBarVisible; }
			set
			{
				object old = Properties.Settings.Default.MenuBarVisible;
				Properties.Settings.Default.MenuBarVisible = value;
				DispatchPropertyChanged("MenuBarVisible", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the language of the application
		/// </summary>
		public static string Language
		{
			get
			{
				string l = Properties.Settings.Default.Language;
				if (l != null) return l;
				return Thread.CurrentThread.CurrentUICulture.IetfLanguageTag;
			}
			set
			{
				object old = Properties.Settings.Default.Language;
				Properties.Settings.Default.Language = value;
				DispatchPropertyChanged("Language", old, value);
			}
		}

		#endregion

        #region Lists

        /// <summary>
        /// Gets or sets the configuration of the source list
        /// </summary>
        public static ViewDetailsConfig SourceListConfig
        {
            get { return Properties.Settings.Default.SourceListConfig; }
            set
            {
                object old = Properties.Settings.Default.SourceListConfig;
                Properties.Settings.Default.SourceListConfig = value;
                DispatchPropertyChanged("SourceListConfig", old, value);
            }
        }

        /// <summary>
        /// Gets or sets the sources where Stoffi looks for music
        /// </summary>
        public static ObservableCollection<SourceData> Sources
        {
            get { return Properties.Settings.Default.Sources; }
            set
            {
                object old = Properties.Settings.Default.Sources;
                Properties.Settings.Default.Sources = value;
                DispatchPropertyChanged("Sources", old, value);
            }
        }

        /// <summary>
        /// Gets or sets the configuration of the plugins list
        /// </summary>
        public static ViewDetailsConfig PluginListConfig
        {
            get { return Properties.Settings.Default.PluginListConfig; }
            set
            {
                object old = Properties.Settings.Default.PluginListConfig;
                Properties.Settings.Default.PluginListConfig = value;
                DispatchPropertyChanged("PluginListConfig", old, value);
            }
        }

        /// <summary>
        /// Gets or sets the plugins
        /// </summary>
        public static ObservableCollection<PluginItem> Plugins
        {
            get { return Properties.Settings.Default.Plugins; }
            set
            {
                object old = Properties.Settings.Default.Plugins;
                Properties.Settings.Default.Plugins = value;
                DispatchPropertyChanged("Plugins", old, value);
            }
        }

		/// <summary>
		/// Gets or sets the configuration of the history list
		/// </summary>
		public static ViewDetailsConfig HistoryListConfig
		{
			get { return Properties.Settings.Default.HistoryListConfig; }
			set
			{
				object old = Properties.Settings.Default.HistoryListConfig;
				Properties.Settings.Default.HistoryListConfig = value;
				DispatchPropertyChanged("HistoryListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the history tracks
		/// </summary>
		public static ObservableCollection<TrackData> HistoryTracks
		{
			get { return Properties.Settings.Default.HistoryTracks; }
			set
			{
				object old = Properties.Settings.Default.HistoryTracks;
				Properties.Settings.Default.HistoryTracks = value;
				DispatchPropertyChanged("HistoryTracks", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the file list
		/// </summary>
		public static ViewDetailsConfig FileListConfig
		{
			get { return Properties.Settings.Default.FileListConfig; }
			set
			{
				object old = Properties.Settings.Default.FileListConfig;
				Properties.Settings.Default.FileListConfig = value;
				DispatchPropertyChanged("FileListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the file tracks
		/// </summary>
		public static ObservableCollection<TrackData> FileTracks
		{
			get { return Properties.Settings.Default.FileTracks; }
			set
			{
				object old = Properties.Settings.Default.FileTracks;
				Properties.Settings.Default.FileTracks = value;
				DispatchPropertyChanged("FileTracks", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the radio list
		/// </summary>
		public static ViewDetailsConfig RadioListConfig
		{
			get { return Properties.Settings.Default.RadioListConfig; }
			set
			{
				object old = Properties.Settings.Default.RadioListConfig;
				Properties.Settings.Default.RadioListConfig = value;
				DispatchPropertyChanged("RadioListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the radio tracks
		/// </summary>
		public static ObservableCollection<TrackData> RadioTracks
		{
			get { return Properties.Settings.Default.RadioTracks; }
			set
			{
				object old = Properties.Settings.Default.RadioTracks;
				Properties.Settings.Default.RadioTracks = value;
				DispatchPropertyChanged("RadioTracks", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the disc list
		/// </summary>
		public static ViewDetailsConfig DiscListConfig
		{
			get { return Properties.Settings.Default.DiscListConfig; }
			set
			{
				object old = Properties.Settings.Default.DiscListConfig;
				Properties.Settings.Default.DiscListConfig = value;
				DispatchPropertyChanged("DiscListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the queue list
		/// </summary>
		public static ViewDetailsConfig QueueListConfig
		{
			get { return Properties.Settings.Default.QueueListConfig; }
			set
			{
				object old = Properties.Settings.Default.QueueListConfig;
				Properties.Settings.Default.QueueListConfig = value;
				DispatchPropertyChanged("QueueListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the queue tracks
		/// </summary>
		public static ObservableCollection<TrackData> QueueTracks
		{
			get { return Properties.Settings.Default.QueueTracks; }
			set
			{
				object old = Properties.Settings.Default.QueueTracks;
				Properties.Settings.Default.QueueTracks = value;
				DispatchPropertyChanged("QueueTracks", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the YouTube list
		/// </summary>
		public static ViewDetailsConfig YouTubeListConfig
		{
			get { return Properties.Settings.Default.YouTubeListConfig; }
			set
			{
				object old = Properties.Settings.Default.YouTubeListConfig;
				Properties.Settings.Default.YouTubeListConfig = value;
				DispatchPropertyChanged("YouTubeListConfig", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the SoundCloud list
		/// </summary>
		public static ViewDetailsConfig SoundCloudListConfig
		{
			get { return Properties.Settings.Default.SoundCloudListConfig; }
			set
			{
				object old = Properties.Settings.Default.SoundCloudListConfig;
				Properties.Settings.Default.SoundCloudListConfig = value;
				DispatchPropertyChanged("SoundCloudListConfig", old, value);
			}
		}

		#endregion

		#region Application parameters

		/// <summary>
		/// Gets the architecture of the application.
		/// </summary>
		public static string Architecture
		{
			get { return Properties.Settings.Default.Architecture; }
		}

		/// <summary>
		/// Gets the channel of the application (Alpha, Beta or Stable).
		/// </summary>
		public static string Channel
		{
			get { return Properties.Settings.Default.Channel; }
		}

		/// <summary>
		/// Gets or sets the ID of the application
		/// </summary>
		public static int ID
		{
			get { return Properties.Settings.Default.ID; }
			set
			{
				// it may be null, although C# say int can't be null... :)
				object old = null;
				try
				{
					old = Properties.Settings.Default.ID;
				}
				catch { }

				Properties.Settings.Default.ID = value;
				DispatchPropertyChanged("ID", old, value);
			}
		}

		/// <summary>
		/// Gets the version of the application
		/// </summary>
		public static long Version
		{
			get { return Properties.Settings.Default.Version; }
		}

		/// <summary>
		/// Gets the release of the application
		/// </summary>
		public static string Release
		{
			get { return Properties.Settings.Default.Release; }
		}

		#endregion

		#region Settings

		/// <summary>
		/// Gets or sets how the upgrades of the application should be performed
		/// </summary>
		public static UpgradePolicy UpgradePolicy
		{
			get { return Properties.Settings.Default.UpgradePolicy; }
			set
			{
				object old = Properties.Settings.Default.UpgradePolicy;
				Properties.Settings.Default.UpgradePolicy = value;
				DispatchPropertyChanged("UpgradePolicy", old, value);
			}
		}

		/// <summary>
		/// Gets or sets how different list should share search filters
		/// </summary>
		public static SearchPolicy SearchPolicy
		{
			get { return Properties.Settings.Default.SearchPolicy; }
			set
			{
				object old = Properties.Settings.Default.SearchPolicy;
				Properties.Settings.Default.SearchPolicy = value;
				DispatchPropertyChanged("SearchPolicy", old, value);
			}
		}

		/// <summary>
		/// Gets or sets how to add a track when it's opened with the application
		/// </summary>
		public static OpenAddPolicy OpenAddPolicy
		{
			get { return Properties.Settings.Default.OpenAddPolicy; }
			set
			{
				object old = Properties.Settings.Default.OpenAddPolicy;
				Properties.Settings.Default.OpenAddPolicy = value;
				DispatchPropertyChanged("OpenAddPolicy", old, value);
			}
		}

		/// <summary>
		/// Gets or sets how to play a track when it's opened with the application
		/// </summary>
		public static OpenPlayPolicy OpenPlayPolicy
		{
			get { return Properties.Settings.Default.OpenPlayPolicy; }
			set
			{
				object old = Properties.Settings.Default.OpenPlayPolicy;
				Properties.Settings.Default.OpenPlayPolicy = value;
				DispatchPropertyChanged("OpenPlayPolicy", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether the application should stay visible in the taskbar when it's minimized
		/// </summary>
		public static bool MinimizeToTray
		{
			get { return Properties.Settings.Default.MinimizeToTray; }
			set
			{
				bool same = value == Properties.Settings.Default.MinimizeToTray;
				Properties.Settings.Default.MinimizeToTray = value;
				object newValue = (object)value;
				object oldValue = same ? newValue : (object)Properties.Settings.Default.MinimizeToTray;
				DispatchPropertyChanged("MinimizeToTray", oldValue, newValue);
			}
		}

		/// <summary>
		/// Gets or sets whether to show a notification when a new track is played
		/// </summary>
		public static bool ShowOSD
		{
			get { return Properties.Settings.Default.ShowOSD; }
			set
			{
				bool same = value == Properties.Settings.Default.ShowOSD;
				Properties.Settings.Default.ShowOSD = value;
				object newValue = (object)value;
				object oldValue = same ? newValue : (object)Properties.Settings.Default.ShowOSD;
				DispatchPropertyChanged("ShowOSD", oldValue, newValue);
			}
		}

		/// <summary>
		/// Gets or sets whether to pause playback while computer is locked
		/// </summary>
		public static bool PauseWhenLocked
		{
			get { return Properties.Settings.Default.PauseWhenLocked; }
			set
			{
				object old = Properties.Settings.Default.PauseWhenLocked;
				Properties.Settings.Default.PauseWhenLocked = value;
				DispatchPropertyChanged("PauseWhenLocked", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to pause playback when the currently playing song reaches the end.
		/// </summary>
		public static bool PauseWhenSongEnds
		{
			get { return Properties.Settings.Default.PauseWhenSongEnds; }
			set
			{
				object old = Properties.Settings.Default.PauseWhenSongEnds;
				Properties.Settings.Default.PauseWhenSongEnds = value;
				DispatchPropertyChanged("PauseWhenSongEnds", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the currently used orofile of keyboard shortcuts
		/// </summary>
		public static string CurrentShortcutProfile
		{
			get { return Properties.Settings.Default.CurrentShortcutProfile; }
			set
			{
				object old = Properties.Settings.Default.CurrentShortcutProfile;
				Properties.Settings.Default.CurrentShortcutProfile = value;
				DispatchPropertyChanged("CurrentShortcutProfile", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the keyboard shortcut profiles
		/// </summary>
		public static List<KeyboardShortcutProfile> ShortcutProfiles
		{
			get { return Properties.Settings.Default.ShortcutProfiles; }
			set
			{
				object old = Properties.Settings.Default.ShortcutProfiles;
				Properties.Settings.Default.ShortcutProfiles = value;
				DispatchPropertyChanged("ShortcutProfiles", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the settings of plugins
		/// </summary>
		public static List<PluginSettings> PluginSettings
		{
			get { return Properties.Settings.Default.PluginSettings; }
			set
			{
				object old = Properties.Settings.Default.PluginSettings;
				Properties.Settings.Default.PluginSettings = value;
				DispatchPropertyChanged("PluginSettings", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the filter to use for YouTube searching.
		/// </summary>
		public static string YouTubeFilter
		{
			get { return Properties.Settings.Default.YouTubeFilter; }
			set
			{
				object old = Properties.Settings.Default.YouTubeFilter;
				Properties.Settings.Default.YouTubeFilter = value;
				DispatchPropertyChanged("YouTubeFilter", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the desired quality of YouTube streaming.
		/// </summary>
		public static string YouTubeQuality
		{
			get { return Properties.Settings.Default.YouTubeQuality; }
			set
			{
				object old = Properties.Settings.Default.YouTubeQuality;
				Properties.Settings.Default.YouTubeQuality = value;
				DispatchPropertyChanged("YouTubeQuality", old, value);
			}
		}

		#endregion

		#region Playback

		/// <summary>
		/// Gets or sets the navigation that the currently playing track belongs to
		/// </summary>
		public static string CurrentActiveNavigation
		{
			get { return Properties.Settings.Default.CurrentActiveNavigation; }
			set
			{
				object old = Properties.Settings.Default.CurrentActiveNavigation;
				Properties.Settings.Default.CurrentActiveNavigation = value;
				DispatchPropertyChanged("CurrentActiveNavigation", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the currently playing track
		/// </summary>
		public static TrackData CurrentTrack
		{
			get { return Properties.Settings.Default.CurrentTrack; }
			set
			{
				object old = Properties.Settings.Default.CurrentTrack;
				Properties.Settings.Default.CurrentTrack = value;
				DispatchPropertyChanged("CurrentTrack", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the current equalizer profile
		/// </summary>
		public static EqualizerProfile CurrentEqualizerProfile
		{
			get { return Properties.Settings.Default.CurrentEqualizerProfile; }
			set
			{
				object old = Properties.Settings.Default.CurrentEqualizerProfile;
				Properties.Settings.Default.CurrentEqualizerProfile = value;
				DispatchPropertyChanged("CurrentEqualizerProfile", old, value);
			}
		}

		/// <summary>
		/// Gets or sets equalizer levels
		/// </summary>
		public static List<EqualizerProfile> EqualizerProfiles
		{
			get { return Properties.Settings.Default.EqualizerProfiles; }
			set
			{
				object old = Properties.Settings.Default.EqualizerProfiles;
				Properties.Settings.Default.EqualizerProfiles = value;
				DispatchPropertyChanged("EqualizerProfiles", old, value);
			}
		}

		/// <summary>
		/// Gets or sets where in history the current playback is
		/// </summary>
		public static int HistoryIndex
		{
			get { return Properties.Settings.Default.HistoryIndex; }
			set
			{
				object old = Properties.Settings.Default.HistoryIndex;
				Properties.Settings.Default.HistoryIndex = value;
				DispatchPropertyChanged("HistoryIndex", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether to use shuffle when selecting the next track
		/// </summary>
		public static bool Shuffle
		{
			get { return Properties.Settings.Default.Shuffle; }
			set
			{
				bool same = value == Properties.Settings.Default.Shuffle;
				Properties.Settings.Default.Shuffle = value;
				object newValue = (object)value;
				object oldValue = same ? newValue : (object)Properties.Settings.Default.Shuffle;
				DispatchPropertyChanged("Shuffle", oldValue, newValue);
			}
		}

		/// <summary>
		/// Gets or sets whether to repeat the tracks or not
		/// </summary>
		public static RepeatState Repeat
		{
			get { return Properties.Settings.Default.Repeat; }
			set
			{
				object old = Properties.Settings.Default.Repeat;
				Properties.Settings.Default.Repeat = value;
				DispatchPropertyChanged("Repeat", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the volume in percent.
		/// </summary>
		public static double Volume
		{
			get { return Properties.Settings.Default.Volume; }
			set
			{
				object old = Properties.Settings.Default.Volume;
				Properties.Settings.Default.Volume = value;
				DispatchPropertyChanged("Volume", old, value);
			}
		}

		/// <summary>
		/// Gets or sets current position of the currently playing
		/// track as a value between 0 and 10
		/// </summary>
		public static double Seek
		{
			get { return Properties.Settings.Default.Seek; }
			set
			{
				object old = Properties.Settings.Default.Seek;
				Properties.Settings.Default.Seek = value;
				DispatchPropertyChanged("Seek", old, value);
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
				DispatchPropertyChanged("BufferSize", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the state of the media player
		/// </summary>
		public static MediaState MediaState
		{
			get { return Properties.Settings.Default.MediaState; }
			set
			{
				if (Properties.Settings.Default.MediaState != value)
				{
					object old = Properties.Settings.Default.MediaState;
					Properties.Settings.Default.MediaState = value;
					DispatchPropertyChanged("MediaState", old, value);
				}
			}
		}

		#endregion

		#region Cloud

		/// <summary>
		/// Gets or sets the Synchronization to an account
		/// </summary>
		public static List<CloudIdentity> CloudIdentities
		{
			get
			{
				if (Properties.Settings.Default.CloudIdentities == null)
					Properties.Settings.Default.CloudIdentities = new List<CloudIdentity>();
				return Properties.Settings.Default.CloudIdentities;
			}
			set
			{
				object old = Properties.Settings.Default.CloudIdentities;
				Properties.Settings.Default.CloudIdentities = value;
				DispatchPropertyChanged("CloudIdentities", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the whether or not tracks should be submitted to the cloud.
		/// </summary>
		public static bool SubmitSongs
		{
			get { return Properties.Settings.Default.SubmitSongs; }
			set
			{
				object old = Properties.Settings.Default.SubmitSongs;
				Properties.Settings.Default.SubmitSongs = value;
				DispatchPropertyChanged("SubmitSongs", old, value);
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
			get { return Properties.Settings.Default.ListenBuffer; }
			set
			{
				object old = Properties.Settings.Default.ListenBuffer;
				Properties.Settings.Default.ListenBuffer = value;
				DispatchPropertyChanged("ListenBuffer", old, value);
			}
		}
		
		#endregion

		#region Misc

		/// <summary>
		/// Gets or sets whether the application has never been run before
		/// </summary>
		public static bool FirstRun
		{
			get { return Properties.Settings.Default.FirstRun; }
			set
			{
				object old = Properties.Settings.Default.FirstRun;
				Properties.Settings.Default.FirstRun = value;
				DispatchPropertyChanged("FirstRun", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the last time the application checked for an upgrade
		/// </summary>
		public static long UpgradeCheck
		{
			get { return Properties.Settings.Default.UpgradeCheck; }
			set
			{
				object old = Properties.Settings.Default.UpgradeCheck;
				Properties.Settings.Default.UpgradeCheck = value;
				DispatchPropertyChanged("UpgradeCheck", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the playlists
		/// </summary>
		public static List<PlaylistData> Playlists
		{
			get { return Properties.Settings.Default.Playlists; }
			set
			{
				object old = Properties.Settings.Default.Playlists;
				Properties.Settings.Default.Playlists = value;
				DispatchPropertyChanged("Playlists", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the OAuth secret
		/// </summary>
		public static string OAuthSecret
		{
			get { return Properties.Settings.Default.OAuthSecret; }
			set
			{
				object old = Properties.Settings.Default.OAuthSecret;
				Properties.Settings.Default.OAuthSecret = value;
				DispatchPropertyChanged("OAuthSecret", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the OAuth token
		/// </summary>
		public static string OAuthToken
		{
			get { return Properties.Settings.Default.OAuthToken; }
			set
			{
				object old = Properties.Settings.Default.OAuthToken;
				Properties.Settings.Default.OAuthToken = value;
				DispatchPropertyChanged("OAuthToken", old, value);
			}
		}

		/// <summary>
		/// Gets or sets the navigation that the currently playing track belongs to
		/// </summary>
		public static string CurrentVisualizer
		{
			get { return Properties.Settings.Default.CurrentVisualizer; }
			set
			{
				object old = Properties.Settings.Default.CurrentVisualizer;
				Properties.Settings.Default.CurrentVisualizer = value;
				DispatchPropertyChanged("CurrentVisualizer", old, value);
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
			#region Lists and collections

			if (FileTracks == null)
				FileTracks = new ObservableCollection<TrackData>();

			if (HistoryTracks == null)
				HistoryTracks = new ObservableCollection<TrackData>();

			if (QueueTracks == null)
				QueueTracks = new ObservableCollection<TrackData>();

			if (RadioTracks == null)
				RadioTracks = new ObservableCollection<TrackData>();

			if (Plugins == null)
				Plugins = new ObservableCollection<PluginItem>();

			if (PluginSettings == null)
				PluginSettings = new List<PluginSettings>();

			if (Sources == null)
				Sources = new ObservableCollection<SourceData>();

			if (Playlists == null)
				Playlists = new List<PlaylistData>();

			#endregion

			#region Equalizer

			if (EqualizerProfiles == null)
			{
				EqualizerProfiles = new List<EqualizerProfile>();
				InitializeEqualizerProfiles(EqualizerProfiles);
				CurrentEqualizerProfile = EqualizerProfiles[0];
			}

			#endregion

			#region Shortcuts

			if (ShortcutProfiles == null)
			{
				ShortcutProfiles = new List<KeyboardShortcutProfile>();
				InitializeShortcutProfiles(ShortcutProfiles);
			}

			#endregion

			#region ViewDetails lists

			// sources list
			if (SourceListConfig == null)
			{
				ViewDetailsConfig sourceConfig = CreateListConfig();
				sourceConfig.Columns.Add(CreateListColumn("Data", U.T("ColumnLocation"), 200));
				sourceConfig.Columns.Add(CreateListColumn("Type", U.T("ColumnType"), 150, "SourceType"));
				sourceConfig.Sorts.Add("asc:Type");
				sourceConfig.Sorts.Add("asc:Data");
				SourceListConfig = sourceConfig;
			}

			// youtube list
			if (YouTubeListConfig == null)
			{
				ViewDetailsConfig youtubeConfig = CreateListConfig();
				youtubeConfig.AcceptFileDrops = false;
				youtubeConfig.IsDragSortable = false;
				youtubeConfig.Columns.Add(CreateListColumn("Artist", U.T("ColumnArtist"), 200));
				youtubeConfig.Columns.Add(CreateListColumn("Title", U.T("ColumnTitle"), 350));
				youtubeConfig.Columns.Add(CreateListColumn("Views", U.T("ColumnViews"), 120, "Number", Alignment.Right));
				youtubeConfig.Columns.Add(CreateListColumn("Length", U.T("ColumnLength"), 100, "Duration", Alignment.Right));
				youtubeConfig.Columns.Add(CreateListColumn("Path", U.T("ColumnPath"), 200, Alignment.Left, false));
				YouTubeListConfig = youtubeConfig;
			}

			// soundcloud list
			if (SoundCloudListConfig == null)
			{
				ViewDetailsConfig soundcloudConfig = CreateListConfig();
				soundcloudConfig.AcceptFileDrops = false;
				soundcloudConfig.IsDragSortable = false;
				soundcloudConfig.Columns.Add(CreateListColumn("Artist", U.T("ColumnArtist"), 150));
				soundcloudConfig.Columns.Add(CreateListColumn("Title", U.T("ColumnTitle"), 300));
				soundcloudConfig.Columns.Add(CreateListColumn("Genre", U.T("ColumnGenre"), 100));
				soundcloudConfig.Columns.Add(CreateListColumn("Length", U.T("ColumnLength"), 150, "Duration", Alignment.Right));
				soundcloudConfig.Columns.Add(CreateListColumn("Path", U.T("ColumnPath"), 200, Alignment.Left, false));
				SoundCloudListConfig = soundcloudConfig;
			}

			// file list
			if (FileListConfig == null)
			{
				ViewDetailsConfig fileList = CreateListConfig();
				InitViewDetailsConfig(fileList);
				fileList.Sorts.Add("asc:Title");
				fileList.Sorts.Add("asc:Track");
				fileList.Sorts.Add("asc:Album");
				fileList.Sorts.Add("asc:Artist");
				FileListConfig = fileList;
			}

			// radio list
			if (RadioListConfig == null)
			{
				ViewDetailsConfig radioList = CreateListConfig();
				radioList.Columns.Add(CreateListColumn("Title", U.T("ColumnTitle"), 300));
				radioList.Columns.Add(CreateListColumn("Genre", U.T("ColumnGenre"), 150));
				radioList.Columns.Add(CreateListColumn("URL", U.T("ColumnURL"), 300));
				radioList.Sorts.Add("asc:Title");
				RadioListConfig = radioList;
			}

			// disc list
			if (DiscListConfig == null)
			{
				ViewDetailsConfig discList = CreateListConfig();
				InitViewDetailsConfig(discList);
				DiscListConfig = discList;
			}

			// queue list
			if (QueueListConfig == null)
			{
				ViewDetailsConfig queueList = CreateListConfig();
				queueList.IsNumberVisible = true;
				InitViewDetailsConfig(queueList);
				queueList.Sorts.Add("asc:Title");
				queueList.Sorts.Add("asc:Track");
				queueList.Sorts.Add("asc:Album");
				queueList.Sorts.Add("asc:Artist");
				QueueListConfig = queueList;
			}

			// history list
			if (HistoryListConfig == null)
			{
				ViewDetailsConfig historyList = CreateListConfig();
				historyList.Columns.Add(CreateListColumn("LastPlayed", U.T("ColumnPlayed"), 200, "DateTime"));
				historyList.Columns.Add(CreateListColumn("Artist", U.T("ColumnArtist"), 100));
				historyList.Columns.Add(CreateListColumn("Album", U.T("ColumnAlbum"), 100));
				historyList.Columns.Add(CreateListColumn("Title", U.T("ColumnTitle"), 100));
				historyList.Columns.Add(CreateListColumn("Genre", U.T("ColumnGenre"), 100));
				historyList.Columns.Add(CreateListColumn("Length", U.T("ColumnLength"), 60, "Duration", Alignment.Right));
				historyList.Columns.Add(CreateListColumn("Year", U.T("ColumnYear"), 100, Alignment.Right, false));
				historyList.Columns.Add(CreateListColumn("PlayCount", U.T("ColumnPlayCount"), 100, "Number", Alignment.Right, false));
				historyList.Columns.Add(CreateListColumn("Path", U.T("ColumnPath"), 200, Alignment.Left, false));
				historyList.Columns.Add(CreateListColumn("Track", U.T("ColumnTrack"), 100, Alignment.Right, false));
				historyList.Sorts.Add("dsc:LastPlayed");
				HistoryListConfig = historyList;
			}

			if (PluginListConfig == null)
			{
				ViewDetailsConfig pluginListConfig = CreateListConfig();
				pluginListConfig.AcceptFileDrops = false;
				pluginListConfig.IsDragSortable = false;
				pluginListConfig.Columns.Add(CreateListColumn("Name", U.T("ColumnName"), 150));
				pluginListConfig.Columns.Add(CreateListColumn("Author", U.T("ColumnAuthor"), 100));
				pluginListConfig.Columns.Add(CreateListColumn("Type", U.T("ColumnType"), 100, "PluginType"));
				pluginListConfig.Columns.Add(CreateListColumn("Installed", U.T("ColumnInstalled"), 150, "DateTime"));
				pluginListConfig.Columns.Add(CreateListColumn("Version", U.T("ColumnVersion"), 80));
				//pluginListConfig.Columns.Add(CreateListColumn("Enabled", U.T("ColumnEnabled"), "Enabled", 20));
				pluginListConfig.Sorts.Add("asc:Name");
				PluginListConfig = pluginListConfig;
			}

			#endregion

			#region Adjustments

			if (ListenBuffer == null)
				ListenBuffer = new Dictionary<string, Tuple<string, string>>();
			
			//Save();
			//foreach (TrackData t in SettingsManager.HistoryTracks)
			//{
			//    if (t.Path.StartsWith("youtube://"))
			//        t.Path = "stoffi:track:youtube:" + t.Path.Substring(10);
			//    else if (t.Path.StartsWith("soundcloud://"))
			//        t.Path = "stoffi:track:soundcloud:" + t.Path.Substring(13);
			//}
			//foreach (TrackData t in SettingsManager.QueueTracks)
			//{
			//    if (t.Path.StartsWith("youtube://"))
			//        t.Path = "stoffi:track:youtube:" + t.Path.Substring(10);
			//    else if (t.Path.StartsWith("soundcloud://"))
			//        t.Path = "stoffi:track:soundcloud:" + t.Path.Substring(13);
			//}

			//foreach (PlaylistData p in SettingsManager.Playlists)
			//    foreach (TrackData t in p.Tracks)
			//    {
			//        if (t.Path.StartsWith("youtube://"))
			//            t.Path = "stoffi:track:youtube:" + t.Path.Substring(10);
			//        else if (t.Path.StartsWith("soundcloud://"))
			//            t.Path = "stoffi:track:soundcloud:" + t.Path.Substring(13);
			//    }

			//foreach (KeyboardShortcutProfile p in ShortcutProfiles)
			//    foreach (KeyboardShortcut s in p.Shortcuts)
			//        s.Name = s.Name.Replace("Plugin", "App").Replace("plugin", "app");

			//FixViewDetailsConfig(SourceListConfig);
			//FixViewDetailsConfig(FileListConfig);
			//FixViewDetailsConfig(QueueListConfig);
			//FixViewDetailsConfig(HistoryListConfig);
			//FixViewDetailsConfig(YouTubeListConfig);
			//foreach (PlaylistData p in Playlists)
			//    FixViewDetailsConfig(p.ListConfig);

			#endregion

			DispatchInitialized();
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

		//public static void FixViewDetailsConfig(ViewDetailsConfig vdc)
		//{
		//    if (vdc.Columns.Count == 2 && vdc.Sorts.Count == 0)
		//    {
		//        vdc.Sorts.Add("asc:Type");
		//        vdc.Sorts.Add("asc:Data");
		//    }
		//    if (vdc.Columns.Count == 10 && vdc.Sorts.Count == 0)
		//    {
		//        vdc.Sorts.Add("asc:Title");
		//        vdc.Sorts.Add("asc:Track");
		//        vdc.Sorts.Add("asc:Album");
		//        vdc.Sorts.Add("asc:Artist");
		//    }

		//    foreach (ViewDetailsColumn column in vdc.Columns)
		//    {
		//        switch (column.Name)
		//        {
		//            case "Length":
		//                column.SortField = "Length";
		//                column.Converter = "Duration";
		//                break;

		//            case "LastPlayed":
		//                column.SortField = "LastPlayed";
		//                column.Converter = "DateTime";
		//                break;

		//            case "Views":
		//                column.SortField = "Views";
		//                column.Converter = "Number";
		//                break;

		//            case "PlayCount":
		//                column.Converter = "Number";
		//                break;

		//            case "Type":
		//                column.Converter = "SourceType";
		//                break;
		//        }
		//    }
		//}

		/// <summary>
		/// Saves the settings
		/// </summary>
		public static void Save()
		{
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Export the current configuration to an xml file
		/// </summary>
		/// <param name="filename">The path to the file</param>
		public static void Export(string filename)
		{
			Config c = new Config();

			// tracks
			c.Collection = ExportTracks(SettingsManager.FileTracks);
			c.History = ExportTracks(SettingsManager.HistoryTracks);
			c.Queue = ExportTracks(SettingsManager.QueueTracks);
			c.Playlists = new List<ConfigPlaylist>();
			foreach (PlaylistData p in SettingsManager.Playlists)
			{
				ConfigPlaylist cp = new ConfigPlaylist();
				cp.Name = p.Name;
				cp.Tracks = ExportTracks(p.Tracks);
				cp.ListConfig = p.ListConfig;
				c.Playlists.Add(cp);
			}

			c.CurrentActiveNavigation = SettingsManager.CurrentActiveNavigation;
			c.CurrentSelectedNavigation = SettingsManager.CurrentSelectedNavigation;
			c.CurrentShortcutProfile = SettingsManager.CurrentShortcutProfile;
			c.CurrentTrack = ExportTrack(SettingsManager.CurrentTrack);
			c.DetailsPaneVisible = SettingsManager.DetailsPaneVisible;
			c.FileListConfig = SettingsManager.FileListConfig;
			c.HistoryIndex = SettingsManager.HistoryIndex;
			c.HistoryListConfig = SettingsManager.HistoryListConfig;
			c.Language = SettingsManager.Language;
			c.MenuBarVisible = SettingsManager.MenuBarVisible;
			c.MinimizeToTray = SettingsManager.MinimizeToTray;
			c.OpenAddPolicy = SettingsManager.OpenAddPolicy;
			c.OpenPlayPolicy = SettingsManager.OpenPlayPolicy;
			c.QueueListConfig = SettingsManager.QueueListConfig;
			c.Repeat = SettingsManager.Repeat;
			c.SearchPolicy = SettingsManager.SearchPolicy;
			c.Seek = SettingsManager.Seek;
			c.Volume = SettingsManager.Volume;
			c.ShortcutProfiles = SettingsManager.ShortcutProfiles;
			c.ShowOSD = SettingsManager.ShowOSD;
			c.Shuffle = SettingsManager.Shuffle;
			c.SourceListConfig = SettingsManager.SourceListConfig;
			c.UpgradePolicy = SettingsManager.UpgradePolicy;
			c.YouTubeListConfig = SettingsManager.YouTubeListConfig;

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
			SettingsManager.FileTracks.Clear();
			SettingsManager.HistoryTracks.Clear();
			SettingsManager.QueueTracks.Clear();
			ImportTracks(c.Collection, SettingsManager.FileTracks, false);
			ImportTracks(c.History, SettingsManager.HistoryTracks);
			ImportTracks(c.Queue, SettingsManager.QueueTracks);
			SettingsManager.Playlists.Clear();
			foreach (ConfigPlaylist p in c.Playlists)
			{
				PlaylistData pl = PlaylistManager.FindPlaylist(p.Name);
				if (pl == null)
					pl = PlaylistManager.CreatePlaylist(p.Name);
				ImportTracks(p.Tracks, pl.Tracks);
				pl.ListConfig = p.ListConfig;
			}

			SettingsManager.CurrentActiveNavigation = c.CurrentActiveNavigation;
			SettingsManager.CurrentSelectedNavigation = c.CurrentSelectedNavigation;
			SettingsManager.CurrentShortcutProfile = c.CurrentShortcutProfile;
			SettingsManager.CurrentTrack = ImportTrack(c.CurrentTrack);
			SettingsManager.DetailsPaneVisible = c.DetailsPaneVisible;
			SettingsManager.FileListConfig = c.FileListConfig;
			SettingsManager.HistoryIndex = c.HistoryIndex;
			SettingsManager.HistoryListConfig = c.HistoryListConfig;
			SettingsManager.Language = c.Language;
			SettingsManager.MenuBarVisible = c.MenuBarVisible;
			SettingsManager.MinimizeToTray = c.MinimizeToTray;
			SettingsManager.OpenAddPolicy = c.OpenAddPolicy;
			SettingsManager.OpenPlayPolicy = c.OpenPlayPolicy;
			SettingsManager.QueueListConfig = c.QueueListConfig;
			SettingsManager.Repeat = c.Repeat;
			SettingsManager.SearchPolicy = c.SearchPolicy;
			SettingsManager.Seek = c.Seek;
			SettingsManager.Volume = c.Volume;
			SettingsManager.ShortcutProfiles = c.ShortcutProfiles;
			SettingsManager.ShowOSD = c.ShowOSD;
			SettingsManager.Shuffle = c.Shuffle;
			SettingsManager.SourceListConfig = c.SourceListConfig;
			SettingsManager.UpgradePolicy = c.UpgradePolicy;
			SettingsManager.YouTubeListConfig = c.YouTubeListConfig;
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
			return EqualizerProfiles[0];
		}

		/// <summary>
		/// Gets the currently used shortcut profile.
		/// </summary>
		/// <returns>The shortcut profile that is currently used if such exists, otherwise null</returns>
		public static KeyboardShortcutProfile GetCurrentShortcutProfile()
		{
			try
			{
				foreach (KeyboardShortcutProfile p in ShortcutProfiles)
					if (p.Name == CurrentShortcutProfile)
						return p;
				return ShortcutProfiles[0];
			}
			catch { return null; }
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
		/// Initializes a configuration of a ViewDetails list.
		/// </summary>
		/// <param name="config">The configuration to initialize</param>
		public static void InitViewDetailsConfig(ViewDetailsConfig config)
		{
			config.Columns.Add(CreateListColumn("Artist", U.T("ColumnArtist"), 180));
			config.Columns.Add(CreateListColumn("Album", U.T("ColumnAlbum"), 160));
			config.Columns.Add(CreateListColumn("Title", U.T("ColumnTitle"), 220));
			config.Columns.Add(CreateListColumn("Genre", U.T("ColumnGenre"), 90));
			config.Columns.Add(CreateListColumn("Length", U.T("ColumnLength"), 70, "Duration", Alignment.Right));
			config.Columns.Add(CreateListColumn("Year", U.T("ColumnYear"), 100, Alignment.Right, false));
			config.Columns.Add(CreateListColumn("LastPlayed", U.T("ColumnLastPlayed"), 150, "DateTime", Alignment.Left, false));
			config.Columns.Add(CreateListColumn("PlayCount", U.T("ColumnPlayCount"), 80, "Number", Alignment.Right));
			config.Columns.Add(CreateListColumn("Track", U.T("ColumnTrack"), "Track", 100, Alignment.Right, false));
			config.Columns.Add(CreateListColumn("Path", U.T("ColumnPath"), "Path", 300, Alignment.Left, false));
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
			Stoffi.ViewDetailsConfig config = new Stoffi.ViewDetailsConfig();
			config.HasNumber = true;
			config.IsNumberVisible = false;
			config.Filter = "";
			config.IsClickSortable = true;
			config.IsDragSortable = true;
			config.LockSortOnNumber = false;
			config.SelectedIndices = new List<int>();
			config.UseIcons = true;
			config.AcceptFileDrops = true;
			config.Columns = new ObservableCollection<Stoffi.ViewDetailsColumn>();
			config.NumberColumn = CreateListColumn("#", "#", "Number", "Number", 60, Alignment.Right, false);
			config.Sorts = new List<string>();
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

		/// <summary>
		/// Looks for the cloud identity with a given user ID.
		/// </summary>
		/// <param name="userID">The user ID to look for.</param>
		/// <returns>The corresponding cloud identity or null if not found.</returns>
		public static CloudIdentity GetCloudIdentity(uint userID)
		{
			foreach (CloudIdentity i in CloudIdentities)
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
		/// Copies a list of ConfigTrack to an observable collection of TrackData
		/// </summary>
		/// <param name="tracks">The tracks to be copied</param>
		/// <returns>The tracks as an observable collection of TrackData</returns>
		private static ObservableCollection<TrackData> ImportTracks(List<ConfigTrack> tracks)
		{
			ObservableCollection<TrackData> c = new ObservableCollection<TrackData>();
			foreach (ConfigTrack t in tracks)
			{
				TrackData track = ImportTrack(t);
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
		private static void ImportTracks(List<ConfigTrack> source, ObservableCollection<TrackData> destination, bool addToList = true)
		{
			foreach (ConfigTrack t in source)
			{
				TrackData dest = null;
				foreach (TrackData track in destination)
				{
					if (track.Path == t.Path)
					{
						dest = track;
						break;
					}
				}
				if (dest == null)
				{
					TrackData track = ImportTrack(t, addToList);
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
		/// Copies a ConfigTrack into a TrackData
		/// </summary>
		/// <param name="track">The track to be copied</param>
		/// <param name="addManually">Whether or not the track has to be added manually (this may be done elsewhere)</param>
		/// <returns>The track as a TrackData</returns>
		private static TrackData ImportTrack(ConfigTrack track, bool addManually = true)
		{
			if (track == null) return null;
			if (YouTubeManager.IsYouTube(track.Path))
			{
				return YouTubeManager.CreateTrack(track.Path);
			}
			else if (File.Exists(track.Path))
			{
				TrackData t = FilesystemManager.CreateTrack(track.Path, !addManually);
				if (addManually)
					FilesystemManager.UpdateTrack(t);
				t.LastPlayed = track.LastPlayed;
				t.PlayCount = track.PlayCount;
				return t;
			}
			return null;
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
		public static void DispatchPropertyChanged(string name, object oldValue, object newValue)
		{
			//Save();
			if (PropertyChanged != null && oldValue != newValue)
			{
				PropertyChanged(null, new PropertyChangedWithValuesEventArgs(name, oldValue, newValue));
			}
		}

		/// <summary>
		/// Trigger the Initialized event
		/// </summary>
		public static void DispatchInitialized()
		{
			U.L(LogLevel.Debug, "SETTINGS", "Dispatching initialized");
			IsInitialized = true;
			if (Initialized != null)
				Initialized(null, new EventArgs());
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

		private string _artist;
		private string _album;
		private string _title;
		private string _genre;
		private string _path;
		private uint _track;
		private uint _year;
		private double _length;
		private uint _count;
		private DateTime _last_played;
		private string _url;
		private int _views;
		private string artURL;

		/// <summary>
		/// The difference in time when RawLength is changed
		/// </summary>
		public int diff = 0;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the artist of the track.
		/// </summary>
		public string Artist
		{
			get { return _artist; }
			set { _artist = value; OnPropertyChanged("Artist"); }
		}

		/// <summary>
		/// Gets or sets the title of the track.
		/// </summary>
		public string Title
		{
			get { return _title; }
			set { _title = value; OnPropertyChanged("Title"); }
		}

		/// <summary>
		/// Gets or sets the album of the track.
		/// </summary>
		public string Album
		{
			get { return _album; }
			set { _album = value; OnPropertyChanged("Album"); }
		}

		/// <summary>
		/// Gets or sets the genre of the track.
		/// </summary>
		public string Genre
		{
			get { return _genre; }
			set { _genre = value; OnPropertyChanged("Genre"); }
		}

		/// <summary>
		/// Gets or sets the number of the track on the album.
		/// </summary>
		public uint Track
		{
			get { return _track; }
			set { _track = value; OnPropertyChanged("Track"); }
		}

		/// <summary>
		/// Gets or sets the year the track was made.
		/// </summary>
		public uint Year
		{
			get { return _year; }
			set { _year = value; OnPropertyChanged("Year"); }
		}

		/// <summary>
		/// Gets or sets the length of the track in seconds.
		/// </summary>
		public double Length
		{
			get { return _length; }
			set { diff = (int)(value - _length); _length = value; OnPropertyChanged("Length"); diff = 0; }
		}

		/// <summary>
		/// Gets or sets the path to the track.
		/// </summary>
		public string Path
		{
			get { return _path; }
			set { _path = value; OnPropertyChanged("Path"); }
		}

		/// <summary>
		/// Gets or sets the number of times that the track has been played.
		/// </summary>
		public uint PlayCount
		{
			get { return _count; }
			set { _count = value; OnPropertyChanged("PlayCount"); }
		}

		/// <summary>
		/// Gets or sets the URL of the radio track.
		/// </summary>
		public string URL
		{
			get { return _url; }
			set { _url = value; OnPropertyChanged("URL"); }
		}

		/// <summary>
		/// Gets or sets the amount of views on YouTube.
		/// </summary>
		public int Views
		{
			get { return _views; }
			set { _views = value; OnPropertyChanged("Views"); }
		}

		/// <summary>
		/// Gets or sets the time the track was last played (in epoch time).
		/// </summary>
		public DateTime LastPlayed
		{
			get { return _last_played; }
			set { _last_played = value; OnPropertyChanged("LastPlayed"); }
		}

		/// <summary>
		/// Gets or sets the URL to the album art.
		/// </summary>
		public string ArtURL
		{
			get { return artURL; }
			set { artURL = value; OnPropertyChanged("ArtURL"); }
		}

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