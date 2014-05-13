/***
 * Metadata.cs
 * 
 * Holds the meta data for a plugin, including unique ID,
 * install date, whether the plugin is running, and any settings
 * for the plugin.
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

using Stoffi.Core.Settings;

namespace Stoffi.Core.Plugins
{
	/// <summary>
	/// Describes a plugin's settings.
	/// </summary>
	/// <remarks>
	/// Used to store the settings in an XML file.
	/// </remarks>
	public class Metadata : PropertyChangedBase
	{
		#region Members

		private string pluginID;
		private ObservableCollection<Stoffi.Plugins.Setting> settings = new ObservableCollection<Stoffi.Plugins.Setting> ();
		private bool enabled = false;
		private DateTime installed;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the ID of the plugin.
		/// </summary>
		public string PluginID
		{
			get { return pluginID; }
			set { SetProp<string> (ref pluginID, value, "PluginID"); }
		}

		/// <summary>
		/// Gets or sets the list of settings.
		/// </summary>
		public ObservableCollection<Stoffi.Plugins.Setting> Settings
		{
			get { return settings; }
			set
			{
				if (settings != null)
					settings.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<Stoffi.Plugins.Setting>> (ref settings, value, "Settings");
				if (settings != null) {
					foreach (var setting in settings) {
						setting.PropertyChanged -= Setting_PropertyChanged;
						setting.PropertyChanged += Setting_PropertyChanged;
					}
					settings.CollectionChanged -= CollectionChanged;
				}
			}
		}

		/// <summary>
		/// Gets or sets whether or not the plugin is activated.
		/// </summary>
		public bool Enabled
		{
			get { return enabled; }
			set { SetProp<bool> (ref enabled, value, "Enabled"); }
		}

		/// <summary>
		/// Gets or sets the date that the plugin was installed.
		/// </summary>
		public DateTime Installed
		{
			get { return installed; }
			set { SetProp<DateTime> (ref installed, value, "Installed"); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Invoked when a property of a setting changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Setting_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged ("Settings");
		}

		/// <summary>
		/// Invoked when a collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if ((ObservableCollection<Stoffi.Plugins.Setting>)sender == settings && settings != null)
			{
				foreach (var s in settings) {
					s.PropertyChanged -= Setting_PropertyChanged;
					s.PropertyChanged += Setting_PropertyChanged;
				}
			}
		}

		#endregion
	}
}

