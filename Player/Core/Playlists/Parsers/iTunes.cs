/***
 * iTunes.cs
 * 
 * Reads and writes playlist files in iTunes format.
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
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Stoffi.Core.Media;
using Stoffi.Core.Sources;

namespace Stoffi.Core.Playlists.Parsers
{
	/// <summary>
	/// Parser of playlists in iTunes format.
	/// </summary>
	public class iTunes : Parser
	{
		/// <summary>
		/// Read a playlist from a stream.
		/// </summary>
		/// <param name="reader">Stream reader.</param>
		/// <param name="path">The relative path of the tracks in the playlist</param>
		/// <param name="resolveMetaData">If true and the playlist contains stream URLs, then a connection will be made to load meta data.</param>
		/// <returns>The playlist.</returns>
		public override List<Playlist> ReadStream (StreamReader reader, string path = "", bool resolveMetaData = true)
		{
			var playlists = new List<Playlist> ();
			try
			{
				var plist = new PList(reader);

				if (!plist.ContainsKey("Tracks"))
					throw new Exception("file is missing Tracks element");
				if (!plist.ContainsKey("Playlists"))
					throw new Exception("file is missing Playlists element");

				foreach (var p in plist["Playlists"])
				{
					if (p.ContainsKey("Distinguished Kind") || p.ContainsKey("Master"))
						continue; // built-in, we're only interested in user-created

					var playlist = new Playlist();
					playlist.Name = p["Name"];

					foreach (var i in p["Playlist Items"])
					{
						var id = i["Track ID"].ToString();
						if (!plist["Tracks"].ContainsKey(id))
						{
							U.L(LogLevel.Warning, "iTunes Parser", "Playlist "+playlist.Name+" references track "+id+" which is not in Tracks dictionary");
							continue;
						}

						var t = plist["Tracks"][id];

						var track = new Track();
						if (t.ContainsKey("Name"))
							track.Title = t["Name"];
						if (t.ContainsKey("Artist"))
							track.Artist = t["Artist"];
						if (t.ContainsKey("Album"))
							track.Album = t["Album"];
						if (t.ContainsKey("Genre"))
							track.Genre = t["Genre"];
						if (t.ContainsKey("Total Time"))
							track.Length = (double)t["Total Time"] / 1000.0;
						if (t.ContainsKey("Track Number"))
							track.TrackNumber = (uint)t["Track Number"];
						if (t.ContainsKey("Year"))
							track.Year = (uint)t["Year"];
						if (t.ContainsKey("Bit Rate"))
							track.Bitrate = t["Bit Rate"];
						if (t.ContainsKey("Sample Rate"))
							track.SampleRate = t["Sample Rate"];
						if (t.ContainsKey("Location"))
							track.Path = t["Location"];

						if (!String.IsNullOrWhiteSpace(track.Path))
							playlist.Tracks.Add(track);
					}

					playlists.Add(playlist);
				}
			}
			catch (IOException e)
			{
				U.L(LogLevel.Warning, "iTunes Parser", "Could not parse plist: " + e.Message);
			}
			return playlists;
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
		/// <returns>true if the path is supported by the parser</returns>
		/// <param name="path">Path.</param>
		public override bool Supports (string path)
		{
			var ext = Path.GetExtension (path);
			if (!String.IsNullOrWhiteSpace (ext) && ext.ToLower () == ".xml")
			{
				// check if plist
				try
				{
					bool doctype = false;
					using (var reader = new StreamReader(path))
					{
						var data = reader.ReadToEnd();
						var settings = new XmlReaderSettings();
						settings.DtdProcessing = DtdProcessing.Ignore;
						settings.IgnoreWhitespace = true;
						using (var xmlReader = XmlReader.Create(new StringReader(data), settings))
						{
							xmlReader.ReadToFollowing("plist");
							if (xmlReader.EOF)
								return false;
						}
					}
					return true;
				}
				catch { }
			}
			return false;
		}
	}
}

