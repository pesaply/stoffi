/**
 * Track.cs
 * 
 * Represents the visualization of a Track data source.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using MonoMac.AppKit;
using MonoMac.Foundation;
using Stoffi.Core;
using Stoffi.Core.Media;

namespace Stoffi.GUI.Models
{
	public class TrackItem : NSObject
	{
		private Track track;

		public Track Track {
			get { return track; }
			set {
				track = value;
				if (track == null)
					Label = new NSString ("");
				else
					Label = new NSString (track.Title);
				if (track != null && !String.IsNullOrWhiteSpace (track.ArtURL)) {
					if (track.ArtURL.StartsWith ("http://") || track.ArtURL.StartsWith ("https://"))
						Icon = new NSImage (new NSUrl (track.ArtURL));
					else if (System.IO.File.Exists (track.ArtURL))
						Icon = new NSImage (track.ArtURL);
				}
			}
		}

		public NSImage Icon { get; private set; }

		public NSString Label { get; private set; }

		public TrackItem (Track track)
		{
			Icon = NSImage.ImageNamed ("default-album-art");
			Track = track;
		}
	}

	/// <summary>
	/// A class used to caching sorted and filtered track collections.
	/// </summary>
	public static class Cached
	{

		#region Classes

		/// <summary>
		/// A class used to cache the sorted and filtered track collection of a track list.
		/// </summary>
		public class TrackList
		{

			#region Constructor

			public TrackList ()
			{
				Tracks = new List<Track> ();
				FilteredAndSortedTracks = new List<Track> ();
				Filter = "";
				Sorts = new NSSortDescriptor[0];
			}

			#endregion

			#region Properties

			/// <summary>
			/// Gets or sets the tracks after filter and sorting.
			/// </summary>
			/// <value>The tracks.</value>
			public List<Track> FilteredAndSortedTracks { get; set; }

			/// <summary>
			/// Gets or sets the tracks.
			/// </summary>
			/// <value>The tracks.</value>
			public List<Track> Tracks { get; set; }

			/// <summary>
			/// Gets or sets the filter.
			/// </summary>
			/// <value>The filter.</value>
			public string Filter { get; set; }

			/// <summary>
			/// Gets or sets the sorts.
			/// </summary>
			/// <value>The sorts.</value>
			public NSSortDescriptor[] Sorts { get; set; }

			#endregion

		}

		#endregion

		#region Constructor

		static Cached ()
		{
			FileList = new TrackList ();
			YouTubeList = new TrackList ();
			SoundCloudList = new TrackList ();
			RadioList = new TrackList ();
			JamendoList = new TrackList ();
			QueueList = new TrackList ();
			HistoryList = new TrackList ();
			Playlists = new Dictionary<string, TrackList> ();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the file list.
		/// </summary>
		/// <value>The file list.</value>
		public static TrackList FileList { get; set; }

		/// <summary>
		/// Gets or sets you tube list.
		/// </summary>
		/// <value>You tube list.</value>
		public static TrackList YouTubeList { get; set; }

		/// <summary>
		/// Gets or sets the sound cloud list.
		/// </summary>
		/// <value>The sound cloud list.</value>
		public static TrackList SoundCloudList { get; set; }

		/// <summary>
		/// Gets or sets the radio list.
		/// </summary>
		/// <value>The radio list.</value>
		public static TrackList RadioList { get; set; }

		/// <summary>
		/// Gets or sets the jamendo list.
		/// </summary>
		/// <value>The jamendo list.</value>
		public static TrackList JamendoList { get; set; }

		/// <summary>
		/// Gets or sets the queue list.
		/// </summary>
		/// <value>The queue list.</value>
		public static TrackList QueueList { get; set; }

		/// <summary>
		/// Gets or sets the history list.
		/// </summary>
		/// <value>The history list.</value>
		public static TrackList HistoryList { get; set; }

		/// <summary>
		/// Gets or sets the playlists.
		/// </summary>
		/// <value>The playlists.</value>
		public static Dictionary<string,TrackList> Playlists { get; set; }

		#endregion

	}

	/// <summary>
	/// The data source for a track model.
	/// </summary>
	public class TrackListDataSource : NSTableViewDataSource
	{

		#region Members

		private ObservableCollection<Track> tracks;
		private Timer refreshTracksDelay = null;
		private string filter = "";
		private NSSortDescriptor[] sortDescriptors = new NSSortDescriptor[0];

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the tracks.
		/// </summary>
		/// <value>The tracks.</value>
		public ObservableCollection<Track> Tracks {
			get {
				return tracks;
			}
			set {
				tracks = value;
				tracks.CollectionChanged += Tracks_CollectionChanged;
				RefreshTracks ();
			}
		}

		/// <summary>
		/// Gets or sets the filter.
		/// </summary>
		/// <value>The filter.</value>
		public string Filter {
			get { return filter; }
			set {
				var changed = !(String.IsNullOrWhiteSpace (filter) && String.IsNullOrWhiteSpace (value)) && filter != value;
				filter = value;
				RefreshTracks (false, changed);
			}
		}

		/// <summary>
		/// Gets or sets the sort descriptors.
		/// </summary>
		/// <value>The sort descriptors.</value>
		public NSSortDescriptor[] SortDescriptors {
			get {
				return sortDescriptors;
			}
			set {
				//var changed = !SortsAreSame (sortDescriptors, value);
				sortDescriptors = value;
				RefreshTracks (true, false);
			}
		}

		/// <summary>
		/// Get the tracks filtered with the filter and ordered using the sort descriptors.
		/// </summary>
		/// <value>The filtered and sorted tracks.</value>
		public ObservableCollection<Track> FilteredAndSortedTracks {
			get;
			set;
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.OSX.Player.Models.TrackDataSource"/> class.
		/// </summary>
		public TrackListDataSource ()
		{
			Tracks = new ObservableCollection<Track> ();
			FilteredAndSortedTracks = Tracks;
//			Files.TrackModified += Files_TrackModified;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.OSX.Player.Models.TrackDataSource"/> class.
		/// </summary>
		public TrackListDataSource (ObservableCollection<Track> tracks)
		{
			Tracks = tracks;
			FilteredAndSortedTracks = Tracks;
		}

		#endregion

		#region Methods

		#region Public

		#endregion

		#region Private

		/// <summary>
		/// Refreshs the tracks.
		/// </summary>
		private void RefreshTracks ()
		{
			var sort = SortDescriptors != null && SortDescriptors.Count () > 0;
			var applyFilter = !String.IsNullOrWhiteSpace (Filter);
			RefreshTracks (sort, applyFilter);
		}

		/// <summary>
		/// Refreshs the tracks.
		/// </summary>
		/// <param name="sort">Indicates whether sorting should be applied to the list.</param>
		/// <param name="applyFilter">Indicates whether the filter should be applied to the list.</param>
		public void RefreshTracks (bool sort, bool applyFilter)
		{
			lock (U.GetLock("FilterAndSortTracks")) {
				var removeSort = sort && (SortDescriptors == null || SortDescriptors.Count () == 0);
				FilteredAndSortedTracks = Tracks;
				if (applyFilter)
					FilteredAndSortedTracks = ApplyFilter (FilteredAndSortedTracks);
				if (sort && !removeSort)
					FilteredAndSortedTracks = Sort (FilteredAndSortedTracks);
				OnTracksRefreshed ();
			}
		}

		/// <summary>
		/// Sort the specified tracks.
		/// </summary>
		/// <param name="tracks">Tracks.</param>
		private ObservableCollection<Track> Sort (ObservableCollection<Track> tracks)
		{
			var sortableTracks = new SortableList<Track> ();
			for (int i = 0; i < tracks.Count; i++)
				if (i < tracks.Count)
					sortableTracks.Add (tracks [i]);

			if (SortDescriptors != null) {
				foreach (var sd in SortDescriptors.Reverse ()) {
					sortableTracks.Sort (sd.Key, sd.Ascending);
				}
			}

			return new ObservableCollection<Track> (sortableTracks);
		}

		/// <summary>
		/// Filter out tracks not matching the specified filter.
		/// </summary>
		private ObservableCollection<Track> ApplyFilter (ObservableCollection<Track> tracks)
		{
			if (String.IsNullOrEmpty (Filter))
				return tracks;
			return new ObservableCollection<Track> (from t in tracks
			                                       where U.TrackMatchesQuery (t, Filter)
			                                       select t);
		}

		/// <summary>
		/// Compares two arrays of sort descriptions and checks whether they are the same.
		/// </summary>
		/// <returns><c>true</c>, if the array are equal, <c>false</c> otherwise.</returns>
		/// <param name="sortsX">Sorts x.</param>
		/// <param name="sortsY">Sorts y.</param>
		private bool SortsAreSame (NSSortDescriptor[] sortsX, NSSortDescriptor[] sortsY)
		{
			if (sortsX == null && sortsY == null)
				return true;
			else if (sortsX == null || sortsY == null || sortsX.Count () != sortsY.Count ())
				return false;
			else {
				for (int i = 0; i < sortsX.Count (); i++)
					if (sortsX [i].Key != sortsY [i].Key || sortsX [i].Ascending != sortsY [i].Ascending)
						return false;
				return true;
			}
		}

		#endregion

		#region Event handlers

		private void Tracks_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			lock (U.GetLock("RefreshTracksWithDelay"))
			{
				if (refreshTracksDelay != null)
					refreshTracksDelay.Dispose ();
				refreshTracksDelay = new Timer (PerformRefreshTracks, null, 100, Timeout.Infinite);
			}
		}

		private void Files_TrackModified (object sender, PropertyChangedEventArgs e)
		{
			lock (U.GetLock("RefreshTracksWithDelay"))
			{
				if (refreshTracksDelay != null)
					refreshTracksDelay.Dispose ();
				refreshTracksDelay = new Timer (PerformRefreshTracks, null, 100, Timeout.Infinite);
			}
		}

		private void PerformRefreshTracks (object state)
		{
			lock (U.GetLock("RefreshTracksWithDelay"))
			{
				RefreshTracks ();
			}
		}

		#endregion

		#region Dispatchers

		private void OnTracksRefreshed ()
		{
			if (TracksRefreshed != null)
				TracksRefreshed (this, new EventArgs ());
		}

		#endregion

		#region Overrides

		public override int GetRowCount (NSTableView tableView)
		{
			if (FilteredAndSortedTracks == null)
				return 0;
			return FilteredAndSortedTracks.Count;
		}

		/// <summary>
		/// Gets the object value.
		/// </summary>
		/// <returns>The object value.</returns>
		/// <param name="tableView">Table view.</param>
		/// <param name="tableColumn">Table column.</param>
		/// <param name="row">Row.</param>
		public override NSObject GetObjectValue (NSTableView tableView, NSTableColumn tableColumn, int row)
		{
			var ident = tableColumn.Identifier.ToString ();

			if (FilteredAndSortedTracks == null)
				return null;

			var item = FilteredAndSortedTracks [row];
			var t = item as Track;

			switch (ident) {
			case "Icon":
				switch (t.Type) {
				case TrackType.File:
					return NSImage.ImageNamed ("files");

				case TrackType.Jamendo:
					return NSImage.ImageNamed ("jamendo_color");

				case TrackType.SoundCloud:
					return NSImage.ImageNamed ("soundcloud_color");

				case TrackType.WebRadio:
					return NSImage.ImageNamed ("radio_color");

				case TrackType.YouTube:
					return NSImage.ImageNamed ("youtube_color");
				}
				return NSImage.ImageNamed ("files");

			case "Artist":
				return (NSString)t.Artist;

			case "Album":
				return (NSString)t.Album;

			case "Title":
				return (NSString)t.Title;

			case "Genre":
				return (NSString)t.Genre;

			case "Length":
				return (NSString)U.TimeSpanToString (TimeSpan.FromSeconds (t.Length));

			case "Year":
				if (t.Year > 0)
					return (NSString)t.Year.ToString ();
				return (NSString)"";

			case "LastPlayed":
				if (t.LastPlayed.Year == 1)
					return (NSString)"";
				return (NSString)U.T (t.LastPlayed);

			case "PlayCount":
				if (t.PlayCount > 0)
					return (NSString)U.T ((int)t.PlayCount);
				return (NSString)"";

			case "Path":
				return (NSString)t.Path;

			case "Views":
				return (NSString)U.T (t.Views);

			case "Track":
				if (t.TrackNumber > 0)
					return (NSString)t.TrackNumber.ToString ();
				return (NSString)"";

			case "URL":
				return (NSString)t.URL;
			}
			return (NSString)("Could not get object value for " + ident);
		}

		#endregion

		#endregion

		#region Events

		public event EventHandler TracksRefreshed;

		#endregion

	}

	public class TrackListDelegate : NSTableViewDelegate
	{

		#region Methods

		#region Overrides

		public override void SelectionDidChange (NSNotification notification)
		{
			OnSelectionDidChange ();
		}

		public override void ColumnDidResize (NSNotification notification)
		{
			OnColumnDidResize ();
		}

		public override void DidDragTableColumn (NSTableView tableView, NSTableColumn tableColumn)
		{
			OnDidDragTableColumn (tableColumn);
		}

		public override void DidClickTableColumn (NSTableView tableView, NSTableColumn tableColumn)
		{
			OnDidClickTableColumn (tableColumn);
		}

		#endregion

		#region Dispatchers

		private void OnSelectionDidChange ()
		{
			if (SelectionDidChangeEvent != null)
				SelectionDidChangeEvent (this, new EventArgs ());
		}

		private void OnColumnDidResize ()
		{
			if (ColumnDidResizeEvent != null)
				ColumnDidResizeEvent (this, new EventArgs ());
		}

		private void OnDidDragTableColumn (NSTableColumn column)
		{
			if (DidDragTableColumnEvent != null)
				DidDragTableColumnEvent (this, new NSTableViewTableEventArgs (column));
		}

		private void OnDidClickTableColumn (NSTableColumn column)
		{
			if (DidClickTableColumnEvent != null)
				DidClickTableColumnEvent (this, new NSTableViewTableEventArgs (column));
		}

		#endregion

		#endregion

		#region Events

		public event EventHandler SelectionDidChangeEvent;
		public event EventHandler ColumnDidResizeEvent;
		public event EventHandler<NSTableViewTableEventArgs> DidDragTableColumnEvent;
		public event EventHandler<NSTableViewTableEventArgs> DidClickTableColumnEvent;

		#endregion

	}
}
