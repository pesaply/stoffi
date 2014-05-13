/***
 * Link.cs
 * 
 * Describes a link between a user's cloud account and an account
 * at a third party service provider such as Facebook, Google or Twitter.
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

using Stoffi.Core.Settings;

namespace Stoffi.Core.Services
{
	/// <summary>
	/// Describes a link to an account at a third party provider.
	/// </summary>
	[Serializable()]
	public class Link : PropertyChangedBase
	{
		#region Members

		private string provider;
		private bool connected;
		private bool canShare;
		private bool doShare;
		private bool canListen;
		private bool doListen;
		private bool canDonate;
		private bool doDonate;
		private bool canCreatePlaylist;
		private bool doCreatePlaylist;
		private string picture;
		private ObservableCollection<string> names = new ObservableCollection<string>();
		private string url;
		private string connectUrl;
		private uint id;
		private string error = null;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the name of the provider.
		/// </summary>
		public string Provider
		{
			get { return provider; }
			set { SetProp<string> (ref provider, value, "Provider"); }
		}

		/// <summary>
		/// Gets or sets whether the link is connected.
		/// </summary>
		public bool Connected
		{
			get { return connected; }
			set { SetProp<bool> (ref connected, value, "Connected"); }
		}

		/// <summary>
		/// Gets or sets whether it's possible to share stuff on the account.
		/// </summary>
		public bool CanShare
		{
			get { return canShare; }
			set { SetProp<bool> (ref canShare, value, "CanShare"); }
		}

		/// <summary>
		/// Gets or sets whether the user allows share stuff on the account.
		/// </summary>
		public bool DoShare
		{
			get { return doShare; }
			set { SetProp<bool> (ref doShare, value, "Doshare"); }
		}

		/// <summary>
		/// Gets or sets whether it's possible to submit plays to the account.
		/// </summary>
		public bool CanListen
		{
			get { return canListen; }
			set { SetProp<bool> (ref canListen, value, "CanListen"); }
		}

		/// <summary>
		/// Gets or sets whether the user allows sending plays to the account.
		/// </summary>
		public bool DoListen
		{
			get { return doListen; }
			set { SetProp<bool> (ref doListen, value, "DoListen"); }
		}

		/// <summary>
		/// Gets or sets whether it's possible to share donations on the account.
		/// </summary>
		public bool CanDonate
		{
			get { return canDonate; }
			set { SetProp<bool> (ref canDonate, value, "CanDonate"); }
		}

		/// <summary>
		/// Gets or sets whether the user allows sharing donations on the account.
		/// </summary>
		public bool DoDonate
		{
			get { return doDonate; }
			set { SetProp<bool> (ref doDonate, value, "DoDonate"); }
		}

		/// <summary>
		/// Gets or sets whether it's possible to share newly created playlists on the account.
		/// </summary>
		public bool CanCreatePlaylist
		{
			get { return canCreatePlaylist; }
			set { SetProp<bool> (ref canCreatePlaylist, value, "CanCreatePlaylist"); }
		}

		/// <summary>
		/// Gets or sets whether the user allows sharing newly created playlists on the account.
		/// </summary>
		public bool DoCreatePlaylist
		{
			get { return doCreatePlaylist; }
			set { SetProp<bool> (ref doCreatePlaylist, value, "DoCreatePlaylist"); }
		}

		/// <summary>
		/// Gets or sets the user's profile picture.
		/// </summary>
		public string Picture
		{
			get { return picture; }
			set { SetProp<string> (ref picture, value, "Picture"); }
		}

		/// <summary>
		/// Gets or sets the user's names.
		/// </summary>
		public ObservableCollection<string> Names
		{
			get { return names; }
			set { SetProp<ObservableCollection<string>> (ref names, value, "Names"); }
		}

		/// <summary>
		/// Gets or sets the URL for the link (either to the object or for creating a connection).
		/// </summary>
		public string URL
		{
			get { return url; }
			set { SetProp<string> (ref url, value, "URL"); }
		}

		/// <summary>
		/// Gets or sets the URL for the creating a connection.
		/// </summary>
		public string ConnectURL
		{
			get { return connectUrl; }
			set { SetProp<string> (ref connectUrl, value, "ConnectURL"); }
		}

		/// <summary>
		/// Gets or sets the ID of the link.
		/// </summary>
		public uint ID
		{
			get { return id; }
			set { SetProp<uint> (ref id, value, "ID"); }
		}

		/// <summary>
		/// The last error while communicating over the link. If null then the last attempt at
		/// communication was successful.
		/// </summary>
		public string Error
		{
			get { return error; }
			set { SetProp<string> (ref error, value, "Error"); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Invoked when a collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if ((ObservableCollection<string>)sender == names && names != null)
				OnPropertyChanged ("Names");
		}

		#endregion
	}
}

