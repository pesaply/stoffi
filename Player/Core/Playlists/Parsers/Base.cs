/***
 * PLS.cs
 * 
 * Reads and writes playlist files in PLS format.
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
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace Stoffi.Core.Playlists
{
	public abstract class Parser
	{
		#region Constructor

		public Parser ()
		{
		}

		#endregion

		#region Methods

		/// <summary>
		/// Read a playlist from a path.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="resolveMetaData">If true and the playlist contains stream URLs, then a connection will be made to load meta data.</param>
		public List<Playlist> Read(string path, bool resolveMetaData = true)
		{
			var playlists = new List<Playlist> ();
			if (String.IsNullOrWhiteSpace (path))
				return playlists;

			try
			{
				if (path.StartsWith("http://") || path.StartsWith("https://"))
				{
					U.L(LogLevel.Debug, "Playlist parser", "Downloading from " + path);
					var request = (HttpWebRequest)WebRequest.Create(path);
					using (var response = (HttpWebResponse)request.GetResponse())
					{
						U.L(LogLevel.Debug, "Playlist parser", "Parsing response from " + path);
						var stream = response.GetResponseStream();
						var encoding = Encoding.GetEncoding("utf-8");
						using (var reader = new StreamReader(stream, encoding))
						{
							playlists = ReadStream(reader, "", resolveMetaData);
							reader.Close();
						}
						response.Close();
					}

					foreach (var playlist in playlists)
						if (String.IsNullOrWhiteSpace(playlist.Name))
						{
							var u = new Uri(path);
							var name = Path.GetFileNameWithoutExtension(u.AbsolutePath);
							playlist.Name = Playlists.Manager.GenerateName(name);
						}
				}
				else
				{
					using (var reader = new StreamReader(path))
					{
						playlists = ReadStream(reader);
						reader.Close();
					}

					foreach (var playlist in playlists)
						if (String.IsNullOrWhiteSpace(playlist.Name))
						{
							string filename = Path.GetFileNameWithoutExtension(path);
							playlist.Name = Playlists.Manager.GenerateName(filename);
						}
				}
			}
			catch (Exception e)
			{
				U.L (LogLevel.Error, "Playlist", "Could not read playlist from path " + path + ": " + e.Message);
			}
			return playlists;
		}

		/// <summary>
		/// Write a playlist to a path.
		/// </summary>
		/// <param name="playlist">Playlist.</param>
		/// <param name="path">Path.</param>
		public void Write(Playlist playlist, string path)
		{
			try
			{
				var writer = File.AppendText(path);
				WriteStream (playlist, writer, Path.GetExtension(path));
				writer.Close();
			}
			catch (Exception e)
			{
				U.L (LogLevel.Error, "Playlist", "Could not write playlist to path " + path + ": " + e.Message);
			}
		}

		/// <summary>
		/// Read playlists from a stream.
		/// </summary>
		/// <param name="reader">Stream reader.</param>
		/// <param name="path">The relative path of the tracks in the playlist</param>
		/// <param name="resolveMetaData">If true and the playlist contains stream URLs, then a connection will be made to load meta data.</param>
		/// <returns>The playlists.</returns>
		public abstract List<Playlist> ReadStream(StreamReader reader, string path = "", bool resolveMetaData = true);

		/// <summary>
		/// Write a playlist to a stream.
		/// </summary>
		/// <param name="playlist">Playlist object.</param>
		/// <param name="writer">Stream writer.</param>
		/// <param name="extension">The extension of the playlist path.</param>
		public abstract void WriteStream(Playlist playlist, StreamWriter writer, string extension = null);

		/// <summary>
		/// Determines if a playlist path is supported by the parser.
		/// </summary>
		/// <returns><c>true</c> if this parser supports the path; otherwise, <c>false</c>.</returns>
		/// <param name="path">Path.</param>
		public abstract bool Supports(string path);

		#endregion
	}
}

