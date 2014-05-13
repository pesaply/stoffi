/***
 * ListConfig.cs
 * 
 * Describes a configuration of a list of content (columned list, icon grid, etc.)
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

namespace Stoffi.Core.Settings
{
	/// <summary>
	/// Describes a configuration for the list displaying content.
	/// </summary>
	public class ListConfig : PropertyChangedBase
	{
		#region Members

		private ObservableCollection<ListColumn> columns = new ObservableCollection<ListColumn>();
		private ListColumn numberColumn = new ListColumn();
		private ObservableCollection<uint> selectedIndices = new ObservableCollection<uint>();
		private ObservableCollection<string> sorts = new ObservableCollection<string>();
		private string filter = "";
		private bool hasNumber = true;
		private bool isNumberVisible = true;
		private int numberIndex = 0;
		private bool useIcons = true;
		private bool acceptFileDrops = false;
		private bool isDragSortable = true;
		private bool isClickSortable = true;
		private bool lockSortOnNumber = false;
		private double verticalScrollOffset = 0;
		private double horizontalScrollOffset = 0;
		private double verticalScrollOffsetWithoutSearch = 0;
		private ViewMode mode = ViewMode.Details;
		private double iconSize = 64;
		private bool isLoading = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the columns
		/// </summary>
		public ObservableCollection<ListColumn> Columns
		{
			get { return columns; }
			set
			{
				if (columns != null)
					columns.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<ListColumn>> (ref columns, value, "Columns");
				if (columns != null)
				{
					foreach (var c in columns) {
						c.PropertyChanged -= Column_PropertyChanged;
						c.PropertyChanged += Column_PropertyChanged;
					}
					columns.CollectionChanged += CollectionChanged;
				}
			}
		}

		/// <summary>
		/// Gets or sets the number column configuration
		/// </summary>
		public ListColumn NumberColumn
		{
			get { return numberColumn; }
			set { SetProp<ListColumn> (ref numberColumn, value, "NumberColumn"); }
		}

		/// <summary>
		/// Gets or sets the indices of the selected items
		/// </summary>
		public ObservableCollection<uint> SelectedIndices
		{
			get { return selectedIndices; }
			set
			{ 
				if (selectedIndices != null)
					selectedIndices.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<uint>> (ref selectedIndices, value, "SelectedIndices");
				if (selectedIndices != null)
					selectedIndices.CollectionChanged += CollectionChanged;
			}
		}

		/// <summary>
		/// Gets or sets the the sort orders
		/// Each sort is represented as a string on the format
		/// "asc/dsc:ColumnName"
		/// </summary>
		public ObservableCollection<string> Sorts
		{
			get { return sorts; }
			set
			{
				if (sorts != null)
					sorts.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<string>> (ref sorts, value, "Sorts");
				if (sorts != null)
					sorts.CollectionChanged += CollectionChanged;
			}
		}

		/// <summary>
		/// Gets or sets text used to filter the list
		/// </summary>
		public string Filter
		{
			get { return filter; }
			set { SetProp<string> (ref filter, value, "Filter"); }
		}

		/// <summary>
		/// Gets or sets whether the number column should be enabled
		/// </summary>
		public bool HasNumber
		{
			get { return hasNumber; }
			set { SetProp<bool> (ref hasNumber, value, "HasNumber"); }
		}

		/// <summary>
		/// Gets or sets whether the number column should be visible
		/// </summary>
		public bool IsNumberVisible
		{
			get { return isNumberVisible; }
			set { SetProp<bool> (ref isNumberVisible, value, "IsNumberVisible"); }
		}

		/// <summary>
		/// Gets or sets the position of the number column
		/// </summary>
		public int NumberIndex
		{
			get { return numberIndex; }
			set { SetProp<int> (ref numberIndex, value, "NumberIndex"); }
		}

		/// <summary>
		/// Gets or sets whether to display icons or not
		/// </summary>
		public bool UseIcons
		{
			get { return useIcons; }
			set { SetProp<bool> (ref useIcons, value, "UseIcons"); }
		}

		/// <summary>
		/// Gets or sets whether files can be dropped onto the list
		/// </summary>
		public bool AcceptFileDrops
		{
			get { return acceptFileDrops; }
			set { SetProp<bool> (ref acceptFileDrops, value, "AcceptFileDrops"); }
		}

		/// <summary>
		/// Gets or sets whether the list can be resorted via drag and drop
		/// </summary>
		public bool IsDragSortable
		{
			get { return isDragSortable; }
			set { SetProp<bool> (ref isDragSortable, value, "IsDragSortable"); }
		}

		/// <summary>
		/// Gets or sets whether the list can be resorted by clicking on a column
		/// </summary>
		public bool IsClickSortable
		{
			get { return isClickSortable; }
			set { SetProp<bool> (ref isClickSortable, value, "IsClickSortable"); }
		}

		/// <summary>
		/// Gets or sets whether only the number column can be used to sort the list
		/// </summary>
		public bool LockSortOnNumber
		{
			get { return lockSortOnNumber; }
			set { SetProp<bool> (ref lockSortOnNumber, value, "LockSortOnNumber"); }
		}

		/// <summary>
		/// Gets or sets the vertical scroll offset
		/// </summary>
		public double VerticalScrollOffset
		{
			get { return verticalScrollOffset; }
			set { SetProp<double> (ref verticalScrollOffset, value, "VerticalScrollOffset"); }
		}

		/// <summary>
		/// Gets or sets the horizontal scroll offset
		/// </summary>
		public double HorizontalScrollOffset
		{
			get { return horizontalScrollOffset; }
			set { SetProp<double> (ref horizontalScrollOffset, value, "HorizontalScrollOffset"); }
		}

		/// <summary>
		/// Gets or sets the vertical scroll offset when no search is active.
		/// </summary>
		public double VerticalScrollOffsetWithoutSearch
		{
			get { return verticalScrollOffsetWithoutSearch; }
			set { SetProp<double> (ref verticalScrollOffsetWithoutSearch, value, "VerticalScrollOffsetWithoutSearch"); }
		}

		/// <summary>
		/// Gets or sets the view mode.
		/// </summary>
		public ViewMode Mode
		{
			get { return mode; }
			set { SetProp<ViewMode> (ref mode, value, "Mode"); }
		}

		/// <summary>
		/// Gets or sets the size of the icons.
		/// </summary>
		/// <remarks>
		/// Only applicable when Mode is Icons.
		/// </remarks>
		public double IconSize
		{
			get { return iconSize; }
			set { SetProp<double> (ref iconSize, value, "IconSize"); }
		}

		/// <summary>
		/// Gets or sets whether the list is currently loading the content.
		/// </summary>
		public bool IsLoading
		{
			get { return isLoading; }
			set { SetProp<bool> (ref isLoading, value, "IsLoading"); }
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.ListViewConfig"/> class.
		/// </summary>
		public ListConfig()
		{
			columns.CollectionChanged += CollectionChanged;
			selectedIndices.CollectionChanged += CollectionChanged;
			sorts.CollectionChanged += CollectionChanged;
			numberColumn.PropertyChanged += Column_PropertyChanged;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Get a specific column.
		/// </summary>
		/// <returns>The column.</returns>
		/// <param name="name">The name of the column.</param>
		public ListColumn GetColumn(string name)
		{
			if (name == "Number")
				return NumberColumn;
			else
				foreach (var c in Columns)
					if (c.Name == name)
						return c;
			return null;
		}

		/// <summary>
		/// Invoked when a property of a column changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Column_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged ("Columns");
		}

		/// <summary>
		/// Invoked when a collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (sender is ObservableCollection<ListColumn> && (ObservableCollection<ListColumn>)sender == columns) {
				foreach (var c in columns) {
					c.PropertyChanged -= Column_PropertyChanged;
					c.PropertyChanged += Column_PropertyChanged;
				}
				OnPropertyChanged ("Columns");
			}
			else if (sender is ObservableCollection<string> && (ObservableCollection<string>)sender == sorts)
				OnPropertyChanged ("Sorts");
			else if (sender is ObservableCollection<uint> && (ObservableCollection<uint>)sender == selectedIndices)
				OnPropertyChanged ("SelectedIndices");
		}
		/// <summary>
		/// Creates a config with default values
		/// </summary>
		/// <returns>The newly created config</returns>
		public static ListConfig Create()
		{
			var config = new ListConfig();
			config.HasNumber = true;
			config.IsNumberVisible = false;
			config.Filter = "";
			config.IsClickSortable = true;
			config.IsDragSortable = true;
			config.LockSortOnNumber = false;
			config.UseIcons = true;
			config.AcceptFileDrops = true;
			config.Columns = new ObservableCollection<ListColumn>();
			config.NumberColumn = ListColumn.Create("#", "#", "Number", "Number", 60, Alignment.Right, false);
			return config;
		}

		/// <summary>
		/// Initializes a configuration of a list.
		/// </summary>
		public void Initialize()
		{
			Columns.Add(ListColumn.Create("Artist", U.T("ColumnArtist"), 180));
			Columns.Add(ListColumn.Create("Album", U.T("ColumnAlbum"), 160));
			Columns.Add(ListColumn.Create("Title", U.T("ColumnTitle"), 220));
			Columns.Add(ListColumn.Create("Genre", U.T("ColumnGenre"), 90));
			Columns.Add(ListColumn.Create("Length", U.T("ColumnLength"), 70, "Duration", Alignment.Right));
			Columns.Add(ListColumn.Create("Year", U.T("ColumnYear"), 100, Alignment.Right, false));
			Columns.Add(ListColumn.Create("LastPlayed", U.T("ColumnLastPlayed"), 150, "DateTime", Alignment.Left, false));
			Columns.Add(ListColumn.Create("PlayCount", U.T("ColumnPlayCount"), 80, "Number", Alignment.Right));
			Columns.Add(ListColumn.Create("Track", U.T("ColumnTrack"), "TrackNumber", 100, Alignment.Right, false));
			Columns.Add(ListColumn.Create("Path", U.T("ColumnPath"), "Path", 300, Alignment.Left, false));
			Sorts.Add ("asc:Title");
			Sorts.Add ("asc:TrackNumber");
			Sorts.Add ("asc:Album");
			Sorts.Add ("asc:Artist");
		}

		#endregion
	}

	/// <summary>
	/// How the content can be displayed.
	/// </summary>
	public enum ViewMode
	{
		/// <summary>
		/// A columned list which scrolls vertically.
		/// </summary>
		Details,

		/// <summary>
		/// A grid of icons.
		/// </summary>
		Icons,

		/// <summary>
		/// A list which scrolls horizontally.
		/// </summary>
		List,

		/// <summary>
		/// A grid of medium sized icons with meta data.
		/// </summary>
		Tiles,

		/// <summary>
		/// A list of medium sized icons with meta data.
		/// </summary>
		Content
	}
}

