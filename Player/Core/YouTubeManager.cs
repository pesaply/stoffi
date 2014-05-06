/**
 * YouTubeManager.cs
 * 
 * Takes care of searching and finding music on YouTube
 * as well as converting results into TrackData structures.
 * 
 * * * * * * * * *
 * 
 * Copyright 2011 Simplare
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;

namespace Stoffi
{
	/// <summary>
	/// Represents a manager that takes care of talking to YouTube
	/// </summary>
	public static class YouTubeManager
	{
		#region Members
		private static YouTubeRequestSettings settings = new YouTubeRequestSettings("Stoffi", "AI39si4y_vkAW2Ngyc2BlMdgkBghua2w5hheyesEI-saNU_CNDIMs5YMPpIBk-HpmFG4qDPAHAvE_YYNWH5qV5S1x5euKKRodw");
		private static string pathPrefix = "stoffi:track:youtube:";
		private static string uriBase = "http://gdata.youtube.com";
		private static Timer reconnectTimer = null;
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets whether the user has Adobe Flash installed or not
		/// </summary>
		public static bool HasFlash { get; set; }

		/// <summary>
		/// Gets the current source of tracks used as ItemsSource for the YouTube track list.
		/// </summary>
		public static ObservableCollection<TrackData> TrackSource { get; private set; }
		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Returns a list of tracks from one of the YouTube feeds
		/// </summary>
		/// <param name="feed">The feed</param>
		/// <returns>An observable collection of TrackData that represents the most viewed YouTube tracks</returns>
		public static ObservableCollection<TrackData> TopFeed(string feed = "top_rated")
		{
			ObservableCollection<TrackData> tracks = new ObservableCollection<TrackData>();

			try
			{
				YouTubeRequest request = new YouTubeRequest(settings);

				int maxFeedItems = 50;

				string filter = SettingsManager.YouTubeFilter;
				if (String.IsNullOrWhiteSpace(filter) || filter == "None")
					filter = "";
				else
					filter = String.Format("_{0}", filter);

				int i = 1;
				Feed<Video> videoFeed = request.Get<Video>(new Uri(uriBase + "/feeds/api/standardfeeds/" + feed + filter + "?format=5"));
				foreach (Video entry in videoFeed.Entries)
				{
					if (i++ > maxFeedItems) break;
					if (entry != null)
						tracks.Add(CreateTrack(entry));
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "YouTube", "Could not fetch top rated: " + e.Message);
				VerifyConnectivity();
			}

			TrackSource = tracks;
			return tracks;
		}

		/// <summary>
		/// Searches YouTube for tracks matching a certain query
		/// </summary>
		/// <param name="query">The query to search for</param>
		/// <returns>An observable collection of TrackData with all YouTube tracks that match query</returns>
		public static ObservableCollection<TrackData> Search(string query)
		{
			ObservableCollection<TrackData> tracks = new ObservableCollection<TrackData>();

			try
			{
				string filter = SettingsManager.YouTubeFilter;

				YouTubeQuery q = new YouTubeQuery(YouTubeQuery.DefaultVideoUri);
				q.OrderBy = "relevance";
				q.Query = query;
				q.Formats.Add(YouTubeQuery.VideoFormat.Embeddable);
				q.NumberToRetrieve = 50;
				q.SafeSearch = YouTubeQuery.SafeSearchValues.None;

				if (!String.IsNullOrWhiteSpace(filter) && filter != "None")
				{
					AtomCategory category = new AtomCategory(filter, YouTubeNameTable.CategorySchema);
					q.Categories.Add(new QueryCategory(category));
				}

				YouTubeRequest request = new YouTubeRequest(settings);

				Feed<Video> videoFeed = request.Get<Video>(q);
				foreach (Video entry in videoFeed.Entries)
				{
					tracks.Add(YouTubeManager.CreateTrack(entry));
				}
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Error, "YOUTUBE", "Error while performing search: " + exc.Message);
				VerifyConnectivity();
			}

			TrackSource = tracks;

			return tracks;
		}

		/// <summary>
		/// Retrieves the URL to the thumbnail for a YouTube track
		/// </summary>
		/// <param name="track">The YouTube track</param>
		public static string GetThumbnail(TrackData track)
		{
			if (IsYouTube(track))
				return "https://img.youtube.com/vi/" + GetYouTubeID(track.Path) + "/1.jpg";
			else
				return "";
		}

		/// <summary>
		/// Retrieves the URL for a YouTube track
		/// </summary>
		/// <param name="track">The YouTube track</param>
		public static string GetURL(TrackData track)
		{
			if (IsYouTube(track))
				return "https://www.youtube.com/watch?v=" + GetYouTubeID(track.Path);
			else
				return "";
		}

		/// <summary>
		/// Creates a track using a YouTube video ID
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>A TrackData structure representing the YouTube track</returns>
		public static TrackData CreateTrack(string path)
		{
			try
			{
				string id = GetYouTubeID(path);
				YouTubeRequest request = new YouTubeRequest(settings);
				Uri url = new Uri(uriBase + "/feeds/api/videos/" + id);
				Video v = request.Retrieve<Video>(url);
				if (v == null)
				{
					U.L(LogLevel.Warning, "YOUTUBE", "Could not find video with ID '" + id + "'");
					return null;
				}
				return CreateTrack(v);
			}
			catch (Exception e)
			{
				return null;
				//return new TrackData { Title = "Error", Artist = "Error" };
			}
		}

		/// <summary>
		/// Creates a track using a YouTube video entry
		/// </summary>
		/// <param name="v">The video entry</param>
		/// <returns>A TrackData structure representing the YouTube track</returns>
		public static TrackData CreateTrack(Video v)
		{
			TrackData track = new TrackData();
			track.Path = pathPrefix + v.VideoId;
			track.Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/YouTube.ico";
			track.Bookmarks = new List<double>();
			track.Processed = true;
			track.Length = Convert.ToDouble(v.Media.Duration.Seconds);
			string[] str = U.ParseTitle(v.Title);
			track.Artist = str[0];
			track.Title = str[1];
			track.IsActive = SettingsManager.CurrentTrack != null && SettingsManager.CurrentTrack.Path == track.Path;
			if (String.IsNullOrWhiteSpace(track.Artist))
				track.Artist = v.Uploader;
			track.Views = v.ViewCount;
			track.URL = "https://www.youtube.com/watch?v=" + v.VideoId;
			return track;
		}

		/// <summary>
		/// Checks whether a given track is a youtube track
		/// </summary>
		/// <param name="t">The track to check</param>
		/// <returns>True if the track is a youtube track</returns>
		public static bool IsYouTube(TrackData t)
		{
			return (t != null && IsYouTube(t.Path));
		}

		/// <summary>
		/// Checks whether a given track path corresponds to a youtube track
		/// </summary>
		/// <param name="path">The path of the track to check</param>
		/// <returns>True if the track is a youtube track</returns>
		public static bool IsYouTube(string path)
		{
			return path.StartsWith(pathPrefix);
		}

		/// <summary>
		/// Extracts the video ID of a YouTube track's path
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>The video ID</returns>
		public static string GetYouTubeID(string path)
		{
			if (IsYouTube(path))
			{
				path = path.Substring(pathPrefix.Length);
				if (path[path.Length - 1] == '/')
					path = path.Substring(0, path.Length - 1);
				return path;
			}

			throw new Exception("Trying to extract YouTube video ID from non-YouTube track: " + path);
		}

		#endregion

		#region Private

		/// <summary>
		/// Checks to see if we are still able to connect to the YouTube server.
		/// </summary>
		private static void VerifyConnectivity()
		{
			if (!U.Ping(uriBase))
			{
				if (reconnectTimer != null)
					reconnectTimer.Dispose();
				reconnectTimer = new Timer(PerformReconnect, null, 15000, 15000);
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Attempts to reconnect to the SoundCloud server.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private static void PerformReconnect(object state)
		{
			if (U.Ping(uriBase))
			{
				if (reconnectTimer != null)
					reconnectTimer.Dispose();
				reconnectTimer = null;
				OnReconnected();
			}
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the Reconnected event
		/// </summary>
		private static void OnReconnected()
		{
			if (Reconnected != null)
				Reconnected(null, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the connectivity has been re-established.
		/// </summary>
		public static event EventHandler Reconnected;

		#endregion
	}

	/// <summary>
	/// Describes the interface that the chromeless YouTube player can call via JavaScript
	/// </summary>
	[ComVisibleAttribute(true)]
	public class YouTubePlayerInterface
	{
		/// <summary>
		/// Invoked when an error occurs within the YouTube player
		/// </summary>
		/// <param name="errorCode">The error code</param>
		public void OnVideoError(int errorCode)
		{
			switch (errorCode)
			{
				case 2:
					U.L(LogLevel.Error, "YOUTUBE", "Player reported that we used bad parameters");
					break;

				case 100:
					U.L(LogLevel.Error, "YOUTUBE", "Player reported that the track has either been removed or marked as private");
					break;

				case 101:
				case 150:
					U.L(LogLevel.Error, "YOUTUBE", "Player reported that the track is restricted");
					break;

				default:
					U.L(LogLevel.Error, "YOUTUBE", "Player reported an unknown error code: " + errorCode);
					break;
			}
			DispatchError(errorCode.ToString());
		}

		/// <summary>
		/// Invoked when user tries to play a youtube track but doesn't have flash installed
		/// </summary>
		public void OnNoFlash()
		{
			DispatchNoFlashDetected();
		}

		/// <summary>
		/// Invoked when the player changes state
		/// </summary>
		/// <param name="state">The new state of the player</param>
		public void OnStateChanged(int state)
		{
			switch (state)
			{
				case -1: // unstarted
					break;

				case 0: // ended
					SettingsManager.MediaState = MediaState.Ended;
					break;

				case 1: // playing
					SettingsManager.MediaState = MediaState.Playing;
					break;

				case 2: // paused
					SettingsManager.MediaState = MediaState.Paused;
					break;

				case 3: // buffering
					break;

				case 5: // cued
					break;
			}
		}

		/// <summary>
		/// Invoked when player is ready
		/// </summary>
		public void OnPlayerReady()
		{
			DispatchPlayerReady();
		}

		/// <summary>
		/// Dispatches the ErrorOccured event
		/// </summary>
		/// <param name="message">The error message</param>
		private void DispatchError(string message)
		{
			if (ErrorOccured != null)
				ErrorOccured(this, message);
		}

		/// <summary>
		/// Dispatches the NoFlashDetected event
		/// </summary>
		private void DispatchNoFlashDetected()
		{
			if (NoFlashDetected != null)
				NoFlashDetected(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the PlayerReady event
		/// </summary>
		private void DispatchPlayerReady()
		{
			if (PlayerReady != null)
				PlayerReady(this, new EventArgs());
		}

		/// <summary>
		/// Occurs when there's an error from the player
		/// </summary>
		public event ErrorEventHandler ErrorOccured;

		/// <summary>
		/// Occurs when the user tries to play a youtube track but there's no flash installed
		/// </summary>
		public event EventHandler NoFlashDetected;

		/// <summary>
		/// Occurs when the player is ready
		/// </summary>
		public event EventHandler PlayerReady;
	}
}
