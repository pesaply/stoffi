/***
 * DigitallyImported.cs
 * 
 * This file contains code for fetching radio station from DI.fm.
 *	
 * * * * * * * * *
 * 
 * Copyright 2014 Simplare
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using Stoffi.Core.Media;

namespace Stoffi.Core.Sources.Radio
{
	/// <summary>
	/// DI.fm music source for radio stations.
	/// </summary>
	public class DigitallyImported : Base
	{
		#region Fields
		private string frontPage = null;
		protected string domain = "di.fm";
		protected string folder = "public2";
		protected string name = "Digitally Imported";
		protected string genre = "Electronic";
		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Fill collection with DI.fm stations.
		/// </summary>
		/// <param name="stations">Station collection.</param>
		public override void FetchStations(ObservableCollection<Track> stations)
		{
			try
			{
				string url = "http://listen."+domain+"/"+folder;
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						ParseStations(stations, JToken.Parse(reader.ReadToEnd()));
					}
					response.Close();
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, name, "Could not retrieve stations: " + e.Message);
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Parses the JSON stations.
		/// </summary>
		/// <param name="stations">Stations.</param>
		/// <param name="json">Json data describing the stations.</param>
		private void ParseStations(ObservableCollection<Track> stations, JToken json)
		{
			if (json.Type != JTokenType.Array)
				throw new Exception ("JSON response should be an array but is " + json.Type.ToString ());

			foreach (JObject channel in (JArray)json)
			{
				var station = ParseStation (channel);
				if (station != null && !U.ContainsPath(stations, station.Path))
					AddStation(name, station, stations);
			}
		}

		/// <summary>
		/// Parse a single radio station represented as JSON.
		/// </summary>
		/// <returns>The station.</returns>
		/// <param name="json">Json representation of the station.</param>
		private Track ParseStation(JObject json)
		{
			var station = new Track ();

			station.Title = (string)json ["name"];
			station.Group = name;
			station.Album = station.Group;
			station.Genre = genre;

			station.URL = (string)json["playlist"];
			station.Path = station.URL;
			if (Playlists.Manager.IsSupported(station.Path))
			{
				U.L(LogLevel.Debug, name, "Resolving streaming URL from "+station.Path);
				var playlists = Playlists.Manager.Parse(station.Path, false);
				if (playlists == null || playlists.Count == 0 || playlists[0].Tracks.Count == 0)
					throw new Exception("No streaming URLs found at " + station.Path);
				// TODO: perhaps we should save all URLs and have some sort of intelligence
				// picking the best one, or let the user switch
				station.Path = playlists[0].Tracks[0].Path;
				station.URL = playlists[0].Tracks[0].URL;
			}

			if (String.IsNullOrWhiteSpace (station.Path))
				return null;

			station.ArtURL = FetchArt (station.Title);
			station.OriginalArtURL = station.ArtURL;
			station.Image = station.ArtURL;
			return station;
		}

		/// <summary>
		/// Fetch the front page if it has not already been fetched.
		/// The front page is used for finding art for the channels.
		/// </summary>
		/// <returns>True if the front page was successfully fetched, otherwise false.</returns>
		private bool FetchFrontPage()
		{
			if (!String.IsNullOrWhiteSpace (frontPage))
				return true;

			try
			{
				string url = "http://"+domain;
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						frontPage = reader.ReadToEnd();
					}
					response.Close();
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, name, "Could not retrieve front page: " + e.Message);
				frontPage = null;
			}

			return !String.IsNullOrWhiteSpace (frontPage);
		}

		/// <summary>
		/// Fetch the art for a given channel by searching the front page.
		/// </summary>
		/// <param name="name">Name of the channel.</param>
		/// <returns>The URL to the art image.</returns>
		private string FetchArt(string name)
		{
			if (!FetchFrontPage ())
			{
				U.L (LogLevel.Warning, name, "Could not fetch front page for locating channel art");
				return null;
			}

			string artUrl = null;

			var attribute = @"\s+[\w_-]+=('[^']*'|""[^""]*"")";
			var alt = String.Format (@"\s+alt=('{0}'|""{0}"")", name);
			var src = @"\s+src=('(?<single>[^']+)'|""(?<double>[^""]+)"")";

			// alt before src
			var img = String.Format (@"<img({0})*{1}({0})*{2}({0})*\s*/?>", attribute, alt, src);
			var m = Regex.Match (frontPage, img, RegexOptions.IgnoreCase);

			if (m.Length > 0)
			{
				if (!String.IsNullOrWhiteSpace (m.Groups ["double"].Value))
					artUrl = m.Groups ["double"].Value;
				else if (!String.IsNullOrWhiteSpace (m.Groups ["single"].Value))
					artUrl = m.Groups ["single"].Value;
			}
			else
			{
				// src before alt
				img = String.Format (@"<img({0})*{1}({0})*{2}({0})*\s*/?>", attribute, src, alt);
				m = Regex.Match (frontPage, img, RegexOptions.IgnoreCase);
				if (m.Length > 0)
				{
					if (!String.IsNullOrWhiteSpace (m.Groups ["double"].Value))
						artUrl = m.Groups ["double"].Value;
					else if (!String.IsNullOrWhiteSpace (m.Groups ["single"].Value))
						artUrl = m.Groups ["single"].Value;
				}
			}

			if (!String.IsNullOrWhiteSpace(artUrl))
				artUrl = artUrl.Replace ("25x25", "64x64");
			return artUrl;
		}

		#endregion

		#endregion
	}
}