/**
 * Base.cs
 * 
 * An empty skeleton of a Stoffi plugin.
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
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using Stoffi.Plugins;

namespace Stoffi.Plugins
{
	/// <summary>
	/// A base plugin containing skeleton code for a Stoffi plugin.
	/// Serves as an example and foundation upon which to build a real plugin.
	/// </summary>
	public class Base : Plugin
	{
		#region Members

		Setting myBooleanValue;
		Setting myStringValue;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of a base plugin class.
		/// </summary>
		/// <param name="id">A unique string identifying the plugin</param>
		/// <param name="version">The version of the plugin, specified in the assembly</param>
		public Base(string id, Version version)
			: base(id, version, new Version(0, 3))
		{
			Author = "Simplare";
			Website = "http://dev.stoffiplayer.com/wiki/PluginAPI";

			myBooleanValue = new Setting
			{
				ID = "BooleanValue",
				Type = typeof(System.Boolean),
				Value = (Object)true,
				IsVisible = true
			};

			myStringValue = new Setting
			{
				ID = "StringValue",
				Type = typeof(System.String),
				Value = (Object)true,
				PossibleValues = new List<Object>() { "Foo", "Bar" },
				IsVisible = true
			};

			Settings.Add(myBooleanValue);
			Settings.Add(myStringValue);
		}

		#endregion

		#region Methods

		#region Overrides

		/// <summary>
		/// Called when plugin is installed
		/// </summary>
		///  <returns>True if set up was successfull, otherwise false</returns>
		public override bool OnInstall()
		{
			return true;
		}

		/// <summary>
		/// Called when the plugin is activated
		/// </summary>
		///  <returns>True if starting was successfull, otherwise false</returns>
		public override bool OnStart()
		{
			return true;
		}

		/// <summary>
		/// Called when the plugin is deactivated
		/// </summary>
		///  <returns>True if stopping was successfull, otherwise false</returns>
		public override bool OnStop()
		{
			return true;
		}

		/// <summary>
		/// Called when the plugin is uninstalled
		/// </summary>
		///  <returns>True if tear down was successfull, otherwise false</returns>
		public override bool OnUninstall()
		{
			return true;
		}

		/// <summary>
		/// Updates the plugin
		/// </summary>
		public override void Refresh()
		{
		}

		#endregion

		#endregion
	}
}
