/***
 * Identity.cs
 * 
 * Describes a user's identity on the Stoffi cloud, including
 * user ID, device ID, links to third party services, and settings.
 * 
 * Note that each client instance carries the device's ID in the identity
 * structure. This ID need to be sent with every request to the cloud
 * except during the initial handshake.
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

using Stoffi.Core.Settings;

namespace Stoffi.Core.Services
{
	/// <summary>
	/// Describes a list of mappings between keys and values.
	/// </summary>
	[Serializable()]
	public class Identity : PropertyChangedBase
	{
		#region Members

		private uint userID;
		private uint configurationID;
		private bool synchronize;
		private bool synchronizePlaylists;
		private bool synchronizeConfig;
		private bool synchronizeQueue;
		private bool synchronizeFiles;
		private uint deviceID;
		private ObservableCollection<Link> links = new ObservableCollection<Link>();

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the ID of the owner.
		/// </summary>
		public uint UserID
		{
			get { return userID; }
			set { SetProp<uint> (ref userID, value, "UserID"); }
		}

		/// <summary>
		/// Gets or sets the ID of the device's current configuration profile.
		/// </summary>
		/// <remarks>
		/// We have deprecated the usage of multiple configurations so we will
		/// always use the first one returned by the server, or create one
		/// named "Default" if no configuration profile exists.
		/// </remarks>
		public uint ConfigurationID
		{
			get { return configurationID; }
			set { SetProp<uint> (ref configurationID, value, "ConfigurationID"); }
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize with the cloud.
		/// </summary>
		public bool Synchronize
		{
			get { return synchronize; }
			set { SetProp<bool> (ref synchronize, value, "Synchronize"); }
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize the playlists.
		/// </summary>
		public bool SynchronizePlaylists
		{
			get { return synchronizePlaylists; }
			set { SetProp<bool> (ref synchronizePlaylists, value, "SynchronizePlaylists"); }
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize the configuration.
		/// </summary>
		public bool SynchronizeConfig
		{
			get { return synchronizeConfig; }
			set { SetProp<bool> (ref synchronizeConfig, value, "SynchronizeConfig"); }
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize the play queue.
		/// </summary>
		public bool SynchronizeQueue
		{
			get { return synchronizeQueue; }
			set { SetProp<bool> (ref synchronizeQueue, value, "SynchronizeQueue"); }
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize the files.
		/// </summary>
		public bool SynchronizeFiles
		{
			get { return synchronizeFiles; }
			set { SetProp<bool> (ref synchronizeFiles, value, "SynchronizeFiles"); }
		}

		/// <summary>
		/// Gets or sets the ID of the device.
		/// </summary>
		public uint DeviceID
		{
			get { return deviceID; }
			set { SetProp<uint> (ref deviceID, value, "DeviceID"); }
		}

		/// <summary>
		/// Gets or sets the links to third party accounts.
		/// </summary>
		public ObservableCollection<Link> Links
		{
			get { return links; }
			set { SetProp<ObservableCollection<Link>> (ref links, value, "Links"); }
		}

		#endregion
	}
}

