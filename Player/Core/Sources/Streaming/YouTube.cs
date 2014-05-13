/**
 * YouTube.cs
 * 
 * Takes care of searching and finding music on YouTube
 * as well as converting results into Track structures.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

using Newtonsoft.Json.Linq;

//using Google.GData.Client;
//using Google.GData.Extensions;
//using Google.GData.YouTube;
//using Google.GData.Extensions.MediaRss;
//using Google.YouTube;

using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Settings;

namespace Stoffi.Core.Sources
{
	/// <summary>
	/// Represents a manager that takes care of talking to YouTube
	/// </summary>
	public class YouTube : Source
	{
		#region Members
		string parts = "part=id%2Csnippet%2CcontentDetails%2Cstatistics";
		string fields = "fields=items(contentDetails%2Fduration%2Cstatistics%2FviewCount%2Cid%2Csnippet(title%2CpublishedAt%2CchannelTitle%2Cthumbnails%2Fdefault))";
		#endregion

		#region Properties

		/// <summary>
		/// The ID for the source, used in the path URI.
		/// </summary>
		/// <value>The source identified.</value>
		protected override string ID { get { return "youtube"; } }

		/// <summary>
		/// If the source is based on an Internet API, this specifies
		/// the base of the URI to the API.
		/// </summary>
		/// <remarks>If this is not null, then VerifyConnectivity can be used to verify that there
		/// is a connection to the API server, if not then the Reconnected event will fire
		/// as soon as the API server can be reached.</remarks>
		/// <value>The path prefix.</value>
		protected override string UriBase { get { return "https://www.googleapis.com"; } }

		/// <summary>
		/// Gets or sets whether the user has Adobe Flash installed or not
		/// </summary>
		public bool HasFlash { get; set; }

		/// <summary>
		/// Gets the fields parameter for the track fetching API request.
		/// </summary>
		/// <value>The fields.</value>
		public string Fields { get { return fields; }}

		/// <summary>
		/// Gets the part parameter for the track fetching API request.
		/// </summary>
		/// <value>The fields.</value>
		public string Parts { get { return parts; }}

		#endregion
		
		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.YouTube"/> class.
		/// </summary>
		public YouTube()
		{
			HasFlash = false;
			ListConfig.Columns.Add(ListColumn.Create("Views", U.T("ColumnViews"), 120, "Number", Alignment.Right));
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Returns a list of tracks from one of the YouTube feeds
		/// </summary>
		/// <param name="feed">The feed</param>
		/// <returns>An observable collection of Track that represents the most viewed YouTube tracks</returns>
		public override ObservableCollection<Track> TopFeed(string feed = "")
		{
			IsLoading = true;
			ObservableCollection<Track> tracks = new ObservableCollection<Track>();
			try
			{
				string filter = FilterID(Settings.Manager.YouTubeFilter);
				string url = CreateURL("videos", new string[] {
					"chart=mostPopular", "maxResults=50", "part=id%2Csnippet%2CcontentDetails%2Cstatistics", "videoCategoryId="+filter,
					fields, parts });
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						var data = reader.ReadToEnd();
						var result = JObject.Parse(data);
						tracks = ParseTracks((JArray)result["items"]);
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "YOUTUBE", "Could not retrieve top tracks: " + e.Message);
				failedRequests++;
				if (failedRequests < 3)
					tracks = TopFeed();
				else
					VerifyConnectivity();
			}
			IsLoading = false;
			return tracks;
		}

		/// <summary>
		/// Searches YouTube for tracks matching a certain query
		/// </summary>
		/// <param name="query">The query to search for</param>
		/// <returns>An observable collection of Track with all YouTube tracks that match query</returns>
		public override ObservableCollection<Track> Search(string query)
		{
			IsLoading = true;
			ObservableCollection<Track> tracks = new ObservableCollection<Track>();

			try
			{
				var ids = new List<string>();
				string filter = FilterID(Settings.Manager.YouTubeFilter);
				string url = CreateURL("search", new string[] {
					"maxResults=50", "part=id", "videoCategoryId="+filter, "type=video", "videoEmbeddable=true",
					"fields=items%2Fid%2FvideoId", U.CreateParam("q", query, "") });
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						var data = reader.ReadToEnd();
						var result = JObject.Parse(data);
						var items = (JArray)result["items"];
						foreach (var item in items)
						{
							var id = (string)((item as JObject)["id"] as JObject)["videoId"];
							if (!String.IsNullOrWhiteSpace(id))
								ids.Add(id);
						}
					}
				}

				if (ids.Count == 0)
					return tracks;

				url = CreateURL("videos", new string[] {
					"maxResults=50", "id=" + String.Join(",", ids), fields, parts });
				request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						var data = reader.ReadToEnd();
						var result = JObject.Parse(data);
						tracks = ParseTracks((JArray)result["items"]);
					}
				}
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Error, "YOUTUBE", "Error while performing search: " + exc.Message);
				failedRequests++;
				if (failedRequests < 3)
					tracks = Search (query);
				else
					VerifyConnectivity();
			}
			IsLoading = false;
			return tracks;
		}

		/// <summary>
		/// Creates a track using a YouTube video ID
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>A Track structure representing the YouTube track</returns>
		public override Track CreateTrack(string path)
		{
			try
			{
				string id = GetID(path);
				var tracks = new ObservableCollection<Track>();

				var url = CreateURL("videos", new string[] { "id=" + id, fields, parts });
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						var data = reader.ReadToEnd();
						var result = JObject.Parse(data);
						tracks = ParseTracks((JArray)result["items"]);
					}
				}

				if (tracks == null || tracks.Count == 0)
				{
					U.L(LogLevel.Warning, "YouTbe", "Could not find video with ID '" + id + "'");
					return null;
				}
				return tracks[0];
			}
			catch (Exception e)
			{
				U.L (LogLevel.Error, "YouTube", "Could not create track: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Gets the ID of a filter given its name.
		/// </summary>
		/// <returns>The ID of the filter.</returns>
		/// <param name="name">Name.</param>
		public string FilterID(string name)
		{
			// TODO: instead of hardcoded values, create a lookup table and populate in constructor using the API
			if (name == "Music")
				return "10";
			return "";
		}

		/// <summary>
		/// Create a request URL.
		/// </summary>
		/// <param name="arguments">A list of arguments in the format "key=value"</param>
		/// <param name="format">The format (either json or xml)</param>
		/// <returns></returns>
		public string CreateURL(string resource, string[] arguments)
		{
			string url = String.Format("{0}/youtube/v3/{1}?key={2}", UriBase, resource, U.YouTubeKey);
			foreach (string arg in arguments)
				url += "&" + arg;
			return url;
		}

		/// <summary>
		/// Parses a JSON array into a list of tracks.
		/// </summary>
		/// <param name="json">The JSON data</param>
		/// <returns>A list of tracks</returns>
		public ObservableCollection<Track> ParseTracks(JArray json)
		{
			ObservableCollection<Track> tracks = new ObservableCollection<Track>();
			foreach (JObject o in json)
			{
				Track t = ParseTrack(o);
				if (t != null)
					tracks.Add(t);
			}
			return tracks;
		}

		#endregion

		#region Private

		/// <summary>
		/// Parses a JSON object into a track.
		/// </summary>
		/// <param name="json">The JSON data</param>
		/// <returns>A track</returns>
		private Track ParseTrack(JObject json)
		{
			if (json == null) return null;
			try
			{
				Track track = new Track();

				var snippet = json["snippet"] as JObject;
				var details = json["contentDetails"] as JObject;
				var stats = json["statistics"] as JObject;

				var thumbnails = snippet["thumbnails"] as JObject;
				var thumbnail = thumbnails["default"] as JObject;

				var length = XmlConvert.ToTimeSpan((string)details["duration"]);
				var date = DateTime.Parse ((string)snippet["publishedAt"]);
				var id = (string)json["id"];
				var parsedTitle = U.ParseTitle((string)snippet["title"]);

				track.Path = String.Format("{0}{1}", PathPrefix, id);
				track.Artist = parsedTitle[0];
				track.Title = parsedTitle[1];
				track.Year = (uint)date.Year;
				track.Length = (double)length.TotalSeconds;
				track.ArtURL = (string)thumbnail["url"];
				track.Image = track.ArtURL;
				track.URL = "https://www.youtube.com/watch?v=" + id;
				track.Views = Convert.ToUInt64((string)stats["viewCount"]);

				if (String.IsNullOrWhiteSpace(track.Artist))
					track.Artist = (string)snippet["channelTitle"];

				return track;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "YouTube", "Could not parse track JSON data: " + e.Message);
				return null;
			}
		}

		#endregion

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
			OnErrorOccured(errorCode.ToString());
		}

		/// <summary>
		/// Invoked when user tries to play a youtube track but doesn't have flash installed
		/// </summary>
		public void OnNoFlash()
		{
			OnNoFlashDetected();
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
				Settings.Manager.MediaState = MediaState.Ended;
					break;

				case 1: // playing
					Settings.Manager.MediaState = MediaState.Playing;
					break;

				case 2: // paused
					Settings.Manager.MediaState = MediaState.Paused;
					break;

				case 3: // buffering
					break;

				case 5: // cued
					break;
			}
		}

		/// <summary>
		/// Dispatches the ErrorOccured event
		/// </summary>
		/// <param name="message">The error message</param>
		public void OnErrorOccured(string message)
		{
			if (ErrorOccured != null)
				ErrorOccured(this, message);
		}

		/// <summary>
		/// Dispatches the NoFlashDetected event
		/// </summary>
		public void OnNoFlashDetected()
		{
			if (NoFlashDetected != null)
				NoFlashDetected(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the PlayerReady event
		/// </summary>
		public void OnPlayerReady()
		{
			if (PlayerReady != null)
				PlayerReady(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the DoubleClick event
		/// </summary>
		public void OnDoubleClick()
		{
			if (DoubleClick != null)
				DoubleClick(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the SingleClick event
		/// </summary>
		public void OnSingleClick()
		{
			if (SingleClick != null)
				SingleClick(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the HideCursor event
		/// </summary>
		public void OnHideCursor()
		{
			if (HideCursor != null)
				HideCursor(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the ShowCursor event
		/// </summary>
		public void OnShowCursor()
		{
			if (ShowCursor != null)
				ShowCursor(this, new EventArgs());
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

		/// <summary>
		/// Occurs when the user double clicks the video.
		/// </summary>
		public event EventHandler DoubleClick;

		/// <summary>
		/// Occurs when the user clicks the video.
		/// </summary>
		public event EventHandler SingleClick;

		/// <summary>
		/// Occurs when the mouse cursor is hidden.
		/// </summary>
		public event EventHandler HideCursor;

		/// <summary>
		/// Occurs when the mouse cursor becomes visible.
		/// </summary>
		public event EventHandler ShowCursor;
	}
}
