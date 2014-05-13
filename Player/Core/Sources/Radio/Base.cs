/***
 * Base.cs
 * 
 * This file contains the base for a radio source.
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
using System.Collections.ObjectModel;
using System.Threading;

using Stoffi.Core.Media;

namespace Stoffi.Core.Sources.Radio
{
	/// <summary>
	/// Describes a source of default radio stations.
	/// </summary>
	public abstract class Base
	{
		/// <summary>
		/// Adds a station to a collection of stations.
		/// </summary>
		/// <param name="sourceName">Name of the station source (used for logging)</param>
		/// <param name="station">Radio station to add</param>
		/// <param name="stations">Collection to add the station to</param>
		/// <remarks>This code is thread safe</remarks>
		protected void AddStation(string sourceName, Track station, ObservableCollection<Track> stations)
		{
			U.L(LogLevel.Information, sourceName, "Added radio station " + station.Path);
			U.GUIContext.Post(_ =>
				{
					while (true)
					{
						try
						{
							stations.Add(station);
							break;
						}
						catch (InvalidOperationException)
						{
							// collection was busy being modified by another thread
							Thread.Sleep(10);
						}
						catch (Exception e)
						{
							U.L(LogLevel.Warning, sourceName, "Could not add radio station: " + e.Message);
							break;
						}
					}
				}, null);
		}

		/// <summary>
		/// Fetch default radio stations and add to collection.
		/// </summary>
		/// <param name="stations">Station collection.</param>
		public abstract void FetchStations(ObservableCollection<Track> stations);
	}
}