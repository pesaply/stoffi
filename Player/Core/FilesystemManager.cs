/**
 * FilesystemManager.cs
 * 
 * Handles all interaction with the filesystem such as scanning,
 * writing and reading the disk as well as watching for changes.
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

#if (!__MonoCS__)
#define Windows
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Tags;

#if (Windows)
using Microsoft.WindowsAPICodePack.Shell;
#endif

namespace Stoffi
{
	/// <summary>
	/// Represents a manager that takes care of talking to the filesystem 
	/// </summary>
	public static class FilesystemManager
	{
		#region Members

		private static List<FileSystemWatcher> janitors = new List<FileSystemWatcher>();
		private static Timer janitorLazyness = null;
		private static List<JanitorTask> janitorTasks = new List<JanitorTask>();
		private static Timer scanDelay = null;
		private static List<KeyValuePair<ScannerCallback, object>> scanDelayCallbacks = new List<KeyValuePair<ScannerCallback, object>>();
		private static Thread scanThread = null;
		private static string librariesPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Roaming\Microsoft\Windows\Libraries\";
		private static List<SourceTree> sourceForest = new List<SourceTree>();
		private static bool haltScanner = false;

		#endregion

		#region Properties

		/// <summary>
		/// Check if scanner should be stopped.
		/// </summary>
		public static bool ShouldStopScanner
		{
			get
			{
				return haltScanner || ProgramIsClosed;
			}
		}

		/// <summary>
		/// Gets or sets the indicator that the scanner will listen to in order to know if it
		/// should die gracefully. To be used when the application wants to close.
		/// </summary>
		public static bool ProgramIsClosed { get; set; }

		/// <summary>
		/// Gets whether the manager has been initialized
		/// </summary>
		public static bool IsInitialized { get; private set; }

		/// <summary>
		/// Gets the current status of the scanner
		/// </summary>
		public static bool IsScanning
		{
			get { return scanThread.IsAlive; }
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a filesystem manager
		/// </summary>
		static FilesystemManager()
		{
			IsInitialized = false;
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initializes the filesystem manager by setting up library watchers and event handlers
		/// </summary>
		public static void Initialize()
		{
			ProgramIsClosed = false;
			
			// setup event handler to check if track changes
			foreach (TrackData t in SettingsManager.FileTracks)
				if (t != null)
					t.PropertyChanged += new PropertyChangedEventHandler(Track_PropertyChanged);
			foreach (TrackData t in SettingsManager.HistoryTracks)
				if (t != null)
					t.PropertyChanged += new PropertyChangedEventHandler(Track_PropertyChanged);
			foreach (TrackData t in SettingsManager.QueueTracks)
				if (t != null)
					t.PropertyChanged += new PropertyChangedEventHandler(Track_PropertyChanged);
			foreach (PlaylistData p in SettingsManager.Playlists)
				foreach (TrackData t in p.Tracks)
					if (t != null)
						t.PropertyChanged += new PropertyChangedEventHandler(Track_PropertyChanged);

			IsInitialized = true;
		}

		/// <summary>
		/// Updates the current record of the source forest.
		/// Needed to be run whenever the source collection has been modified.
		/// </summary>
		public static void RefreshForest()
		{
			sourceForest = GetSourceForest();
		}

		/// <summary>
		/// Get a specific track
		/// </summary>
		/// <param name="filename">The filename of the track</param>
		/// <param name="length">Length of the file, used to match filename in any folder</param>
		/// <returns>The TrackData for the track</returns>
		public static TrackData GetTrack(String filename, double length = -1)
		{
			bool fullPath = filename.Contains('\\');
			foreach (TrackData t in SettingsManager.FileTracks)
			{
				if ((fullPath && t.Path == filename) ||
					(length != -1 && Path.GetFileName(t.Path) == filename &&
					Math.Round(t.Length, 2) == Math.Round(length, 2)))
					return t;
			}
			return null;
		}

		/// <summary>
		/// Writes the current track data to the file
		/// </summary>
		/// <param name="track">The track to save</param>
		public static void SaveTrack(TrackData track)
		{
			TagLib.File file = TagLib.File.Create(track.Path);
			file.Tag.Performers = track.Artist.Split(',');
			file.Tag.Album = track.Album;
			file.Tag.Title = track.Title;
			file.Tag.Year = Convert.ToUInt32(track.Year);
			file.Tag.Genres = track.Genre.Split(',');
			file.Tag.Track = Convert.ToUInt32(track.Track);

			file.Save();
			UpdateTrack(track);
		}

		/// <summary>
		/// Checks if the meta data of a track has been updated since last read.
		/// </summary>
		/// <param name="track">The track to check</param>
		/// <returns>True if the track has been written to since last read</returns>
		public static bool Updated(TrackData track)
		{
			try
			{
				FileInfo fInfo = new FileInfo(track.Path);
				return fInfo.LastWriteTimeUtc.Ticks > track.LastWrite;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Reads the metadata of a track and updates it
		/// </summary>
		/// <param name="track">The track to update</param>
		/// <param name="copy">Whether the data should be copied to queue/history/playlists</param>
		public static void UpdateTrack(TrackData track, bool copy = true)
		{
			if (!Updated(track)) return;

			try
			{
				TagLib.File file = TagLib.File.Create(track.Path, TagLib.ReadStyle.Average);

				track.Artist = U.CleanXMLString(file.Tag.JoinedPerformers);
				track.Album = U.CleanXMLString(file.Tag.Album);
				track.Title = U.CleanXMLString(file.Tag.Title);
				track.Genre = U.CleanXMLString(file.Tag.JoinedGenres);
				track.Track = file.Tag.Track;
				track.Year = file.Tag.Year;
				track.Length = file.Properties.Duration.TotalMilliseconds / 1000.0;
				track.Bitrate = file.Properties.AudioBitrate;
				track.Channels = file.Properties.AudioChannels;
				track.SampleRate = file.Properties.AudioSampleRate;
				track.Codecs = "";
				foreach (TagLib.ICodec c in file.Properties.Codecs)
					if (c != null)
						track.Codecs += c.Description + ", ";
				track.Codecs = track.Codecs.Substring(0, track.Codecs.Length - 2);
			}
			catch (Exception e)
			{
				if (track.Artist == U.T("MetaDataLoading")) track.Artist = "";
				if (track.Album == U.T("MetaDataLoading")) track.Album = "";
				if (track.Title == U.T("MetaDataLoading")) track.Title = "";
				if (track.Genre == U.T("MetaDataLoading")) track.Genre = "";
				U.L(LogLevel.Warning, "FILESYSTEM", "Could not read ID3 data of file: " + track.Path);
				U.L(LogLevel.Warning, "FILESYSTEM", e.Message);
			}

			try
			{
				FileInfo fInfo = new FileInfo(track.Path);
				track.LastWrite = fInfo.LastWriteTimeUtc.Ticks;
				if (String.IsNullOrWhiteSpace(track.Title))
				{
					track.Title = Path.GetFileNameWithoutExtension(track.Path);
					track.Title = track.Title.Trim();
					string s = Regex.Replace(track.Title, @"^(\d+\s)+", "");
					if (!String.IsNullOrWhiteSpace(s))
						track.Title = s;
					track.Title = U.PrettifyTag(track.Title);
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "FILESYSTEM", "Could not access file: " + track.Path);
				U.L(LogLevel.Warning, "FILESYSTEM", e.Message);
			}

			track.Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/FileAudio.ico";
			track.Processed = true;

			if (!copy) return;

			for (int i = 0; i < SettingsManager.FileTracks.Count; i++)
			{
				if (SettingsManager.FileTracks[i].Path == track.Path)
				{
					CopyTrackInfo(track, SettingsManager.FileTracks[i]);
					break;
				}
			}
			for (int i = 0; i < SettingsManager.QueueTracks.Count; i++)
			{
				if (SettingsManager.QueueTracks[i].Path == track.Path)
				{
					CopyTrackInfo(track, SettingsManager.QueueTracks[i]);
					break;
				}
			}
			for (int i = 0; i < SettingsManager.HistoryTracks.Count; i++)
			{
				if (SettingsManager.HistoryTracks[i].Path == track.Path)
				{
					CopyTrackInfo(track, SettingsManager.HistoryTracks[i]);
					break;
				}
			}
			foreach (PlaylistData p in SettingsManager.Playlists)
				for (int i = 0; i < p.Tracks.Count; i++)
				{
					if (p.Tracks[i].Path == track.Path)
					{
						CopyTrackInfo(track, p.Tracks[i]);
						break;
					}
				}
		}

		/// <summary>
		/// Toggle a source between Include and Ignore
		/// </summary>
		/// <param name="source">The source to toggle</param>
		public static void ToggleSource(SourceData source)
		{
			source.Include = !source.Include;
			ScanSources();
		}

		/// <summary>
		/// Add a source to the collection
		/// </summary>
		/// <param name="path">The path of source to be added</param>
		/// <param name="callback">A function that will be sent along with the SourceModified event</param>
		/// <param name="callbackParams">The parameters for the callback function</param>
		public static void AddSource(string path, ScannerCallback callback = null, object callbackParams = null)
		{
			SourceData s = null;
			if (File.Exists(path))
			{
				s = new SourceData()
				{
					Data = path,
					Type = SourceType.File,
					Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/FileAudio.ico",
					Include = true
				};
			}
			else if (Directory.Exists(path))
			{
				s = new SourceData()
				{
					Data = path,
					Type = SourceType.Folder,
					Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/Folder.ico",
					Include = true
				};
			}
			else
			{
				s = new SourceData()
				{
					Data = path,
					Type = SourceType.Library,
					Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/Library.ico",
					Include = true
				};
			}
			if (s != null)
				AddSource(s, callback, callbackParams);
		}

		/// <summary>
		/// Add a source to the collection
		/// </summary>
		/// <param name="source">The source to be added</param>
		/// <param name="callback">A function that will be sent along with the SourceModified event</param>
		/// <param name="callbackParams">The parameters for the callback function</param>
		public static void AddSource(SourceData source, ScannerCallback callback = null, object callbackParams = null)
		{
			U.L(LogLevel.Information, "FILESYSTEM", String.Format("{0} the {1} {2}", 
				(source.Include ? "Adding" : "Ignoring"), source.Type, source.Data));

			SourceTree s = GetSourceTree(source.Data, sourceForest);

			if (s != null)
			{
				U.L(LogLevel.Warning, "FILESYSTEM", String.Format("Removing currently {0} {1} {2}",
					(source.Include ? "added" : "ignored"), source.Type, source.Data));
				RemoveSource(source);
			}

			DispatchSourceAdded(source);
			ScanSourcesWithDelay(callback, callbackParams);
		}

		/// <summary>
		/// Remove a source from the collection
		/// </summary>
		/// <param name="source">The source to be removed</param>
		public static void RemoveSource(SourceData source)
		{
			if (!SettingsManager.Sources.Contains(source))
			{
				U.L(LogLevel.Warning, "FILESYSTEM", "Trying to remove non-existing source " + source.Data);
				return;
			}
			DispatchSourceRemoved(source);
			ScanSources();
		}

		/// <summary>
		/// Retrieves a source given its data
		/// </summary>
		/// <param name="data">The data of the source</param>
		/// <returns>A source matching criterion if found, otherwise null</returns>
		public static SourceData GetSource(string data)
		{
			foreach (SourceData s in SettingsManager.Sources)
				if (s.Data == data)
					return s;
			return null;
		}

		/// <summary>
		/// Scans for system specific folders of music and adds them to the source collection.
		/// </summary>
		/// <remarks>
		/// The following will be added for supported platforms:
		/// Windows: Libraries of type "Music"
		/// </remarks>
		/// <param name="ScanSourcesWhenDone">Set to true to scan the folders afterwards</param>
		/// <param name="InBackground">Set to true to perform the scan in a background thread</param>
		public static void AddSystemFolders(bool ScanSourcesWhenDone = false, bool InBackground = true)
		{
			WaitForInitialization();

			ThreadStart ScanThread = delegate()
			{
#if (Windows)
				String librariesPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
					+ @"\AppData\Roaming\Microsoft\Windows\Libraries\";

				// remove non-existing libraries from sources
				for (int i = 0; i < SettingsManager.Sources.Count; i++)
					if (SettingsManager.Sources[i].Type == SourceType.Library &&
						!System.IO.File.Exists(librariesPath + SettingsManager.Sources[i].Data + ".library-ms"))
						SettingsManager.Sources.RemoveAt(i--);

				// add newly created libraries
				DirectoryInfo librariesInfo = new DirectoryInfo(librariesPath);
				FileInfo[] libraries = librariesInfo.GetFiles("*.library-ms");
				foreach (FileInfo libraryInfo in libraries)
				{
					String libraryName = System.IO.Path.GetFileNameWithoutExtension(libraryInfo.FullName);
					ShellLibrary library = ShellLibrary.Load(libraryName, librariesPath, true);

					// newly created libraries will not get a LibraryType so we get an exception here
					try
					{
						// make sure that the library is added
						if (library.LibraryType == LibraryFolderType.Music)
						{
							bool AlreadyAdded = false;
							foreach (SourceData src in SettingsManager.Sources)
							{
								if (src.Data == libraryName && src.Type == SourceType.Library)
								{
									AlreadyAdded = true;
									break;
								}
							}
							if (!AlreadyAdded)
							{
								SourceData s = new SourceData();
								s.Data = libraryName;
								s.Automatic = true;
								s.Type = SourceType.Library;
								s.Include = true;
								s.Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/Library.ico";
								AddSource(s);
							}
						}
						else // remove the library if neccessary
						{
							for (int i = 0; i < SettingsManager.Sources.Count; i++)
								if (SettingsManager.Sources[i].Data == libraryName && SettingsManager.Sources[i].Type == SourceType.Library)
									SettingsManager.Sources.RemoveAt(i--);
						}
					}
					catch
					{
						// the library was (probably) newly created and didn't get a LibraryType, so we ignore it
					}


					// setup a janitor for libraries
					FileSystemWatcher fsw = new FileSystemWatcher();
					fsw.Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Roaming\Microsoft\Windows\Libraries\";
					fsw.Filter = "*.library-ms";
					fsw.IncludeSubdirectories = false;
					fsw.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
					fsw.Changed += Janitor_LibraryChanged;
					fsw.Created += Janitor_LibraryChanged;
					fsw.Deleted += Janitor_LibraryChanged;
					fsw.Renamed += Janitor_LibraryRenamed;
					fsw.EnableRaisingEvents = true;
					janitors.Add(fsw);
				}
				if (ScanSourcesWhenDone) ScanSources();
#else
				SettingsManager.Sources.Add(new SourceData
				{
					HumanType = U.T("SourcesTypeFolder"),
					Type = SourceType.Folder,
					Automatic = true,
					Data = @"C:\Users\ephracis\Music\",
					Include = true,
					Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/Folder.ico"
				});
#endif
			};

			Thread thread = new Thread(ScanThread);
			thread.Name = "Scan libraries";
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Start();
			if (!InBackground)
				thread.Join();
		}

		/// <summary>
		/// Scans the sources after a slight delay with buffering of callbacks and their parameters.
		/// </summary>
		/// <param name="callback">The callback to be called after scan is complete</param>
		/// <param name="callbackParams">Parameters for the callback</param>
		public static void ScanSourcesWithDelay(ScannerCallback callback = null, object callbackParams = null)
		{
			if (scanDelay != null)
				scanDelay.Dispose();

			if (callback != null)
				scanDelayCallbacks.Add(new KeyValuePair<ScannerCallback,object>(callback, callbackParams));

			scanDelay = new Timer(DoScanSources, null, 1000, 3600000);
		}

		/// <summary>
		/// Scan all folders in the collection for supported music tracks
		/// </summary>
		/// <param name="callback">A function that will be sent along with the SourceModified event</param>
		/// <param name="callbackParams">The parameters for the callback function</param>
		public static void ScanSources(ScannerCallback callback = null, object callbackParams = null)
		{
			List<KeyValuePair<ScannerCallback, object>> callbacks = new List<KeyValuePair<ScannerCallback, object>>();
			if (callback != null)
				callbacks.Add(new KeyValuePair<ScannerCallback,object>(callback, callbackParams));
			ScanSources(callbacks);
		}

		/// <summary>
		/// Scan all folders in the collection for supported music tracks
		/// </summary>
		/// <param name="callbacks">The callbacks and their parameters that will be sent along with the SourceModified event</param>
		public static void ScanSources(List<KeyValuePair<ScannerCallback, object>> callbacks)
		{
			WaitForInitialization();

			ThreadStart ScanThread = delegate()
			{
				U.L(LogLevel.Information, "FILESYSTEM", "Starting scan of sources");
				DispatchProgressChanged(0, "start");

				U.L(LogLevel.Debug, "FILESYSTEM", "Refreshing meta data of existing tracks");
				foreach (TrackData t in SettingsManager.FileTracks)
				{
					if (Updated(t))
						DispatchSourceModified(t, SourceModificationType.Updated);
				}

				U.L(LogLevel.Debug, "FILESYSTEM", "Getting forest");
				List<SourceTree> forest = GetSourceForest();

				List<SourceTree> addedPaths = new List<SourceTree>();
				List<SourceTree> removedPaths = new List<SourceTree>();
				U.L(LogLevel.Debug, "FILESYSTEM", "Calculating forest change");
				GetForestDifference(sourceForest, forest, addedPaths, removedPaths);

				U.L(LogLevel.Debug, "FILESYSTEM", String.Format("Removing {0} paths ", removedPaths.Count));
				foreach (SourceTree tree in removedPaths)
				{
					bool added = PathIsAdded(tree.Data, forest);
					bool ignored = PathIsIgnored(tree.Data, forest);

					if ((tree.Include && added) || (tree.Ignore && (!added || ignored)))
						continue;

					// see if there's any other sources on the same drive
					bool remove = true;
					foreach (SourceTree t in forest)
					{
						if (Path.GetPathRoot(t.Data) == Path.GetPathRoot(tree.Data))
						{
							remove = false;
							break;
						}
					}
					if (remove)
						RemoveJanitor(Path.GetPathRoot(tree.Data));
					tree.Ignore = tree.Include;
					ScanPath(tree.Data, tree, true, callbacks);
				}

				U.L(LogLevel.Debug, "FILESYSTEM", String.Format("Adding {0} paths", addedPaths.Count));
				foreach (SourceTree tree in addedPaths)
				{
					SetupJanitor(Path.GetPathRoot(tree.Data));
					ScanPath(tree.Data, tree, true, callbacks);
				}

				U.L(LogLevel.Debug, "FILESYSTEM", String.Format("Checking existing tracks"));
				foreach (TrackData track in SettingsManager.FileTracks)
				{
					if (PathIsIgnored(track.Path) || !File.Exists(track.Path))
					{
						RemoveFile(track.Path, callbacks);
					}
				}

				if (addedPaths.Count == 0 && removedPaths.Count == 0)
				{
					foreach (KeyValuePair<ScannerCallback,object> pair in callbacks)
					{
						ScannerCallback callback = pair.Key;
						object callbackParams = pair.Value;
						if (callback != null)
							callback(callbackParams);
					}
				}

				if (ShouldStopScanner)
					return;

				sourceForest = forest;

				List<TrackData> tracksToRemove = new List<TrackData>();
				foreach (TrackData t in SettingsManager.FileTracks)
				{
					// remove files that should not be in collection
					if (!PathIsAdded(t.Path))
						tracksToRemove.Add(t);
				}

				if (tracksToRemove.Count > 0)
					U.L(LogLevel.Debug, "FILESYSTEM", "Dispatching SourceModified for all removals");
				foreach (TrackData t in tracksToRemove)
					DispatchSourceModified(t, SourceModificationType.Removed);

				DispatchProgressChanged(100, "done");
				U.L(LogLevel.Information, "FILESYSTEM", "Finished scan of sources");
			};

			try
			{
				StopScan();
				haltScanner = false;
				scanThread = new Thread(ScanThread);
				scanThread.Name = "Scan thread";
				scanThread.Priority = ThreadPriority.Lowest;
				scanThread.Start();
			}
			catch { }
		}

		/// <summary>
		/// Stop the scanner
		/// </summary>
		public static void StopScan()
		{
			haltScanner = true;
			if (scanThread != null)
				scanThread.Join(15000);
		}

		/// <summary>
		/// Create a track given a filename
		/// </summary>
		/// <param name="filename">The filename of the track</param>
		/// <param name="dispatchModified">Whether or not to dispatch that the track has been added</param>
		/// <returns>The newly created track</returns>
		public static TrackData CreateTrack(String filename, bool dispatchModified = true)
		{
			if (!MediaManager.IsSupported(filename))
			{
				U.L(LogLevel.Warning, "FILESYSTEM", "Cannot create track " + filename + ": unsupported format");
				return null;
			}
			if (!File.Exists(filename))
			{
				U.L(LogLevel.Warning, "FILESYSTEM", "Cannot create track " + filename + ": file does not exist");
				return null;
			}

			FileInfo fInfo = new FileInfo(filename);
			TrackData track = new TrackData
			{
				Processed = false,
				Artist = U.T("MetaDataLoading"),
				Album = U.T("MetaDataLoading"),
				Title = U.T("MetaDataLoading"),
				Genre = U.T("MetaDataLoading"),
				Year = 0,
				Length = 0.0,
				PlayCount = 0,
				Track = 0,
				Path = filename,
				Bitrate = 0,
				Channels = 0,
				SampleRate = 0,
				Codecs = "",
				Number = 0,
				Bookmarks = new List<double>(),
				Source = "Files",
				Icon = @"..\..\Platform\Windows 7\GUI\Images\Icons\FileAudio.ico",
				LastWrite = 0
			};
			track.PropertyChanged += new PropertyChangedEventHandler(Track_PropertyChanged);
			if (dispatchModified)
				DispatchSourceModified(track, SourceModificationType.Added);
			return track;
		}

		/// <summary>
		/// Check of a given path is being watched
		/// </summary>
		/// <param name="path">The path to check</param>
		/// <returns>true if <paramref name="path"/> is being watched, otherwise false</returns>
		public static bool PathIsAdded(String path)
		{
			return PathIsAdded(path, sourceForest);
		}

		/// <summary>
		/// Check of a given path is being watched
		/// </summary>
		/// <param name="path">The path to check</param>
		/// <param name="forest">The forest to check in</param>
		/// <returns>true if <paramref name="path"/> is being watched, otherwise false</returns>
		public static bool PathIsAdded(String path, List<SourceTree> forest)
		{
			SourceTree s = GetSourceTree(path, forest, false);
			if (s == null)
				return false;
			else
				return s.Include;
		}

		/// <summary>
		/// Check of a given path is being ignored
		/// </summary>
		/// <param name="path">The path to check</param>
		/// <returns>true if <paramref name="path"/> is being ignored, otherwise false</returns>
		public static bool PathIsIgnored(String path)
		{
			return PathIsIgnored(path, sourceForest);
		}

		/// <summary>
		/// Check of a given path is being ignored
		/// </summary>
		/// <param name="path">The path to check</param>
		/// <param name="forest">The forest to check in</param>
		/// <returns>true if <paramref name="path"/> is being ignored, otherwise false</returns>
		public static bool PathIsIgnored(String path, List<SourceTree> forest)
		{
			SourceTree s = GetSourceTree(path, forest, false);
			if (s == null) return false;
			else return s.Ignore;
		}

		/// <summary>
		/// Checks if a track has been added to a collection of tracks.
		/// </summary>
		/// <param name="path">The path to check for</param>
		/// <param name="tracks">The collection to check in</param>
		/// <returns>True if a track with the same path is found</returns>
		public static bool TrackIsAdded(string path, ObservableCollection<TrackData> tracks)
		{
			foreach (TrackData t in tracks)
				if (t.Path == path)
					return true;
			return false;
		}

		/// <summary>
		/// Checks if a track has been added to the file collection.
		/// </summary>
		/// <param name="path">The path to check for</param>
		/// <returns>True if a track with the same path is found</returns>
		public static bool TrackIsAdded(string path)
		{
			return TrackIsAdded(path, SettingsManager.FileTracks);
		}

		#endregion

		#region Private

		/// <summary>
		/// Pauses until both the filesystem manager and media manager has been initialized
		/// </summary>
		private static void WaitForInitialization()
		{
			if (!MediaManager.IsInitialized)
			{
				U.L(LogLevel.Debug, "FILESYSTEM", "Waiting for Media Manager to be initialized, pausing scanner");
				while (!MediaManager.IsInitialized) ;
				U.L(LogLevel.Debug, "FILESYSTEM", "Media Manager has been detected as initialized, resuming scanner");
			}

			if (!IsInitialized)
			{
				U.L(LogLevel.Debug, "FILESYSTEM", "Waiting for Filesystem Manager to be initialized, pausing scanner");
				while (!IsInitialized) ;
				U.L(LogLevel.Debug, "FILESYSTEM", "Filesystem Manager has been detected as initialized, resuming scanner");
			}
		}

		/// <summary>
		/// Check if we have permission to read a file or folder.
		/// </summary>
		/// <param name="path">The path to check</param>
		/// <returns>True if read permission is granted, otherwise false.</returns>
		private static bool CanRead(string path)
		{
			AuthorizationRuleCollection acl = null;
			if (File.Exists(path))
			{
				var sec = File.GetAccessControl(path);
				acl = sec.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
			}
			else if (Directory.Exists(path))
			{
				var sec = Directory.GetAccessControl(path);
				acl = sec.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
			}
			
			if (acl == null)
				return false;
			
            foreach (FileSystemAccessRule ace in acl)
			{
				var rights = ace.FileSystemRights;
				var allow = ace.AccessControlType == AccessControlType.Allow;
				var read = FileSystemRights.Read;
				var readDataAndAttributes = FileSystemRights.ReadData | FileSystemRights.ReadAttributes;
				
				if (((rights & read) == read) || ((rights & readDataAndAttributes) == readDataAndAttributes))
					return allow;
			}
			
			return false;
		}

		/// <summary>
		/// Scan a path for files and synchronize its content with our collection
		/// </summary>
		/// <param name="path">The path to scan</param>
		/// <param name="tree">The tree that is either the path itself or the parent</param>
		/// <param name="removeIgnored">Whether or not we should remove any paths that we find which is set to ignore</param>
		/// <param name="callback">The callback which will be sent along with the SourceModified event</param>
		private static void ScanPath(String path, SourceTree tree, bool removeIgnored = true, ScannerCallback callback = null, object callbackParams = null)
		{
			List<KeyValuePair<ScannerCallback, object>> callbacks = new List<KeyValuePair<ScannerCallback, object>>();
			if (callback != null)
				callbacks.Add(new KeyValuePair<ScannerCallback,object>(callback, callbackParams));
			ScanPath(path, tree, removeIgnored, callbacks);
		}

		/// <summary>
		/// Scan a path for files and synchronize its content with our collection
		/// </summary>
		/// <param name="path">The path to scan</param>
		/// <param name="tree">The tree that is either the path itself or the parent</param>
		/// <param name="removeIgnored">Whether or not we should remove any paths that we find which is set to ignore</param>
		/// <param name="callbacks">A list of callbacks and their respective parameters which will be sent along with the SourceModified event</param>
		private static void ScanPath(String path, SourceTree tree, bool removeIgnored, List<KeyValuePair<ScannerCallback, object>> callbacks)
		{
			// ignore the recycle bin
			if (path.Substring(1).ToUpper().StartsWith(@":\$RECYCLE.BIN")) return;

			// check if scanner should stop
			if (ShouldStopScanner) return;

			// break if no read permission
			if (!CanRead(path)) return;

			WaitForInitialization();

			// path is a file
			if (File.Exists(path))
			{
				if (tree.Include) AddFile(path, false, callbacks);
				else if (removeIgnored) RemoveFile(path, callbacks);
			}

			// path is a folder
			else if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path);
				string[] folders = Directory.GetDirectories(path);

				// calculate the amount of progress to increase each file/folder with
				//double progressIncrease = deltaProgress / (files.Count() + folders.Count());

				// scan all toplevel files
				string[] ext = MediaManager.SupportedFormatsExtensions.Split(';');
				try
				{
					for (int i=0; i<files.Count(); i++)
					{
						// check if scanner should stop
						if (ShouldStopScanner) return;

						string file = files[i];

						//int progressPos = (int)(progressPosition + (i * progressIncrease));
						//DispatchProgressChanged(progressPos, "progress");

						if (!ext.Contains("*" + Path.GetExtension(file)))
						    continue;

						bool include = tree.Include;
						SourceTree st = GetSourceTree(file, tree.Children);
						if (st != null)
							include = st.Include;

						foreach (SourceTree childTree in tree.Children)
						{
							if (childTree.Data == file)
							{
								include = childTree.Include;
								break;
							}
						}
						if (include) AddFile(file, false, callbacks);
						else if (removeIgnored) RemoveFile(file, callbacks);
					}

					// dive inside subfolders
					for (int i=0; i<folders.Count(); i++)
					{
						// check if scanner should stop
						if (ShouldStopScanner) return;

						string subfolder = folders[i];

						//int progressPos = (int)(progressPosition + ((files.Count() + i) * progressIncrease));
						//DispatchProgressChanged(progressPos, "progress");

						SourceTree subtree = GetSourceTree(subfolder, tree.Children);
						if (subtree == null) subtree = tree;
						ScanPath(subfolder, subtree, removeIgnored, callbacks);
					}
				}
				catch (Exception e)
				{
					// check if scanner should stop
					if (ShouldStopScanner) return;

					U.L(LogLevel.Warning, "FILESYSTEM", "Could not scan " + path + ": " + e.Message);
				}
			}
		}

		/// <summary>
		/// Copy all metadata from one track to another
		/// </summary>
		/// <param name="source">The source track from which the metadata is copied</param>
		/// <param name="destination">The destination track to which the metadata is copied</param>
		private static void CopyTrackInfo(TrackData source, TrackData destination)
		{
			destination.Artist = source.Artist;
			destination.Album = source.Album;
			destination.Genre = source.Genre;
			destination.Title = source.Title;
			destination.Track = source.Track;
			destination.Year = source.Year;
			destination.Length = source.Length;
			destination.LastWrite = source.LastWrite;
			destination.Bitrate = source.Bitrate;
			destination.Views = source.Views;
			destination.Icon = source.Icon;
			destination.Codecs = source.Codecs;
			destination.Channels = source.Channels;
			destination.SampleRate = source.SampleRate;
		}
		
		/// <summary>
		/// Add a file to the collection
		/// </summary>
		/// <param name="filename">The path of the file</param>
		/// <param name="scanMetaData">Whether to scan the tracks meta data as well</param>
		/// <param name="callback">A function that will be sent along with the SourceModified event</param>
		/// <param name="callbackParams">The parameters for the callback function</param>
		private static void AddFile(String filename, bool scanMetaData = false, ScannerCallback callback = null, object callbackParams = null)
		{
			List<KeyValuePair<ScannerCallback, object>> callbacks = new List<KeyValuePair<ScannerCallback, object>>();
			if (callback != null)
				callbacks.Add(new KeyValuePair<ScannerCallback,object>(callback, callbackParams));
			AddFile(filename, scanMetaData, callbacks);
		}

		/// <summary>
		/// Add a file to the collection
		/// </summary>
		/// <param name="filename">The path of the file</param>
		/// <param name="scanMetaData">Whether to scan the tracks meta data as well</param>
		/// <param name="callbacks">The callbacks and their respective parameters that will be sent along with the SourceModified event</param>
		private static void AddFile(String filename, bool scanMetaData, List<KeyValuePair<ScannerCallback, object>> callbacks)
		{
			FileInfo fInfo = new FileInfo(filename);
			TrackData track = new TrackData
			{
				Processed = false,
				Artist = U.T("MetaDataLoading"),
				Album = U.T("MetaDataLoading"),
				Title = U.T("MetaDataLoading"),
				Genre = U.T("MetaDataLoading"),
				Year = 0,
				Length = 0.0,
				PlayCount = 0,
				Bitrate = 0,
				Channels = 0,
				SampleRate = 0,
				Codecs = "",
				Track = 0,
				Path = fInfo.FullName,
				Number = 0,
				Icon = @"..\..\Platform\Windows 7\GUI\Images\Icons\FileAudio.ico",
				LastWrite = 0
			};

			DispatchSourceModified(track, SourceModificationType.Added, callbacks);
			track.PropertyChanged += new PropertyChangedEventHandler(Track_PropertyChanged);
			if (scanMetaData)
				DispatchPathModified(track.Path);
		}

		/// <summary>
		/// Remove a file from the collection
		/// </summary>
		/// <param name="filename">The path of the file</param>
		/// <param name="callback">A function that will be sent along with the SourceModified event</param>
		/// <param name="callbackParams">The parameters for the callback function</param>
		private static void RemoveFile(String filename, ScannerCallback callback = null, object callbackParams = null)
		{
			List<KeyValuePair<ScannerCallback, object>> callbacks = new List<KeyValuePair<ScannerCallback, object>>();
			if (callback != null)
				callbacks.Add(new KeyValuePair<ScannerCallback,object>(callback, callbackParams));
			RemoveFile(filename, callbacks);
		}

		/// <summary>
		/// Remove a file from the collection
		/// </summary>
		/// <param name="filename">The path of the file</param>
		/// <param name="callbacks">The callbacks and their respective parameters that will be sent along with the SourceModified event</param>
		private static void RemoveFile(String filename, List<KeyValuePair<ScannerCallback, object>> callbacks)
		{
			TrackData track = null;
			foreach (TrackData t in SettingsManager.FileTracks)
			{
				if (t.Path == filename)
				{
					track = t;
					break;
				}
			}
			if (track != null)
				DispatchSourceModified(track, SourceModificationType.Removed, callbacks);
			else
			{
				foreach (KeyValuePair<ScannerCallback, object> pair in callbacks)
				{
					ScannerCallback callback = pair.Key;
					object callbackParams = pair.Value;
					if (callback != null)
						callback(callbackParams);
				}
			}
		}

		/// <summary>
		/// Changes the path of a set of tracks
		/// </summary>
		/// <param name="oldName">The current path of the tracks</param>
		/// <param name="newName">The new path of the tracks</param>
		/// <param name="tracks">The set of tracks</param>
		private static void RenamePath(String oldName, String newName, ObservableCollection<TrackData> tracks)
		{
			foreach (TrackData track in tracks)
				if (track.Path.StartsWith(oldName))
					track.Path = track.Path.Replace(oldName, newName);
		}

		/// <summary>
		/// Gets a specific source tree given its data
		/// </summary>
		/// <param name="data">The data of the source tree</param>
		/// <param name="forest">The parent of the source to look for</param>
		/// <param name="exactMatch">Whether the path match should be exact or just initial</param>
		/// <returns>The source with the corresponding data if one could be found, otherwise null</returns>
		private static SourceTree GetSourceTree(String data, List<SourceTree> forest, bool exactMatch = true)
		{
			foreach (SourceTree source in forest)
			{
				if (data == source.Data && exactMatch)
					return source;
				SourceTree child = GetSourceTree(data, source.Children, exactMatch);
				if (child != null) return child;
				else if (!exactMatch && data.StartsWith(source.Data))
					return source;
			}

			return null;
		}

		/// <summary>
		/// Gets a specific source given its data
		/// </summary>
		/// <param name="data">The data of the source</param>
		/// <returns>The source with the corresponding data if one could be found, otherwise null</returns>
		private static SourceData GetSourceData(String data)
		{
			foreach (SourceData s in SettingsManager.Sources)
				if (s.Data == data)
					return s;

			return null;
		}

		/// <summary>
		/// Gets a specific janitor
		/// </summary>
		/// <param name="drive">The drive that the janitor is responsible of</param>
		/// <returns>The janitor if one could be found, otherwise null</returns>
		private static FileSystemWatcher GetJanitor(String drive)
		{
			foreach (FileSystemWatcher fsw in janitors)
				if (fsw.Path == drive)
					return fsw;
			return null;
		}

		/// <summary>
		/// Creates a janitor for a specific drive
		/// </summary>
		/// <param name="drive">The drive that the janitor is responsible of</param>
		private static void SetupJanitor(String drive)
		{
			if (GetJanitor(drive) != null) return;

			FileSystemWatcher fsw = new FileSystemWatcher();

			if (!Directory.Exists(drive)) return;

			U.L(LogLevel.Debug, "FILESYSTEM", "Started watching for events on " + drive);
			fsw.Path = drive;
			fsw.IncludeSubdirectories = true;

			fsw.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName |
				NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
			fsw.Changed += Janitor_FileChanged;
			fsw.Created += Janitor_FileChanged;
			fsw.Deleted += Janitor_FileChanged;
			fsw.Renamed += Janitor_FileRenamed;
			fsw.EnableRaisingEvents = true;
			janitors.Add(fsw);
		}

		/// <summary>
		/// Removes a specific janitor
		/// </summary>
		/// <param name="drive">The drive that the janitor is responsible of</param>
		private static void RemoveJanitor(String drive)
		{
			FileSystemWatcher fsw = GetJanitor(drive);
			if (fsw == null) return;

			U.L(LogLevel.Debug, "FILESYSTEM", "Stopped watching for events on " + drive);
			fsw.EnableRaisingEvents = false;
			janitors.Remove(fsw);
		}

		/// <summary>
		/// Get a list of folders inside a Windows 7 Library
		/// </summary>
		/// <param name="name">The name of the library</param>
		/// <returns>A list of all folders inside <paramref name="name"/></returns>
		private static List<String> GetLibraryFolders(string name)
		{
			List<string> ret = new List<string>();
#if (Windows)
			if (System.IO.File.Exists(librariesPath + name + ".library-ms"))
			{
				ShellLibrary lib = ShellLibrary.Load(name, librariesPath, true);
				foreach (ShellFileSystemFolder folder in lib)
					ret.Add(folder.Path);
				lib.Close();
			}
#endif
			return ret;
		}

		/// <summary>
		/// Creates the forest <see cref="sourceForest"/> using
		/// the sources in PropertiesWindow.
		/// </summary>
		private static List<SourceTree> GetSourceForest()
		{
			List<SourceTree> forest = new List<SourceTree>();
			foreach (SourceData source in SettingsManager.Sources)
			{
				if (source.Type == SourceType.Library)
					foreach (String folder in GetLibraryFolders(source.Data))
						InsertSourceTree(new SourceTree() { Data = folder, Ignore = source.Ignore, Children = new List<SourceTree>() }, forest);
				else
					InsertSourceTree(new SourceTree() { Data = source.Data, Ignore = source.Ignore, Children = new List<SourceTree>() }, forest);
			}
			return forest;
		}

		/// <summary>
		/// Insert a source tree into a forest. The tree will be inserted into the forest so that
		/// the parent will be a folder which contains the folder that the tree describes.
		/// </summary>
		/// <param name="tree">The tree to insert</param>
		/// <param name="forest">The forest to insert the tree into</param>
		private static void InsertSourceTree(SourceTree tree, List<SourceTree> forest)
		{
			for (int i=0; i<forest.Count; i++)
			{
				SourceTree t = forest[i];

				if (tree.Data == t.Data)
					return;

				// insert tree into t
				else if (tree.Data.StartsWith(t.Data))
				{
					InsertSourceTree(tree, t.Children);
					return;
				}

				// swap t for tree and make t a child of tree
				else if (t.Data.StartsWith(tree.Data))
				{
					tree.Children.Add(t);
					forest.RemoveAt(i);
					forest.Insert(i, tree);

					// continue through the forest and find any additional children for tree
					for (int j = i + 1; j < forest.Count; j++)
					{
						t = forest[j];
						if (t.Data.StartsWith(tree.Data))
						{
							tree.Children.Add(t);
							forest.RemoveAt(j--);
						}
					}
					return;
				}
			}
			// insert tree into forest as top tree
			forest.Add(tree);
		}

		/// <summary>
		/// Remove a source tree from a forest
		/// </summary>
		/// <param name="tree">The data of the tree to be removed</param>
		/// <param name="forest">The forest to remove the tree from</param>
		private static void RemoveSourceTree(String tree, List<SourceTree> forest)
		{
			// iterate through the forest and find the tree's position
			for (int i=0; i < forest.Count; i++)
			{
				SourceTree t = forest[i];

				// found the tree inside the forest
				if (t.Data == tree)
				{
					// put all children into the parent
					foreach (SourceTree child in t.Children)
						forest.Add(child);

					forest.RemoveAt(i);
					return;
				}

				// found a parent of the tree
				else if (t.Data.StartsWith(tree))
				{
					RemoveSourceTree(tree, t.Children);
					return;
				}
			}
		}

		/// <summary>
		/// Calculates the difference between two source forests
		/// </summary>
		/// <param name="forest1">The original forest</param>
		/// <param name="forest2">The updated forest</param>
		/// <param name="added">A list which will after calculation contain all trees in forest2 and not forest1</param>
		/// <param name="removed">A list which will after calculation contain all trees in forest1 and not forest2</param>
		private static void GetForestDifference(List<SourceTree> forest1, List<SourceTree> forest2, List<SourceTree> added, List<SourceTree> removed)
		{
			// find all trees that are in forest1 but not forest2
			foreach (SourceTree tree1 in forest1)
			{
				bool isRemoved = true;
				foreach (SourceTree tree2 in forest2)
				{
					if (tree2.Data == tree1.Data)
					{
						GetForestDifference(tree1.Children, tree2.Children, added, removed);
						isRemoved = false;

						if (tree2.Ignore && tree1.Include)
							added.Add(tree1);
						else if (tree2.Include && tree1.Ignore)
							removed.Add(tree1);

						break;
					}
				}
				if (isRemoved && tree1.Include)
					removed.Add(tree1);
				else if (isRemoved && tree1.Ignore)
					added.Add(tree1);
			}

			// find all trees that are in forest2 but not forest1
			foreach (SourceTree tree2 in forest2)
			{
				bool isAdded = true;
				foreach (SourceTree tree1 in forest1)
				{
					if (tree2.Data == tree1.Data)
					{
						isAdded = false;

						break;
					}
				}
				if (isAdded && tree2.Ignore)
					removed.Add(tree2);
				else if (isAdded && tree2.Include)
					added.Add(tree2);
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when a property of the settings manager changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Sources":
					ScanSources();
					break;
			}
		}
		
		/// <summary>
		/// Event handler that gets called when a Library has been renamed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Janitor_LibraryRenamed(object sender, RenamedEventArgs e)
		{
			string oldName = System.IO.Path.GetFileNameWithoutExtension(e.OldName);
			string newName = System.IO.Path.GetFileNameWithoutExtension(e.Name);

			foreach (JanitorTask jt in janitorTasks)
			{
				// on delete this event will fire afterwards, but we should skip it
				if (jt.Job == JanitorJob.DeleteLibrary && jt.Data == newName)
					return;

				// check if this job is already in the queue
				if (jt.Job == JanitorJob.RenameLibrary && jt.Data == oldName + "\n" + newName)
					return;
			}

			if (janitorLazyness != null)
				janitorLazyness.Dispose();
			janitorTasks.Add(new JanitorTask() { Data = oldName + "\n" + newName, Job = JanitorJob.RenameLibrary });
			janitorLazyness = new Timer(Janitor_Clean, null, 400, 3600);
		}

		/// <summary>
		/// Event handler that gets called when a Library has been modified
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Janitor_LibraryChanged(object sender, FileSystemEventArgs e)
		{
			JanitorTask task = new JanitorTask() { Data = System.IO.Path.GetFileNameWithoutExtension(e.Name) };
			if (e.ChangeType == WatcherChangeTypes.Deleted)
				task.Job = JanitorJob.DeleteLibrary;
			else if (e.ChangeType == WatcherChangeTypes.Created)
				task.Job = JanitorJob.CreateLibrary;
			else if (e.ChangeType == WatcherChangeTypes.Changed)
				task.Job = JanitorJob.UpdateLibrary;
			else
				return;

			foreach (JanitorTask jt in janitorTasks)
			{
				// on rename and create this event will fire afterwards, but we should skip it
				if (e.ChangeType == WatcherChangeTypes.Changed &&
					((jt.Job == JanitorJob.RenameLibrary && jt.Data.EndsWith("\n" + System.IO.Path.GetFileNameWithoutExtension(e.Name))) ||
					(jt.Job == JanitorJob.CreateLibrary && jt.Data == System.IO.Path.GetFileNameWithoutExtension(e.Name))))
				{
					return;
				}

				// check if this job is already in the queue
				if (jt.Job == task.Job && jt.Data == task.Data)
				{
					return;
				}

				// an update may be preceded by a delete, so we change the delete task into an update task
				if (task.Job == JanitorJob.UpdateLibrary && jt.Job == JanitorJob.DeleteLibrary && task.Data == jt.Data)
				{
					jt.Job = JanitorJob.UpdateLibrary;
					return;
				}

				// an update may be followed by a delete, so we skip the delete
				if (task.Job == JanitorJob.DeleteLibrary && jt.Job == JanitorJob.UpdateLibrary && task.Data == jt.Data)
				{
					return;
				}
			}

			if (janitorLazyness != null)
				janitorLazyness.Dispose();
			janitorTasks.Add(task);
			janitorLazyness = new Timer(Janitor_Clean, null, 400, 3600);
		}

		/// <summary>
		/// Event handler that gets called when a file has been renamed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Janitor_FileRenamed(object sender, RenamedEventArgs e)
		{
			bool isDir = Directory.Exists(e.FullPath);

			if (!isDir && !MediaManager.IsSupported(e.FullPath))
				return;

			JanitorTask task = new JanitorTask() { Data = e.OldFullPath + "\n" + e.FullPath, Job = isDir ? JanitorJob.RenameFolder : JanitorJob.RenameFile };

			foreach (JanitorTask jt in janitorTasks)
			{
				// on delete this event will fire afterwards, but we should skip it
				if (jt.Job == JanitorJob.DeleteFile && jt.Data == e.FullPath)
					return;

				// check if this job is already in the queue
				if (jt.Job == task.Job && jt.Data == task.Data)
					return;
			}
			//janitorLazyness.Stop();
			if (janitorLazyness != null)
				janitorLazyness.Dispose();
			janitorTasks.Add(task);
			janitorLazyness = new Timer(Janitor_Clean, null, 400, 3600);
		}

		/// <summary>
		/// Event handler that gets called when a file has been modified
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Janitor_FileChanged(object sender, FileSystemEventArgs e)
		{
			bool isDir = Directory.Exists(e.FullPath);

			if (!PathIsAdded(e.FullPath) || !MediaManager.IsSupported(e.FullPath))
				return;

			JanitorTask task = new JanitorTask() { Data = e.FullPath };
			if (e.ChangeType == WatcherChangeTypes.Deleted)
			{
				if (MediaManager.IsSupported(e.FullPath))
					task.Job = JanitorJob.DeleteFile;
				else if (Directory.Exists(e.FullPath))
					task.Job = JanitorJob.DeleteFolder;
				else
					return;
			}
			else if (e.ChangeType == WatcherChangeTypes.Created)
				task.Job = isDir ? JanitorJob.CreateFolder : JanitorJob.CreateFile;

			else if (e.ChangeType == WatcherChangeTypes.Changed)
			{
				if (isDir) return;
				task.Job = JanitorJob.UpdateFile;
			}
			else
				return;

			foreach (JanitorTask jt in janitorTasks)
			{
				// on create folder this event will fire afterwards, but we should skip it
				if (!isDir && jt.Job == JanitorJob.CreateFolder && e.ChangeType == WatcherChangeTypes.Changed && jt.Data == e.FullPath)
					return;

				// on rename and create this event will fire afterwards, but we should skip it
				if (e.ChangeType == WatcherChangeTypes.Changed &&
					((jt.Job == JanitorJob.RenameFile && jt.Data.EndsWith("\n" + e.FullPath)) ||
					(jt.Job == JanitorJob.CreateFile && jt.Data == e.FullPath)))
					return;

				// check if this job is already in the queue
				if (jt.Job == task.Job && jt.Data == task.Data)
					return;
			}

			if (janitorLazyness != null)
				janitorLazyness.Dispose();
			janitorTasks.Add(task);
			janitorLazyness = new Timer(Janitor_Clean, null, 400, 3600);
		}

		/// <summary>
		/// Performs the actual cleaning of the janitor.
		/// This is called from a timer because when a file is modified several events will fire but
		/// we only need to act once.
		/// 
		/// This method will take action depending on what have happened, keeping the collection in 
		/// sync with the filesystem.
		/// </summary>
		/// <param name="state">The timer state (not used)</param>
		private static void Janitor_Clean(object state)
		{
			if (janitorLazyness != null)
				janitorLazyness.Dispose();
			janitorLazyness = null;

			// copy tasks and clear the list in case another event is fired while we are working
			List<JanitorTask> tasks = new List<JanitorTask>(janitorTasks);
			janitorTasks.Clear();

			foreach (JanitorTask task in tasks)
			{
				SourceData s = null;
				switch (task.Job)
				{
					case JanitorJob.CreateFile:
						AddFile(task.Data, true);
						break;

					case JanitorJob.DeleteFile:
						RemoveFile(task.Data);
						break;

					case JanitorJob.CreateLibrary:
#if Windows
						try
						{
							ShellLibrary library = ShellLibrary.Load(task.Data, librariesPath, true);
							s = GetSourceData(task.Data);
							if (library.LibraryType == LibraryFolderType.Music && s == null)
								AddSource(new SourceData 
								{ 
									Data = task.Data,
 									Include = true,
									Type = SourceType.Library,
									Automatic = true
								});
						}
						catch (Exception exc)
						{
							U.L(LogLevel.Warning, "FILESYSTEM", "Could not read newly created Library " + task.Data + ": " + exc.Message);
						}
#endif
						break;

					case JanitorJob.CreateFolder:
					case JanitorJob.DeleteFolder:
						ScanSources();
						break;

					case JanitorJob.DeleteLibrary:
						s = GetSourceData(task.Data);
						if (s != null)
							RemoveSource(s);
						break;

					case JanitorJob.RenameFolder:
					case JanitorJob.RenameFile:
						string[] names = task.Data.Split('\n');
						RenamePath(names[0], names[1], SettingsManager.FileTracks);
						RenamePath(names[0], names[1], SettingsManager.QueueTracks);
						RenamePath(names[0], names[1], SettingsManager.HistoryTracks);
						foreach (PlaylistData p in SettingsManager.Playlists)
							RenamePath(names[0], names[1], p.Tracks);							
						break;

					case JanitorJob.RenameLibrary:
						string[] lnames = task.Data.Split('\n');
						String oldLibraryName = System.IO.Path.GetFileNameWithoutExtension(lnames[0]);
						String newLibraryName = System.IO.Path.GetFileNameWithoutExtension(lnames[1]);
						s = GetSourceData(oldLibraryName);
						if (s != null)
							s.Data = newLibraryName;
						break;

					case JanitorJob.UpdateFile:
						DispatchPathModified(task.Data);
						break;

					case JanitorJob.UpdateLibrary:
#if Windows
						try
						{
							ShellLibrary lib = ShellLibrary.Load(task.Data, librariesPath, true);

							s = GetSourceData(task.Data);

							// remove if type changed to non-music
							if (s != null && lib.LibraryType != LibraryFolderType.Music)
								RemoveSource(s);

							// if still type music, detect changed folders
							else if (s != null)
								AddSystemFolders(true);

							// add if changed to type music
							else if (lib.LibraryType == LibraryFolderType.Music)
								AddSource(new SourceData
								{
									Data = task.Data,
									Type = SourceType.Library,
									Automatic = true,
									Include = true
								});
						}
						catch (Exception exc)
						{
							U.L(LogLevel.Warning, "FILESYSTEM", "Could not read newly updated Library " + task.Data + ": " + exc.Message);
						}
#endif
						break;

					default: // we really shouldn't ever get here
						return;
				}
			}
		}

		/// <summary>
		/// Invoked when the delay timer for ScanSourcesWithDelay it ticked.
		/// </summary>
		/// <param name="state">Not used</param>
		private static void DoScanSources(object state)
		{
			if (scanDelay != null)
				scanDelay.Dispose();
			scanDelay = null;

			List<KeyValuePair<ScannerCallback, object>> callbacks = new List<KeyValuePair<ScannerCallback, object>>();
			foreach (KeyValuePair<ScannerCallback, object> pair in scanDelayCallbacks)
				callbacks.Add(pair);
			scanDelayCallbacks.Clear();

			ScanSources(callbacks);
		}

		/// <summary>
		/// Invoked when something happens to a track.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">Event arguments</param>
		public static void Track_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			TrackData track = sender as TrackData;
			DispatchTrackModified(track, e);
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// The dispatcher of the <see cref="FilesystemManager.SourcesModified"/> event
		/// </summary>
		/// <param name="track">The track that was either added, removed or updated</param>
		/// <param name="modType">The modification type of the source</param>
		private static void DispatchSourceModified(TrackData track, SourceModificationType modType)
		{
			DispatchSourceModified(track, modType, new List<KeyValuePair<ScannerCallback, object>>());
		}

		/// <summary>
		/// The dispatcher of the <see cref="FilesystemManager.SourcesModified"/> event
		/// </summary>
		/// <param name="track">The track that was either added, removed or updated</param>
		/// <param name="modType">The modification type of the source</param>
		/// <param name="callback">The callbacks and their respective parameters that will be sent along with the SourceModified event</param>
		private static void DispatchSourceModified(TrackData track, SourceModificationType modType, List<KeyValuePair<ScannerCallback, object>> callbacks)
		{
			if (SourceModified != null)
				SourceModified(null, new SourceModifiedEventArgs(track, modType, callbacks));
		}

		/// <summary>
		/// The dispatcher of the <see cref="SourceAdded"/> event
		/// </summary>
		/// <param name="source">The source that was added</param>
		private static void DispatchSourceAdded(SourceData source)
		{
			if (SourceAdded != null)
				SourceAdded(null, new SourcesModifiedEventArgs(source));
		}

		/// <summary>
		/// The dispatcher of the <see cref="SourceRemoved"/> event
		/// </summary>
		/// <param name="source">The source that was removed</param>
		private static void DispatchSourceRemoved(SourceData source)
		{
			if (SourceRemoved != null)
				SourceRemoved(null, new SourcesModifiedEventArgs(source));
		}

		/// <summary>
		/// The dispatcher of the <see cref="TrackModified"/> event
		/// </summary>
		/// <param name="track">The track that was modified</param>
		/// <param name="e">The event data</param>
		private static void DispatchTrackModified(TrackData track, PropertyChangedEventArgs e)
		{
			if (TrackModified != null)
				TrackModified(track, e);
		}

		/// <summary>
		/// The dispatcher of the <see cref="PathRenamed"/> event
		/// </summary>
		/// <param name="oldName">The old name of the path</param>
		/// <param name="newName">The new name of the path</param>
		private static void DispatchPathRenamed(String oldName, String newName)
		{
			if (PathRenamed != null)
				PathRenamed(null, new RenamedEventArgs(WatcherChangeTypes.Renamed, null, newName, oldName));
		}

		/// <summary>
		/// The dispatcher of the <see cref="PathModified"/> event
		/// </summary>
		/// <param name="path">The path that was modified</param>
		private static void DispatchPathModified(string path)
		{
			if (PathModified != null)
				PathModified(null, new PathModifiedEventArgs(path));
		}

		/// <summary>
		/// The dispatcher of the <see cref="ProgressChanged"/> event
		/// </summary>
		/// <param name="progressPercentage">The percentage of the current progress</param>
		/// <param name="state">The state of the progress</param>
		private static void DispatchProgressChanged(int progressPercentage, string state)
		{
			if (ProgressChanged != null)
				ProgressChanged(null, new ProgressChangedEventArgs(progressPercentage, (object)state));
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when a track has been modified
		/// </summary>
		public static event PropertyChangedEventHandler TrackModified;

		/// <summary>
		/// Occurs when a path has been renamed
		/// </summary>
		public static event RenamedEventHandler PathRenamed;

		/// <summary>
		/// Occurs when a path has been modified
		/// </summary>
		public static event PathModifiedEventHandler PathModified;

		/// <summary>
		/// Occurs when progress of a scan has changed
		/// </summary>
		public static event ProgressChangedEventHandler ProgressChanged;

		/// <summary>
		/// Occurs when a source has been modified
		/// </summary>
		public static event SourceModifiedEventHandler SourceModified;

		/// <summary>
		/// Occurs when a source has been added
		/// </summary>
		public static event SourcesModifiedEventHandler SourceAdded;

		/// <summary>
		/// Occurs when a source has been removed
		/// </summary>
		public static event SourcesModifiedEventHandler SourceRemoved;

		#endregion Events
	}

	#region Delegates

	/// <summary>
	/// Represents the method that will handle the <see cref="FilesystemManager.PathModified"/> event.
	/// </summary>
	/// <param name="sender">The sender of the event</param>
	/// <param name="e">The event data</param>
	public delegate void PathModifiedEventHandler(object sender, PathModifiedEventArgs e);

	/// <summary>
	/// Represents the method that will handle the <see cref="FilesystemManager.SourcesModified"/> event.
	/// </summary>
	/// <param name="sender">The sender of the event</param>
	/// <param name="e">The event data</param>
	public delegate void SourceModifiedEventHandler(object sender, SourceModifiedEventArgs e);

	/// <summary>
	/// Represents the method that will handle the <see cref="FilesystemManager.SourceAdded"/> or <see cref="FilesystemManager.SourceRemoved"/> event.
	/// </summary>
	/// <param name="sender">The sender of the event</param>
	/// <param name="e">The event data</param>
	public delegate void SourcesModifiedEventHandler(object sender, SourcesModifiedEventArgs e);

	/// <summary>
	/// Represents the method that will be called when the scanner is finished.
	/// </summary>
	/// <param name="param">
	/// A list of three objects:
	///  * The ViewDetails that was dropped upon
	///  * The FileDropEventArgs containing the event data for the drop
	///  * The string of the path that was scanned
	///  </param>
	public delegate void ScannerCallback(object param);

	#endregion Delegates

	/// <summary>
	/// The task which a Janitor should perform
	/// </summary>
	public class JanitorTask
	{
		#region Properties

		/// <summary>
		/// The specific job that the Janitor should perform
		/// </summary>
		public JanitorJob Job { get; set; }

		/// <summary>
		/// The filename, path or library name on which the task should be performed
		/// </summary>
		public String Data { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Prints the job as a string
		/// </summary>
		/// <returns>A string describing the job</returns>
		public override string ToString()
		{
			return String.Format("'{0}' on {1}", Job, Data);
		}

		#endregion
	}

	/// <summary>
	/// The job of a janitor.
	/// This is used by a janitor to know what it should do when Clean() is called.
	/// </summary>
	public enum JanitorJob
	{
		/// <summary>
		/// A new Windows 7 Library has been created and needs to be scanned
		/// </summary>
		CreateLibrary,

		/// <summary>
		/// A Windows 7 Library was deleted
		/// </summary>
		DeleteLibrary,

		/// <summary>
		/// A Windows 7 Library was renamed
		/// </summary>
		RenameLibrary,

		/// <summary>
		/// A Windows 7 Library was changed and needs to be rescanned
		/// </summary>
		UpdateLibrary,

		/// <summary>
		/// A new file was created inside a watched folder
		/// </summary>
		CreateFile,

		/// <summary>
		/// A file was removed from a watched folder
		/// </summary>
		DeleteFile,

		/// <summary>
		/// A file was renamed inside a watched folder
		/// </summary>
		RenameFile,

		/// <summary>
		/// A file inside a watched folder was updated
		/// </summary>
		UpdateFile,

		/// <summary>
		/// A folder that resides inside a watched folder was created
		/// </summary>
		CreateFolder,

		/// <summary>
		/// A folder that resides inside a watched folder was removed
		/// </summary>
		DeleteFolder,

		/// <summary>
		/// A folder that resides inside a watched folder was renamed
		/// </summary>
		RenameFolder
	}

	/// <summary>
	/// The way that the source was modified
	/// </summary>
	public enum SourceModificationType
	{
		/// <summary>
		/// A track was added
		/// </summary>
		Added,

		/// <summary>
		/// A track was removed
		/// </summary>
		Removed,

		/// <summary>
		/// A track was updated
		/// </summary>
		Updated
	}

	/// <summary>
	/// A source where Stoffi should look for music
	/// </summary>
	public class SourceTree
	{
		#region Properties

		/// <summary>
		/// A list of child sources (only applicable on folders)
		/// </summary>
		public List<SourceTree> Children { get; set; }

		/// <summary>
		/// The filename or path
		/// </summary>
		public String Data { get; set; }

		/// <summary>
		/// Whether the file (or contents of the folder) should be included in the library
		/// </summary>
		public Boolean Include { get; set; }

		/// <summary>
		/// Whether the file (or contents of the folder) should not be included in the library
		/// </summary>
		public Boolean Ignore { get { return !Include; } set { Include = !value; } }

		#endregion
	}

	#region Event arguments

	/// <summary>
	/// Provides data for the <see cref="FilesystemManager.SourceModified"/> event
	/// </summary>
	public class SourceModifiedEventArgs : EventArgs
	{
		#region Properties

		/// <summary>
		/// Gets the track that was either added or removed
		/// </summary>
		public TrackData Track { get; private set; }

		/// <summary>
		/// Gets the modification type of the source
		/// </summary>
		public SourceModificationType ModificationType { get; private set; }

		/// <summary>
		/// A list of callbacks and their respective paramters.
		/// </summary>
		public List<KeyValuePair<ScannerCallback, object>> Callbacks { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="SourcesModifiedEventArgs"/> class
		/// </summary>
		/// <param name="track">The track that was either added or removed</param>
		/// <param name="modType">The modification type of the source</param>
		/// <param name="callback">The callbacks and their respective parameters which to call when the event is handled</param>
		public SourceModifiedEventArgs(TrackData track, SourceModificationType modType, List<KeyValuePair<ScannerCallback, object>> callbacks)
		{
			Track = track;
			ModificationType = modType;
			Callbacks = callbacks;
		}

		#endregion
	}

	/// <summary>
	/// Provides data for the <see cref="FilesystemManager.SourceAdded"/> or <see cref="FilesystemManager.SourceRemoved"/> event
	/// </summary>
	public class SourcesModifiedEventArgs : EventArgs
	{
		#region Properties

		/// <summary>
		/// Gets the source that was modified
		/// </summary>
		public SourceData Source { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="SourcesModifiedEventArgs"/> class
		/// </summary>
		/// <param name="source">The source that was modified</param>
		public SourcesModifiedEventArgs(SourceData source)
		{
			Source = source;
		}

		#endregion
	}

	/// <summary>
	/// Provides data for the <see cref="FilesystemManager.PathModified"/> event
	/// </summary>
	public class PathModifiedEventArgs : EventArgs
	{
		#region Properties

		/// <summary>
		/// Gets the path that was modified
		/// </summary>
		public String Path { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="PathModifiedEventArgs"/> class
		/// </summary>
		/// <param name="path">The path that was modified</param>
		public PathModifiedEventArgs(String path)
		{
			Path = path;
		}

		#endregion
	}

	#endregion
}
