/***
 * Cloud.cs
 * 
 * Reads playlists from the Stoffi Cloud.
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
using System.IO;
using System.Net;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace Stoffi.Core.Playlists.Parsers
{
	/// <summary>
	/// Parser of playlists in the Stoffi cloud.
	/// </summary>
	public class Cloud : Parser
	{
		#region Members
		private static String pathPrefix = "stoffi:playlist:";
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
				var id = Convert.ToUInt32(path.Substring(pathPrefix.Length));
				return Services.Manager.FollowPlaylist(id);
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
			return path.StartsWith(pathPrefix);
		}

		#endregion
	}
}

