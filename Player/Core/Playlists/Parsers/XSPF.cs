/***
 * XSPF.cs
 * 
 * Reads and writes playlist files in XSPF format.
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
	/// Parser of playlists in XSPF format.
	/// </summary>
	public class XSPF : Parser
	{
		#region Methods

		/// <summary>
		/// Parse the trackList element in the XML file.
		/// </summary>
		/// <param name="xmlReader">XmlReader object.</param>
		/// <param name="playlist">Playlist.</param>
		private void ParseTrackList(XmlReader xmlReader, Playlist playlist)
		{
			while (xmlReader.ReadToFollowing ("track"))
			{
				var track = new Track();
				try
				{
					while (xmlReader.Read())
					{
						if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "track")
							break;

						if (xmlReader.NodeType != XmlNodeType.Element)
							continue;

						switch (xmlReader.Name.ToLower())
						{
						case "title":
							xmlReader.Read();
							track.Title = xmlReader.Value;
							break;

						case "creator":
							xmlReader.Read();
							track.Artist = xmlReader.Value;
							break;

						case "location":
							xmlReader.Read();
							track.Path = xmlReader.Value;
							if (!String.IsNullOrWhiteSpace(track.URL))
								track.URL = track.Path;
							break;

						case "image":
							xmlReader.Read();
							track.ArtURL = xmlReader.Value;
							track.OriginalArtURL = track.ArtURL;
							track.Image = track.ArtURL;
							break;

						case "album":
							xmlReader.Read();
							track.Album = xmlReader.Value;
							break;

						case "trackNum":
							xmlReader.Read();
							track.TrackNumber = Convert.ToUInt32(xmlReader.Value);
							break;

						case "duration":
							xmlReader.Read();
							track.Length = Convert.ToDouble(xmlReader.Value) / 1000;
							break;

						case "link":
							xmlReader.Read();
							track.URL = xmlReader.Value;
							break;
						}
					}
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "XSPF Parser", "Could not parse entry: " + e.Message);
				}

				if (!String.IsNullOrWhiteSpace(track.Path))
				{
					playlist.Tracks.Add(track);
				}
			}
		}

		/// <summary>
		/// Read a playlist from a stream.
		/// </summary>
		/// <param name="reader">Stream reader.</param>
		/// <param name="path">The relative path of the tracks in the playlist</param>
		/// <param name="resolveMetaData">If true and the playlist contains stream URLs, then a connection will be made to load meta data.</param>
		/// <returns>The playlist.</returns>
		public override List<Playlist> ReadStream (StreamReader reader, string path = "", bool resolveMetaData = true)
		{
			var playlist = new Playlist ();
			var data = reader.ReadToEnd();
			try
			{
				using (var xmlReader = XmlReader.Create(new StringReader(data)))
				{
					xmlReader.ReadToFollowing("playlist");
					var version = xmlReader.GetAttribute("version");
					if (version != "1")
					{
						U.L(LogLevel.Warning, "XSPF Parser", "Unsupported version: " + version);
						return null;
					}

					while (xmlReader.Read())
					{
						if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "playlist")
							break;

						if (xmlReader.NodeType != XmlNodeType.Element)
							continue;

						switch (xmlReader.Name.ToLower())
						{
						case "title":
							xmlReader.Read();
							playlist.Name = xmlReader.Value;
							break;

						case "tracklist":
							ParseTrackList(xmlReader, playlist);
							break;
						}
					}
					xmlReader.Close();
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "XSPF Parser", "Could not parse playlist: " + e.Message);
				return new List<Playlist> ();
			}
			return new List<Playlist>() {playlist};
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
			try
			{
				var settings = new XmlWriterSettings();
				settings.Indent = true;
				settings.NewLineChars = "\r\n";
				settings.IndentChars = "  ";
				settings.NewLineHandling = NewLineHandling.Replace;

				using (var xmlWriter = XmlWriter.Create(writer, settings))
				{
					xmlWriter.WriteStartElement("playlist", "http://xspf.org/ns/0/");
					xmlWriter.WriteAttributeString("version", "1");
					xmlWriter.WriteElementString("title", playlist.Name);

					xmlWriter.WriteStartElement("trackList");

					foreach (var track in playlist.Tracks)
					{
						xmlWriter.WriteStartElement("track");

						if (!String.IsNullOrWhiteSpace(track.Title))
							xmlWriter.WriteElementString("title", track.Title);

						if (!String.IsNullOrWhiteSpace(track.Artist))
							xmlWriter.WriteElementString("creator", track.Artist);

						if (!String.IsNullOrWhiteSpace(track.Path))
							xmlWriter.WriteElementString("location", track.Path);

						if (!String.IsNullOrWhiteSpace(track.OriginalArtURL))
							xmlWriter.WriteElementString("image", track.OriginalArtURL);
						else if (!String.IsNullOrWhiteSpace(track.ArtURL) && track.ArtURL.StartsWith("http"))
							xmlWriter.WriteElementString("image", track.ArtURL);
						else if (!String.IsNullOrWhiteSpace(track.Image) && track.Image.StartsWith("http"))
							xmlWriter.WriteElementString("image", track.Image);

						if (!String.IsNullOrWhiteSpace(track.Album))
							xmlWriter.WriteElementString("album", track.Album);

						if (track.TrackNumber > 0)
							xmlWriter.WriteElementString("trackNum", U.T(track.TrackNumber));

						if (track.Length > 0)
							xmlWriter.WriteElementString("duration", U.T(track.Length));

						if (!String.IsNullOrWhiteSpace(track.URL))
							xmlWriter.WriteElementString("link", track.URL);

						xmlWriter.WriteEndElement();
					}

					xmlWriter.WriteEndElement();
					xmlWriter.WriteEndElement();
					xmlWriter.WriteWhitespace("\n");
					xmlWriter.Close();
				}
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "XSPF Parser", "Could not write playlist: " + e.Message);
			}
		}

		/// <summary>
		/// Determines if a playlist path is supported by the parser.
		/// </summary>
		/// <returns>true if the path is supported by the parser</returns>
		/// <param name="path">Path.</param>
		public override bool Supports (string path)
		{
			var ext = Path.GetExtension (path);
			return !String.IsNullOrWhiteSpace (ext) && ext.ToLower () == ".xspf";
		}

		#endregion
	}
}

