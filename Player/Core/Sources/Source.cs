/**
 * Source.cs
 * 
 * Describe a source from which music can be played.
 * 
 * Includes methods for searching and loading meta data.
 * 
 * In the future this will become a plugin type and
 * will be expanded to define playback of the audio as well.
 * By then it will also be modified to account for local
 * files as well.
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
 **/

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

using Stoffi.Core.Media;
using Stoffi.Core.Settings;

namespace Stoffi.Core.Sources
{
	/// <summary>
	/// A source from which music can be played.
	/// </summary>
	public abstract class Source
	{
		#region Members

		private int reconnectInterval = 15000; // ms
		private Timer reconnectTimer = null;
		private Timer searchTimer = null;
		private bool isLoading = false;
		private ListConfig listConfig = new ListConfig();
		protected int failedRequests = 0;

		#endregion

		#region Properties

		/// <summary>
		/// The ID for the source, used in the path URI.
		/// </summary>
		/// <value>The source identified.</value>
		protected abstract string ID { get; }

		/// <summary>
		/// The prefix for the path uri, identifying a track.
		/// </summary>
		/// <value>The path prefix.</value>
		public string PathPrefix { get { return String.Format("stoffi:{0}:track:", ID); } }

		/// <summary>
		/// If the source is based on an Internet API, this specifies
		/// the base of the URI to the API.
		/// </summary>
		/// <remarks>
		/// If this is not null, then VerifyConnectivity can be used to verify that there
		/// is a connection to the API server, if not then the Reconnected event will fire
		/// as soon as the API server can be reached.
		/// </remarks>
		/// <value>The path prefix.</value>
		protected abstract string UriBase { get; }

		/// <summary>
		/// Gets the track collection.
		/// </summary>
		public ObservableCollection<Track> Tracks { get; set; }

		/// <summary>
		/// Gets or sets the configuration of the list displaying the tracks.
		/// </summary>
		public ListConfig ListConfig
		{
			get { return listConfig; }
			set
			{
				if (listConfig != null)
					listConfig.PropertyChanged -= Config_PropertyChanged;
				listConfig = value;
				if (listConfig != null)
				{
					listConfig.PropertyChanged -= Config_PropertyChanged;
					listConfig.PropertyChanged += Config_PropertyChanged;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating the the track list is being populated.
		/// </summary>
		/// <value><c>true</c> if loading; otherwise, <c>false</c>.</value>
		public bool IsLoading
		{
			get { return isLoading; }
			protected set {
				bool changed = isLoading != value;
				isLoading = value;
				ListConfig.IsLoading = value;
				if (changed)
					OnPropertyChanged ("IsLoading");
			}
		}

		#endregion

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.Sources.Source"/> class.
		/// </summary>
		public Source ()
		{
			Tracks = new ObservableCollection<Track> ();
			ListConfig = ListConfig.Create();
			ListConfig.AcceptFileDrops = false;
			ListConfig.IsDragSortable = false;
			ListConfig.Columns.Add(ListColumn.Create("Artist", U.T("ColumnArtist"), 200));
			ListConfig.Columns.Add(ListColumn.Create("Title", U.T("ColumnTitle"), 350));
			ListConfig.Columns.Add(ListColumn.Create("Length", U.T("ColumnLength"), 100, "Duration", Alignment.Right));
			ListConfig.Columns.Add(ListColumn.Create("Year", U.T("ColumnYear"), 100, Alignment.Right, false));
			ListConfig.Columns.Add(ListColumn.Create("URL", U.T("ColumnURL"), 300));
			ListConfig.Columns.Add(ListColumn.Create("Path", U.T("ColumnPath"), 200, Alignment.Left, false));
		}
		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Popualates the track list with tracks from either a search or a top feed
		/// </summary>
		/// <param name="query">An optional search query for selecting tracks.</param>
		public void PopulateTracks(string query = "")
		{
			var t = new Thread (delegate() {
				ObservableCollection<Track> tracks = null;
				if (String.IsNullOrWhiteSpace (query))
					tracks = TopFeed ();
				else
					tracks = Search (query);
				Tracks.Clear ();
				if (tracks != null)
					foreach (var track in tracks)
						Tracks.Add (track);
			});
			t.Name = "Populate tracks";
			t.Priority = ThreadPriority.BelowNormal;
			t.Start ();
		}

		/// <summary>
		/// Returns a list of tracks from one of the top feeds.
		/// </summary>
		/// <param name="feed">The feed.</param>
		/// <returns>An collection of tracks</returns>
		public abstract ObservableCollection<Track> TopFeed(string feed = "");

		/// <summary>
		/// Searches the source for tracks matching a certain query.
		/// </summary>
		/// <param name="query">The query to search for</param>
		/// <returns>An collection of tracks with all tracks that match the query</returns>
		public abstract ObservableCollection<Track> Search(string query);

		/// <summary>
		/// Creates a track using a track path.
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>A track structure representing the track on the source</returns>
		public abstract Track CreateTrack(string path);

		/// <summary>
		/// Checks whether a given track is from this source.
		/// </summary>
		/// <param name="t">The track to check</param>
		/// <returns>True if the track is from this source</returns>
		public bool IsFromHere(Track t)
		{
			return (t != null && IsFromHere(t.Path));
		}

		/// <summary>
		/// Checks whether a given track is from this source.
		/// </summary>
		/// <param name="t">The track to check</param>
		/// <returns>True if the track is from this source</returns>
		public bool IsFromHere(string path)
		{
			if (String.IsNullOrWhiteSpace(path))
				return false;
			return path.ToLower().StartsWith(PathPrefix);
		}

		/// <summary>
		/// Extracts the ID of a track's path.
		/// </summary>
		/// <param name="path">The path of the track</param>
		/// <returns>The track ID</returns>
		public string GetID(string path)
		{
			if (IsFromHere(path))
			{
				path = path.Substring(PathPrefix.Length);
				if (path[path.Length - 1] == '/')
					path = path.Substring(0, path.Length - 1);
				return path;
			}

			throw new Exception("Trying to extract ID from track which is not from this source: Source=" + this.ID + ", Track=" + path);
		}

		#endregion

		#region Protected

		/// <summary>
		/// Checks to see if we are still able to connect to the API server.
		/// </summary>
		protected void VerifyConnectivity()
		{
			lock (U.GetLock("VerifyConnectivity"))
			{
				if (reconnectTimer != null)
					reconnectTimer.Dispose();
				reconnectTimer = new Timer(PerformReconnect, null, 0, reconnectInterval);
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Attempts to reconnect to the API server.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private void PerformReconnect(object state)
		{
			lock (U.GetLock("VerifyConnectivity"))
			{
				if (!String.IsNullOrWhiteSpace (UriBase) && U.Ping (UriBase)) {
					if (reconnectTimer != null)
						reconnectTimer.Dispose ();
					reconnectTimer = null;
					OnReconnected ();
				}
			}
		}

		/// <summary>
		/// Searches the source.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private void PerformSearch(object state)
		{
			lock (U.GetLock("SearchSource" + ID))
			{
				var f = "";
				if (listConfig != null)
					f = listConfig.Filter;
				PopulateTracks (f);
			}
		}

		/// <summary>
		/// Invoked when a property of the list config changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Filter")
			{
				lock (U.GetLock("SearchSource" + ID))
				{
					if (searchTimer != null)
						searchTimer.Dispose();
					searchTimer = new Timer(PerformSearch, null, 500, Timeout.Infinite);
				}
			}
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the Reconnected event
		/// </summary>
		private void OnReconnected()
		{
			if (Reconnected != null)
				Reconnected(null, new EventArgs());
		}

		/// <summary>
		/// Raises the property changed event.
		/// </summary>
		/// <param name="propertyName">Property name.</param>
		protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (null, new PropertyChangedEventArgs (this.ID+":"+propertyName));
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the connectivity has been re-established.
		/// </summary>
		public event EventHandler Reconnected;

		/// <summary>
		/// Occurs when a property changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}

