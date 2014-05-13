/***
 * WindowsMediaMetafile.cs
 * 
 * Reads and writes playlist files in ASX, WAX, or WVX format.
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
	/// Parser of playlists in ASX, WAX, or WVX format.
	/// </summary>
	public class WindowsMediaMetafile : Parser
	{
		#region Methods

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
					xmlReader.ReadToFollowing("title");
					xmlReader.Read();
					playlist.Name = xmlReader.Value;

					while (xmlReader.ReadToFollowing ("entry"))
					{
						var track = new Track();
						try
						{
							while (xmlReader.Read())
							{
								if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "entry")
									break;

								if (xmlReader.NodeType != XmlNodeType.Element)
									continue;

								switch (xmlReader.Name.ToLower())
								{
								case "title":
									xmlReader.Read();
									track.Title = xmlReader.Value;
									break;

								case "author":
									xmlReader.Read();
									track.Artist = xmlReader.Value;
									break;

								case "ref":
									track.Path = xmlReader.GetAttribute("href");
									track.URL = track.Path;
									break;
								}
							}
						}
						catch (Exception e)
						{
							U.L(LogLevel.Warning, "Windows Media Metafile Parser", "Could not parse entry: " + e.Message);
						}

						if (!String.IsNullOrWhiteSpace(track.Path))
						{
							playlist.Tracks.Add(track);
						}
					}
					xmlReader.Close();
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "Windows Media Metafile Parser", "Could not parse playlist: " + e.Message);
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
			// decide which formats to support
			var ext = extension.ToLower ();
			var formats = new List<string> ();
			formats.Add (".asf");
			if (ext == ".wax" || ext == ".wvx")
				formats.Add (".wma");
			if (ext == ".wvx")
				formats.Add (".wmv");

			try
			{
				var settings = new XmlWriterSettings();
				settings.Indent = true;
				settings.NewLineChars = "\r\n";
				settings.IndentChars = "  ";
				settings.NewLineHandling = NewLineHandling.Replace;
				settings.OmitXmlDeclaration = true;

				using (var xmlWriter = XmlWriter.Create(writer, settings))
				{
					xmlWriter.WriteStartElement("asx");
					xmlWriter.WriteAttributeString("version", "3.0");
					xmlWriter.WriteElementString("title", playlist.Name);

					foreach (var track in playlist.Tracks)
					{
						var x = Path.GetExtension(track.Path).ToLower();
						if (!formats.Contains(x))
						{
							U.L(LogLevel.Information, "Windows Media Metafile Parser", "Skipping unsupported track: " + track.Path);
							continue;
						}

						xmlWriter.WriteStartElement("entry");

						if (!String.IsNullOrWhiteSpace(track.Title))
							xmlWriter.WriteElementString("title", track.Title);

						if (!String.IsNullOrWhiteSpace(track.Artist))
							xmlWriter.WriteElementString("author", track.Artist);
						xmlWriter.WriteStartElement("ref");
						xmlWriter.WriteAttributeString("href", track.Path);
						xmlWriter.WriteEndElement();
						xmlWriter.WriteEndElement();
					}

					xmlWriter.WriteEndElement();
					xmlWriter.WriteWhitespace("\n");
					xmlWriter.Close();
				}
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Windows Media Metafile Parser", "Could not write playlist: " + e.Message);
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
			var supported = new List<string> () { ".asx", ".wax", ".wvx" };
			return !String.IsNullOrWhiteSpace (ext) && supported.Contains (ext.ToLower ());
		}

		#endregion
	}
}

