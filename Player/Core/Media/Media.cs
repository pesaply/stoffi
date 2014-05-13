/**
 * Media.cs
 * 
 * Handles playback of track and the play logic.
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
using System.Reflection;
using System.Text;
using System.Threading;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;

using Stoffi.Core.Playlists;
using Stoffi.Core.Settings;
using Stoffi.Core.Sources;

namespace Stoffi.Core.Media
{
	/// <summary>
	/// Represents a manager that takes care of handling playback
	/// </summary>
	public static class Manager
	{
		#region Members

		private static List<Track> songsAlreadyPlayed = new List<Track>();
		private static bool trackWasSkipped = false;
		private static int stream;
		private static Track loadedTrack = null;
		private static String supportedFormatsFilter;
		private static String supportedFormatsExtensions;
		private static bool isInitialized = false;
		private static int fxEchoHandle = 0;
		private static int fxEQHandle = 0;
		private static int fxCompHandle = 0;
		//private static bool ignoreSeekChange = false;
		private static List<Thread> url_parse_threads = new List<Thread>();
		private static int tickInterval = 200; // ms
		private static BASSTimer ticker = null;
		private static SYNCPROC sync = null;
		private static int syncer = 0;
		private static bool stopTicker = false;
		private static readonly object locker = new object();
		private static bool isTicking = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the seek position.
		/// </summary>
		/// <remarks>
		/// Use this instead of Settings.Manager.Seek to force the
		/// seek of the playback to change.
		/// </remarks>
		public static double Seek
		{
			get { return Settings.Manager.Seek; }
			set
			{
				var oldValue = Settings.Manager.Seek;
				var newValue = value;
				if (Double.IsNaN(value)) return;

				if ((double)newValue - (double)oldValue > 0) trackWasSkipped = true;

				if (Settings.Manager.CurrentTrack.Type == TrackType.YouTube)
				{
					if (Sources.Manager.YouTube.HasFlash)
					{
						bool playing = Settings.Manager.MediaState == MediaState.Playing;
						double pos = (value / 10.0) * Length;
						InvokeScript("seekTo", new object[] { pos }); // this will change media state to playing
						if (!playing) InvokeScript("pause");
					}
				}
				else
				{
					if (stream == 0) return;
					double per = value / 10.0;
					long pos = (long)(per * Bass.BASS_ChannelGetLength(stream));
					Bass.BASS_ChannelSetPosition(stream, pos);
				}
				//ignoreSeekChange = true;
				Settings.Manager.Seek = value;
				//ignoreSeekChange = false;
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
				if (Settings.Manager.CurrentTrack != null && Settings.Manager.CurrentTrack.Type == TrackType.YouTube)
				{
					if (Sources.Manager.YouTube.HasFlash)
					{
						try {
							return Convert.ToDouble(InvokeScript("getCurrentTime"));
						}
						catch {
							return 0.0;
						}
					}
					else
						return 0.0;
				}

				if (stream == 0)
				{
					if (Settings.Manager.CurrentTrack == null) return -1;
					else return (Settings.Manager.Seek / 10) * Length;
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
				if (Settings.Manager.CurrentTrack != null && Settings.Manager.CurrentTrack.Type == TrackType.YouTube)
				{
					if (Sources.Manager.YouTube.HasFlash)
					{
						try {
							return Convert.ToDouble(InvokeScript("getDuration"));
						}
						catch {
							return 0.0;
						}
					}
					else
						return 0.0;
				}

				if (stream == 0)
				{
					if (Settings.Manager.CurrentTrack == null) return -1;
					else return Settings.Manager.CurrentTrack.Length;
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
				if (stream == 0 && Settings.Manager.CurrentTrack == null) return -1;
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
				if (Settings.Manager.CurrentTrack != null && 
					Settings.Manager.CurrentTrack.Type == TrackType.YouTube && 
					Sources.Manager.YouTube.HasFlash)
				{
					try{
						object fraction = InvokeScript("getFractionLoaded");
						if (fraction != null)
						{
							double f = Convert.ToDouble(fraction);
							return f * 10;
						}
					}
					catch {
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
			BassNet.Registration(U.BassMail, U.BassKey);

			StartTicker();
			sync = new SYNCPROC(EndPosition);

			if (Settings.Manager.HistoryIndex > Settings.Manager.HistoryTracks.Count)
				Settings.Manager.HistoryIndex = Settings.Manager.HistoryTracks.Count - 1;

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

			Settings.Manager.PropertyChanged += Settings_PropertyChanged;

			Settings.Manager.MediaState = MediaState.Paused;
			loadedTrack = Settings.Manager.CurrentTrack;

			// soundcloud doesn't support starting to stream from anything other than beginning
			if (Settings.Manager.CurrentTrack != null)
				switch (Settings.Manager.CurrentTrack.Type)
				{
					case TrackType.SoundCloud:
					case TrackType.WebRadio:
						Settings.Manager.Seek = 0;
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
		public static void Load(Track track, bool moveForward = true)
		{
			if (moveForward)
				DispatchLoadedTrack(track);
			else
				Settings.Manager.HistoryIndex--;

			loadedTrack = track;
			if (Settings.Manager.MediaState == MediaState.Playing)
				Start(track);
			else if (track.Type == TrackType.YouTube)
			{
				InvokeScript("cueNewVideo", new object[] { Sources.Manager.YouTube.GetID(track.Path), 0 });
			}
		}

		/// <summary>
		/// Play the specified track.
		/// </summary>
		/// <param name="track">Track.</param>
		public static void Play(Track track)
		{
			Load (track);
			Play ();
		}

		/// <summary>
		/// Plays the currently loaded track if there is one
		/// or continue playing current track if there is one
		/// </summary>
		public static void Play()
		{
			if (Settings.Manager.MediaState == MediaState.Playing) return;

			if (loadedTrack != null)
				Start(loadedTrack);

			else if (Settings.Manager.CurrentTrack != null)
			{
				if (Settings.Manager.CurrentTrack.Type == TrackType.YouTube)
				{
					if (Sources.Manager.YouTube.HasFlash)
						InvokeScript("play");
				}

				else if (stream != 0)
				{
					Bass.BASS_Start();
					Settings.Manager.MediaState = MediaState.Playing;
				}
				else
				{
					Start(Settings.Manager.CurrentTrack);
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
			if (Settings.Manager.MediaState != MediaState.Paused)
			{
				Settings.Manager.MediaState = MediaState.Paused;
				Bass.BASS_Pause();
				if (Sources.Manager.YouTube.HasFlash)
					InvokeScript("pause");
			}
		}

		/// <summary>
		/// Stops playback
		/// </summary>
		public static void Stop()
		{
			Settings.Manager.CurrentTrack = null;
			loadedTrack = null;
			if (Sources.Manager.YouTube.HasFlash)
				InvokeScript("pause");
			Settings.Manager.MediaState = MediaState.Stopped;
			Settings.Manager.Seek = 0;
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
				if (Settings.Manager.Repeat == RepeatState.RepeatOne && Settings.Manager.CurrentTrack != null && !ignoreSingleRepeat)
				{
					Track t = Settings.Manager.CurrentTrack;
					Stop();
					Load(t);
					Play();
					return;
				}

				Track nextTrack = null;
				List<Track> trackCollection = FetchCollectionCallback();

				if (trackCollection == null)
				{
					Stop();
					return;
				}

				if (Settings.Manager.CurrentTrack == null)
				{
					if (trackCollection.Count > 0)
					{
						nextTrack = (Track)trackCollection[0];
						Settings.Manager.HistoryIndex = Settings.Manager.HistoryTracks.Count;
					}
					else
						return;
				}

				// play track from history?
				if (Settings.Manager.HistoryIndex < Settings.Manager.HistoryTracks.Count - 1)
				{
					nextTrack = Settings.Manager.HistoryTracks.ElementAt<Track>(Settings.Manager.HistoryIndex + 1);
				}

				// play track from queue?
				else if (Settings.Manager.QueueTracks.Count > 0)
					nextTrack = Settings.Manager.QueueTracks.First<Track>();

				// play track from track collection
				else
				{
					if (trackCollection.Count < 1) // no track found, nothing more to do here...
					{
						Stop();
						return;
					}

					// apply search filter
					bool ApplySearch = (Filter != null && Filter != "" && Filter != U.T("PlaybackSearch"));

					// remove all songs we have already played
					ObservableCollection<Track> tracksLeft = new ObservableCollection<Track>();
					foreach (Track t in trackCollection)
						if (!songsAlreadyPlayed.Contains(t) &&
						    (!ApplySearch || U.TrackMatchesQuery(t, Filter)))
							tracksLeft.Add(t);

					if (tracksLeft.Count < 1) // we have played all songs
					{
						songsAlreadyPlayed.Clear();
						if (Settings.Manager.Repeat == RepeatState.RepeatAll) // we have repeat on, so we add all track again and start over
						{
							foreach (Track t in trackCollection)
								if (!ApplySearch || U.TrackMatchesQuery(t, Filter))
									tracksLeft.Add(t);
						}
						else // repeat is off, so we stop playing
						{
							Stop();
							return;
						}
					}

					if (Settings.Manager.Shuffle) // shuffle is on, so we find a random song
					{
						Random r = new Random();
						int x = r.Next(tracksLeft.Count - 1);
						nextTrack = tracksLeft.ElementAt<Track>(x);
					}
					else // shuffle is off, so we get the next song in the list
					{
						if (trackCollection.Count <= 0)
							return;

						// find CurrentTrack in TrackCollection (Contains() cannot be used since CurrentTrack may be a copy)
						int i = -1;
						if (Settings.Manager.CurrentTrack != null)
						{
							foreach (Track t in trackCollection)
							{
								if (t.Path == Settings.Manager.CurrentTrack.Path)
								{
									i = trackCollection.IndexOf(t);
									break;
								}
							}
						}

						if (Settings.Manager.CurrentTrack != null && i >= 0)
						{
							if (i >= trackCollection.Count - 1)
								i = -1;
							nextTrack = (Track)trackCollection[i + 1];
						}
						else
							nextTrack = (Track)trackCollection[0];
					}
				}

				DispatchTrackSwitched(GetSourceTrack(Settings.Manager.CurrentTrack), nextTrack);

				// if we are playing we start to play the next track
				if (Settings.Manager.MediaState == MediaState.Playing || startPlayback)
				{
					Stop();
					Load(nextTrack);
					Play();
				}

				// otherwise we just change the track
				else
				{
					Load(nextTrack);
					Settings.Manager.CurrentTrack = nextTrack;
				}
			}
			catch (WebException e)
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
			if ((Settings.Manager.MediaState == MediaState.Playing && stream != 0 && 
				(Bass.BASS_ChannelGetPosition(stream) / Bass.BASS_ChannelGetLength(stream)) > 0.05 && 
				Settings.Manager.HistoryTracks.Count > 1) ||
				(Settings.Manager.HistoryIndex <= 0 || Settings.Manager.HistoryTracks.Count <= 0))
			{
				Bass.BASS_ChannelSetPosition(stream, 0);
			}

			// else we play previous in history
			else
			{
				if (Settings.Manager.HistoryIndex > Settings.Manager.HistoryTracks.Count - 1)
					Settings.Manager.HistoryIndex = Settings.Manager.HistoryTracks.Count - 1;

				Track prevTrack = Settings.Manager.HistoryTracks.ElementAt<Track>(Settings.Manager.HistoryIndex - 1);

				DispatchTrackSwitched(GetSourceTrack(Settings.Manager.CurrentTrack), prevTrack);

				// if we are playing we start to play the previous track
				if (Settings.Manager.MediaState == MediaState.Playing)
				{
					Stop();
					Load(prevTrack, false);
					Play();
				}

				// otherwise we just change the display
				else
				{
					Settings.Manager.CurrentTrack = prevTrack;
					Load(prevTrack, false);
				}
			}
		}

		/// <summary>
		/// Adds tracks to the queue
		/// </summary>
		/// <param name="tracks">List of tracks to be added</param>
		/// <param name="pos">The position to insert the track at (-1 means at the end)</param>
		public static void Queue(List<Track> tracks, int pos = -1)
		{
			foreach (Track track in tracks)
			{
				U.L(LogLevel.Debug, "MEDIA", "Queue track: " + track.Path);
				if (!Settings.Manager.QueueTracks.Contains(track))
				{
					if (pos >= 0 && pos < Settings.Manager.QueueTracks.Count)
						Settings.Manager.QueueTracks.Insert(pos, track);
					else
					{
						Settings.Manager.QueueTracks.Add(track);
						track.Number = Settings.Manager.QueueTracks.Count;
					}
				}
			}
			if (pos >= 0 && pos < Settings.Manager.QueueTracks.Count)
				foreach (Track track in Settings.Manager.QueueTracks)
					track.Number = Settings.Manager.QueueTracks.IndexOf(track) + 1;
		}

		/// <summary>
		/// Remove tracks from the queue
		/// </summary>
		/// <param name="tracks">List of tracks to be removed</param>
		public static void Dequeue(IEnumerable<Track> tracks)
		{
			foreach (Track track in tracks)
			{
				Settings.Manager.QueueTracks.Remove(track);
			}

			foreach (Track track in Settings.Manager.QueueTracks)
				track.Number = Settings.Manager.QueueTracks.IndexOf(track) + 1;
		}

		/// <summary>
		/// Add the tracks that are not in queue and remove those that are
		/// </summary>
		/// <param name="tracks">List of tracks to be toggled</param>
		public static void ToggleQueue(List<Track> tracks)
		{
			foreach (Track track in tracks)
				if (Settings.Manager.QueueTracks.Contains(track))
					Settings.Manager.QueueTracks.Remove(track);
				else
					Settings.Manager.QueueTracks.Add(track);

			foreach (Track track in Settings.Manager.QueueTracks)
				track.Number = Settings.Manager.QueueTracks.IndexOf(track) + 1;
		}

		/// <summary>
		/// Returns a localized string describing the type of the track
		/// </summary>
		/// <param name="track">The track</param>
        /// <param name="plural">Whether or not plural is used</param>
		/// <returns>A localized string describing the type of the track</returns>
		public static string HumanTrackType(Track track, bool plural = false)
		{
            string t = plural ? "Plural" : "Text";
			switch (track.Type)
			{
				case TrackType.YouTube:
					return U.T("FileTypeYouTube", t);

				case TrackType.WebRadio:
					return U.T("FileTypeRadio", t);

				case TrackType.SoundCloud:
					return U.T("FileTypeSoundCloud", t);

				case TrackType.Jamendo:
					return U.T("FileTypeJamendo", t);

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
			if (Settings.Manager.CurrentTrack == null) return;

			Track t = GetLibrarySourceTrack(Settings.Manager.CurrentTrack);
			if (t.Bookmarks == null || t.Bookmarks.Count <= 0) return;

			double pos = Settings.Manager.Seek * 10;
			int i = -1;
			foreach (var b in t.Bookmarks)
			{
				if (b.Item2 > pos)
				{
					i = t.Bookmarks.IndexOf(b);
					break;
				}
			}
			if (i >= 0)
				Settings.Manager.Seek = t.Bookmarks[i].Item2 / 10;
		}

		/// <summary>
		/// Jump to the previous bookmark on the track if there is any
		/// </summary>
		public static void JumpToPreviousBookmark()
		{
			if (Settings.Manager.CurrentTrack == null) return;

			Track t = GetLibrarySourceTrack(Settings.Manager.CurrentTrack);
			if (t.Bookmarks == null || t.Bookmarks.Count <= 0) return;

			double pos = Settings.Manager.Seek * 10;
			int i = -1;
			double margin = 2.0;

			foreach (var b in t.Bookmarks)
			{
				if (b.Item2 > pos - margin)
				{
					i = t.Bookmarks.IndexOf(b) - 1;
					break;
				}
				else
					i++;
			}
			if (i >= 0)
				Settings.Manager.Seek = t.Bookmarks[i].Item2 / 10;
		}

		/// <summary>
		/// Jumps to the last bookmark on the track if there are any
		/// </summary>
		public static void JumpToLastBookmark()
		{
			if (Settings.Manager.CurrentTrack == null) return;

			Track t = GetLibrarySourceTrack(Settings.Manager.CurrentTrack);
			if (t.Bookmarks == null || t.Bookmarks.Count <= 0) return;

			Settings.Manager.Seek = t.Bookmarks.Last().Item2 / 10;
		}

		/// <summary>
		/// Jump to the first bookmark on the track if there are any
		/// </summary>
		public static void JumpToFirstBookmark()
		{
			if (Settings.Manager.CurrentTrack == null) return;

			Track t = GetLibrarySourceTrack(Settings.Manager.CurrentTrack);
			if (t.Bookmarks == null || t.Bookmarks.Count <= 0) return;

			Settings.Manager.Seek = t.Bookmarks.First().Item2 / 10;
		}

		/// <summary>
		/// Jumps to a specific bookmark if such can be found
		/// </summary>
		/// <param name="n">The index of the bookmark</param>
		public static void JumpToBookmark(int n)
		{
			if (Settings.Manager.CurrentTrack == null) return;

			Track t = GetLibrarySourceTrack(Settings.Manager.CurrentTrack);
			
			if (t.Bookmarks == null || t.Bookmarks.Count < n) return;

			Settings.Manager.Seek = t.Bookmarks[n - 1].Item2 / 10;
		}

		/// <summary>
		/// Creates a bookmark at the current position of the current track
		/// </summary>
		/// <param name="type">The type of bookmark (start,end,normal).</param>
		/// <returns>
		/// The position (in %) of the newly created bookmark
		/// or -1 if non were created.
		/// </returns>
		public static double CreateBookmark(string type = "normal")
		{
			if (Settings.Manager.CurrentTrack != null)
			{
				Track t = GetLibrarySourceTrack(Settings.Manager.CurrentTrack);
				double pos = Settings.Manager.Seek * 10;

				// check if bookmark is either too close to either start or end or another bookmark
				double margin = 1.4; // bookmark is 4px, min width of slide is 147px = 1.4% margin
				if (pos < margin || pos > 100 - margin) return -1;

				foreach (var b in Settings.Manager.CurrentTrack.Bookmarks)
					if (b.Item2 - margin < pos && pos < b.Item2 + margin) return -1;

				int i = 0;
				foreach (var b in t.Bookmarks)
				{
					if (b.Item2 > pos) break;
					else i++;
				}
				t.Bookmarks.Insert(i, new Tuple<string,double>(type,pos));
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
			if (Settings.Manager.CurrentTrack == null)
				return;

			for (int i=0; i < Settings.Manager.CurrentTrack.Bookmarks.Count; i++)
			{
				if (Settings.Manager.CurrentTrack.Bookmarks[i].Item2 == pos)
				{
					Settings.Manager.CurrentTrack.Bookmarks.RemoveAt (i);
					break;
				}
			}
		}

		/// <summary>
		/// Check whether a file is supported by the media player
		/// </summary>
		/// <remarks>This function will stall until Initialized has been called</remarks>
		/// <param name="path">Filename of the track</param>
		/// <returns>True if the file is supported, otherwise false</returns>
		public static bool IsSupported(String path)
		{
			if (Track.GetType(path) == TrackType.YouTube || 
				Track.GetType(path) == TrackType.SoundCloud || 
				Track.GetType(path) == TrackType.Jamendo)
				return true;
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
		public static Track GetTrack(string path)
		{
			switch (Track.GetType(path))
			{
				case TrackType.File:
					return Files.GetTrack(path);

				case TrackType.SoundCloud:
					return Sources.Manager.SoundCloud.CreateTrack(path);

				case TrackType.Jamendo:
					return Sources.Manager.Jamendo.CreateTrack(path);

				case TrackType.WebRadio:
					return ParseURL(path);

				case TrackType.YouTube:
					return Sources.Manager.YouTube.CreateTrack(path);
			}
			return null;
		}

		/// <summary>
		/// Creates a track given a path
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>A track structure with meta data of the song</returns>
		public static Track CreateTrack(string path)
		{
			switch (Track.GetType(path))
			{
				case TrackType.File:
					return Files.CreateTrack(path);

				case TrackType.SoundCloud:
					return Sources.Manager.SoundCloud.CreateTrack(path);

				case TrackType.Jamendo:
					return Sources.Manager.Jamendo.CreateTrack(path);

				case TrackType.WebRadio:
					return ParseURL(path);

				case TrackType.YouTube:
					return Sources.Manager.YouTube.CreateTrack(path);
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
		public static Track GetSourceTrack(Track track)
		{
			if (track == null) return null;

			if (track.Source != null && track.Source.StartsWith("Playlist:"))
			{
				Playlist p = Playlists.Manager.Get(track.Source.Split(new[]{':'},2)[1]);
				if (p != null)
					foreach (Track t in p.Tracks)
						if (t.Path == track.Path)
							return t;
			}

			ObservableCollection<Track> tracks = null;
			switch (track.Type)
			{
				case TrackType.YouTube:
					tracks = Sources.Manager.YouTube.Tracks;
					break;

				case TrackType.SoundCloud:
					tracks = Sources.Manager.SoundCloud.Tracks;
					break;

				case TrackType.Jamendo:
					tracks = Sources.Manager.Jamendo.Tracks;
					break;

				case TrackType.WebRadio:
					tracks = Settings.Manager.RadioTracks;
					break;

				case TrackType.File:
					tracks = Settings.Manager.FileTracks;
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
		public static Track GetLibrarySourceTrack(Track track)
		{
			var t = Files.GetTrack(track.Path);
			if (t != null)
				return t;
			return track;
		}

		/// <summary>
		/// Parses a URL and extract meta data.
		/// </summary>
		/// <param name="URL">The URL to parse</param>
		/// <returns>The track representing the audio at the URL</returns>
		public static Track ParseURL(string URL)
		{
			Track track = new Track()
			{
				PlayCount = 0,
				Source = "Radio",
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
				Track track = ParseURL(URL);

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
		/// Sets the effects (volume, equalizer, echo, etc) on the audio channels.
		/// </summary>
		/// <param name="profile">The equalizer profile to use (defaults to the current in Settings)</param>
		/// <remarks>
		/// This should only be called once for each play of a given stream. Subsequent calls will add gain values, not replace it.
		/// Use RefreshFX() to change gain values while a stream is active.
		/// </remarks>
		public static void SetFX(EqualizerProfile profile = null)
		{
			if (profile == null)
				profile = Settings.Manager.CurrentEqualizerProfile;

			Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, (float)Settings.Manager.Volume / 100);

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
		/// <param name="profile">The equalizer profile to use (defaults to the current in Settings)</param>
		public static void RefreshFX(EqualizerProfile profile = null)
		{
			if (profile == null)
				profile = Settings.Manager.CurrentEqualizerProfile;
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
		private static void Start(Track track)
		{
			Settings.Manager.MediaState = MediaState.Playing;
			Settings.Manager.CurrentTrack = track;

			//U.L(LogLevel.Information, "MEDIA", "Playing " + track.Path);
			ThreadStart PlayThread = delegate()
			{
				trackWasSkipped = false;

				// add song to played/recent list
				songsAlreadyPlayed.Add(track);
				TrackType type = track.Type;

				bool skip = false;

				lock (locker)
				{
					if (stream != 0)
					{
						Bass.BASS_Stop();
						Bass.BASS_StreamFree(stream);
						stream = 0;
					}

					if (Sources.Manager.YouTube.HasFlash)
						InvokeScript("pause");

					switch (type)
					{
						case TrackType.YouTube:
							string vid = Sources.Manager.YouTube.GetID(track.Path);
							InvokeScript("setVolume", new object[] { Settings.Manager.Volume });
							InvokeScript("setQuality", new object[] { Settings.Manager.YouTubeQuality });
							Settings.Manager.MediaState = MediaState.Stopped;
							InvokeScript("loadNewVideo", new object[] { vid, 0 });
							break;

						case TrackType.File:
							stream = Bass.BASS_StreamCreateFile(track.Path, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);
							break;

						case TrackType.Jamendo:
							stream = Bass.BASS_StreamCreateURL(Sources.Manager.Jamendo.GetStreamURL(track), 0, BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
							break;

						case TrackType.SoundCloud:
							stream = Bass.BASS_StreamCreateURL(Sources.Manager.SoundCloud.GetStreamURL(track), 0, BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
							break;

						case TrackType.WebRadio:
							stream = Bass.BASS_StreamCreateURL(track.Path, 0, BASSFlag.BASS_SAMPLE_FLOAT, null, IntPtr.Zero);
							break;

						default:
							U.L(LogLevel.Error, "MEDIA", "Unsupported track type: " + type);
							Settings.Manager.CurrentTrack = track;
							skip = true;
							break;
					}

					if (stream != 0)
					{
						Bass.BASS_ChannelRemoveSync(stream, syncer);
						syncer = Bass.BASS_ChannelSetSync(stream, BASSSync.BASS_SYNC_END, 0, sync, IntPtr.Zero);

						if (type == TrackType.File)
						{
							double p = Settings.Manager.Seek;
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
						Playlists.Manager.TrackWasPlayed(Settings.Manager.CurrentTrack);
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
			Settings.Manager.MediaState = MediaState.Ended;
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
		private static void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName)
			{
				//case "Seek":
				//    if (ignoreSeekChange) break;
				//    Seek = Settings.Manager.Seek;
				//    break;

				case "Volume":
					if (Settings.Manager.CurrentTrack != null && Settings.Manager.CurrentTrack.Type == TrackType.YouTube)
					{
						if (Sources.Manager.YouTube.HasFlash)
							InvokeScript("setVolume", new object[] { Settings.Manager.Volume });
					}
					else
					{
						if (stream == 0) return;
						Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, (float)Settings.Manager.Volume / 100);
					}
					break;

				case "YouTubeQuality":
					InvokeScript("setQuality", new object[] { Settings.Manager.YouTubeQuality });
					break;

				case "CurrentTrack":
					Track t = (Track)e.OldValue;
					if (t != null)
					{
						Track sourceTrack = GetSourceTrack(t);
						sourceTrack.IsActive = false;
					}

					t = Settings.Manager.CurrentTrack;
					if (t != null)
					{
						Track sourceTrack = GetSourceTrack(t);
						sourceTrack.IsActive = true;
					}
					break;

				case "HistoryIndex":
					Track oldTrack = null;
					Track newTrack = null;

					int oldIndex = (int)e.OldValue - 1;
					int newIndex = (int)e.NewValue - 1;

					if (oldIndex >= Settings.Manager.HistoryTracks.Count)
						oldIndex = Settings.Manager.HistoryTracks.Count - 1;
					else if (oldIndex == Settings.Manager.HistoryTracks.Count - 1 &&
						newIndex == Settings.Manager.HistoryTracks.Count)
						oldIndex = Settings.Manager.HistoryTracks.Count - 2;

					if (newIndex >= Settings.Manager.HistoryTracks.Count)
						newIndex = Settings.Manager.HistoryTracks.Count - 1;

					if (Settings.Manager.HistoryTracks.Count > oldIndex &&
						oldIndex >= 0)
						oldTrack = Settings.Manager.HistoryTracks[oldIndex];

					if (Settings.Manager.HistoryTracks.Count > newIndex &&
						newIndex >= 0)
						newTrack = Settings.Manager.HistoryTracks[newIndex];

					if (oldTrack != null)
						oldTrack.IsActive = false;

					if (newTrack != null)
						newTrack.IsActive = true;
					break;

				case "MediaState":
					switch (Settings.Manager.MediaState)
					{
						case MediaState.Paused:
						case MediaState.Stopped:
							// keep the ticker on if a youtube track is loaded (need it to see buffer size changes)
							if (Settings.Manager.CurrentTrack == null || Settings.Manager.CurrentTrack.Type != TrackType.YouTube)
								StopTicker();
							break;

						case MediaState.Playing:
							StartTicker();
							break;

						case MediaState.Ended:
							if (Settings.Manager.PauseWhenSongEnds)
							{
								Pause();
								Settings.Manager.Seek = 0;
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
			if (stopTicker || isTicking) return;
			isTicking = true;
			try
			{
				if (Settings.Manager.CurrentTrack != null)
				{
					bool youtube = Settings.Manager.CurrentTrack.Type == TrackType.YouTube;
					object ps = null;
					BASSActive active = Bass.BASS_ChannelIsActive(stream);
					if (youtube && Sources.Manager.YouTube.HasFlash)
						ps = InvokeScript("getPlayerState");

					if (youtube && (ps == null || Convert.ToInt32(ps) == 0))
					{
						StopTicker();
					}
					else if ((youtube && Settings.Manager.MediaState == MediaState.Playing) || active == BASSActive.BASS_ACTIVE_PLAYING)
					{
						double pos = Position;
						double len = Length;
						if (pos < 0) pos = 0;
						if (len < 0) len = 0;
						double seek = (pos / len) * 10;
						if (Double.IsNaN(seek) || Double.IsInfinity(seek)) seek = 0;
						Settings.Manager.Seek = seek;

						// update FFT data points
						if (stream != 0)
							Bass.BASS_ChannelGetData(stream, FFTData, (int)BASSData.BASS_DATA_FFT2048);

//						if (youtube && Sources.Manager.YouTube.HasFlash)
//						{
//							double b = Buffer;
//							Settings.Manager.BufferSize = b;
//
//							// shut down ticker if we have full buffer
//							if (Settings.Manager.MediaState != MediaState.Playing && b >= 10)
//								StopTicker();
//						}
					}
					else if (Settings.Manager.MediaState == MediaState.Playing)
					{
//						if (active == BASSActive.BASS_ACTIVE_STOPPED)
//							Settings.Manager.MediaState = MediaState.Ended;
						//if (active != BASSActive.BASS_ACTIVE_STALLED)
						//    Settings.Manager.MediaState = MediaState.Ended;
					}
				}
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "PLAYBACK", "Could not update seeker: " + exc.Message);
			}
			finally {
				isTicking = false;
			}
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// The dispatcher of the <see cref="TrackSwitched"/> event.
		/// </summary>
		/// <param name="oldTrack">The track before the switch</param>
		/// <param name="newTrack">The track after the switch</param>
		private static void DispatchTrackSwitched(Track oldTrack, Track newTrack)
		{
			if (TrackSwitched != null)
				TrackSwitched(new TrackSwitchedEventArgs(oldTrack, newTrack));
		}

		/// <summary>
		/// The dispatcher of the <see cref="LoadedTrack"/> event.
		/// </summary>
		/// <param name="track">The track that was loaded</param>
		private static void DispatchLoadedTrack(Track track)
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
	/// Media.StateChanged event occurs
	/// </summary>
	/// <param name="e">The event data</param>
	public delegate void MediaStateEventHandler(MediaStateEventArgs e);

	/// <summary>
	/// Represents the method that is called when the
	/// Media.TrackSwitched event occurs
	/// </summary>
	/// <param name="e">The event data</param>
	public delegate void TrackSwitchedEventHandler(TrackSwitchedEventArgs e);

	/// <summary>
	/// Represents the method that 
	/// </summary>
	/// <param name="function"></param>
	/// <param name="param"></param>
	/// <returns></returns>
	public delegate object InvokeScriptDelegate(string function, object[] param = null);

	/// <summary>
	/// Represents the method that is called when the
	/// Media.NavigateBrowser event occurs.
	/// </summary>
	/// <param name="source">The URI to navigate to</param>
	public delegate void NavigateBrowserDelegate(Uri source);

	/// <summary>
	/// Represents the method that is called when the
	/// Media.LoadedTrack event occurs.
	/// </summary>
	/// <param name="track">The track that was loaded</param>
	public delegate void LoadedTrackDelegate(Track track);

	/// <summary>
	/// Represents the method that is called when a URL has been
	/// parsed successfully.
	/// </summary>
	/// <param name="track">The track representing the audio at the URL</param>
	public delegate void ParsedURLDelegate(Track track);

	/// <summary>
	/// Represents the callback method for retrieving the current track collection
	/// used by the media manager to determine the next track to play.
	/// </summary>
	/// <returns>The list of tracks to choose the next track from</returns>
	public delegate List<Track> FetchCollectionDelegate();

	#endregion

	#region Event arguments

	/// <summary>
	/// Provides data for the Media.StateChanged event
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
		public Track OldTrack { get; set; }

		/// <summary>
		/// Gets or sets the new track
		/// </summary>
		public Track NewTrack { get; set; }

		/// <summary>
		/// Creates an instance of the TrackSwitchedEventArgs class
		/// </summary>
		/// <param name="oldTrack">The track before the switch</param>
		/// <param name="newTrack">The track after the switch</param>
		public TrackSwitchedEventArgs(Track oldTrack, Track newTrack)
		{
			OldTrack = oldTrack;
			NewTrack = newTrack;
		}
	}

	#endregion

	#region Enums

	#endregion
}