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
using System.IO;

using Stoffi.Core.Media;
using Stoffi.Core.Sources;

namespace Stoffi.Core.Playlists.Parsers
{
	/// <summary>
	/// Parser of playlists in PLS format.
	/// </summary>
	public class PLS : Parser
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
			bool hdr = false;
			string version = "";
			int noe = 0;
			int nr = 0;
			string line;

			List<string> lines = new List<string>();

			while ((line = reader.ReadLine()) != null)
			{
				lines.Add(line);
				nr++;
				if (line == "[playlist]")
					hdr = true;
				else if (!hdr)
					U.L(LogLevel.Warning, "PLAYLIST", "Bad format on line "
						+ nr + ": expecting '[playlist]'");
				else if (line.ToLower().StartsWith("numberofentries="))
					noe = Convert.ToInt32(line.Split('=')[1]);
				else if (line.ToLower().StartsWith("version="))
					version = line.Split('=')[1];
			}

			if (!hdr)
				U.L(LogLevel.Warning, "PLAYLIST", "No header found");
			else
			{
				string[,] tracks = new string[noe, 3];
				nr = 0;
				foreach (string l in lines)
				{
					var _l = l.ToLower ();
					if (_l.StartsWith("file") || _l.StartsWith("title") || _l.StartsWith("length"))
					{
						int tmp = 4;
						int index = 0;
						if (_l.StartsWith("title")) { tmp = 5; index = 1; }
						else if (_l.StartsWith("length")) { tmp = 6; index = 2; }

						string[] split = l.Split('=');
						int number = Convert.ToInt32(split[0].Substring(tmp));

						if (number > noe)
							U.L(LogLevel.Warning, "PLAYLIST", "Bad format on line "
								+ nr + ": entry number is '" + number + "' but NumberOfEntries is '" + noe + "'");
						else
							tracks[number - 1, index] = split[1];
					}
					else if (!_l.StartsWith("numberofentries") && _l != "[playlist]" && !_l.StartsWith("version="))
					{
						U.L(LogLevel.Warning, "PLAYLIST", "Bad format on line "
							+ nr + ": unexpected '" + l + "'");
					}
				}
				for (int i = 0; i < noe; i++)
				{
					string p = tracks[i, 0];

					TrackType type = Track.GetType(p);
					Track track;

					switch (type)
					{
					case TrackType.File:
						if (!File.Exists(p) && File.Exists(Path.Combine(path, p)))
							p = Path.Combine(path, p);

						if (File.Exists(p))
						{
							if (!Files.PathIsAdded(p))
								Files.AddSource(p);
							foreach (Track t in Settings.Manager.FileTracks)
								if (t.Path == p)
								{
									if (!playlist.Tracks.Contains(t))
										playlist.Tracks.Add(t);
									break;
								}
						}
						break;

					case TrackType.WebRadio:
						if (resolveMetaData)
							track = Media.Manager.ParseURL (p);
						else
							track = new Track() { Path = p };
						if (track != null && !playlist.Tracks.Contains(track))
						{
							if (String.IsNullOrWhiteSpace(track.Title))
								track.Title = tracks[i, 1];
							if (String.IsNullOrWhiteSpace(track.URL))
								track.URL = p;
							playlist.Tracks.Add(track);
						}
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
			writer.WriteLine("[playlist]");
			writer.WriteLine("");
			int i = 0;
			foreach (var track in playlist.Tracks)
			{
				i++;
				writer.WriteLine(String.Format("File{0}={1}", i, track.Path));
				writer.WriteLine(String.Format("Title{0}={1}", i, track.Title));
				writer.WriteLine(String.Format("Length{0}={1}", i, (int)track.Length));
				writer.WriteLine("");
			}
			writer.WriteLine("NumberOfEntries=" + i);
			writer.WriteLine("Version=2");
		}

		/// <summary>
		/// Determines if a playlist path is supported by the parser.
		/// </summary>
		/// <returns>true if the path is supported by the parser</returns>
		/// <param name="path">Path.</param>
		public override bool Supports (string path)
		{
			var ext = Path.GetExtension (path);
			return !String.IsNullOrWhiteSpace (ext) && ext.ToLower () == ".pls";
		}

		#endregion
	}
}

