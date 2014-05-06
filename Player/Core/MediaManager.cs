/**
 * MediaManager.cs
 * 
 * Handles playback of track and the play logic.
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

namespace Stoffi
{
	/// <summary>
	/// Represents a manager that takes care of handling playback
	/// </summary>
	public static class MediaManager
	{
		#region Members

		private static List<TrackData> songsAlreadyPlayed = new List<TrackData>();
		private static bool trackWasSkipped = false;
		private static int stream;
		private static TrackData loadedTrack = null;
		private static String supportedFormatsFilter;
		private static String supportedFormatsExtensions;
		private static bool isInitialized = false;
		private static int fxEchoHandle = 0;
		private static int fxEQHandle = 0;
		private static int fxCompHandle = 0;
		private static bool ignoreSeekChange = false;
		private static List<Thread> url_parse_threads = new List<Thread>();
		private static int tickInterval = 50; // ms
		private static BASSTimer ticker = null;
		private static SYNCPROC sync = null;
		private static int syncer = 0;
		private static bool stopTicker = false;
		private static readonly object locker = new object();

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the seek position.
		/// </summary>
		/// <remarks>
		/// When using this property, instead of SettingsManager.Seek, the MediaManager
		/// will not adjust its internal position in the playback stream.
		/// </remarks>
		public static double Seek
		{
			get { return SettingsManager.Seek; }
			set
			{
				ignoreSeekChange = true;
				SettingsManager.Seek = value;
				ignoreSeekChange = false;
			}
		}

		/// <summary>
		/// A float array of size 1024 containing FFT data points.
		/// </summary>
		public static float[] FFTData { get; set; }

		/// <summary>
		/// Gets or sets the callback used to invoke scripts in the browser for playing YouTube tracks.
		/// </summary>
		public static InvokeScriptDelegate InvokeScriptCallback { get; set; }

		/// <summary>
		/// Gets or sets the callback used for fetching the track collection used to determine the next
		/// track to play.
		/// </summary>
		public static FetchCollectionDelegate FetchCollectionCallback { get; set; }

		/// <summary>
		/// Gets the filter of all supported formats (to use with file dialogs)
		/// </summary>
		public static string SupportedFormatsFilter { get { return supportedFormatsFilter; } }

		/// <summary>
		/// Gets the extensions of all supported formats seperated by semicolon
		/// </summary>
		public static string SupportedFormatsExtensions { get { return supportedFormatsExtensions; } }

		/// <summary>
		/// Gets or sets whether the manager has been initialized or not
		/// </summary>
		public static bool IsInitialized { get { return isInitialized; } private set { isInitialized = value; } }

		/// <summary>
		/// Gets or sets the method that will determine if a certain track matches a given string
		/// </summary>
		public static SearchMatchDelegate SearchMatch { get; set; }

		/// <summary>
		/// Gets or sets the string that will filter out tracks
		/// </summary>
		public static string Filter { get; set; }
		
		/// <summary>
		/// Gets the position of the current track in seconds.
		/// </summary>
		public static double Position
		{
			get
			{
				if (SettingsManager.CurrentTrack != null && YouTubeManager.IsYouTube(SettingsManager.CurrentTrack.Path))
				{
					if (YouTubeManager.HasFlash)
					{
						object o = InvokeScript("getCurrentTime");
						return (o == null ? 0.0 : Convert.ToDouble(o));
					}
					else
						return 0.0;
				}

				if (stream == 0)
				{
					if (SettingsManager.CurrentTrack == null) return -1;
					else return (SettingsManager.Seek / 10) * Length;
				}

				long pos = Bass.BASS_ChannelGetPosition(stream); // position in bytes
				return Bass.BASS_ChannelBytes2Seconds(stream, pos);
			}
		}
		
		/// <summary>
		/// Gets the length of the current track in seconds.
		/// </summary>
		public static double Length
		{
			get
			{
				if (SettingsManager.CurrentTrack != null && YouTubeManager.IsYouTube(SettingsManager.CurrentTrack.Path))
				{
					if (YouTubeManager.HasFlash)
					{
						object o = InvokeScript("getDuration");
						return (o == null ? 0.0 : Convert.ToDouble(o));
					}
					else
						return 0.0;
				}

				if (stream == 0)
				{
					if (SettingsManager.CurrentTrack == null) return -1;
					else return SettingsManager.CurrentTrack.Length;
				}

				long len = Bass.BASS_ChannelGetLength(stream); // length in bytes
				return Bass.BASS_ChannelBytes2Seconds(stream, len);
			}
		}
		
		/// <summary>
		/// Gets the seconds left of the current track.
		/// </summary>
		public static double TimeLeft
		{
			get
			{
				if (stream == 0 && SettingsManager.CurrentTrack == null) return -1;
				return Length-Position;
			}
		}

		/// <summary>
		/// Gets the size of the buffered stream in fraction of 10 (10 = everything buffered).
		/// </summary>
		public static double Buffer
		{
			get
			{
				if (SettingsManager.CurrentTrack != null && 
					YouTubeManager.IsYouTube(SettingsManager.CurrentTrack.Path) && 
					YouTubeManager.HasFlash)
				{
					object fraction = InvokeScript("getFractionLoaded");
					if (fraction != null)
					{
						double f = Convert.ToDouble(fraction);
						return f * 10;
					}
				}
				return 0.0;
			}
		}

		#endregion

		#region Methods
   
		#region Public

		/// <summary>
		/// Initialize the manager
		/// </summary>
		public static void Initialize()
		{
			U.L(LogLevel.Debug, "MEDIA", "Init BASS");

			FFTData = new float[1024];
			BassNet.Registration("christoffer.brodd.reijer@gmail.com", "2X2313734152222");

			StartTicker();
			sync = new SYNCPROC(EndPosition);

			if (SettingsManager.HistoryIndex > SettingsManager.HistoryTracks.Count)
				SettingsManager.HistoryIndex = SettingsManager.HistoryTracks.Count - 1;

			Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_DEV_DEFAULT, true);
			Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_FLOATDSP, true);
			Bass.BASS_Init(-1, 44100, 0, (IntPtr)0);

			U.L(LogLevel.Debug, "MEDIA", "Load codecs");
			// load codecs
			string codecDir = Path.Combine(U.BasePath, "Codecs");
			Dictionary<int, string> loadedPlugins = Bass.BASS_PluginLoadDirectory(codecDir);
			supportedFormatsFilter = Utils.BASSAddOnGetSupportedFileFilter(loadedPlugins, "All supported audio files", true);
			supportedFormatsExtensions = Utils.BASSAddOnGetSupportedFileExtensions(loadedPlugins, true);

			// remove .mov and .mp4 cause they don't work anyway :(
			supportedFormatsExtensions = SupportedFormatsExtensions.Replace("*.mov;", "");
			supportedFormatsExtensions = SupportedFormatsExtensions.Replace("*.mp4;", "");
			IsInitialized = true;

			SettingsManager.PropertyChanged += SettingsManager_PropertyChanged;

			SettingsManager.MediaState = MediaState.Paused;
			loadedTrack = SettingsManager.CurrentTrack;

			// soundcloud doesn't support starting to stream from anything other than beginning
			switch (GetType(SettingsManager.CurrentTrack))
			{
				case TrackType.SoundCloud:
				case TrackType.WebRadio:
					SettingsManager.Seek = 0;
					break;
			}

			// seems we need to call this in order for the FX to be applied
			BassFx.BASS_FX_GetVersion();

			U.L(LogLevel.Debug, "MEDIA", "Initialized");
		}

		/// <summary>
		/// Updates the FFTData property.
		/// </summary>
		public static void UpdateFFTData()
		{
			if (stream != 0)
				Bass.BASS_ChannelGetData(stream, FFTData, (int)BASSData.BASS_DATA_FFT2048);
		}

		/// <summary>
		/// Loads a track to be played
		/// </summary>
		/// <param name="track">Track to be played</param>
		/// <param name="moveForward">If true then increase HistoryIndex (and potentially add track to History), otherwise decrease it</param>
		/// <seealso cref="Play"/>
		public static void Load(TrackData track, bool moveForward = true)
		{
			if (moveForward)
				DispatchLoadedTrack(track);
			else
				SettingsManager.HistoryIndex--;

			loadedTrack = track;
			if (SettingsManager.MediaState == MediaState.Playing)
				Start(track);
			else if (GetType(track) == TrackType.YouTube)
			{
				InvokeScript("cueNewVideo", new object[] { YouTubeManager.GetYouTubeID(track.Path), 0 });
			}
		}

		/// <summary>
		/// Plays the currently loaded track if there is one
		/// or continue playing current track if there is one
		/// </summary>
		public static void Play()
		{
			if (SettingsManager.MediaState == MediaState.Playing) return;

			if (loadedTrack != null)
				Start(loadedTrack);

			else if (SettingsManager.CurrentTrack != null)
			{
				if (YouTubeManager.IsYouTube(SettingsManager.CurrentTrack))
				{
					if (YouTubeManager.HasFlash)
						InvokeScript("play");
				}

				else if (stream != 0)
				{
					Bass.BASS_Start();
					SettingsManager.MediaState = MediaState.Playing;
				}
				else
				{
					Start(SettingsManager.CurrentTrack);
					return;
				}

				StartTicker();
				loadedTrack = null;
				DispatchStarted();
			}
		}

		/// <summary>
		/// Pauses playback
		/// </summary>
		public static void Pause()
		{
			if (SettingsManager.MediaState != MediaState.Paused)
			{
				SettingsManager.MediaState = MediaState.Paused;
				Bass.BASS_Pause();
				if (YouTubeManager.HasFlash)
					InvokeScript("pause");
			}
		}

		/// <summary>
		/// Stops playback
		/// </summary>
		public static void Stop()
		{
			SettingsManager.CurrentTrack = null;
			loadedTrack = null;
			if (YouTubeManager.HasFlash)
				InvokeScript("pause");
			SettingsManager.MediaState = MediaState.Stopped;
			SettingsManager.Seek = 0;
			Bass.BASS_Stop();
			Bass.BASS_StreamFree(stream);
			stream = 0;
		}

		/// <summary>
		/// Loads the next track and plays it
		/// </summary>
		/// <param name="ignoreSingleRepeat">Ignore repeating a single track</param>
		/// <param name="startPlayback">Starts the playback</param>
		/// <seealso cref="Shuffle"/>
		/// <seealso cref="Repeat"/>
		/// <seealso cref="HistoryIndex"/>
		/// <seealso cref="Queue"/>
		public static void Next(bool ignoreSingleRepeat = false, bool startPlayback = false)
		{
			try
			{
				// repeat current track?
				if (SettingsManager.Repeat == RepeatState.RepeatOne && SettingsManager.CurrentTrack != null && !ignoreSingleRepeat)
				{
					TrackData t = SettingsManager.CurrentTrack;
					Stop();
					Load(t);
					Play();
					return;
				}

				TrackData nextTrack = null;
				List<TrackData> trackCollection = FetchCollectionCallback();

				if (trackCollection == null)
				{
					Stop();
					return;
				}

				if (SettingsManager.CurrentTrack == null)
				{
					if (trackCollection.Count > 0)
					{
						nextTrack = (TrackData)trackCollection[0];
						SettingsManager.HistoryIndex = SettingsManager.HistoryTracks.Count;
					}
					else
						return;
				}

				// play track from history?
				if (SettingsManager.HistoryIndex < SettingsManager.HistoryTracks.Count - 1)
				{
					nextTrack = SettingsManager.HistoryTracks.ElementAt<TrackData>(SettingsManager.HistoryIndex + 1);
				}

				// play track from queue?
				else if (SettingsManager.QueueTracks.Count > 0)
					nextTrack = SettingsManager.QueueTracks.First<TrackData>();

				// play track from track collection
				else
				{
					if (trackCollection.Count < 1) // no track found, nothing more to do here...
					{
						Stop();
						return;
					}

					// apply search filter
					bool ApplySearch = (SearchMatch != null && Filter != null && Filter != "" && Filter != U.T("PlaybackSearch"));

					// remove all songs we have already played
					ObservableCollection<TrackData> tracksLeft = new ObservableCollection<TrackData>();
					foreach (TrackData t in trackCollection)
						if (!songsAlreadyPlayed.Contains(t) &&
							(!ApplySearch || SearchMatch(t, Filter)))
							tracksLeft.Add(t);

					if (tracksLeft.Count < 1) // we have played all songs
					{
						songsAlreadyPlayed.Clear();
						if (SettingsManager.Repeat == RepeatState.RepeatAll) // we have repeat on, so we add all track again and start over
						{
							foreach (TrackData t in trackCollection)
								if (!ApplySearch || SearchMatch(t, Filter))
									tracksLeft.Add(t);
						}
						else // repeat is off, so we stop playing
						{
							Stop();
							return;
						}
					}

					if (SettingsManager.Shuffle) // shuffle is on, so we find a random song
					{
						Random r = new Random();
						int x = r.Next(tracksLeft.Count - 1);
						nextTrack = tracksLeft.ElementAt<TrackData>(x);
					}
					else // shuffle is off, so we get the next song in the list
					{
						if (trackCollection.Count <= 0)
							return;

						// find CurrentTrack in TrackCollection (Contains() cannot be used since CurrentTrack may be a copy)
						int i = -1;
						if (SettingsManager.CurrentTrack != null)
						{
							foreach (TrackData t in trackCollection)
							{
								if (t.Path == SettingsManager.CurrentTrack.Path)
								{
									i = trackCollection.IndexOf(t);
									break;
								}
							}
						}

						if (SettingsManager.CurrentTrack != null && i >= 0)
						{
							if (i >= trackCollection.Count - 1)
								i = -1;
							nextTrack = (TrackData)trackCollection[i + 1];
						}
						else
							nextTrack = (TrackData)trackCollection[0];
					}
				}

				DispatchTrackSwitched(GetSourceTrack(SettingsManager.CurrentTrack), nextTrack);

				// if we are playing we start to play the next track
				if (SettingsManager.MediaState == MediaState.Playing || startPlayback)
				{
					Stop();
					Load(nextTrack);
					Play();
				}

				// otherwise we just change the track
				else
				{
					Load(nextTrack);
					SettingsManager.CurrentTrack = nextTrack;
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "MEDIA", "Could not select next track: " + e.Message);
				Stop();
			}
		}

		/// <summary>
		/// Loads the previous track in history and plays it
		/// </summary>
		/// <seealso cref="HistoryIndex"/>
		public static void Previous()
		{
			// jump to start of song if we have played more than 5% or at the last track
			if ((SettingsManager.MediaState == MediaState.Playing && stream != 0 && 
				(Bass.BASS_ChannelGetPosition(stream) / Bass.BASS_ChannelGetLength(stream)) > 0.05 && 
				SettingsManager.HistoryTracks.Count > 1) ||
				(SettingsManager.HistoryIndex <= 0 || SettingsManager.HistoryTracks.Count <= 0))
			{
				Bass.BASS_ChannelSetPosition(stream, 0);
			}

			// else we play previous in history
			else
			{
				if (SettingsManager.HistoryIndex > SettingsManager.HistoryTracks.Count - 1)
					SettingsManager.HistoryIndex = SettingsManager.HistoryTracks.Count - 1;

				TrackData prevTrack = SettingsManager.HistoryTracks.ElementAt<TrackData>(SettingsManager.HistoryIndex - 1);

				DispatchTrackSwitched(GetSourceTrack(SettingsManager.CurrentTrack), prevTrack);

				// if we are playing we start to play the previous track
				if (SettingsManager.MediaState == MediaState.Playing)
				{
					Stop();
					Load(prevTrack, false);
					Play();
				}

				// otherwise we just change the display
				else
				{
					SettingsManager.CurrentTrack = prevTrack;
					Load(prevTrack, false);
				}
			}
		}

		/// <summary>
		/// Adds tracks to the queue
		/// </summary>
		/// <param name="tracks">List of tracks to be added</param>
		/// <param name="pos">The position to insert the track at (-1 means at the end)</param>
		public static void Queue(List<TrackData> tracks, int pos = -1)
		{
			foreach (TrackData track in tracks)
			{
				U.L(LogLevel.Debug, "MEDIA", "Queue track: " + track.Path);
				if (!SettingsManager.QueueTracks.Contains(track))
				{
					if (pos >= 0 && pos < SettingsManager.QueueTracks.Count)
						SettingsManager.QueueTracks.Insert(pos, track);
					else
					{
						SettingsManager.QueueTracks.Add(track);
						track.Number = SettingsManager.QueueTracks.Count;
					}
				}
			}
			if (pos >= 0 && pos < SettingsManager.QueueTracks.Count)
				foreach (TrackData track in SettingsManager.QueueTracks)
					track.Number = SettingsManager.QueueTracks.IndexOf(track) + 1;
		}

		/// <summary>
		/// Remove tracks from the queue
		/// </summary>
		/// <param name="tracks">List of tracks to be removed</param>
		public static void Dequeue(List<TrackData> tracks)
		{
			foreach (TrackData track in tracks)
			{
				SettingsManager.QueueTracks.Remove(track);
			}

			foreach (TrackData track in SettingsManager.QueueTracks)
				track.Number = SettingsManager.QueueTracks.IndexOf(track) + 1;
		}

		/// <summary>
		/// Add the tracks that are not in queue and remove those that are
		/// </summary>
		/// <param name="tracks">List of tracks to be toggled</param>
		public static void ToggleQueue(List<TrackData> tracks)
		{
			foreach (TrackData track in tracks)
				if (SettingsManager.QueueTracks.Contains(track))
					SettingsManager.QueueTracks.Remove(track);
				else
					SettingsManager.QueueTracks.Add(track);

			foreach (TrackData track in SettingsManager.QueueTracks)
				track.Number = SettingsManager.QueueTracks.IndexOf(track) + 1;
		}

		/// <summary>
		/// Returns a localized string describing the type of the track
		/// </summary>
		/// <param name="track">The track</param>
        /// <param name="plural">Whether or not plural is used</param>
		/// <returns>A localized string describing the type of the track</returns>
		public static string HumanTrackType(TrackData track, bool plural = false)
		{
            string t = plural ? "Plural" : "Text";
			switch (GetType(track))
			{
				case TrackType.YouTube:
					return U.T("FileTypeYouTube", t);

				case TrackType.WebRadio:
					return U.T("FileTypeRadio", t);

				case TrackType.SoundCloud:
					return U.T("FileTypeSoundCloud", t);

				case TrackType.File:
					string ext = Path.GetExtension(track.Path).ToUpper().Substring(1);
					return String.Format(U.T("FileTypeExtension", t), ext);

				default:
					return U.T("FileTypeUnknown", t);
			}
		}

		/// <summary>
		/// Jumps to the next bookmark on the track if there is any
		/// </summary>
		public static void JumpToNextBookmark()
		{
			if (SettingsManager.CurrentTrack == null) return;

			TrackData t = GetLibrarySourceTrack(SettingsManager.CurrentTrack);
			if (t.Bookmarks == null || t.Bookmarks.Count <= 0) return;

			double pos = SettingsManager.Seek * 10;
			int i = -1;
			foreach (double b in t.Bookmarks)
			{
				if (b > pos)
				{
					i = t.Bookmarks.IndexOf(b);
					break;
				}
			}
			if (i >= 0)
				SettingsManager.Seek = t.Bookmarks[i] / 10;
		}

		/// <summary>
		/// Jump to the previous bookmark on the track if there is any
		/// </summary>
		public static void JumpToPreviousBookmark()
		{
			if (SettingsManager.CurrentTrack == null) return;

			TrackData t = GetLibrarySourceTrack(SettingsManager.CurrentTrack);
			if (t.Bookmarks == null || t.Bookmarks.Count <= 0) return;

			double pos = SettingsManager.Seek * 10;
			int i = -1;
			double margin = 2.0;

			foreach (double b in t.Bookmarks)
			{
				if (b > pos - margin)
				{
					i = t.Bookmarks.IndexOf(b) - 1;
					break;
				}
				else
					i++;
			}
			if (i >= 0)
				SettingsManager.Seek = t.Bookmarks[i] / 10;
		}

		/// <summary>
		/// Jumps to the last bookmark on the track if there are any
		/// </summary>
		public static void JumpToLastBookmark()
		{
			if (SettingsManager.CurrentTrack == null) return;

			TrackData t = GetLibrarySourceTrack(SettingsManager.CurrentTrack);
			if (t.Bookmarks == null || t.Bookmarks.Count <= 0) return;

			SettingsManager.Seek = t.Bookmarks.Last() / 10;
		}

		/// <summary>
		/// Jump to the first bookmark on the track if there are any
		/// </summary>
		public static void JumpToFirstBookmark()
		{
			if (SettingsManager.CurrentTrack == null) return;

			TrackData t = GetLibrarySourceTrack(SettingsManager.CurrentTrack);
			if (t.Bookmarks == null || t.Bookmarks.Count <= 0) return;

			SettingsManager.Seek = t.Bookmarks.First() / 10;
		}

		/// <summary>
		/// Jumps to a specific bookmark if such can be found
		/// </summary>
		/// <param name="n">The index of the bookmark</param>
		public static void JumpToBookmark(int n)
		{
			if (SettingsManager.CurrentTrack == null) return;

			TrackData t = GetLibrarySourceTrack(SettingsManager.CurrentTrack);
			
			if (t.Bookmarks == null || t.Bookmarks.Count < n) return;

			SettingsManager.Seek = t.Bookmarks[n - 1] / 10;
		}

		/// <summary>
		/// Creates a bookmark at the current position of the current track
		/// </summary>
		/// <returns>
		/// The position (in %) of the newly created bookmark
		/// or -1 if non were created.
		/// </returns>
		public static double CreateBookmark()
		{
			if (SettingsManager.CurrentTrack != null)
			{
				TrackData t = GetLibrarySourceTrack(SettingsManager.CurrentTrack);
				double pos = SettingsManager.Seek * 10;

				// check if bookmark is either too close to either start or end or another bookmark
				double margin = 1.4; // bookmark is 4px, min width of slide is 147px = 1.4% margin
				if (pos < margin || pos > 100 - margin) return -1;

				if (SettingsManager.CurrentTrack.Bookmarks == null)
					SettingsManager.CurrentTrack.Bookmarks = new List<double>();
				if (t.Bookmarks == null)
					t.Bookmarks = new List<double>();

				foreach (double b in SettingsManager.CurrentTrack.Bookmarks)
					if (b - margin < pos && pos < b + margin) return -1;

				int i = 0;
				foreach (double b in t.Bookmarks)
				{
					if (b > pos) break;
					else i++;
				}
				t.Bookmarks.Insert(i, pos);
				return pos;
			}
			return -1;
		}

		/// <summary>
		/// Remove a specific bookmark from the current track if such can be found
		/// </summary>
		/// <param name="pos">The position of the bookmark</param>
		public static void RemoveBookmark(double pos)
		{
			if (SettingsManager.CurrentTrack != null && SettingsManager.CurrentTrack.Bookmarks.Contains(pos))
				SettingsManager.CurrentTrack.Bookmarks.Remove(pos);
		}

		/// <summary>
		/// Check whether a file is supported by the media player
		/// </summary>
		/// <remarks>This function will stall until Initialized has been called</remarks>
		/// <param name="path">Filename of the track</param>
		/// <returns>True if the file is supported, otherwise false</returns>
		public static bool IsSupported(String path)
		{
			if (YouTubeManager.IsYouTube(path) || SoundCloudManager.IsSoundCloud(path)) return true;
			while (!isInitialized) ; // wait for initialization (WARNING: will hang until it's done!)
			string ext = "*" + Path.GetExtension(path).ToLower();
			if (ext == "*") return false;
			return (SupportedFormatsExtensions.StartsWith(ext) || SupportedFormatsExtensions.Contains(";" + ext));
		}

		/// <summary>
		/// Creates a track given a path
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>A track structure with meta data of the song</returns>
		public static TrackData GetTrack(string path)
		{
			switch (GetType(path))
			{
				case TrackType.File:
					return FilesystemManager.GetTrack(path);

				case TrackType.SoundCloud:
					return SoundCloudManager.CreateTrack(path);

				case TrackType.WebRadio:
					return ParseURL(path);

				case TrackType.YouTube:
					return YouTubeManager.CreateTrack(path);
			}
			return null;
		}

		/// <summary>
		/// Creates a track given a path
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>A track structure with meta data of the song</returns>
		public static TrackData CreateTrack(string path)
		{
			switch (GetType(path))
			{
				case TrackType.File:
					return FilesystemManager.CreateTrack(path);

				case TrackType.SoundCloud:
					return SoundCloudManager.CreateTrack(path);

				case TrackType.WebRadio:
					return ParseURL(path);

				case TrackType.YouTube:
					return YouTubeManager.CreateTrack(path);
			}
			return null;
		}

		/// <summary>
		/// Clear up all objects and timers
		/// </summary>
		public static void Clean()
		{
			StopTicker();
			Bass.BASS_Stop();
			Bass.BASS_Free();
		}

		/// <summary>
		/// Finds the original source track, inside the library or a playlist, from which a given track originates
		/// </summary>
		/// <param name="track">The track to find the source for</param>
		/// <returns>The track that was the original source for <b>track</b>, or <b>track</b> if no such track was found</returns>
		public static TrackData GetSourceTrack(TrackData track)
		{
			if (track == null) return null;

			if (track.Source != null && track.Source.StartsWith("Playlist:"))
			{
				PlaylistData p = PlaylistManager.FindPlaylist(track.Source.Split(new[]{':'},2)[1]);
				if (p != null)
					foreach (TrackData t in p.Tracks)
						if (t.Path == track.Path)
							return t;
			}

			ObservableCollection<TrackData> tracks = null;
			switch (MediaManager.GetType(track))
			{
				case TrackType.YouTube:
					tracks = YouTubeManager.TrackSource;
					break;

				case TrackType.SoundCloud:
					tracks = SoundCloudManager.TrackSource;
					break;

				case TrackType.WebRadio:
					tracks = SettingsManager.RadioTracks;
					break;

				case TrackType.File:
					tracks = SettingsManager.FileTracks;
					break;

			}

			if (tracks != null)
				for (int i = 0; i < tracks.Count; i++)
				{
					if (tracks[i].Path == track.Path)
						return tracks[i];
				}

   			return track;
		}

		/// <summary>
		/// Finds the track inside the library from which a given track originates
		/// </summary>
		/// <param name="track">The track find the library source for</param>
		/// <returns>The track that was the original source, inside the library, for <b>track</b>, or <b>track</b> if no such track was found</returns>
		public static TrackData GetLibrarySourceTrack(TrackData track)
		{
			foreach (TrackData t in SettingsManager.FileTracks)
				if (t.Path == track.Path)
					return t;

			return track;
		}

		/// <summary>
		/// Parses a URL and extract meta data.
		/// </summary>
		/// <param name="URL">The URL to parse</param>
		/// <returns>The track representing the audio at the URL</returns>
		public static TrackData ParseURL(string URL)
		{
			TrackData track = new TrackData()
			{
				PlayCount = 0,
				Source = "Radio",
				Icon = @"..\..\Platform\Windows 7\GUI\Images\Icons\Radio.ico",
			};
			track.Path = URL;

			int stream = Un4seen.Bass.Bass.BASS_StreamCreateURL(URL, 0, Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
			if (stream != 0)
			{
				//Un4seen.Bass.Bass.BASS_ChannelPlay(stream, true);
				string[] tags = Bass.BASS_ChannelGetTagsICY(stream);
				SortedList<string, string> meta = new SortedList<string, string>();
				if (tags != null && tags.Length > 0)
				{
					foreach (string tag in tags)
					{
						string[] s = tag.Split(new char[] { ':' }, 2);
						if (s.Length == 2)
							meta.Add(s[0], s[1]);
					}

					if (meta.Keys.Contains("icy-name"))
						track.Title = meta["icy-name"];

					if (meta.Keys.Contains("icy-genre"))
						track.Genre = meta["icy-genre"];

					if (meta.Keys.Contains("icy-url"))
						track.URL = meta["icy-url"];
				}
			}
			return track;
		}

		/// <summary>
		/// Parses a URL asynchronously and extracts meta data.
		/// </summary>
		/// <param name="URL">The URL to parse</param>
		/// <param name="callback">The function to call when the parsing is finished</param>
		/// <param name="restart">Whether or not to restart any current background parsing</param>
		public static void ParseURLAsync(string URL, ParsedURLDelegate callback, bool restart = false)
		{
			if (restart)
			{
				foreach (Thread t in url_parse_threads)
				{
					try { t.Abort(); }
					catch { }
				}
				url_parse_threads.Clear();
			}

			ThreadStart ParseThread = delegate()
			{
				TrackData track = ParseURL(URL);

				if (callback != null)
					callback(track);
			};
			Thread parse_thread = new Thread(ParseThread);
			parse_thread.Name = "URL Parser";
			parse_thread.IsBackground = true;
			parse_thread.Priority = ThreadPriority.Lowest;

			url_parse_threads.Add(parse_thread);

			parse_thread.Start();
		}

		/// <summary>
		/// Gets the type of a track.
		/// </summary>
		/// <param name="track">The track to get the type of</param>
		/// <returns>The type of the given track</returns>
		public static TrackType GetType(TrackData track)
		{
			if (track != null)
				return GetType(track.Path);
			else
				return TrackType.Unknown;
		}

		/// <summary>
		/// Gets the track type of a path.
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>The type of the given track path</returns>
		public static TrackType GetType(string path)
		{
			if (path.StartsWith("http://") ||
				path.StartsWith("https://"))
				return TrackType.WebRadio;

			else if (YouTubeManager.IsYouTube(path))
				return TrackType.YouTube;

			else if (SoundCloudManager.IsSoundCloud(path))
				return TrackType.SoundCloud;

			else
				return TrackType.File;
		}

		/// <summary>
		/// Sets the effects (volume, equalizer, echo, etc) on the audio channels.
		/// </summary>
		/// <param name="profile">The equalizer profile to use (defaults to the current in SettingsManager)</param>
		/// <remarks>
		/// This should only be called once for each play of a given stream. Subsequent calls will add gain values, not replace it.
		/// Use RefreshFX() to change gain values while a stream is active.
		/// </remarks>
		public static void SetFX(EqualizerProfile profile = null)
		{
			if (profile == null)
				profile = SettingsManager.CurrentEqualizerProfile;

			Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, (float)SettingsManager.Volume / 100);

			fxEQHandle = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_BFX_PEAKEQ, 2);
			BASS_BFX_PEAKEQ eq = new BASS_BFX_PEAKEQ();
			eq.fQ = 0f;
			eq.fBandwidth = 2.5f;
			eq.lChannel = BASSFXChan.BASS_BFX_CHANALL;

			float[] bands = new float[10] { 32f, 63f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f };
			for (int i = 0; i < bands.Count(); i++)
			{
				eq.lBand = i;
				eq.fCenter = bands[i];
				Bass.BASS_FXSetParameters(fxEQHandle, eq);
				SetEQ(i, profile.Levels[i]);
			}

			fxEchoHandle = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_DX8_ECHO, 1);
			SetEcho(profile.EchoLevel);

			fxCompHandle = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_BFX_COMPRESSOR2, 0);
			SetCompressor(1.0f, 10.0f, 0.3f);
		}

		/// <summary>
		/// Refreshes the effect on the currently active channel given an equalizer profile.
		/// </summary>
		/// <param name="profile">The equalizer profile to use (defaults to the current in SettingsManager)</param>
		public static void RefreshFX(EqualizerProfile profile = null)
		{
			if (profile == null)
				profile = SettingsManager.CurrentEqualizerProfile;
			SetEcho(profile.EchoLevel);
			for (int i = 0; i < profile.Levels.Count(); i++)
				SetEQ(i, profile.Levels[i]);
		}

		/// <summary>
		/// Sets a gain on an equalizer band.
		/// </summary>
		/// <param name="band">The band to apply gain to</param>
		/// <param name="gain">The gain to apply (in decibel)</param>
		public static void SetEQ(int band, float gain)
		{
			if (fxEQHandle == 0) return;
			BASS_BFX_PEAKEQ eq = new BASS_BFX_PEAKEQ();
			eq.lBand = band;
			if (Bass.BASS_FXGetParameters(fxEQHandle, eq))
			{
				eq.fGain = gain;
				Bass.BASS_FXSetParameters(fxEQHandle, eq);
			}
		}

		/// <summary>
		/// Sets the echo wet/dry mix.
		/// </summary>
		/// <param name="wetDryMix">The wet/dry mix (0-10 where 0 is dry)</param>
		public static void SetEcho(float wetDryMix)
		{
			if (fxEchoHandle == 0) return;
			BASS_DX8_ECHO echo = new BASS_DX8_ECHO();
			echo.fWetDryMix = wetDryMix/5;
			Bass.BASS_FXSetParameters(fxEchoHandle, echo);
		}

		/// <summary>
		/// Sets the compressor on the currently active stream.
		/// </summary>
		public static void SetCompressor(float attack, float release, float threshold, float gain = 0f, float ratio = 0f)
		{
			if (fxCompHandle == 0) return;
			BASS_BFX_COMPRESSOR2 comp = new BASS_BFX_COMPRESSOR2();
			if (Bass.BASS_FXGetParameters(fxCompHandle, comp))
			{
				//comp.fGain = gain;
				comp.fAttack = attack;
				comp.fRelease = release;
				comp.fThreshold = threshold;
				//comp.fRatio = ratio;
				Bass.BASS_FXSetParameters(fxCompHandle, comp);
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Attempts to invoke a script in the browser via the callback.
		/// </summary>
		/// <param name="function">The name of the function to invoke</param>
		/// <param name="param">Optional parameters to send to the function</param>
		/// <returns>The return value of the invoked function</returns>
		private static object InvokeScript(string function, object[] param = null)
		{
			try
			{
				if (InvokeScriptCallback != null)
					return InvokeScriptCallback(function, param);
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "MEDIA", "Could not invoke script in browser: " + e.Message);
			}
			return null;
		}

		/// <summary>
		/// Starts playback of the media player.
		/// </summary>
		/// <param name="track">The track to start playing</param>
		private static void Start(TrackData track)
		{
			SettingsManager.MediaState = MediaState.Playing;
			SettingsManager.CurrentTrack = track;

			//U.L(LogLevel.Information, "MEDIA", "Playing " + track.Path);
			ThreadStart PlayThread = delegate()
			{
				trackWasSkipped = false;

				// add song to played/recent list
				songsAlreadyPlayed.Add(track);
				TrackType type = GetType(track);

				bool skip = false;

				lock (locker)
				{
					if (stream != 0)
					{
						Bass.BASS_Stop();
						Bass.BASS_StreamFree(stream);
						stream = 0;
					}

					if (YouTubeManager.HasFlash)
						InvokeScript("pause");

					switch (type)
					{
						case TrackType.YouTube:
							string vid = YouTubeManager.GetYouTubeID(track.Path);
							InvokeScript("setVolume", new object[] { SettingsManager.Volume });
							InvokeScript("setQuality", new object[] { SettingsManager.YouTubeQuality });
							SettingsManager.MediaState = MediaState.Stopped;
							InvokeScript("loadNewVideo", new object[] { vid, 0 });
							break;

						case TrackType.File:
							stream = Bass.BASS_StreamCreateFile(track.Path, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);
							break;

						case TrackType.SoundCloud:
							stream = Bass.BASS_StreamCreateURL(SoundCloudManager.GetStreamURL(track), 0, BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
							break;

						case TrackType.WebRadio:
							stream = Bass.BASS_StreamCreateURL(track.Path, 0, BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
							break;

						default:
							U.L(LogLevel.Error, "MEDIA", "Unsupported track type: " + type);
							SettingsManager.CurrentTrack = track;
							skip = true;
							break;
					}

					if (stream != 0)
					{
						Bass.BASS_ChannelRemoveSync(stream, syncer);
						syncer = Bass.BASS_ChannelSetSync(stream, BASSSync.BASS_SYNC_END, 0, sync, IntPtr.Zero);

						if (type == TrackType.File)
						{
							double p = SettingsManager.Seek;
							Bass.BASS_ChannelSetPosition(stream, (long)((p / 10.0) * Bass.BASS_ChannelGetLength(stream)));
						}

						Bass.BASS_Start();
						Bass.BASS_ChannelPlay(stream, false);

						// set some fx
						SetFX();
					}

					if (stream == 0 && type != TrackType.YouTube)
					{
						U.L(LogLevel.Error, "MEDIA", "Could not start track: " + track.Path);
						skip = true;
					}
					else
					{
						loadedTrack = null;
						PlaylistManager.TrackWasPlayed(SettingsManager.CurrentTrack);
						DispatchStarted();
					}
				}

				if (skip)
					Next(false, true);
			};
			Thread play_thread = new Thread(PlayThread);
			play_thread.Name = "Playback";
			play_thread.IsBackground = true;
			play_thread.Priority = ThreadPriority.BelowNormal;
			play_thread.Start();
		}

		/// <summary>
		/// Callback for when the stream reaches the end.
		/// </summary>
		/// <param name="handle">TODO</param>
		/// <param name="channel">TODO</param>
		/// <param name="data">TODO</param>
		/// <param name="user">TODO</param>
		private static void EndPosition(int handle, int channel, int data, IntPtr user)
		{
			SettingsManager.MediaState = MediaState.Ended;
		}

		/// <summary>
		/// Stops the ticker which calls the Tick method.
		/// </summary>
		private static void StopTicker()
		{
			stopTicker = true;
		}

		/// <summary>
		/// Starts the ticker which calls the Tick method.
		/// </summary>
		private static void StartTicker()
		{
			stopTicker = false;
			if (ticker == null)
			{
				ticker = new BASSTimer(tickInterval);
				ticker.Tick += new EventHandler(Tick);
				ticker.Start();
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when a property of the settings manager is changed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Seek":
					if (ignoreSeekChange) break;
					if (Double.IsNaN(SettingsManager.Seek)) return;

					if ((double)e.NewValue - (double)e.OldValue > 0) trackWasSkipped = true;

					if (YouTubeManager.IsYouTube(SettingsManager.CurrentTrack))
					{
						if (YouTubeManager.HasFlash)
						{
							bool playing = SettingsManager.MediaState == MediaState.Playing;
							double pos = (SettingsManager.Seek / 10.0) * Length;
							InvokeScript("seekTo", new object[] { pos }); // this will change media state to playing
							if (!playing) InvokeScript("pause");
						}
					}
					else
					{
						if (stream == 0) return;
						double per = SettingsManager.Seek / 10.0;
						long pos = (long)(per * Bass.BASS_ChannelGetLength(stream));
						Bass.BASS_ChannelSetPosition(stream, pos);
					}
					break;

				case "Volume":
					if (YouTubeManager.IsYouTube(SettingsManager.CurrentTrack))
					{
						if (YouTubeManager.HasFlash)
							InvokeScript("setVolume", new object[] { SettingsManager.Volume });
					}
					else
					{
						if (stream == 0) return;
						Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, (float)SettingsManager.Volume / 100);
					}
					break;

				case "YouTubeQuality":
					InvokeScript("setQuality", new object[] { SettingsManager.YouTubeQuality });
					break;

				case "CurrentTrack":
					TrackData t = (TrackData)e.OldValue;
					if (t != null)
					{
						TrackData sourceTrack = GetSourceTrack(t);
						sourceTrack.IsActive = false;
					}

					t = SettingsManager.CurrentTrack;
					if (t != null)
					{
						TrackData sourceTrack = GetSourceTrack(t);
						sourceTrack.IsActive = true;
					}
					break;

				case "HistoryIndex":
					TrackData oldTrack = null;
					TrackData newTrack = null;

					int oldIndex = (int)e.OldValue - 1;
					int newIndex = (int)e.NewValue - 1;

					if (oldIndex >= SettingsManager.HistoryTracks.Count)
						oldIndex = SettingsManager.HistoryTracks.Count - 1;
					else if (oldIndex == SettingsManager.HistoryTracks.Count - 1 &&
						newIndex == SettingsManager.HistoryTracks.Count)
						oldIndex = SettingsManager.HistoryTracks.Count - 2;

					if (newIndex >= SettingsManager.HistoryTracks.Count)
						newIndex = SettingsManager.HistoryTracks.Count - 1;

					if (SettingsManager.HistoryTracks.Count > oldIndex &&
						oldIndex >= 0)
						oldTrack = SettingsManager.HistoryTracks[oldIndex];

					if (SettingsManager.HistoryTracks.Count > newIndex &&
						newIndex >= 0)
						newTrack = SettingsManager.HistoryTracks[newIndex];

					if (oldTrack != null)
						oldTrack.IsActive = false;

					if (newTrack != null)
						newTrack.IsActive = true;
					break;

				case "MediaState":
					switch (SettingsManager.MediaState)
					{
						case MediaState.Paused:
						case MediaState.Stopped:
							// keep the ticker on if a youtube track is loaded (need it to see buffer size changes)
							if (GetType(SettingsManager.CurrentTrack) != TrackType.YouTube)
								StopTicker();
							break;

						case MediaState.Playing:
							// don't waste CPU cycles with ticker if streaming radio
							if (GetType(SettingsManager.CurrentTrack) != TrackType.WebRadio)
								StartTicker();
							break;

						case MediaState.Ended:
							if (SettingsManager.PauseWhenSongEnds)
							{
								Pause();
								SettingsManager.Seek = 0;
								Next(false, false);
							}
							else
								Next(false, true);
							break;
					}
					break;

				case "CurrentEqualizerProfile":
					RefreshFX();
					break;
			}
		}

		/// <summary>
		/// Invoked at every tick of the ticker.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Tick(object sender, EventArgs e)
		{
			if (stopTicker) return;
			try
			{
				if (SettingsManager.CurrentTrack != null)
				{
					bool youtube = YouTubeManager.IsYouTube(SettingsManager.CurrentTrack);
					object ps = null;
					BASSActive active = Bass.BASS_ChannelIsActive(stream);
					if (youtube && YouTubeManager.HasFlash)
						ps = InvokeScript("getPlayerState");

					if (youtube && ps == null) return;
					if ((youtube && (int)ps != 0) || active == BASSActive.BASS_ACTIVE_PLAYING)
					{
						double pos = Position;
						double len = Length;
						if (pos < 0) pos = 0;
						if (len < 0) len = 0;
						double seek = (pos / len) * 10;
						if (Double.IsNaN(seek) || Double.IsInfinity(seek)) seek = 0;
						ignoreSeekChange = true;
						SettingsManager.Seek = seek;
						ignoreSeekChange = false;

						// update FFT data points
						if (stream != 0)
							Bass.BASS_ChannelGetData(stream, FFTData, (int)BASSData.BASS_DATA_FFT2048);

						if (youtube && YouTubeManager.HasFlash)
						{
							double b = Buffer;
							SettingsManager.BufferSize = b;

							// shut down ticker if we have full buffer
							if (SettingsManager.MediaState != MediaState.Playing && b >= 10)
								StopTicker();
						}
					}
					else if (SettingsManager.MediaState == MediaState.Playing)
					{
						//if (active != BASSActive.BASS_ACTIVE_STALLED)
						//    SettingsManager.MediaState = MediaState.Ended;
					}
				}
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "PLAYBACK", "Could not update seeker: " + exc.Message);
			}
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// The dispatcher of the <see cref="TrackSwitched"/> event.
		/// </summary>
		/// <param name="oldTrack">The track before the switch</param>
		/// <param name="newTrack">The track after the switch</param>
		private static void DispatchTrackSwitched(TrackData oldTrack, TrackData newTrack)
		{
			if (TrackSwitched != null)
				TrackSwitched(new TrackSwitchedEventArgs(oldTrack, newTrack));
		}

		/// <summary>
		/// The dispatcher of the <see cref="LoadedTrack"/> event.
		/// </summary>
		/// <param name="track">The track that was loaded</param>
		private static void DispatchLoadedTrack(TrackData track)
		{
			if (LoadedTrack != null)
				LoadedTrack(track);
		}

		/// <summary>
		/// The dispatcher of the <see cref="Started"/> event.
		/// </summary>
		private static void DispatchStarted()
		{
			if (Started != null)
				Started(null, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the media player switches tracks.
		/// </summary>
		public static event TrackSwitchedEventHandler TrackSwitched;

		/// <summary>
		/// Occurs when the a new track is loaded.
		/// </summary>
		/// <remarks>
		/// Only occurs when a new track is loaded, that is one which
		/// moves the history index forward.
		/// When this event occurs the track should be added to a history
		/// list.
		/// </remarks>
		public static event LoadedTrackDelegate LoadedTrack;

		/// <summary>
		/// Occurs when the playback has started.
		/// </summary>
		/// <remarks>
		/// This may not mean that there will be audio since something
		/// could have gone wrong when playing the track. This just
		/// means that the attempt to start playback is finished, no
		/// matter the outcome.
		/// </remarks>
		public static event EventHandler Started;

		#endregion
	}

	#region Delegates

	/// <summary>
	/// Represents the method that is called when the 
	/// MediaManager.StateChanged event occurs
	/// </summary>
	/// <param name="e">The event data</param>
	public delegate void MediaStateEventHandler(MediaStateEventArgs e);

	/// <summary>
	/// Represents the method that is called when the
	/// MediaManager.TrackSwitched event occurs
	/// </summary>
	/// <param name="e">The event data</param>
	public delegate void TrackSwitchedEventHandler(TrackSwitchedEventArgs e);

	/// <summary>
	/// Represents the method that will determine whether an item matches a filter string or not
	/// </summary>
	/// <param name="track">The track which should be examined</param>
	/// <param name="filterString">The string which should be matched</param>
	/// <returns>True if the track matches the string, otherwise false</returns>
	public delegate bool SearchMatchDelegate(TrackData track, string filterString);

	/// <summary>
	/// Represents the method that 
	/// </summary>
	/// <param name="function"></param>
	/// <param name="param"></param>
	/// <returns></returns>
	public delegate object InvokeScriptDelegate(string function, object[] param = null);

	/// <summary>
	/// Represents the method that is called when the
	/// MediaManager.NavigateBrowser event occurs.
	/// </summary>
	/// <param name="source">The URI to navigate to</param>
	public delegate void NavigateBrowserDelegate(Uri source);

	/// <summary>
	/// Represents the method that is called when the
	/// MediaManager.LoadedTrack event occurs.
	/// </summary>
	/// <param name="track">The track that was loaded</param>
	public delegate void LoadedTrackDelegate(TrackData track);

	/// <summary>
	/// Represents the method that is called when a URL has been
	/// parsed successfully.
	/// </summary>
	/// <param name="track">The track representing the audio at the URL</param>
	public delegate void ParsedURLDelegate(TrackData track);

	/// <summary>
	/// Represents the callback method for retrieving the current track collection
	/// used by the media manager to determine the next track to play.
	/// </summary>
	/// <returns>The list of tracks to choose the next track from</returns>
	public delegate List<TrackData> FetchCollectionDelegate();

	#endregion

	#region Event arguments

	/// <summary>
	/// Provides data for the MediaManager.StateChanged event
	/// </summary>
	public class MediaStateEventArgs
	{
		/// <summary>
		/// Gets or sets the state of the media player
		/// </summary>
		public MediaState State { get; set; }

		/// <summary>
		/// Creates an instance of the MediaStateEventArgs class
		/// </summary>
		/// <param name="state">The state of the media player</param>
		public MediaStateEventArgs(MediaState state)
		{
			State = state;
		}
	}

	/// <summary>
	/// Provides data for the TrackSwitched event
	/// </summary>
	public class TrackSwitchedEventArgs
	{
		/// <summary>
		/// Gets or sets the old track
		/// </summary>
		public TrackData OldTrack { get; set; }

		/// <summary>
		/// Gets or sets the new track
		/// </summary>
		public TrackData NewTrack { get; set; }

		/// <summary>
		/// Creates an instance of the TrackSwitchedEventArgs class
		/// </summary>
		/// <param name="oldTrack">The track before the switch</param>
		/// <param name="newTrack">The track after the switch</param>
		public TrackSwitchedEventArgs(TrackData oldTrack, TrackData newTrack)
		{
			OldTrack = oldTrack;
			NewTrack = newTrack;
		}
	}

	#endregion

	#region Enums

	/// <summary>
	/// Represents a type of a track.
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
		/// An unknown track type
		/// </summary>
		Unknown
	}

	#endregion
}