/***
 * M3U.cs
 * 
 * Reads and writes playlist files in M3U format.
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
	/// Parser of playlists in M3U format.
	/// </summary>
	public class M3U : Parser
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
			bool ext = false;
			string inf = "";
			int nr = 0;
			while ((line = reader.ReadLine()) != null)
			{
				nr++;
				if (line.ToLower() == "#extm3u")
					ext = true;
				else if (ext && line.ToLower().StartsWith("#extinf:"))
					inf = line.Substring(8);
				else if (line.StartsWith("#") || line == "")
					continue;
				else
				{
					string p = line;
					TrackType type = Track.GetType(p);
					Track track;

					string length = "";
					string artist = "";
					string title = "";
					if (inf != "")
					{
						if (!inf.Contains(","))
						{
							U.L(LogLevel.Warning, "M3U Parser", "Bad format on line "
								+ nr + ": expecting ','");
							continue;
						}
						string[] split = inf.Split(',');
						length = split[0];
						if (split[1].Contains("-"))
						{
							artist = split[1].Split('-')[0];
							title = split[1].Split('-')[1];
						}
						else
							title = split[1];
					}

					switch (type)
					{
					case TrackType.File:

						if (!File.Exists(p) && File.Exists(Path.Combine(path, p)))
							p = Path.Combine(path, p);

						if (File.Exists(path))
						{
							if (!Files.PathIsAdded(path))
								Files.AddSource(p);
							foreach (Track t in Settings.Manager.FileTracks)
								if (t.Path == path)
								{
									if (!playlist.Tracks.Contains(t))
										playlist.Tracks.Add(t);
									break;
								}
							inf = "";
						}
						break;

					case TrackType.WebRadio:
						if (resolveMetaData)
							track = Media.Manager.ParseURL (p);
						else
							track = new Track() { Path = p };
						if (String.IsNullOrWhiteSpace(track.URL))
							track.URL = p;
						if (String.IsNullOrWhiteSpace(track.Title))
							track.Title = title;
						if (track != null && !playlist.Tracks.Contains(track))
							playlist.Tracks.Add(track);
						break;

					case TrackType.YouTube:
						track = Sources.Manager.YouTube.CreateTrack(p);
						if (track != null && !playlist.Tracks.Contains(track))
							playlist.Tracks.Add(track);
						break;

					case TrackType.SoundCloud:
						track = Sources.Manager.SoundCloud.CreateTrack(p);
						if (track != null && !playlist.Tracks.Contains(track))
							playlist.Tracks.Add(track);
						break;

					case TrackType.Jamendo:
						track = Sources.Manager.Jamendo.CreateTrack(p);
						if (track != null && !playlist.Tracks.Contains(track))
							playlist.Tracks.Add(track);
						break;
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
			writer.WriteLine("#EXTM3U");
			writer.WriteLine("");
			foreach (var track in playlist.Tracks)
			{
				writer.WriteLine(String.Format("#EXTINF:{0},{1} - {2}", (int)track.Length, track.Artist, track.Title));
				writer.WriteLine(track.Path);
				writer.WriteLine("");
			}
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
			var supported = new List<string>() { ".m3u", ".m3u8" };
			return !String.IsNullOrWhiteSpace (ext) && supported.Contains (ext.ToLower ());
		}

		#endregion
	}
}

