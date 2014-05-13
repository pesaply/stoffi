/***
 * YouTube.cs
 * 
 * Reads playlists from YouTube.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using Stoffi.Core.Media;

namespace Stoffi.Core.Playlists.Parsers
{
	/// <summary>
	/// Parser of playlists on YouTube.
	/// </summary>
	public class YouTube : Parser
	{
		#region Members
		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.YouTube"/> class.
		/// </summary>
		public YouTube()
		{
		}

		#endregion

		#region Methods

		/// <summary>
		/// Read a playlist from a path.
		/// </summary>
		/// <param name="path">Path.</param>
		public new Playlist Read(string path)
		{
			try
			{
				var pl = new Playlists.Playlist();
				string id = GetPlaylistID(path);

				var ids = new List<string>();
				string filter = Sources.Manager.YouTube.FilterID(Settings.Manager.YouTubeFilter);
				string url = Sources.Manager.YouTube.CreateURL("playlistItems", new string[] {
					"part=contentDetails", "playlistId="+id, "fields=items%2FcontentDetails%2FvideoId" });
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
							var videoId = (string)((item as JObject)["contentDetails"] as JObject)["videoId"];
							if (!String.IsNullOrWhiteSpace(id))
								ids.Add(videoId);
						}
					}
				}

				if (ids.Count > 0)
				{
					var fields = Sources.Manager.YouTube.Fields;
					var parts = Sources.Manager.YouTube.Parts;
					url = Sources.Manager.YouTube.CreateURL("videos", new string[] {
						"id=" + String.Join(",", ids), fields, parts });
					request = (HttpWebRequest)WebRequest.Create(url);
					using (var response = (HttpWebResponse)request.GetResponse())
					{
						using (var reader = new StreamReader(response.GetResponseStream()))
						{
							var data = reader.ReadToEnd();
							var result = JObject.Parse(data);
							pl.Tracks = Sources.Manager.YouTube.ParseTracks((JArray)result["items"]);
						}
					}
				}

				return pl;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "PLAYLIST", "Error fetching playlist: " + e.Message);
			}
			return null;
		}

		/// <summary>
		/// Read playlists from a stream.
		/// </summary>
		/// <param name="reader">Stream reader.</param>
		/// <param name="path">The relative path of the tracks in the playlist</param>
		/// <param name="resolveMetaData">If true and the playlist contains stream URLs, then a connection will be made to load meta data.</param>
		/// <returns>The playlists.</returns>
		public override List<Playlist> ReadStream (StreamReader reader, string path = "", bool resolveMetaData = true)
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Write a playlist to a stream.
		/// </summary>
		/// <param name="reader">Stream reader.</param>
		/// <param name="playlist">Playlist.</param>
		/// <param name="writer">Writer.</param>
		/// <param name="extension">The extension of the playlist path.</param>
		public override void WriteStream (Playlist playlist, StreamWriter writer, string extension = null)
		{
			throw new NotImplementedException ();
		}

		/// <summary>
		/// Determines if a playlist path is supported by the parser.
		/// </summary>
		/// <returns>true</returns>
		/// <c>false</c>
		/// <param name="path">Path.</param>
		public override bool Supports (string path)
		{
			string pattern = @"https?://(www\.)?youtube.com/playlist\?(\w+=[^&=]*&)*list=(PLA)?\w+(&\w+=[^&=]*)*";
			Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
			return rgx.IsMatch(path);
		}
		/// <summary>
		/// Extracts the playlist ID of a YouTube playlist's URL.
		/// </summary>
		/// <param name="url">The URL of the playlist</param>
		/// <returns>The playlist ID</returns>
		public string GetPlaylistID(string url)
		{
			if (Supports(url))
			{
				string pattern = @"https?://(www\.)?youtube.com/playlist\?(\w+=[^&=]*&)*list=(\w+)";
				Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
				Match m = rgx.Match(url);
				if (m != null)
				{
					GroupCollection groups = m.Groups;
					return groups[groups.Count - 1].Value;
				}
			}

			throw new Exception("Trying to extract YouTube playlist ID from non-YouTube playlist: " + url);
		}

		#endregion
	}
}

