/***
 * RAM.cs
 * 
 * Reads and writes playlist files in RAM format.
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
using System.IO;

using Stoffi.Core.Media;
using Stoffi.Core.Sources;

namespace Stoffi.Core.Playlists.Parsers
{
	/// <summary>
	/// Parser of playlists in RAM format.
	/// </summary>
	public class RAM : Parser
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
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				if (String.IsNullOrWhiteSpace (line) || line.StartsWith ("#"))
					continue;

				if (Media.Manager.IsSupported(line))
				{
					try
					{
						var track = new Track ();
						track.Path = line;
						track.Title = Path.GetFileNameWithoutExtension (line);
						playlist.Tracks.Add (track);
					}
					catch (Exception e)
					{
						U.L (LogLevel.Warning, "RAM Parser", "Could not parse playlist entry: " + e.Message);
					}
				}
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
			foreach (var track in playlist.Tracks)
				writer.WriteLine(track.Path);
		}

		/// <summary>
		/// Determines if a playlist path is supported by the parser.
		/// </summary>
		/// <returns>true</returns>
		/// <c>false</c>
		/// <param name="path">Path.</param>
		public override bool Supports (string path)
		{
			var ext = Path.GetExtension (path);
			return !String.IsNullOrWhiteSpace (ext) && ext.ToLower() == ".ram";
		}

		#endregion
	}
}

