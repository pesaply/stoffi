/**
 * SoundCloud.cs
 * 
 * Takes care of searching and finding music on SoundCloud
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
using System.Net;
using System.IO;
using System.Linq;
using System.Threading;

using Newtonsoft.Json.Linq;

using Stoffi.Core.Media;
using Stoffi.Core.Settings;

namespace Stoffi.Core.Sources
{
	/// <summary>
	/// Represents a manager that takes care of talking to SoundCloud
	/// </summary>
	public class SoundCloud : Source
	{
		#region Members
		#endregion

		#region Properties

		/// <summary>
		/// The ID for the source, used in the path URI.
		/// </summary>
		/// <value>The source identified.</value>
		protected override string ID { get { return "soundcloud"; } }

		/// <summary>
		/// If the source is based on an Internet API, this specifies
		/// the base of the URI to the API.
		/// </summary>
		/// <remarks>If this is not null, then VerifyConnectivity can be used to verify that there
		/// is a connection to the API server, if not then the Reconnected event will fire
		/// as soon as the API server can be reached.</remarks>
		/// <value>The path prefix.</value>
		protected override string UriBase { get { return "https://api.soundcloud.com"; } }

		#endregion
		
		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.SoundCloud"/> class.
		/// </summary>
		public SoundCloud()
		{
			ListConfig.Columns.Add(ListColumn.Create("Genre", U.T("ColumnGenre"), 100));
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Returns a list of top tracks
		/// </summary>
		/// <returns>An observable collection of Track that represents the top SoundCloud tracks</returns>
		public override ObservableCollection<Track> TopFeed(string feed = "")
		{
			IsLoading = true;
			ObservableCollection<Track> tracks = new ObservableCollection<Track>();
			try
			{
				// soundcloud removed order=hotness and seemingly any order.
				// so instead we fetch the unofficial explore feed and do a second
				// request to resolve IDs to track structures.
				try
				{
					var url = "https://api-v2.soundcloud.com/explore/Popular%2BMusic?limit=50&offset=0&linked_partitioning=1";
					var request = (HttpWebRequest)WebRequest.Create(url);
					var unsortedTracks = new ObservableCollection<Track>();
					using (var response = (HttpWebResponse)request.GetResponse())
					{
						using (var reader = new StreamReader(response.GetResponseStream()))
						{
							// fetch a list of IDs for all tracks in explore categories
							JObject exploreFeed = JObject.Parse(reader.ReadToEnd());
							JArray exploreCollection = exploreFeed["tracks"] as JArray;
							List<string> trackIDs = new List<string>();
							foreach (JObject track in exploreCollection)
							{
								try
								{
									trackIDs.Add(track["id"].ToString());
								}
								catch { }
							}

							// turn IDs into track structures
							if (trackIDs.Count > 0)
							{
								int offset = 0;
								while (true)
								{
									url = CreateURL("tracks", new string[] { "ids=" + String.Join(",", trackIDs), String.Format("offset={0}", offset), "limit=50" });
									request = (HttpWebRequest)WebRequest.Create(url);
									using (var tracksResponse = (HttpWebResponse)request.GetResponse())
									{
										using (var tracksReader = new StreamReader(tracksResponse.GetResponseStream()))
										{
											try
											{
												ObservableCollection<Track> nextTracks = ParseTracks(JArray.Parse(tracksReader.ReadToEnd()));
												if (nextTracks.Count == 0)
													break;

												foreach (Track t in nextTracks)
													unsortedTracks.Add(t);
											}
											catch { }
										}
									}
									offset += 50;
								}
							}
						}
					}

					// sort the tracks by playback count
					var sortedTracks = unsortedTracks.OrderByDescending(t => t.Views);
					foreach (Track t in sortedTracks)
						tracks.Add(t);
					
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "soundcloud", "Could not fetch explore feed: " + e.Message);
					VerifyConnectivity();
				}

				// run old code as backup
				if (tracks.Count == 0)
				{
					string url = CreateURL("tracks", new string[] { "order=hotness", "limit=50" });
					var request = (HttpWebRequest)WebRequest.Create(url);
					using (var response = (HttpWebResponse)request.GetResponse())
					{
						using (var reader = new StreamReader(response.GetResponseStream()))
						{
							tracks = ParseTracks(JArray.Parse(reader.ReadToEnd()));
						}
					}
				}

				failedRequests = 0;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SOUNDCLOUD", "Could not retrieve top tracks: " + e.Message);
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
		/// Searches SoundCloud for tracks matching a certain query
		/// </summary>
		/// <param name="query">The query to search for</param>
		/// <returns>An observable collection of Track with all SoundCloud tracks that match query</returns>
		public override ObservableCollection<Track> Search(string query)
		{
			IsLoading = true;
			ObservableCollection<Track> tracks = new ObservableCollection<Track>();
			try
			{
				string url = CreateURL("tracks", new string[] { "limit=50", U.CreateParam("q", query, "") });
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						tracks = ParseTracks(JArray.Parse(reader.ReadToEnd()));
					}
				}
				failedRequests = 0;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SOUNDCLOUD", "Could not perform search: " + e.Message);
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
		/// Retrieves the stream URL for a SoundCloud track
		/// </summary>
		/// <param name="track">The SoundCloud track</param>
		public string GetStreamURL(Track track)
		{
			return String.Format("{0}/tracks/{1}/stream?client_id={2}", UriBase, track.Path.Substring(PathPrefix.Length), U.SoundCloudID);
		}

		/// <summary>
		/// Creates a track given a SoundCloud path.
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>The track if it could be found, otherwise null</returns>
		public override Track CreateTrack(string path)
		{
			try
			{
				string url = CreateURL("tracks/" + GetID(path), new string[] {});
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						return ParseTrack(JObject.Parse(reader.ReadToEnd()));
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SOUNDCLOUD", "Could not fetch track: " + e.Message);
				VerifyConnectivity();
			}
			return null;
		}

		#endregion

		#region Private

		/// <summary>
		/// Create a request URL.
		/// </summary>
		/// <param name="property">The property object</param>
		/// <param name="arguments">A list of arguments in the format "key=value"</param>
		/// <param name="format">The format (either json or xml)</param>
		/// <returns></returns>
		private string CreateURL(string property, string[] arguments, string format = "json")
		{
			if (!String.IsNullOrWhiteSpace(format) && format[0] != '.')
				format = '.' + format;
			string url = String.Format("{0}/{1}{2}?client_id={3}", UriBase, property, format, U.SoundCloudID);
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
				JObject user = (JObject)json["user"];

				Track track = new Track();
				track.Path = String.Format("{0}{1}", PathPrefix, json["id"]);
				track.Length = Convert.ToDouble((int)json["duration"]) / 1000.0;
				track.Genre = (string)json["genre"];
				track.ArtURL = (string)json["artwork_url"];
				track.URL = (string)json["permalink_url"];
				track.IsActive = Settings.Manager.CurrentTrack != null && Settings.Manager.CurrentTrack.Path == track.Path;

				track.Image = track.ArtURL;
				if (String.IsNullOrWhiteSpace(track.Image))
					track.Image = track.Icon;

				JToken token = json["release_year"];
				if (token != null && token.Type != JTokenType.Null)
					track.Year = (uint)token;

				token = json["playback_count"];
				if (token != null && token.Type != JTokenType.Null)
					track.Views = (ulong)token;

				string[] str = U.ParseTitle((string)json["title"]);
				track.Artist = str[0];
				track.Title = str[1];
				if (String.IsNullOrWhiteSpace(track.Artist) && user != null)
					track.Artist = (string)user["username"];

				return track;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SOUNDCLOUD", "Could not parse track JSON data: " + e.Message);
				return null;
			}
		}

		#endregion

		#endregion

	}
}
