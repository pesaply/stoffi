/**
 * ViewDetails.cs
 * 
 * A modified ListView that looks extremely sexy.
 * 
 * Features:
 *    Drag-n-Drop
 *    Column sort
 *    Column toggle
 *    Icons
 *    Strikethrough
 *    Active items (graphical highlight)
 *    Explorer-like look
 *    
 * It also sports a convenient storage structure used to
 * import and export of the configuration in order to allow 
 * easy saving of the configuration between different sessions.
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;

namespace Stoffi
{
	/// <summary>
	/// <para>
	/// A modified ListView that looks extremely sexy.
	/// </para>
	/// <para>
	/// Features:
	///     Drag-n-Drop
	///     Column sort
	///     Column toggle
	///     Icons
	///     Strikethrough
	///     Active items (graphical highlight)
	///     Explorer-like look
	/// </para>
	/// <para>
	/// It also sports a convenient storage structure used to
	/// import and export of the configuration in order to allow 
	/// easy saving of the configuration between different sessions.
	/// </para>
	/// </summary>
	public partial class ViewDetails : ListView
	{
		#region Members

		private ContextMenu headerMenu = new ContextMenu();
		private ContextMenu itemMenu = new ContextMenu();
		private MenuItem clearSortMenuItem = new MenuItem();
		private Hashtable columns = new Hashtable();
		private Hashtable columnTable = new Hashtable();
		private Hashtable headerMenuTable = new Hashtable();
		private GridView columnGrid = new GridView();
		private GridViewColumnHeader currentSortColumn = null;
		private ListSortDirection currentSortDirection = ListSortDirection.Ascending;
		private ViewDetailsDropTarget dropTarget;
		private ViewDetailsSearchOverlay searchOverlay;
		private ViewDetailsColumn numberColumn = new ViewDetailsColumn();
		private double lastScroll = 0;
		private bool hasNumber = false;
		private bool isNumberVisible = false;
		private int numberIndex = 0;
		private bool lockSortOnNumber = false;
		private ViewDetailsConfig config = null;
		private string filter = "";
		private int focusItemIndex = -1;
		private bool useAeroHeaders = true;
		private bool scrollViewerInitialized = false;

		#endregion Members

		#region Properties

		/// <summary>
		/// Gets or sets whether the list can be sorted by clicking.
		/// </summary>
		public bool IsClickSortable { get; set; }

		/// <summary>
		/// Gets or sets whether the list can be sorted by dragging.
		/// Will be ignored if LockSortOnNumber is turned on.
		/// </summary>
		public bool IsDragSortable { get; set; }

		/// <summary>
		/// Gets or sets whether files can be dropped onto the list.
		/// </summary>
		public bool AcceptFileDrops { get; set; }

		/// <summary>
		/// Gets or sets whether to use icons or not (requires an Icon property on the sources).
		/// </summary>
		public bool UseIcons { get; set; }
		
		/// <summary>
		/// Gets or sets whether the number column is visible (requires the HasNumber property).
		/// </summary>
		public bool IsNumberVisible
		{
			get { return isNumberVisible; }
			set
			{
				if (HasNumber)
				{
					ToggleColumn("#", value);
					isNumberVisible = value;
					if (config != null)
					{
						config.IsNumberVisible = value;
						config.NumberColumn.IsVisible = value;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the position of the number column.
		/// </summary>
		public int NumberIndex
		{
			get { return numberIndex; }
			set
			{
				if (HasNumber && numberIndex >= 0)
				{
					GridViewColumn gvc = columnGrid.Columns[numberIndex];
					columnGrid.Columns.RemoveAt(numberIndex);
					MenuItem mi = (MenuItem)headerMenu.Items[numberIndex];
					headerMenu.Items.RemoveAt(numberIndex);

					if (value >= 0)
					{
						headerMenu.Items.Insert(value, mi);
						columnGrid.Columns.Insert(value, gvc);
					}
				}
				numberIndex = value;
				if (config != null)
					config.NumberIndex = value;
			}
		}

		/// <summary>
		/// Gets or sets whether to use a number column (requires a Number property on the sources).
		/// </summary>
		public bool HasNumber
		{
			get { return hasNumber; }
			set
			{
				numberColumn.IsVisible = value;
				numberColumn.IsAlwaysVisible = value && lockSortOnNumber;
				hasNumber = value;
				if (value)
					AddColumn(numberColumn, NumberIndex, false);
				else
					RemoveColumn(numberColumn, false);

				if (config != null)
				{
					config.HasNumber = value;
					config.NumberColumn.IsVisible = value;
					config.NumberColumn.IsAlwaysVisible = value && lockSortOnNumber;
				}
			}
		}

		/// <summary>
		/// Gets or sets whether to only allow sorting on the number column.
		/// Requires HasNumber and IsClickSortable.
		/// </summary>
		public bool LockSortOnNumber
		{
			get { return lockSortOnNumber; }
			set
			{
				numberColumn.IsAlwaysVisible = value && hasNumber;
				lockSortOnNumber = value;
				if (value)
				{
					if (Items.SortDescriptions.Count > 0)
						Items.SortDescriptions.Clear();
					Sort(numberColumn, ListSortDirection.Ascending);
				}
				if (config != null)
					config.LockSortOnNumber = value;
			}
		}

		/// <summary>
		/// Gets or sets the configuration of the ViewDetails class.
		/// This will erase all current configuration.
		/// </summary>
		public ViewDetailsConfig Config
		{
			get { return config; }
			set
			{
				if (value != null)
				{
					// clear current columns
					columns.Clear();
					columnGrid.Columns.Clear();
					columnTable.Clear();
					headerMenuTable.Clear();
					headerMenu.Items.Clear();
					InitHeaderMenu();

					// copy configuration
					numberColumn = value.NumberColumn;
					IsClickSortable = value.IsClickSortable;
					IsDragSortable = value.IsDragSortable;
					UseIcons = value.UseIcons;
					Filter = value.Filter;
					AcceptFileDrops = value.AcceptFileDrops;
					SelectIndices(value.SelectedIndices);

					value.Columns.CollectionChanged += new NotifyCollectionChangedEventHandler(Columns_CollectionChanged);

					// add columns
					foreach (ViewDetailsColumn vdc in value.Columns)
						AddColumn(vdc, -1, false);

					NumberIndex = value.NumberIndex;
					HasNumber = value.HasNumber;
					IsNumberVisible = value.IsNumberVisible;
					LockSortOnNumber = value.LockSortOnNumber;

					// apply sorting
					if (value.Sorts != null)
					{
						foreach (string sort in value.Sorts)
						{
							ListSortDirection dir = sort.Substring(0, 3) == "asc" ? ListSortDirection.Ascending : ListSortDirection.Descending;
							string name = sort.Substring(4);
							if (name == "Number") name = "#";
							Sort(columns[name] as ViewDetailsColumn, dir);
						}
					}

					var sv = GetScrollViewer() as ScrollViewer;
					if (sv != null)
					{
						if (value.VerticalScrollOffset != null)
							sv.ScrollToVerticalOffset(value.VerticalScrollOffset);
						if (value.HorizontalScrollOffset != null)
							sv.ScrollToHorizontalOffset(value.HorizontalScrollOffset);
					}

					config = value;
					config.PropertyChanged += new PropertyChangedEventHandler(Config_PropertyChanged);
				}
			}
		}

		/// <summary>
		/// Gets whether or not a selected item has focus or not.
		/// </summary>
		public bool SelectedItemIsFocused
		{
			get
			{
				foreach (ViewDetailsItemData d in SelectedItems)
				{
					ViewDetailsItem i = ItemContainerGenerator.ContainerFromItem(SelectedItem) as ViewDetailsItem;
					if (i != null && i.IsSelected)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Sets the method that will be used to determine whether a specific item matches
		/// a specific string or not.
		/// </summary>
		public ViewDetailsSearchDelegate FilterMatch { get; set; }

		/// <summary>
		/// Gets or sets whether the sort arrow indicator should be inverted.
		/// </summary>
		public bool InvertSortIndicator { get; set; }

		/// <summary>
		/// Gets or sets the string that is used to filter items.
		/// </summary>
		public string Filter
		{
			get { return filter; }
			set
			{
				filter = value;

				if (String.IsNullOrWhiteSpace(value))
					Items.Filter = null;

				else if (FilterMatch != null)
				{
					Items.Filter = delegate(object item)
					{
						return FilterMatch((ViewDetailsItemData)item, value);
					};
				}

				if (config != null && config.Filter != value)
					config.Filter = value;

				var sv = GetScrollViewer() as ScrollViewer;
				if (sv != null && config != null)
				{
					if (String.IsNullOrWhiteSpace(value))
						sv.ScrollToVerticalOffset(config.VerticalScrollOffsetWithoutSearch);
					else
						sv.ScrollToVerticalOffset(0);
				}
			}
		}

		/// <summary>
		/// Gets or sets whether or not to use Aero styled headers
		/// </summary>
		public bool UseAeroHeaders
		{
			get { return useAeroHeaders; }
			set
			{
				useAeroHeaders = value;
				columnGrid.ColumnHeaderContainerStyle = value ? (Style)FindResource("AeroHeaderStyle") : null;
			}
		}

		/// <summary>
		/// Gets or sets the visibility of the search overlay.
		/// </summary>
		public Visibility SearchOverlay
		{
			get { return searchOverlay.Visibility; }
			set { searchOverlay.Visibility = value; }
		}

		#endregion PropertiesWindow

		#region Constructor

		/// <summary>
		/// Creates an instance of the ViewDetails class.
		/// </summary>
		public ViewDetails()
		{
			//U.L(LogLevel.Debug, "VIEW DETAILS", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "VIEW DETAILS", "Initialized");

			// create column headers
			UseAeroHeaders = System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName != "";

			InitHeaderMenu();
			columnGrid.ColumnHeaderContextMenu = headerMenu;

			View = columnGrid;

			numberColumn.Alignment = Alignment.Right;
			numberColumn.Binding = "Number";
			numberColumn.IsVisible = false;
			numberColumn.SortField = "Number";
			numberColumn.Text = "#";
			numberColumn.Name = "#";
			numberColumn.Width = 60;
			numberColumn.IsSortable = true;

			IsClickSortable = true;
			IsDragSortable = true;
			AllowDrop = true;
			UseIcons = true;
			HasNumber = false;
			LockSortOnNumber = false;
			FilterMatch = null;
			AcceptFileDrops = true;

			columnGrid.Columns.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Columns_CollectionChanged);

			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName != "")
				ItemContainerStyle = (Style)TryFindResource("AeroRowStyle");
			else
				ItemContainerStyle = (Style)TryFindResource("ClassicRowStyle");
		}

		#endregion Constructor

		#region Methods

		#region Public

		/// <summary>
		/// Adds a column to the list
		/// </summary>
		/// <param name="column">The column to be added</param>
		/// <param name="index">The index to insert at (-1 means last)</param>
		/// <param name="addToConfig">Whether the column should be added to the config</param>
		public void AddColumn(ViewDetailsColumn column, int index = -1, bool addToConfig = true)
		{
			// create header
			GridViewColumnHeader gvch = new GridViewColumnHeader();
			gvch.Tag = column.Binding;
			gvch.Content = column.Text;
			gvch.HorizontalAlignment = ConvertHAlignment(column.Alignment);
			gvch.Click += Column_Clicked;
			gvch.SizeChanged += Column_SizeChanged;
			gvch.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;

			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName != "")
			{
				gvch.SetResourceReference(GridViewColumnHeader.TemplateProperty, "AeroHeaderTemplate");
				gvch.ContentTemplate = (DataTemplate)FindResource("HeaderTemplate");
			}

			// create column
			GridViewColumn gvc = new GridViewColumn();
			gvc.Header = gvch;
			gvc.CellTemplate = CreateDataTemplate(column.Binding, column.Alignment, false, (UseIcons && columnGrid.Columns.Count == 0), column.Converter);
			gvc.Width = column.Width;

			// create header menu item
			MenuItem mi = new MenuItem();
			mi.Header = column.Text;
			mi.IsCheckable = !column.IsAlwaysVisible;
			mi.Click += new RoutedEventHandler(HeaderMenu_Click);
			mi.IsChecked = column.IsVisible;
			mi.Tag = column.Name;

			columns.Add(column.Name, column);
			columnTable.Add(column.Name, gvc);
			headerMenuTable.Add(column.Name, mi);

			if (index >= 0)
				headerMenu.Items.Insert(index, mi);
			else if (headerMenu.Items.Count > 1)
				headerMenu.Items.Insert(headerMenu.Items.Count - 2, mi);
			else
				headerMenu.Items.Add(mi);

			if (column.IsVisible && index >= 0)
				columnGrid.Columns.Insert(index, gvc);
			else if (column.IsVisible)
				columnGrid.Columns.Add(gvc);

			if (config != null && addToConfig)
			{
				if (config.Columns == null)
					config.Columns = new ObservableCollection<ViewDetailsColumn>();
				config.Columns.Add(column);
			}

			RefreshHeaderMenu();

			column.PropertyChanged += new PropertyChangedEventHandler(ConfigColumn_PropertyChanged);
		}

		/// <summary>
		/// Adds a column to the list
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The text to be displayed</param>
		/// <param name="binding">The value to bind the column to</param>
		/// <param name="sortField">The value to sort on when clicked</param>
		/// <param name="width">The width of the column</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isVisible">Whether the column is visible (only effective if isAlwaysVisible is false)</param>
		public void AddColumn(string name, string text, string binding, string sortField, double width, bool isSortable = true, bool isAlwaysVisible = false, bool isVisible = true)
		{
			ViewDetailsColumn vdc = new ViewDetailsColumn();
			vdc.Name = name;
			vdc.Text = text;
			vdc.Binding = binding;
			vdc.SortField = sortField;
			vdc.IsAlwaysVisible = isAlwaysVisible;
			vdc.Width = width;
			vdc.IsVisible = (isVisible || isAlwaysVisible);
			vdc.IsSortable = isSortable;
			AddColumn(vdc);
		}

		/// <summary>
		/// Adds a column to the list
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The text to be displayed</param>
		/// <param name="binding">The value to bind the column to</param>
		/// <param name="width">The width of the column</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isVisible">Whether the column is visible (only effective if isAlwaysVisible is false)</param>
		public void AddColumn(string name, string text, string binding, double width, bool isSortable = true, bool isAlwaysVisible = false, bool isVisible = true)
		{
			AddColumn(name, text, binding, binding, width, isSortable, isAlwaysVisible, isVisible);
		}

		/// <summary>
		/// Removes a column from the list
		/// </summary>
		/// <param name="column">The column to remove</param>
		/// <param name="removeFromConfig">Whether the column should be removed from the config</param>
		public void RemoveColumn(ViewDetailsColumn column, bool removeFromConfig = true)
		{
			if (headerMenuTable.ContainsKey(column.Text))
			{
				MenuItem mi = (MenuItem)headerMenuTable[column.Text];
				headerMenu.Items.Remove(mi);
				headerMenuTable.Remove(column.Text);
			}

			if (columnTable.ContainsKey(column.Text))
			{
				GridViewColumn gvc = (GridViewColumn)columnTable[column.Text];
				columnGrid.Columns.Remove(gvc);
				columnTable.Remove(column.Text);
			}

			if (columns.ContainsKey(column.Text))
			{
				ViewDetailsColumn vdc = (ViewDetailsColumn)columns[column.Text];
				if (config != null && config.Columns != null && removeFromConfig)
					config.Columns.Remove(vdc);
				columns.Remove(column.Text);
			}
		}

		/// <summary>
		/// Selects a given list of indices of items
		/// </summary>
		/// <param name="indices">The indices of the items to select</param>
		public void SelectIndices(List<int> indices)
		{
			List<object> itemsToSelect = new List<object>();
			foreach (int index in indices)
				if (0 <= index && index < Items.Count)
					itemsToSelect.Add(Items[index]);
			SetSelectedItems(itemsToSelect);
			//if (itemsToSelect.Count > 0)
			//    ScrollIntoView(itemsToSelect.First<object>());

		}

		/// <summary>
		/// Selects an item, gives it focus and scrolls it into view
		/// </summary>
		/// <param name="item">The item inside the list</param>
		public void SelectItem(ViewDetailsItemData item)
		{
			if (item == null)
				return;

			if (Items.Contains(item))
				SelectedItem = item;

			else
			{
				foreach (ViewDetailsItemData i in Items)
				{
					if (i == item)
					{
						SelectedItem = i;
						break;
					}
				}
			}

			FocusItem();
		}

		/// <summary>
		/// Gives the first selected item focus and scrolls it into view
		/// </summary>
		public void FocusItem()
		{
			focusItemIndex = SelectedIndex;
			ItemContainerGenerator.ItemsChanged += FocusItem; // in case we have to wait...
			FocusItem(null, null);
		}

		/// <summary>
		/// Removes the current sorting
		/// </summary>
		/// <param name="keepPositions">Whether or not to keep all items at their current position</param>
		public void ClearSort(bool keepPositions = true)
		{
			if (Items.SortDescriptions.Count > 0)
			{
				if (keepPositions)
				{
					// move items in the source so they are in the same
					// order as the gui items (which are order by sort conditions)
					ObservableCollection<object> items = ItemsSource as ObservableCollection<object>;
					for (int j = 0; j < Items.Count; j++)
						DispatchMoveItem(Items[j], j);
				}

				// remove sort indicators
				if (!LockSortOnNumber)
				{
					Items.SortDescriptions.Clear();
					config.Sorts.Clear();
					foreach (DictionaryEntry c in columnTable)
					{
						ViewDetailsColumn vdc = (ViewDetailsColumn)columns[c.Key];
						GridViewColumn gvc = (GridViewColumn)c.Value;
						gvc.CellTemplate = CreateDataTemplate(vdc.Binding, vdc.Alignment, false, (UseIcons && columnGrid.Columns.IndexOf(gvc) == 0), vdc.Converter);
						((GridViewColumnHeader)((GridViewColumn)c.Value).Header).ContentTemplate = (DataTemplate)FindResource("HeaderTemplate");
					}
					currentSortColumn = null;
					if (SelectedItems.Count > 0)
						ScrollIntoView(SelectedItems[0]);
					else if (Items.Count > 0)
						ScrollIntoView(Items[0]);
				}
			}
		}

		/// <summary>
		/// Gets an item source at a given index in the graphical list
		/// </summary>
		/// <param name="index">The graphical index of the item</param>
		/// <returns>The item source</returns>
		public ViewDetailsItemData GetItemAt(int index)
		{
			return Items[index] as ViewDetailsItemData;
		}

		/// <summary>
		/// Returns the graphical index of an item source
		/// </summary>
		/// <param name="logicalObject">The item source</param>
		/// <returns>The graphical index of <paramref name="logicalObject"/></returns>
		public int IndexOf(ViewDetailsItemData logicalObject)
		{
			return Items.IndexOf(logicalObject);
		}

		/// <summary> 
		/// Request the focus to be set on the specified list view item 
		/// </summary> 
		/// <param name="itemIndex">index of item to receive the initial focus</param>
		public void FocusAndSelectItem(int itemIndex)
		{
			Dispatcher.BeginInvoke(new FocusAndSelectItemDelegate(TryFocusAndSelectItem),
				DispatcherPriority.ApplicationIdle, itemIndex);
		}

		/// <summary>
		/// Places focus on the list and the selected items in the list.
		/// </summary>
		public new void Focus()
		{
			base.Focus();
			if (SelectedIndex >= 0)
			{
				ListViewItem lvi = ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as ListViewItem;
				if (lvi != null)
				{
					this.ScrollIntoView(lvi);
					lvi.IsSelected = true;
					Keyboard.ClearFocus();
					Keyboard.Focus(lvi);
				}
			}
		}

		/// <summary>
		/// Refreshes the view of the source.
		/// </summary>
		public void RefreshView()
		{
			ICollectionView view = CollectionViewSource.GetDefaultView(ItemsSource);
			if (view != null)
				view.Refresh();
			searchOverlay.RefreshStrings();
			clearSortMenuItem.Header = U.T("MenuClearSort");
		}

		#endregion Public

		#region Private

		/// <summary> 
		/// Make sure a list view item is within the visible area of the list view 
		/// and then select and set focus to it. 
		/// </summary> 
		/// <param name="itemIndex">index of item</param> 
		private void TryFocusAndSelectItem(int itemIndex)
		{
			ListViewItem lvi = ItemContainerGenerator.ContainerFromIndex(itemIndex) as ListViewItem;
			if (lvi != null)
			{
				this.ScrollIntoView(lvi);
				lvi.IsSelected = true;
				Keyboard.ClearFocus();
				Keyboard.Focus(lvi);
			}
		}

		/// <summary>
		/// Toggles a columns visibility
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="visible">Whether the column should be visible</param>
		private void ToggleColumn(string name, bool visible)
		{
			MenuItem item = headerMenuTable[name] as MenuItem;
			GridViewColumn column = columnTable[name] as GridViewColumn;
			ViewDetailsColumn vdc = columns[name] as ViewDetailsColumn;
			item.IsChecked = visible;
			if (visible)
			{
				// calculate the position to insert the column based on the position in the context menu
				int pos = 0;
				foreach (MenuItem mi in columnGrid.ColumnHeaderContextMenu.Items)
				{
					ViewDetailsColumn c = columns[mi.Tag] as ViewDetailsColumn;
					if (c == vdc)
						break;
					if (c.IsVisible) // only count visible columns
						pos++;
				}
				if (columnGrid.Columns.Contains(column))
					columnGrid.Columns.Remove(column);
				columnGrid.Columns.Insert(pos, column);
			}
			else
				columnGrid.Columns.Remove(column);

			int i = 0;
			foreach (GridViewColumn gvc in columnGrid.Columns)
			{
				string n = (string)((GridViewColumnHeader)gvc.Header).Content;
				ViewDetailsColumn col = FindColumn(n);
				gvc.CellTemplate = CreateDataTemplate(col.Binding, col.Alignment, (currentSortColumn == (GridViewColumnHeader)gvc.Header), i < 1, col.Converter);
				i++;
			}

			if (HasNumber)
			{
				numberIndex = columnGrid.Columns.IndexOf((GridViewColumn)columnTable["#"]);
				if (config != null)
					config.NumberIndex = numberIndex;
			}

			vdc.IsVisible = item.IsChecked;
			RefreshHeaderMenu();
		}

		/// <summary>
		/// Adds the default, permanent menu items to the header context menu.
		/// </summary>
		private void InitHeaderMenu()
		{
			clearSortMenuItem.Header = U.T("MenuClearSort");
			clearSortMenuItem.HorizontalContentAlignment = HorizontalAlignment.Left;
			clearSortMenuItem.VerticalContentAlignment = VerticalAlignment.Center;
			clearSortMenuItem.Click += new RoutedEventHandler(ClearSort_Click);
			headerMenu.Items.Add(new Separator());
			headerMenu.Items.Add(clearSortMenuItem);
		}

		/// <summary>
		/// Goes through all items in the header menu, if only
		/// one column is visible it is disabled, preventing
		/// the user from hiding all columns.
		/// </summary>
		private void RefreshHeaderMenu()
		{
			// look for a single visible column (if there is any)
			ViewDetailsColumn onlyVisible = numberColumn != null && numberColumn.IsVisible ? numberColumn : null;
			foreach (ViewDetailsColumn column in columns.Values)
				if (column.IsVisible && onlyVisible == null)
					onlyVisible = column;
				else if (column.IsVisible)
				{
					onlyVisible = null;
					break;
				}

			// by default allow any column to be toggled
			for (int i = 0; i < headerMenu.Items.Count - 2; i++)
			{
				MenuItem mi = headerMenu.Items[i] as MenuItem;
				if (mi != null)
					mi.IsEnabled = true;
			}

			// if there's only one single column visible we need
			// to disable the ability to hide it
			if (onlyVisible != null && headerMenuTable.ContainsKey(onlyVisible.Name))
			{
				MenuItem mi = headerMenuTable[onlyVisible.Name] as MenuItem;
				mi.IsEnabled = false;
			}
		}

		/// <summary>
		/// Gives an item focus and scrolls it into view
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void FocusItem(object sender, EventArgs e)
		{
			if (ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
			{
				ItemContainerGenerator.StatusChanged -= FocusItem;
				Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(delegate
				{
					if (focusItemIndex > Items.Count || focusItemIndex < 0) return;
					ScrollIntoView(Items.GetItemAt(focusItemIndex));
					ListBoxItem item = ItemContainerGenerator.ContainerFromIndex(focusItemIndex) as ListBoxItem;
					if (item != null)
					{
						item.Focus();
						focusItemIndex = -1;
					}
				}));
			}
		}

		/// <summary>
		/// Sorts the list
		/// </summary>
		/// <param name="vdc">The column to sort on</param>
		/// <param name="direction">The sort direction</param>
		private void Sort(ViewDetailsColumn vdc, ListSortDirection direction)
		{
			try
			{
				// try to find the corresponding column header
				if (!columnTable.ContainsKey(vdc.Name)) return;
				GridViewColumn column = (GridViewColumn)columnTable[vdc.Name];
				GridViewColumnHeader header = (GridViewColumnHeader)column.Header;

				foreach (DictionaryEntry c in columnTable)
				{
					string key = c.Key as string;
					GridViewColumn gvc = c.Value as GridViewColumn;
					GridViewColumnHeader gvch = gvc.Header as GridViewColumnHeader;
					ViewDetailsColumn vdc_ = (ViewDetailsColumn)columns[key];
					bool active = (key == vdc.Name);
					bool rightMost = columnGrid.Columns.IndexOf(gvc) == 0;
					gvc.CellTemplate = CreateDataTemplate(vdc_.Binding, vdc_.Alignment, active, rightMost, vdc_.Converter);
					string headerTemplate = "HeaderTemplate" + (active ?
							(!InvertSortIndicator && direction == ListSortDirection.Ascending) ||
							(InvertSortIndicator && direction == ListSortDirection.Descending)
							? "ArrowUp" : "ArrowDown" : "");
					if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName != "")
						gvch.ContentTemplate = (DataTemplate)TryFindResource(headerTemplate);
				}

				// apply sorting
				Items.SortDescriptions.Insert(0, new SortDescription(vdc.SortField, direction));

				currentSortColumn = header;
				currentSortDirection = direction;

				if (SelectedItems.Count > 0)
					ScrollIntoView(SelectedItems[0]);
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "VIEWDETAILS", "Could not apply sorting: " + e.Message);
			}
		}

		/// <summary>
		/// Uses certain parameters to create a DataTemplate which can be used as a CellTemplate for a
		/// specific column in the list.
		/// </summary>
		/// <param name="binding">The value to bind to</param>
		/// <param name="alignment">Horizontal alignment of the content</param>
		/// <param name="active">Whether the column is active or not</param>
		/// <param name="rightMost">Whether the column is the right most</param>
		/// <returns>DataTemplate to use as a CellTemplate for a column</returns>
		private DataTemplate CreateDataTemplate(string binding, Alignment alignment, bool active, bool rightMost, string converter)
		{
			FrameworkElementFactory dp = new FrameworkElementFactory(typeof(DockPanel));
			dp.SetValue(DockPanel.LastChildFillProperty, true);

			if (rightMost && UseIcons)
			{
				double iconSize = 16.0;
				FrameworkElementFactory icon = new FrameworkElementFactory(typeof(Image));
				icon.SetBinding(Image.SourceProperty, new Binding("Icon") { Converter = new StringToBitmapImageConverter() });
				icon.SetValue(Image.WidthProperty, iconSize);
				icon.SetValue(Image.HeightProperty, iconSize);
				icon.SetValue(Image.MarginProperty, new Thickness(15, 0, 5, 0));
				icon.SetValue(Grid.ColumnProperty, 0);
				dp.AppendChild(icon);
			}

			Binding b = new Binding(binding);
			switch (converter)
			{
				case "Number":
					b.Converter = new NumberConverter();
					break;

				case "DateTime":
					b.Converter = new DateTimeConverter();
					break;

				case "Duration":
					b.Converter = new DurationConverter();
					break;

				case "SourceType":
					b.Converter = new SourceTypeConverter();
					break;

				case "PluginType":
					b.Converter = new PluginTypeConverter();
					break;

			}

			FrameworkElementFactory tb = new FrameworkElementFactory(typeof(TextBlock));
			tb.SetBinding(TextBlock.TextProperty, b);
			tb.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
			tb.SetValue(TextBlock.HorizontalAlignmentProperty, ConvertHAlignment(alignment));
			tb.SetValue(Grid.ColumnProperty, 1);
			if (rightMost && !UseIcons)
				tb.SetValue(TextBlock.MarginProperty, new Thickness(15, 0, 5, 0));


			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName != "")
				tb.SetValue(TextBlock.ForegroundProperty, (active ? Brushes.Black : Brushes.Gray));

			DataTemplate dt = new DataTemplate();

			dp.AppendChild(tb);

			dt.VisualTree = dp;
			return dt;
		}

		/// <summary>
		/// Find the corresponding column configuration given the content of the column
		/// </summary>
		/// <param name="content">The displayed text on the column</param>
		/// <returns>The column configuration for the column</returns>
		private ViewDetailsColumn FindColumn(string content)
		{
			if (content == "#")
				return numberColumn;

			foreach (DictionaryEntry i in columns)
			{
				if (((ViewDetailsColumn)i.Value).Text == content)
					return (ViewDetailsColumn)i.Value;
			}
			return null;
		}

		/// <summary>
		/// Gets the ScrollViewer of the list.
		/// </summary>
		/// <param name="o">The object in which to look for the scrollbar </param>
		/// <returns>The ScrollViewer of this list</returns>
		public  DependencyObject GetScrollViewer(DependencyObject o = null)
		{
			if (o == null) o = this;

			// Return the DependencyObject if it is a ScrollViewer
			if (o is ScrollViewer)
			{ return o; }

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = VisualTreeHelper.GetChild(o, i);

				var result = GetScrollViewer(child);
				if (result == null)
				{
					continue;
				}
				else
				{
					return result;
				}
			}
			return null;
		}

		/// <summary>
		/// Converts an Alignment to a HorizontalAlignment.
		/// </summary>
		/// <param name="alignment">The alignment to convert</param>
		/// <returns>The corresponding alignment if found, otherwise Left</returns>
		private HorizontalAlignment ConvertHAlignment(Alignment alignment)
		{
			switch (alignment)
			{
				default:
				case Alignment.Left:
					return HorizontalAlignment.Left;

				case Alignment.Right:
					return HorizontalAlignment.Right;

				case Alignment.Center:
				case Alignment.Middle:
					return HorizontalAlignment.Center;
			}
		}

		/// <summary>
		/// Converts an Alignment to a VerticalAlignment.
		/// </summary>
		/// <param name="alignment">The alignment to convert</param>
		/// <returns>The corresponding alignment if found, otherwise Top</returns>
		private VerticalAlignment ConvertVAlignment(Alignment alignment)
		{
			switch (alignment)
			{
				default:
				case Alignment.Top:
					return VerticalAlignment.Top;

				case Alignment.Bottom:
					return VerticalAlignment.Bottom;

				case Alignment.Center:
				case Alignment.Middle:
					return VerticalAlignment.Center;
			}
		}

		#endregion Private

		#region Overrides

		/// <summary>
		/// Updates the desired size of the element.
		/// </summary>
		/// <param name="constraint">A size to constrain the element to</param>
		/// <returns>The desired size of the element</returns>
		protected override Size MeasureOverride(Size constraint)
		{
			Size size = base.MeasureOverride(constraint);
			if (searchOverlay != null)
				searchOverlay.Measure(constraint);
			return size;
		}

		/// <summary>
		/// Creates and return a ViewDetailsItem container.
		/// </summary>
		/// <returns>A ViewDetailsItem container</returns>
		protected override DependencyObject GetContainerForItemOverride()
		{
			return new ViewDetailsItem();
		}

		/// <summary>
		/// Invoked when the context menu is opening
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnContextMenuOpening(ContextMenuEventArgs e)
		{
			// prevent the context menu from opening if the item under the mouse is not an item
			ListViewItem lvi = ViewDetailsUtilities.TryFindParent<ListViewItem>((DependencyObject)e.OriginalSource);
			GridViewColumnHeader gvch = ViewDetailsUtilities.TryFindParent<GridViewColumnHeader>((DependencyObject)e.OriginalSource);
			if (lvi == null && gvch == null)
				e.Handled = true;
			else
			{
				base.OnContextMenuOpening(e);
			}
		}

		/// <summary>
		/// Invoked when the ItemsSource property is changed.
		/// </summary>
		/// <param name="oldValue">The old source</param>
		/// <param name="newValue">The new source</param>
		/// <remarks>Will enumerate all items and set Number</remarks>
		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			// if all numbers are zero we fix the numbers
			bool allZero = true;
			foreach (ViewDetailsItemData item in newValue)
				if (item.Number != 0)
				{
					allZero = false;
					break;
				}
			int i = 1;
			if (allZero)
				foreach (ViewDetailsItemData item in newValue)
					item.Number = i++;

			base.OnItemsSourceChanged(oldValue, newValue);
		}

		/// <summary>
		/// Invoked when the user double-clicks the list
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
			// prevent the context menu from opening if the item under the mouse is not an item
			ListViewItem lvi = ViewDetailsUtilities.TryFindParent<ListViewItem>((DependencyObject)e.OriginalSource);
			if (lvi == null)
				e.Handled = true;
			else
				base.OnMouseDoubleClick(e);
		}

		/// <summary>
		/// Invoked when something is dropped on the list
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnDrop(DragEventArgs e)
		{
			if (!(e.Data.GetDataPresent(DataFormats.FileDrop) && AcceptFileDrops) &&
				!(e.Data.GetDataPresent(typeof(List<object>).FullName) && IsDragSortable))
			{
				e.Effects = DragDropEffects.None;
				return;
			}

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] paths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
				ListBoxItem lvi = ViewDetailsUtilities.TryFindFromPoint<ListBoxItem>(this, e.GetPosition(this));
				if (lvi != null)
				{
					int i = this.ItemContainerGenerator.IndexFromContainer(lvi);
					if (e.GetPosition(lvi).Y > lvi.RenderSize.Height / 2) i++;
					DispatchFilesDropped(paths, i);
				}
				else
					DispatchFilesDropped(paths, Items.Count);
			}

			else if (e.Data.GetDataPresent(typeof(List<object>).FullName))
			{
				ListBoxItem lvi = ViewDetailsUtilities.TryFindFromPoint<ListBoxItem>(this, e.GetPosition(this));
				if (lvi != null)
				{
					List<object> items = e.Data.GetData(typeof(List<object>).FullName) as List<object>;
					int i = this.ItemContainerGenerator.IndexFromContainer(lvi);
					if (e.GetPosition(lvi).Y > lvi.RenderSize.Height / 2) i++;

					// items may be out of order so we sort them
					List<int> indices = new List<int>();
					foreach (object t in items) // put all indices in a list
						indices.Add(Items.IndexOf(t));
					indices.Sort(); // sort the list
					items.Clear();
					foreach (int j in indices) // put back all items according to the sorted list
						items.Add(Items.GetItemAt(j) as object);

					// reorder source and remove GUI sorting
					ClearSort(true);

					foreach (object t in items)
					{
						int j = i;
						if (Items.IndexOf(t) > i) j++;
						DispatchMoveItem(t, i);
						i = j;
					}

					// change number value if we have a number column
					if (HasNumber)
					{
						foreach (ViewDetailsItemData o in Items)
						{
							o.Number = Items.IndexOf(o) + 1;
						}
					}

					SetSelectedItems(items);
				}
			}
			dropTarget.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Invoked when an item is dragged over the list
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnDragOver(DragEventArgs e)
		{
			if (!(e.Data.GetDataPresent(DataFormats.FileDrop) && AcceptFileDrops) &&
				!(e.Data.GetDataPresent(typeof(List<object>).FullName) && IsDragSortable))
			{
				e.Effects = DragDropEffects.None;
				return;
			}

			ListBoxItem lvi = ViewDetailsUtilities.TryFindFromPoint<ListBoxItem>(this, e.GetPosition(this));
			if (lvi != null)
			{
				ScrollViewer sv = ViewDetailsUtilities.GetVisualChild<ScrollViewer>(this);
				if (sv != null && sv.ComputedVerticalScrollBarVisibility == Visibility.Visible)
					dropTarget.ScrollBar = true;
				else
					dropTarget.ScrollBar = false;

				dropTarget.Visibility = Visibility.Visible;
				Point p = lvi.TranslatePoint(new Point(0, 0), this);
				if (e.GetPosition(this).Y < p.Y + (lvi.RenderSize.Height / 2))
					dropTarget.Position = p.Y;
				else
					dropTarget.Position = p.Y + lvi.RenderSize.Height + 1;

				double scrollMargin = 50.0;
				double scrollStep = 1;
				double scrollSpeed = 0.05;
				if (sv != null && sv.CanContentScroll && ((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds) - lastScroll > scrollSpeed)
				{
					if (e.GetPosition(this).Y > this.RenderSize.Height - scrollMargin)
					{
						sv.ScrollToVerticalOffset(sv.VerticalOffset + scrollStep);
						lastScroll = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
					}

					else if (e.GetPosition(this).Y < scrollMargin + 20.0)
					{
						sv.ScrollToVerticalOffset(sv.VerticalOffset - scrollStep);
						lastScroll = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
					}
				}
			}
			else
				dropTarget.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Invoked when an unhandled DragLeave attached event reaches an element
		/// in its route that is derived from this class
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnDragLeave(DragEventArgs e)
		{
			base.OnDragLeave(e);
			Point p = e.GetPosition(this);
			double x = p.X / ActualWidth;
			double y = p.Y / ActualHeight;
			if (x < 0.1 || 0.9 < x || y < 0.1 || 0.9 < y)
				dropTarget.Visibility = System.Windows.Visibility.Collapsed;
		}

		/// <summary>
		/// Responds to a list box selection change by raising a SelectionChanged event and
		/// saving the selection to the config structure is such as structure is set.
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			if (config != null)
			{
				// remove
				for (int i = 0; i < config.SelectedIndices.Count; i++)
				{
					if (i >= Items.Count || !SelectedItems.Contains(Items.GetItemAt(i)))
						config.SelectedIndices.RemoveAt(i--);
				}

				// add
				foreach (object o in SelectedItems)
				{
					int i = Items.IndexOf(o);
					if (!config.SelectedIndices.Contains(i))
						config.SelectedIndices.Add(i);
				}
			}
			base.OnSelectionChanged(e);
		}

		/// <summary>
		/// Called when the control is being rendered.
		/// </summary>
		/// <param name="drawingContext">The drawing instructions for a specific element</param>
		protected override void OnRender(DrawingContext drawingContext)
		{
			if (!scrollViewerInitialized)
			{
				var sv = GetScrollViewer() as ScrollViewer;
				if (sv != null)
				{
					sv.ScrollChanged += new ScrollChangedEventHandler(ScrollViewer_ScrollChanged);
					scrollViewerInitialized = true;
				}
			}
			base.OnRender(drawingContext);
		}

		#endregion Overrides

		#region Event handlers

		/// <summary>
		/// Invoked when the source collection changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Add:
					foreach (ViewDetailsItemData o in Items.SourceCollection)
						o.Number = Items.IndexOf(o) + 1;
					break;

				default:
				case NotifyCollectionChangedAction.Reset:
					break;
			}
		}

		/// <summary>
		/// Invoked when the list is initialized
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		protected virtual void ListView_Loaded(object sender, RoutedEventArgs e)
		{
			dropTarget = new ViewDetailsDropTarget(this);
			dropTarget.Visibility = Visibility.Hidden;

			searchOverlay = new ViewDetailsSearchOverlay(this);
			searchOverlay.Visibility = Visibility.Hidden;

			AdornerLayer al = AdornerLayer.GetAdornerLayer(this);
			if (al != null)
			{
				al.Add(dropTarget);
				al.Add(searchOverlay);
			}
		}

		/// <summary>
		/// Invoked when a property of the config changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		protected virtual void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Filter")
				Filter = config.Filter;
		}

		/// <summary>
		/// Invoked when a menu item in the header context menu is clicked
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void HeaderMenu_Click(object sender, RoutedEventArgs e)
		{
			MenuItem item = sender as MenuItem;
			String name = (string)item.Tag;

			if (name == "#")
				IsNumberVisible = item.IsChecked;
			else
				ToggleColumn(name, item.IsChecked);

			RefreshHeaderMenu();
		}

		/// <summary>
		/// Invoked when the user clicks on "Clear sorting" in the header menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ClearSort_Click(object sender, RoutedEventArgs e)
		{
			ClearSort(false);
		}

		/// <summary>
		/// Invoked when the size of a column is changed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Column_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			GridViewColumnHeader gvch = sender as GridViewColumnHeader;
			ViewDetailsColumn vdc = FindColumn((string)gvch.Content);
			if (vdc != null)
				vdc.Width = gvch.ActualWidth;
		}

		/// <summary>
		/// Invoked when a column is clicked
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Column_Clicked(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader column = sender as GridViewColumnHeader;
			ListSortDirection direction;
			ViewDetailsColumn vdc = FindColumn((string)column.Content);
			if (vdc == null) return;

			if (!IsClickSortable || !vdc.IsSortable || (vdc != numberColumn && LockSortOnNumber))
				return;

			// get direction
			if (column != currentSortColumn) direction = ListSortDirection.Ascending;
			else if (currentSortDirection == ListSortDirection.Ascending) direction = ListSortDirection.Descending;
			else direction = ListSortDirection.Ascending;

			// apply sorting
			Sort(vdc, direction);

			if (config != null)
			{
				if (config.Sorts == null)
					config.Sorts = new List<string>();

				// remove previous sorts on this column
				string str1 = "asc:" + vdc.Name;
				if (config.Sorts.Contains(str1))
					config.Sorts.Remove(str1);
				string str2 = "dsc:" + vdc.Name;
				if (config.Sorts.Contains(str2))
					config.Sorts.Remove(str2);

				config.Sorts.Add((direction == ListSortDirection.Ascending ? "asc:" : "dsc:") + vdc.Name);

			}
		}

		/// <summary>
		/// Invoked when the configuration of columns changed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ConfigColumns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// TODO: add and remove new columns
		}

		/// <summary>
		/// Invoked when the property of a column changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ConfigColumn_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			ViewDetailsColumn vdc = sender as ViewDetailsColumn;

			GridViewColumn gvc = columnTable[vdc.Name] as GridViewColumn;
			if (gvc == null) return;

			// rename headers and menu items
			if (e.PropertyName == "Text")
			{
				GridViewColumnHeader gvch = gvc.Header as GridViewColumnHeader;
				gvch.Content = vdc.Text;

				MenuItem mi = headerMenuTable[vdc.Name] as MenuItem;
				mi.Header = vdc.Text;
			}

			// TODO: implement other properties
		}

		/// <summary>
		/// Invoked when the columns are reordered
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Move)
			{
				// move the icon to the far left
				if ((e.OldStartingIndex == 0 || e.NewStartingIndex == 0) && UseIcons)
				{
					int oldIndex = e.OldStartingIndex == 0 ? e.NewStartingIndex : 1;
					GridViewColumn oldFirst = columnGrid.Columns[oldIndex];
					GridViewColumn newFirst = columnGrid.Columns[0];

					ViewDetailsColumn oldVdc = FindColumn((string)((GridViewColumnHeader)oldFirst.Header).Content);
					ViewDetailsColumn newVdc = FindColumn((string)((GridViewColumnHeader)newFirst.Header).Content);

					bool oldIsActive = IsClickSortable && ((GridViewColumnHeader)oldFirst.Header) == currentSortColumn;
					bool newIsActive = IsClickSortable && ((GridViewColumnHeader)newFirst.Header) == currentSortColumn;

					oldFirst.CellTemplate = CreateDataTemplate(oldVdc.Binding, oldVdc.Alignment, oldIsActive, false, oldVdc.Converter);
					newFirst.CellTemplate = CreateDataTemplate(newVdc.Binding, newVdc.Alignment, newIsActive, true, newVdc.Converter);
				}

				// we may need to rearrange the column order in the config as well
				if (config != null)
				{
					// since we may have a number column in the menu but not in the column
					// list we have to compensate the indices accordingly
					bool wasNumber = false;
					int newAdjust = 0;
					int oldAdjust = 0;
					if (HasNumber)
					{
						if (NumberIndex == e.OldStartingIndex)
						{
							numberIndex = e.NewStartingIndex;
							config.NumberIndex = numberIndex;
							wasNumber = true;
						}
						else // adjust indices
						{
							if (NumberIndex < e.OldStartingIndex && IsNumberVisible) oldAdjust = 1;

							// adjust number index
							if (e.OldStartingIndex <= NumberIndex && NumberIndex < e.NewStartingIndex && IsNumberVisible) numberIndex--;
							else if (e.NewStartingIndex <= NumberIndex && NumberIndex < e.OldStartingIndex && IsNumberVisible) numberIndex++;

							if (NumberIndex <= e.NewStartingIndex && IsNumberVisible) newAdjust = 1;
						}
					}

					if (!wasNumber) // the number column is special, not in the list we rearrange
					{
						ViewDetailsColumn vdc = config.Columns[e.OldStartingIndex - oldAdjust];
						config.Columns.Remove(vdc);
						config.Columns.Insert(e.NewStartingIndex - newAdjust, vdc);
					}
				}

				MenuItem mi = headerMenu.Items[e.OldStartingIndex] as MenuItem;
				headerMenu.Items.Remove(mi);
				headerMenu.Items.Insert(e.NewStartingIndex, mi);
			}
		}

		/// <summary>
		/// Invoked when the scroll position is changed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			var sv = sender as ScrollViewer;
			if (sv != null && config != null)
			{
				config.VerticalScrollOffset = sv.VerticalOffset;
				config.HorizontalScrollOffset = sv.HorizontalOffset;
				if (String.IsNullOrWhiteSpace(Filter))
					config.VerticalScrollOffsetWithoutSearch = sv.VerticalOffset;
			}
		}

		#endregion Event handlers

		#region Dispatchers

		/// <summary>
		/// The dispatcher of the <see cref="ViewDetails.FilesDropped"/> event
		/// </summary>
		/// <param name="paths">The track that was either added or removed</param>
		/// <param name="position">The index where the files where dropped</param>
		private void DispatchFilesDropped(string[] paths, int position)
		{
			if (FilesDropped != null)
				FilesDropped(this, new FileDropEventArgs(paths, position));
		}

		/// <summary>
		/// The dispatcher of the <see cref="ViewDetails.MoveItem"/> event
		/// </summary>
		/// <param name="item">The item that is to be moved</param>
		/// <param name="position">The index that the item is to be moved to</param>
		private void DispatchMoveItem(object item, int position)
		{
			if (MoveItem != null)
				MoveItem(this, new MoveItemEventArgs(item, position));
		}

		#endregion

		#endregion Methods

		#region Events

		/// <summary>
		/// Occurs when files are dropped on the list.
		/// </summary>
		public event FileDropEventHandler FilesDropped;

		/// <summary>
		/// Occurs when an item needs to be moved
		/// </summary>
		public event MoveItemEventHandler MoveItem;

		#endregion
	}

	#region Delegates

	/// <summary>
	/// Represents the method that will handle the <see cref="ViewDetails.FilesDropped"/> event.
	/// </summary>
	/// <param name="sender">The sender of the event</param>
	/// <param name="e">The event data</param>
	public delegate void FileDropEventHandler(object sender, FileDropEventArgs e);

	/// <summary>
	/// Represents the method that will handle the <see cref="ViewDetails.MoveItem"/> event.
	/// </summary>
	/// <param name="sender">The sender of the event</param>
	/// <param name="e">The event data</param>
	public delegate void MoveItemEventHandler(object sender, MoveItemEventArgs e);

	/// <summary>
	/// Represents the method that will determine whether an item matches a filter string or not
	/// </summary>
	/// <param name="item">The item which should be examined</param>
	/// <param name="filterString">The string which should be matched</param>
	/// <returns>True if the item matches the string, otherwise False</returns>
	public delegate bool ViewDetailsSearchDelegate(ViewDetailsItemData item, string filterString);

	/// <summary>
	/// Represents the method that is called to focus and select an item of the ListView.
	/// </summary>
	/// <param name="itemIndex">The index of the item to focus and select</param>
	public delegate void FocusAndSelectItemDelegate(int itemIndex);

	#endregion

	#region Event arguments

	/// <summary>
	/// Provides data for the <see cref="ViewDetails.FilesDropped"/> event
	/// </summary>
	public class FileDropEventArgs
	{
		#region Properties
		
		/// <summary>
		/// Gets the paths of the files that were dropped
		/// </summary>
		public string[] Paths { get; private set; }

		/// <summary>
		/// Gets the index where the files were dropped
		/// </summary>
		public int Position { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDropEventArgs"/> class
		/// </summary>
		/// <param name="paths">The paths that was dropped</param>
		/// <param name="position">The index where the files where dropped</param>
		public FileDropEventArgs(string[] paths, int position)
		{
			Paths = paths;
			Position = position;
		}

		#endregion
	}

	/// <summary>
	/// Provides data for the <see cref="ViewDetails.MoveItem"/> event
	/// </summary>
	public class MoveItemEventArgs
	{
		#region Properties

		/// <summary>
		/// Gets the paths of the item that is to be moved
		/// </summary>
		public object Item { get; private set; }

		/// <summary>
		/// Gets the index that the item is to be moved to
		/// </summary>
		public int Position { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="MoveItemEventArgs"/> class
		/// </summary>
		/// <param name="item">The item that is to be moved</param>
		/// <param name="position">The index that the item is to be moved to</param>
		public MoveItemEventArgs(object item, int position)
		{
			Item = item;
			Position = position;
		}

		#endregion
	}

	#endregion

	#region Data structures

	/// <summary>
	/// Describes the data source of an item inside the ViewDetails list
	/// </summary>
	public class ViewDetailsItemData : INotifyPropertyChanged
	{
		#region Members

		private int number;
		private bool isActive;
		private string icon;
		private bool strike;
		private bool disabled = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the index number of the item
		/// </summary>
		public int Number
		{
			get { return number; }
			set { number = value; OnPropertyChanged("Number"); }
		}

		/// <summary>
		/// Gets or sets whether the item is marked as active or not
		/// </summary>
		public bool IsActive
		{
			get { return isActive; }
			set { isActive = value; OnPropertyChanged("IsActive"); }
		}

		/// <summary>
		/// Gets or sets the icon of the item
		/// </summary>
		public string Icon
		{
			get { return icon; }
			set { icon = value; OnPropertyChanged("Icon"); }
		}

		/// <summary>
		/// Gets or sets whether the items should feature a strikethrough
		/// </summary>
		public bool Strike
		{
			get { return strike; }
			set { strike = value; OnPropertyChanged("Strike"); }
		}

		/// <summary>
		/// Gets or sets whether the items should be viewed as disabled (for example grayed out)
		/// </summary>
		public bool Disabled
		{
			get { return disabled; }
			set { disabled = value; OnPropertyChanged("Disabled"); }
		}

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion
	}

	/// <summary>
	/// Describes a configuration for the ViewDetails class
	/// </summary>
	public class ViewDetailsConfig : INotifyPropertyChanged
	{
		#region Members

		string filter = "";

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the columns
		/// </summary>
		public ObservableCollection<ViewDetailsColumn> Columns { get; set; }

		/// <summary>
		/// Gets or sets the number column configuration
		/// </summary>
		public ViewDetailsColumn NumberColumn { get; set; }

		/// <summary>
		/// Gets or sets the indices of the selected items
		/// </summary>
		public List<int> SelectedIndices { get; set; }

		/// <summary>
		/// Gets or sets the the sort orders
		/// Each sort is represented as a string on the format
		/// "asc/dsc:ColumnName"
		/// </summary>
		public List<string> Sorts { get; set; }

		/// <summary>
		/// Gets or sets text used to filter the list
		/// </summary>
		public string Filter
		{
			get { return filter; }
			set
			{
				filter = value;
				OnPropertyChanged("Filter");
			}
		}

		/// <summary>
		/// Gets or sets whether the number column should be enabled
		/// </summary>
		public bool HasNumber { get; set; }

		/// <summary>
		/// Gets or sets whether the number column should be visible
		/// </summary>
		public bool IsNumberVisible { get; set; }

		/// <summary>
		/// Gets or sets the position of the number column
		/// </summary>
		public int NumberIndex { get; set; }

		/// <summary>
		/// Gets or sets whether to display icons or not
		/// </summary>
		public bool UseIcons { get; set; }

		/// <summary>
		/// Gets or sets whether files can be dropped onto the list
		/// </summary>
		public bool AcceptFileDrops { get; set; }

		/// <summary>
		/// Gets or sets whether the list can be resorted via drag and drop
		/// </summary>
		public bool IsDragSortable { get; set; }

		/// <summary>
		/// Gets or sets whether the list can be resorted by clicking on a column
		/// </summary>
		public bool IsClickSortable { get; set; }

		/// <summary>
		/// Gets or sets whether only the number column can be used to sort the list
		/// </summary>
		public bool LockSortOnNumber { get; set; }

		/// <summary>
		/// Gets or sets the vertical scroll offset
		/// </summary>
		public double VerticalScrollOffset { get; set; }

		/// <summary>
		/// Gets or sets the horizontal scroll offset
		/// </summary>
		public double HorizontalScrollOffset { get; set; }

		/// <summary>
		/// Gets or sets the vertical scroll offset when no search is active.
		/// </summary>
		public double VerticalScrollOffsetWithoutSearch { get; set; }

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion
	}

	/// <summary>
	/// Represents a column of a details list
	/// </summary>
	public class ViewDetailsColumn : INotifyPropertyChanged
	{
		#region Members

		private string text;
		private string binding;
		private string converter;
		private string sortField;
		private bool isAlwaysVisible = false;
		private bool isSortable = true;
		private double width = 50.0;
		private bool isVisible = true;
		private Alignment alignment = Alignment.Left;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the name of the column
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the displayed text
		/// </summary>
		public string Text
		{
			get { return text; }
			set
			{
				text = value;
				OnPropertyChanged("Text");
			}
		}

		/// <summary>
		/// Gets or sets the value to bind to
		/// </summary>
		public string Binding
		{
			get { return binding; }
			set
			{
				binding = value;
				OnPropertyChanged("Binding");
			}
		}

		/// <summary>
		/// Gets or sets the converter that should be used to present the value of the binding.
		/// </summary>
		public string Converter
		{
			get { return converter; }
			set
			{
				converter = value;
				OnPropertyChanged("Converter");
			}
		}

		/// <summary>
		/// Gets or sets the value to sort on
		/// </summary>
		public string SortField
		{
			get { return sortField; }
			set
			{
				sortField = value;
				OnPropertyChanged("SortField");
			}
		}

		/// <summary>
		/// Gets or sets whether the column is always visible
		/// </summary>
		public bool IsAlwaysVisible
		{
			get { return isAlwaysVisible; }
			set
			{
				isAlwaysVisible = value;
				OnPropertyChanged("IsAlwaysVisible");
			}
		}

		/// <summary>
		/// Gets or sets whether the column is sortable
		/// </summary>
		public bool IsSortable
		{
			get { return isSortable; }
			set
			{
				isSortable = value;
				OnPropertyChanged("IsSortable");
			}
		}

		/// <summary>
		/// Gets or sets the width of the column
		/// </summary>
		public double Width
		{
			get { return width; }
			set
			{
				width = value;
				OnPropertyChanged("Width");
			}
		}

		/// <summary>
		/// Gets or sets whether the column is visible (only effective if IsAlwaysVisible is false)
		/// </summary>
		public bool IsVisible
		{
			get { return isVisible; }
			set
			{
				isVisible = value;
				OnPropertyChanged("IsVisible");
			}
		}

		/// <summary>
		/// Gets or sets the text alignment of the displayed text
		/// </summary>
		public Alignment Alignment
		{
			get { return alignment; }
			set
			{
				alignment = value;
				OnPropertyChanged("Alignment");
			}
		}

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion
	}

	#endregion

	#region Controls

	/// <summary>
	/// The graphical drag-n-drop target of ViewDetails
	/// </summary>
	public class ViewDetailsDropTarget : Adorner
	{
		#region Members

		private double position;
		private bool scrollbar = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the vertical position of the drop target
		/// </summary>
		public double Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
				this.InvalidateVisual();

			}
		}

		/// <summary>
		/// Gets or sets whether the drop target should make space for a vertical scroll bar
		/// </summary>
		public bool ScrollBar
		{
			get
			{
				return scrollbar;
			}
			set
			{
				scrollbar = value;
				this.InvalidateVisual();

			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the DropTarget class
		/// </summary>
		/// <param name="adornedElement">The element that the drop target should adorn</param>
		public ViewDetailsDropTarget(UIElement adornedElement)
			: base(adornedElement)
		{
			position = adornedElement.DesiredSize.Height / 2;
			IsHitTestVisible = false;
			SnapsToDevicePixels = true;
		}

		#endregion

		#region Override

		/// <summary>
		/// Invoked when the target is rendered.
		/// Does the actual painting of the drop target.
		/// </summary>
		/// <param name="drawingContext">The drawing context of the rendering</param>
		protected override void OnRender(DrawingContext drawingContext)
		{
			Point left = new Point(0, position);
			double width = AdornedElement.DesiredSize.Width;
			if (ScrollBar) width -= 18;
			Point right = new Point(width, position);

			PointCollection points = new PointCollection();
			points.Add(new Point(0, 0));
			points.Add(new Point(0, 6));
			points.Add(new Point(3, 3));

			PathFigure pfig1 = new PathFigure();
			pfig1.StartPoint = new Point(2, position - 2);
			pfig1.Segments.Add(new LineSegment(new Point(4, position), true));
			pfig1.Segments.Add(new LineSegment(new Point(width - 4, position), true));
			pfig1.Segments.Add(new LineSegment(new Point(width - 2, position - 2), true));
			pfig1.Segments.Add(new LineSegment(new Point(width - 2, position + 3), true));
			pfig1.Segments.Add(new LineSegment(new Point(width - 4, position + 1), true));
			pfig1.Segments.Add(new LineSegment(new Point(4, position + 1), true));
			pfig1.Segments.Add(new LineSegment(new Point(2, position + 3), true));

			PathGeometry p = new PathGeometry();
			p.Figures.Add(pfig1);

			drawingContext.DrawGeometry(Brushes.Black, new Pen(Brushes.Black, 1), p);
		}

		#endregion
	}

	/// <summary>
	/// The overlay displaying a progressbar and a text informing the
	/// user that a search is in progress.
	/// </summary>
	public class ViewDetailsSearchOverlay : Adorner
	{
		#region Members

		private DockPanel panel = new DockPanel();
		private StackPanel innerPanel = new StackPanel();
		private TextBlock text = new TextBlock();
		private ProgressBar progress = new ProgressBar();

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the DropTarget class
		/// </summary>
		/// <param name="adornedElement">The element that the drop target should adorn</param>
		public ViewDetailsSearchOverlay(UIElement adornedElement)
			: base(adornedElement)
		{
			IsHitTestVisible = false;
			SnapsToDevicePixels = true;
			MinHeight = 200;
			text.Text = U.T("Searching");
			text.FontSize = 20;
			text.Foreground = Brushes.Black;
			text.HorizontalAlignment = HorizontalAlignment.Center;
			text.VerticalAlignment = VerticalAlignment.Center;
			text.Margin = new Thickness(0, 0, 0, 5);

			progress.IsIndeterminate = true;
			progress.Width = 300;
			progress.Height = 10;
			progress.HorizontalAlignment = HorizontalAlignment.Center;
			progress.VerticalAlignment = VerticalAlignment.Center;

			innerPanel.Children.Add(text);
			innerPanel.Children.Add(progress);
			innerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
			innerPanel.VerticalAlignment = VerticalAlignment.Center;

			DockPanel.SetDock(innerPanel, Dock.Top);

			panel.Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
			panel.Children.Add(innerPanel);
			panel.HorizontalAlignment = HorizontalAlignment.Stretch;
			panel.VerticalAlignment = VerticalAlignment.Stretch;

			HorizontalAlignment = HorizontalAlignment.Stretch;
			VerticalAlignment = VerticalAlignment.Stretch;

			AddVisualChild(panel);
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Updates the strings of the GUI which are programatically set.
		/// </summary>
		public void RefreshStrings()
		{
			text.Text = U.T("Searching");
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Gets the number of visual children.
		/// </summary>
		protected override int VisualChildrenCount
		{
			get
			{
				return 1;
			}
		}

		/// <summary>
		/// Returns the child at the specified index location.
		/// </summary>
		/// <param name="index">The index of the child</param>
		/// <returns>The child located at the specified index</returns>
		protected override Visual GetVisualChild(int index)
		{
			if (index != 0) throw new ArgumentOutOfRangeException();
			return panel;
		}

		/// <summary>
		/// Calculates the measuring size of the adorner.
		/// </summary>
		/// <param name="constraint">A size to constrain the adorner to</param>
		/// <returns>The desired size of the adorner</returns>
		protected override Size MeasureOverride(Size constraint)
		{
			Size s = AdornedElement.DesiredSize;
			ViewDetails vd = AdornedElement as ViewDetails;
			if (vd != null)
				s = new Size(vd.ActualWidth, vd.ActualHeight);
			return s;
		}

		/// <summary>
		/// Positions child elements and determines a size for the adorner.
		/// </summary>
		/// <param name="finalSize">
		/// The final area within the parent that the adorner
		/// should use to arrange itself and its children
		/// </param>
		/// <returns>The actual size of the adorner</returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			panel.Arrange(new Rect(new Point(0, 0), finalSize));
			return new Size(panel.ActualWidth, panel.ActualHeight);
		}

		#endregion

		#endregion
	}

	/// <summary>
	/// A single item in the list of the ViewDetails control
	/// </summary>
	public partial class ViewDetailsItem : ListViewItem
	{
		#region Members

		private Point startDragPoint;
		private bool isDragging = false;
		private bool doDeselect = false;

		#endregion

		#region Override

		/// <summary>
		/// Called when the user presses the right mouse button over the ViewDetailsItem.
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (!IsSelected)
				base.OnMouseLeftButtonDown(e);
			else
				doDeselect = true;
			isDragging = true;
			startDragPoint = e.GetPosition(this);
		}

		/// <summary>
		/// Called when the user releases the right mouse button over the ViewDetailsItem.
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (doDeselect)
				base.OnMouseLeftButtonDown(e);
			base.OnMouseLeftButtonUp(e);
			doDeselect = false;
			isDragging = false;
		}

		/// <summary>
		/// Called when the user moves the mouse over the ViewDetailsItem.
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (!isDragging) return;
			if (e.LeftButton == MouseButtonState.Released)
			{
				isDragging = false;
				return;
			}
			Vector diff = startDragPoint - e.GetPosition(this);
			if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
				return;

			ListBoxItem lvi = ViewDetailsUtilities.TryFindFromPoint<ListBoxItem>(this, e.GetPosition(this));
			ViewDetails vd = ViewDetailsUtilities.TryFindParent<ViewDetails>(lvi);
			if (vd == null)
				return;

			if (vd.SelectedItems.Count <= 0)
				return; // halt if we don't have any items to drag

			List<object> DraggedItems = new List<object>();

			foreach (object DraggedItem in vd.SelectedItems)
				DraggedItems.Add(DraggedItem);

			DragDropEffects AllowedEffects = DragDropEffects.Move;

			DragDrop.DoDragDrop(this, DraggedItems, AllowedEffects);
		}

		#endregion
	}

	#endregion

	#region Toolbox

	/// <summary>
	/// A collection of some static help methods for ViewDetails
	/// </summary>
	public static class ViewDetailsUtilities
	{

		/// <summary>Finds a parent of a given item on the visual tree.</summary>
		/// <typeparam name="T">The type of the queried item.</typeparam>
		/// <param name="iChild">A direct or indirect child of the queried item.</param>
		/// <returns>The first parent item that matches the submitted type parameter. If not matching item can be found, a null reference is being returned.</returns>
		public static T TryFindParent<T>(this DependencyObject iChild)
		  where T : DependencyObject
		{
			// Get parent item.
			DependencyObject parentObject = GetParentObject(iChild);

			// We've reached the end of the tree.
			if (parentObject == null)
				return null;

			// Check if the parent matches the type we're looking for.
			// Else use recursion to proceed with next level.
			T parent = parentObject as T;
			return parent ?? TryFindParent<T>(parentObject);
		}

		/// <summary>
		/// This method is an alternative to WPF's <see cref="VisualTreeHelper.GetParent"/> method, which also
		/// supports content elements. Keep in mind that for content element, this method falls back to the logical tree of the element!
		/// </summary>
		/// <param name="iChild">The item to be processed.</param>
		/// <returns>The submitted item's parent, if available. Otherwise null.</returns>
		public static DependencyObject GetParentObject(this DependencyObject iChild)
		{
			if (iChild == null)
			{
				return null;
			}

			// Handle content elements separately.
			ContentElement contentElement = iChild as ContentElement;
			if (contentElement != null)
			{
				DependencyObject parent = ContentOperations.GetParent(contentElement);
				if (parent != null) return parent;

				FrameworkContentElement frameworkContentElement = contentElement as FrameworkContentElement;
				return frameworkContentElement != null ? frameworkContentElement.Parent : null;
			}

			// Also try searching for parent in framework elements (such as DockPanel, etc).
			FrameworkElement frameworkElement = iChild as FrameworkElement;
			if (frameworkElement != null)
			{
				DependencyObject parent = frameworkElement.Parent;
				if (parent != null) return parent;
			}

			// If it's not a ContentElement/FrameworkElement, rely on VisualTreeHelper.
			return VisualTreeHelper.GetParent(iChild);
		}

		/// <summary>Tries to locate a given item within the visual tree, starting with the dependency object at a given position.</summary>
		/// <typeparam name="T">The type of the element to be found on the visual tree of the element at the given location.</typeparam>
		/// <param name="iReference">The main element which is used to perform hit testing.</param>
		/// <param name="iPoint">The position to be evaluated on the origin.</param>
		public static T TryFindFromPoint<T>(this UIElement iReference, Point iPoint) where T : DependencyObject
		{
			DependencyObject element = iReference.InputHitTest(iPoint) as DependencyObject;
			if (element == null)
			{
				return null;
			}
			else if (element is T)
				return (T)element;
			else
				return TryFindParent<T>(element);
		}

		/// <summary>
		/// Tries to locate a child of a given item within the visual tree
		/// </summary>
		/// <typeparam name="T">The type of the element to be found on the visual tree of the parent to the element</typeparam>
		/// <param name="referenceVisual">A direct or indirect parent of the element to be found</param>
		/// <returns>The first child item that matches the submitted type parameter. If not matching item can be found, a null reference is being returned.</returns>
		public static T GetVisualChild<T>(Visual referenceVisual) where T : Visual
		{
			Visual child = null;
			for (Int32 i = 0; i < VisualTreeHelper.GetChildrenCount(referenceVisual); i++)
			{
				child = VisualTreeHelper.GetChild(referenceVisual, i) as Visual;
				if (child != null && (child.GetType() == typeof(T)))
				{
					break;
				}
				else if (child != null)
				{
					child = GetVisualChild<T>(child);
					if (child != null && (child.GetType() == typeof(T)))
					{
						break;
					}
				}
			}
			return child as T;
		}
	}

	#endregion

	#region Converters

	/// <summary>
	/// Represents a converter for turning an icon path into a bitmap image
	/// </summary>
	public class StringToBitmapImageConverter : IValueConverter
	{
		/// <summary>
		/// Converts a path into a bitmap
		/// </summary>
		/// <param name="value">The path to the image</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>A bitmap image created from the path given</returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null) return null;
			string uristring = value as string;
			if (uristring.Substring(uristring.Length-4) == ".ico")
				return Utilities.GetIcoImage(uristring, 16, 16);
			return new BitmapImage(new Uri(uristring, UriKind.RelativeOrAbsolute));
		}

		/// <summary>
		/// This method is not implemented and will throw an exception if used
		/// </summary>
		/// <param name="value">The image</param>
		/// <param name="targetType">The target type</param>
		/// <param name="parameter">Additional parameters</param>
		/// <param name="culture">The current culture</param>
		/// <returns>Nothing</returns>
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Represents a converter for turning an amount of seconds into a formatted string.
	/// </summary>
	public class DurationConverter : IValueConverter
	{
		/// <summary>
		/// Converts a duration to a length.
		/// </summary>
		/// <param name="value">The amount of seconds</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>The length as DD HH:MM:SS</returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null) return "";
			try
			{
				int amount = System.Convert.ToInt32(value);
				return U.TimeSpanToString(new TimeSpan(0, 0, amount));
			}
			catch
			{
				return "N/A";
			}
		}

		/// <summary>
		/// This method is not implemented and will throw an exception if used.
		/// </summary>
		/// <param name="value">The length</param>
		/// <param name="targetType">The target type</param>
		/// <param name="parameter">Additional parameters</param>
		/// <param name="culture">The current culture</param>
		/// <returns>Nothing</returns>
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Represents a converter for turning a DateTime object into a formatted string according to locale.
	/// </summary>
	public class DateTimeConverter : IValueConverter
	{
		/// <summary>
		/// Converts a DateTime to a formatted string according to the current culture.
		/// </summary>
		/// <param name="value">The DateTime</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>A localized representation of the DateTime</returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || !(value is DateTime)) return "";
			DateTime dt = (DateTime)value;
			if (dt.Year < 2) return "";
			string date = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
			string time = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
			return dt.ToString(date+" "+time);
		}

		/// <summary>
		/// This method is not implemented and will throw an exception if used.
		/// </summary>
		/// <param name="value">The length</param>
		/// <param name="targetType">The target type</param>
		/// <param name="parameter">Additional parameters</param>
		/// <param name="culture">The current culture</param>
		/// <returns>Nothing</returns>
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Represents a converter for turning a number into a formatted string.
	/// </summary>
	public class NumberConverter : IValueConverter
	{
		/// <summary>
		/// Converts a number to a formatted string according to the current culture.
		/// </summary>
		/// <param name="value">The number</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>A localized format of the number</returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null) return "";
			if (value is int) return U.T((int)value);
			if (value is double) return U.T((double)value);
			return value.ToString();
		}

		/// <summary>
		/// This method is not implemented and will throw an exception if used.
		/// </summary>
		/// <param name="value">The length</param>
		/// <param name="targetType">The target type</param>
		/// <param name="parameter">Additional parameters</param>
		/// <param name="culture">The current culture</param>
		/// <returns>Nothing</returns>
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Represents a converter for turning a SourceType enum into a translated description.
	/// </summary>
	public class SourceTypeConverter : IValueConverter
	{
		/// <summary>
		/// Converts a SourceType to a translated description.
		/// </summary>
		/// <param name="value">The SourceType</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>A translated description of the type</returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null) return "";
			return U.T((SourceType)value);
		}

		/// <summary>
		/// This method is not implemented and will throw an exception if used.
		/// </summary>
		/// <param name="value">The length</param>
		/// <param name="targetType">The target type</param>
		/// <param name="parameter">Additional parameters</param>
		/// <param name="culture">The current culture</param>
		/// <returns>Nothing</returns>
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Represents a converter for turning a PluginType enum into a translated description.
	/// </summary>
	public class PluginTypeConverter : IValueConverter
	{
		/// <summary>
		/// Converts a PluginType to a translated description.
		/// </summary>
		/// <param name="value">The PluginType</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>A translated description of the type</returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null) return "";
			return U.T((Plugins.PluginType)value);
		}

		/// <summary>
		/// This method is not implemented and will throw an exception if used.
		/// </summary>
		/// <param name="value">The length</param>
		/// <param name="targetType">The target type</param>
		/// <param name="parameter">Additional parameters</param>
		/// <param name="culture">The current culture</param>
		/// <returns>Nothing</returns>
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	#endregion
}
