/**
 * Jamendo.cs
 * 
 * Takes care of searching and finding music on Jamendo
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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;

using Newtonsoft.Json.Linq;

using Stoffi.Core.Media;
using Stoffi.Core.Settings;

namespace Stoffi.Core.Sources
{
	/// <summary>
	/// Represents a manager that takes care of talking to Jamendo.
	/// </summary>
	public class Jamendo : Source
	{
		#region Members

		#endregion

		#region Properties

		/// <summary>
		/// The ID for the source, used in the path URI.
		/// </summary>
		/// <value>The source identified.</value>
		protected override string ID { get { return "jamendo"; } }

		/// <summary>
		/// If the source is based on an Internet API, this specifies
		/// the base of the URI to the API.
		/// </summary>
		/// <remarks>If this is not null, then VerifyConnectivity can be used to verify that there
		/// is a connection to the API server, if not then the Reconnected event will fire
		/// as soon as the API server can be reached.</remarks>
		/// <value>The path prefix.</value>
		protected override string UriBase { get { return "http://api.jamendo.com/v3.0"; } }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.Jamendo"/> class.
		/// </summary>
		public Jamendo()
		{
			ListConfig.Columns.Add(ListColumn.Create("Album", U.T("ColumnAlbum"), 150));
			ListConfig.Columns.Add(ListColumn.Create("Views", U.T("ColumnViews"), 120, "Number", Alignment.Right));
			ListConfig.Columns.Add(ListColumn.Create("Genre", U.T("ColumnGenre"), 100));
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Returns a list of top tracks
		/// </summary>
		/// <returns>An observable collection of Track that represents the top Jamendo tracks</returns>
		public override ObservableCollection<Track> TopFeed(string feed = "")
		{
			IsLoading = true;
			ObservableCollection<Track> tracks = new ObservableCollection<Track>();
			try
			{
				string url = CreateURL(new string[] { "order=popularity_week_desc", "limit=50" });
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						var data = reader.ReadToEnd();
						var result = JObject.Parse(data);
						tracks = ParseTracks((JArray)result["results"]);
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "JAMENDO", "Could not retrieve top tracks: " + e.Message);
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
		/// Searches Jamendo for tracks matching a certain query.
		/// </summary>
		/// <param name="query">The query to search for</param>
		/// <returns>An observable collection of Track with all Jamendo tracks that match query</returns>
		public override ObservableCollection<Track> Search(string query)
		{
			IsLoading = true;
			ObservableCollection<Track> tracks = new ObservableCollection<Track>();
			try
			{
				string url = CreateURL(new string[] { "limit=50", U.CreateParam("search", query, "") });
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						var result = JObject.Parse(reader.ReadToEnd());
						tracks = ParseTracks((JArray)result["results"]);
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "JAMENDO", "Could not perform search: " + e.Message);
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
		/// Retrieves the stream URL for a Jamendo track
		/// </summary>
		/// <param name="track">The Jamendo track</param>
		public string GetStreamURL(Track track)
		{
			return String.Format("http://storage-new.newjamendo.com/?trackid={0}&format=mp31&u=0", GetID(track.Path));
		}

		/// <summary>
		/// Creates a track given a Jamendo path.
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>The track if it could be found, otherwise null</returns>
		public override Track CreateTrack(string path)
		{
			try
			{
				string url = CreateURL(new string[] { "id="+GetID (path) });
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						var result = JObject.Parse(reader.ReadToEnd());
						JArray tracks = (JArray)result["results"];
						return ParseTrack((JObject)tracks[0]);
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "JAMENDO", "Could not fetch track: " + e.Message);
			}
			return null;
		}

		#endregion

		#region Private

		/// <summary>
		/// Create a request URL.
		/// </summary>
		/// <param name="arguments">A list of arguments in the format "key=value"</param>
		/// <param name="format">The format (either json or xml)</param>
		/// <returns></returns>
		private string CreateURL(string[] arguments, string format = "json")
		{
			string url = String.Format("{0}/tracks?client_id={1}&format={2}&include=stats+musicinfo", UriBase, U.JamendoKey, format);
			foreach (string arg in arguments)
				url += "&" + arg;
			return url;
		}

		/// <summary>
		/// Parses a JSON array into a list of tracks.
		/// </summary>
		/// <param name="json">The JSON data</param>
		/// <returns>A list of tracks</returns>
		private ObservableCollection<Track> ParseTracks(JArray json)
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
				track.Path = String.Format("{0}{1}", PathPrefix, json["id"]);
				track.Title = (string)json["name"];
				track.Genre = (string)json["genre"];
				track.Artist = (string)json["artist_name"];
				track.Album = (string)json["album_name"];

				if (json["releasedate"] != null)
				{
					var date = DateTime.Parse ((string)json["releasedate"]);
					if (date.Year > 1)
						track.Year = (uint)date.Year;
				}

				int d = (int)json["duration"];
				track.Length = (double)d;

				if (json["image"] != null)
					track.ArtURL = (string)json["image"];
				else if (json["album_image"] != null)
					track.ArtURL = (string)json["album_image"];
				else if (json["artist_image"] != null)
					track.ArtURL = (string)json["artist_image"];
				track.Image = track.ArtURL;

				if (json["prourl"] != null)
					track.URL = (string)json["prourl"];
				else
					track.URL = String.Format ("https://www.jamendo.com/track/{0}", json["id"]);

				if (track.Image == null || track.Image == "")
					track.Image = track.Icon;

				var stats = json["stats"];
				if (stats != null)
				{
					track.Views = (ulong)stats["rate_listened_total"];
				}

				var info = json["musicinfo"];
				if (info != null)
				{
					var tags = info["tags"];
					if (tags != null)
					{
						var genres = (JArray)tags["genres"];
						if (genres != null)
						{
							for (int i=0; i < genres.Count; i++)
								genres[i] = U.Titleize((string)genres[i]);
							track.Genre = String.Join (", ", genres);
						}
					}
				}

				return track;
			}
			catch (WebException e)
			{
				U.L(LogLevel.Warning, "JAMENDO", "Could not parse track JSON data: " + e.Message);
				return null;
			}
		}

		#endregion

		#endregion
	}
}
