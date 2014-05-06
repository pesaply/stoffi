/**
 * Visualizer.cs
 * 
 * All the functionality for a visualizer plugin
 * 
 * * * * * * * * *
 * 
 * Copyright 2012 Simplare
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
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stoffi.Plugins
{
	/// <summary>
	/// A visualizer plugin.
	/// </summary>
	public class Visualizer : Plugin
	{
		#region Members
		#endregion

		#region Properties

		/// <summary>
		/// A float array of size 1024 containing FFT data points.
		/// This will be updated by Stoffi.
		/// </summary>
		public float[] FFTData { get; set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of this class.
		/// </summary>
		/// <param name="id">A string identifying the plugin</param>
		/// <param name="version">The assembly version</param>
		/// <param name="platformVersion">The minimum version required of the plugin platform</param>
		public Visualizer(string id, Version version, Version platformVersion)
			: base(id, version, platformVersion)
		{
			pluginType = PluginType.Visualizer;
			FFTData = new float[1024];
		}

		#endregion

		#region Methods

		#region Protected

		#endregion

		#endregion
	}
}
