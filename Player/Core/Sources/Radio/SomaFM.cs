/***
 * SomaFM.cs
 * 
 * This file contains code for fetching radio station from SomaFM.
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
using System.Xml;

using Stoffi.Core.Media;

namespace Stoffi.Core.Sources.Radio
{
	/// <summary>
	/// SomaFM music source for radio stations.
	/// </summary>
	public class SomaFM : Base
	{
		/// <summary>
		/// Fill collection with SomaFM stations.
		/// </summary>
		/// <param name="stations">Station collection.</param>
		public override void FetchStations(ObservableCollection<Track> stations)
		{
			var stationGroup = "SomaFM";
			try
			{
				string url = "http://somafm.com/channels.xml";
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				{
					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						var data = reader.ReadToEnd();
						using (var xmlReader = XmlReader.Create(new StringReader(data)))
						{
							xmlReader.ReadToFollowing("channels");
							while (xmlReader.ReadToFollowing("channel"))
							{
								xmlReader.MoveToAttribute("id");
								var id = xmlReader.Value;
								U.L(LogLevel.Debug, "SomaFM", "Adding SonaFM station "+id);
								try
								{
									var station = new Track();
									station.Album = stationGroup;
									station.Group = station.Album;

									while (xmlReader.Read())
									{
										if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "channel")
											break;

										if (xmlReader.NodeType != XmlNodeType.Element)
											continue;

										var name = xmlReader.Name;
										xmlReader.Read();
										var value = xmlReader.Value;

										switch (name)
										{
										case "title":
											station.Title = value;
											break;

										case "dj":
											station.Artist = value;
											break;

										case "genre":
											if (!String.IsNullOrWhiteSpace(value))
												station.Genre = String.Join(", ", from x in value.Split('|') select U.Titleize(x));
											break;

										case "image":
											station.ArtURL = value;
											station.OriginalArtURL = value;
											station.Image = value;
											break;

										case "fastpls":
											if (String.IsNullOrWhiteSpace(station.URL))
											{
												station.URL = value;
												station.Path = station.URL;
												if (Playlists.Manager.IsSupported(value))
												{
													U.L(LogLevel.Debug, "SomaFM", "Resolving streaming URL from "+value);
													var playlists = Playlists.Manager.Parse(value, false);
													if (playlists == null || playlists.Count == 0 || playlists[0].Tracks.Count == 0)
														throw new Exception("No streaming URLs found at " + value);
													// TODO: perhaps we should save all URLs and have some sort of intelligence
													// picking the best one, or let the user switch
													station.Path = playlists[0].Tracks[0].Path;
													station.URL = playlists[0].Tracks[0].URL;
												}
											}
											break;

										case "listeners":
											station.Views = Convert.ToUInt64(value);
											break;
										}
									}

									if (!String.IsNullOrWhiteSpace(station.Path) && !U.ContainsPath(stations, station.Path))
										AddStation("SomaFM", station, stations);
								}
								catch (Exception e)
								{
									U.L(LogLevel.Warning, "SomaFM", "Could not retrieve SonaFM station " + id + ": " + e.Message);
								}
							}
							xmlReader.Close();
						}
						reader.Close();
					}
					response.Close();
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SomaFM", "Could not retrieve SonaFM stations: " + e.Message);
			}
		}
	}
}

