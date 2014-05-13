/***
 * WPL.cs
 * 
 * Reads and writes playlist files in WPL format.
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
	/// Parser of playlists in WPL format.
	/// </summary>
	public class WPL : Parser
	{
		#region Methods

		/// <summary>
		/// Parse the head element.
		/// </summary>
		/// <param name="xmlReader">XmlReader object.</param>
		/// <param name="playlist">Playlist.</param>
		private void ParseHead(XmlReader xmlReader, Playlist playlist)
		{
			xmlReader.ReadToFollowing("head");
			xmlReader.Read ();
			while (xmlReader.Read())
			{
				if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "head")
					break;

				if (xmlReader.NodeType != XmlNodeType.Element)
					continue;

				switch (xmlReader.Name.ToLower ()) {
				case "title":
					xmlReader.Read ();
					playlist.Name = xmlReader.Value;
					break;

				case "meta":
					var name = xmlReader.GetAttribute ("name");
					var content = xmlReader.GetAttribute ("content");
					if (name == "totalDuration")
						playlist.Time = Convert.ToDouble (content);
					break;
				}
			}
		}

		/// <summary>
		/// Parse the body element.
		/// </summary>
		/// <param name="xmlReader">XmlReader object.</param>
		/// <param name="playlist">Playlist.</param>
		private void ParseBody(XmlReader xmlReader, Playlist playlist)
		{
			xmlReader.ReadToFollowing("body");
			while (xmlReader.ReadToFollowing ("media"))
			{
				var track = new Track();
				try
				{
					var src = xmlReader.GetAttribute("src");
					track.Path = src;
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "WPL Parser", "Could not parse entry: " + e.Message);
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
					ParseHead(xmlReader, playlist);
					ParseBody(xmlReader, playlist);
					xmlReader.Close();
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "WPL Parser", "Could not parse playlist: " + e.Message);
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
				settings.OmitXmlDeclaration = true;

				using (var xmlWriter = XmlWriter.Create(writer, settings))
				{
					writer.WriteLine("<?wpl version=\"1.0\"?>");
					xmlWriter.WriteStartElement("smil");

					xmlWriter.WriteStartElement("head");
					xmlWriter.WriteElementString("title", playlist.Name);

					xmlWriter.WriteStartElement("meta");
					xmlWriter.WriteAttributeString("name", "Generator");
					xmlWriter.WriteAttributeString("content", "Stoffi Music Player -- " + Settings.Manager.Version.ToString());
					xmlWriter.WriteEndElement();

					xmlWriter.WriteStartElement("meta");
					xmlWriter.WriteAttributeString("name", "TotalDuration");
					xmlWriter.WriteAttributeString("content", U.T((uint)playlist.Time));
					xmlWriter.WriteEndElement();

					xmlWriter.WriteStartElement("meta");
					xmlWriter.WriteAttributeString("name", "ItemCount");
					xmlWriter.WriteAttributeString("content", U.T(playlist.Tracks.Count));
					xmlWriter.WriteEndElement();

					xmlWriter.WriteEndElement(); // head

					xmlWriter.WriteStartElement("body");
					xmlWriter.WriteStartElement("seq");

					foreach (var track in playlist.Tracks)
					{
						xmlWriter.WriteStartElement("media");
						xmlWriter.WriteAttributeString("src", track.Path);
						xmlWriter.WriteEndElement();
					}

					xmlWriter.WriteEndElement(); // seq
					xmlWriter.WriteEndElement(); // body

					xmlWriter.WriteEndElement(); // smil
					xmlWriter.WriteWhitespace("\n");
					xmlWriter.Close();
				}
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "WPL Parser", "Could not write playlist: " + e.Message);
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
			return !String.IsNullOrWhiteSpace (ext) && ext.ToLower () == ".wpl";
		}

		#endregion
	}
}

