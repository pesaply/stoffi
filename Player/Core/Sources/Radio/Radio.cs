/***
 * Radio.cs
 * 
 * This file contains all the default radio stations in Stoffi.
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
	/// Radio station tools.
	/// </summary>
	public static class Manager
	{
		#region Fields
		private static ObservableCollection<Track> stations = new ObservableCollection<Track>();
		private static List<Radio.Base> sources = new List<Radio.Base>();
		#endregion

		#region Properties
		/// <summary>
		/// Gets the default radio stations.
		/// </summary>
		/// <value>The default stations.</value>
		public static ObservableCollection<Track> DefaultStations { get { return stations; }}
		#endregion

		#region Constructor
		static Manager()
		{
			sources.Add (new SomaFM ());
			sources.Add (new DigitallyImported ());
			sources.Add (new JazzRadio ());
			sources.Add (new RockRadio ());
			sources.Add (new SkyFM ());
		}
		#endregion

		#region Methods

		#region Public
		/// <summary>
		/// Populate the radio station list with default stations.
		/// </summary>
		public static void PopulateDefaults()
		{
			var t = new Thread (delegate() {
				foreach (var s in sources)
					s.FetchStations (Settings.Manager.RadioTracks);
			});
			t.Name = "Populate default radio stations";
			t.Priority = ThreadPriority.BelowNormal;
			t.Start ();
		}
		#endregion

		#region Private

		#endregion

		#endregion
	}
}

