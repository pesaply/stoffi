/***
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;

using Stoffi.Core;
using Stoffi.Core.Settings;

namespace Stoffi.Player.GUI.Controls
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

		private GridView multiColumnGrid = new GridView();
		private GridView singleColumnGrid = new GridView();
		private GridView listGrid = new GridView();
		private GridView contentGrid = new GridView();

		private ViewMode viewMode = ViewMode.Details;

		private ItemsPanelTemplate listTemplate = null;
		private ItemsPanelTemplate wrapTemplate = null;

		private string primary = null;
		private string secondary1 = null;
		private string secondary2 = null;
		private string tertiary1 = null;
		private string tertiary2 = null;

		private string primaryFormat = "{0}";
		private string secondary1Format = "{0}";
		private string secondary2Format = "{0}";
		private string tertiary1Format = "{0}";
		private string tertiary2Format = "{0}";

		private IValueConverter primaryConverter = null;
		private IValueConverter secondary1Converter = null;
		private IValueConverter secondary2Converter = null;
		private IValueConverter tertiary1Converter = null;
		private IValueConverter tertiary2Converter = null;

		private string primaryConverterName = null;
		private string secondary1ConverterName = null;
		private string secondary2ConverterName = null;
		private string tertiary1ConverterName = null;
		private string tertiary2ConverterName = null;

		private ContextMenu headerMenu = new ContextMenu();
		private ContextMenu itemMenu = new ContextMenu();
		private MenuItem clearSortMenuItem = new MenuItem();
		private Hashtable columns = new Hashtable();
		private Hashtable columnTable = new Hashtable();
		private Hashtable headerMenuTable = new Hashtable();
		private GridViewColumnHeader currentSortColumn = null;
		private ListSortDirection currentSortDirection = ListSortDirection.Ascending;
		private ViewDetailsDropTarget dropTarget;
		private ViewDetailsSearchOverlay searchOverlay;
		private ListColumn numberColumn = new ListColumn();
		private double lastScroll = 0;
		private bool hasNumber = false;
		private bool isNumberVisible = false;
		private int numberIndex = 0;
		private bool lockSortOnNumber = false;
		private ListConfig config = null;
		private ViewDetailsSearchDelegate filterMatch;
		private string filter = "";
		private int focusItemIndex = -1;
		private bool useAeroHeaders = true;
		private double listItemWidth = 160.0;
		private RelativeSource relativeSource = new RelativeSource()
		{
			AncestorType = typeof(ViewDetails),
			Mode = RelativeSourceMode.FindAncestor
		};
		private bool grouping = false;

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
					GridViewColumn gvc = multiColumnGrid.Columns[numberIndex];
					multiColumnGrid.Columns.RemoveAt(numberIndex);
					MenuItem mi = (MenuItem)headerMenu.Items[numberIndex];
					headerMenu.Items.RemoveAt(numberIndex);

					if (value >= 0)
					{
						headerMenu.Items.Insert(value, mi);
						multiColumnGrid.Columns.Insert(value, gvc);
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
		public ListConfig Config
		{
			get { return config; }
			set
			{
				if (value != null)
				{
					// clear current columns
					columns.Clear();
					multiColumnGrid.Columns.Clear();
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
					IconSize = value.IconSize;
					Mode = value.Mode;
					AcceptFileDrops = value.AcceptFileDrops;
					SelectIndices(value.SelectedIndices);

					value.Columns.CollectionChanged += new NotifyCollectionChangedEventHandler(Columns_CollectionChanged);

					// add columns
					foreach (ListColumn vdc in value.Columns)
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
							if (String.IsNullOrWhiteSpace(sort) || sort.Length < 3)
								continue;
							ListSortDirection dir = sort.Substring(0, 3) == "asc" ? ListSortDirection.Ascending : ListSortDirection.Descending;
							string name = sort.Substring(4);
							if (name == "Number") name = "#";
							foreach (var column in value.Columns)
							{
								if (column.Binding == name)
								{
									Sort(column, dir);
									break;
								}
							}
						}
					}

					config = value;
					UpdateScrollPosition();
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
				foreach (var d in SelectedItems)
				{
					ViewDetailsItem i = ItemContainerGenerator.ContainerFromItem(SelectedItem) as ViewDetailsItem;
					if (i != null && i.IsSelected)
						return true;
				}
				return false;
			}
		}

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

				Items.Filter = delegate(object item)
				{
					return U.TrackMatchesQuery((ListItem)item, value);
				};

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
				multiColumnGrid.ColumnHeaderContainerStyle = value ? (Style)FindResource("AeroHeaderStyle") : null;
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

		/// <summary>
		/// Gets or sets the view mode.
		/// </summary>
		public ViewMode Mode
		{
			get { return viewMode; }
			set
			{
				viewMode = value;

				if (config != null)
					config.Mode = viewMode;

				GridView view = singleColumnGrid;
				ItemsPanelTemplate panel = wrapTemplate;
				string container = "IconStyle";
				Thickness padding = new Thickness(10, 0, 10, 0);
				Orientation wrapOrientation = Orientation.Horizontal;

				ScrollBarVisibility horizontalScrollBar = ScrollBarVisibility.Disabled;
				ScrollBarVisibility verticalScrollBar = ScrollBarVisibility.Auto;

				switch (viewMode)
				{
					case ViewMode.Details:
						view = multiColumnGrid;
						panel = listTemplate;
						container = "RowStyle";
						padding = new Thickness(0);
						horizontalScrollBar = ScrollBarVisibility.Auto;
						verticalScrollBar = ScrollBarVisibility.Auto;
						break;

					case ViewMode.Content:
						view = singleColumnGrid;
						panel = listTemplate;
						container = "ContentStyle";
						padding = new Thickness(0);
						break;

					case ViewMode.List:
						wrapOrientation = Orientation.Vertical;
						horizontalScrollBar = ScrollBarVisibility.Auto;
						verticalScrollBar = ScrollBarVisibility.Disabled;
						break;
				}

				if (panel != null)
					ItemsPanel = panel;

				if (VisualStyleInformation.DisplayName != "")
					ItemContainerStyle = (Style)TryFindResource("Aero" + container);
				else
					ItemContainerStyle = (Style)TryFindResource("Classic" + container);
				
				RefreshItemTemplate();
				Padding = padding;
				View = view;
				WrapOrientation = wrapOrientation;

				ScrollViewer sv = GetScrollViewer();
				if (sv != null)
				{
					sv.HorizontalScrollBarVisibility = horizontalScrollBar;
					sv.VerticalScrollBarVisibility = verticalScrollBar;
				}

				UpdateSingleColumnWidth();
				DispatchPropertyChanged("Mode");
			}
		}

		/// <summary>
		/// Gets or sets whether or not the tracks should be grouped into categories.
		/// </summary>
		public bool Grouping
		{
			get { return grouping; }
			set
			{
				grouping = value;
				try
				{
					CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.ItemsSource);
					view.GroupDescriptions.Clear();
					if (value)
					{
						GroupStyle gs = (GroupStyle)TryFindResource("AeroGroupStyle");
						if (gs != null)
							this.GroupStyle.Add(gs);
						PropertyGroupDescription groupDescription = new PropertyGroupDescription("Group");
						view.GroupDescriptions.Add(groupDescription);
					}
					else
						this.GroupStyle.Clear();
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "LIST", "Could not change grouping to " + value + ": " + e.Message);
				}
			}
		}

		#region Columns

		/// <summary>
		/// Gets or sets the primary column.
		/// </summary>
		public string Primary
		{
			get { return primary; }
			set { primary = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the first secondary column.
		/// </summary>
		public string Secondary1
		{
			get { return secondary1; }
			set { secondary1 = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the second secondary column.
		/// </summary>
		public string Secondary2
		{
			get { return secondary2; }
			set { secondary2 = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the first tertiary column.
		/// </summary>
		public string Tertiary1
		{
			get { return tertiary1; }
			set { tertiary1 = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the second tertiary column.
		/// </summary>
		public string Tertiary2
		{
			get { return tertiary2; }
			set { tertiary2 = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the format of the primary column.
		/// </summary>
		public string PrimaryFormat
		{
			get { return primaryFormat; }
			set { primaryFormat = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the format of the first secondary column.
		/// </summary>
		public string Secondary1Format
		{
			get { return secondary1Format; }
			set { secondary1Format = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the format of the second secondary column.
		/// </summary>
		public string Secondary2Format
		{
			get { return secondary2Format; }
			set { secondary2Format = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the format of the first tertiary column.
		/// </summary>
		public string Tertiary1Format
		{
			get { return tertiary1Format; }
			set { tertiary1Format = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the format of the second tertiary column.
		/// </summary>
		public string Tertiary2Format
		{
			get { return tertiary2Format; }
			set { tertiary2Format = value; RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the converter of the primary column.
		/// </summary>
		public string PrimaryConverter
		{
			get { return primaryConverterName; }
			set { primaryConverterName = value; primaryConverter = GetConverter(value); RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the converter of the first secondary column.
		/// </summary>
		public string Secondary1Converter
		{
			get { return secondary1ConverterName; }
			set { secondary1ConverterName = value; secondary1Converter = GetConverter(value); RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the converter of the second secondary column.
		/// </summary>
		public string Secondary2Converter
		{
			get { return secondary2ConverterName; }
			set { secondary2ConverterName = value; secondary2Converter = GetConverter(value); RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the converter of the first tertiary column.
		/// </summary>
		public string Tertiary1Converter
		{
			get { return tertiary1ConverterName; }
			set { tertiary1ConverterName = value; tertiary1Converter = GetConverter(value); RefreshItemTemplate(); }
		}

		/// <summary>
		/// Gets or sets the converter of the second tertiary column.
		/// </summary>
		public string Tertiary2Converter
		{
			get { return tertiary2ConverterName; }
			set { tertiary2ConverterName = value; tertiary2Converter = GetConverter(value); RefreshItemTemplate(); }
		}

		#endregion

		#region Dependency properties

		/// <summary>
		/// Gets or sets the size of the icons.
		/// </summary>
		/// <remarks>
		/// Only applicable when Mode is Icons.
		/// </remarks>
		public Double IconSize
		{
			get { return (Double)GetValue(IconSizeProperty); }
			set
			{
				if (value < 16) value = 16;
				else if (value > 256) value = 256;

				if (config != null)
					config.IconSize = value;

				if (value <= 30)
				{
					IconMargin = new Thickness(0, 0, 0, 0);
					IconPadding = new Thickness(0, 0, 0, 0);
				}
				else
				{
					IconMargin = new Thickness(5, 5, 5, 0);
					IconPadding = new Thickness(0, 5, 0, 5);
				}

				SetValue(IconSizeProperty, value);
				UpdateSingleColumnWidth();
				RefreshItemTemplate();

				VirtualizingWrapPanel vwp = ViewDetailsUtilities.GetVisualChild<VirtualizingWrapPanel>(this);
				if (vwp != null)
					vwp.doResize = true;
			}
		}

		/// <summary>
		/// The icon size dependency property.
		/// </summary>
		public static readonly DependencyProperty IconSizeProperty =
			DependencyProperty.Register(
			"IconSize",
			typeof(Double),
			typeof(ViewDetails),
			new PropertyMetadata(96.0));

		/// <summary>
		/// Gets or sets the margin of the icons in the icon grid.
		/// </summary>
		public Thickness IconMargin
		{
			get { return (Thickness)GetValue(IconMarginProperty); }
			set { SetValue(IconMarginProperty, value); }
		}

		/// <summary>
		/// The icon margin dependency property.
		/// </summary>
		public static readonly DependencyProperty IconMarginProperty =
			DependencyProperty.Register(
			"IconMargin",
			typeof(Thickness),
			typeof(ViewDetails),
			new PropertyMetadata(new Thickness(5, 5, 5, 0)));

		/// <summary>
		/// Gets or sets the padding of the icons in the icon grid.
		/// </summary>
		public Thickness IconPadding
		{
			get { return (Thickness)GetValue(IconPaddingProperty); }
			set { SetValue(IconPaddingProperty, value); }
		}

		/// <summary>
		/// The icon margin dependency property.
		/// </summary>
		public static readonly DependencyProperty IconPaddingProperty =
			DependencyProperty.Register(
			"IconPadding",
			typeof(Thickness),
			typeof(ViewDetails),
			new PropertyMetadata(new Thickness(0, 5, 0, 5)));

		/// <summary>
		/// Gets or sets the orientation of the wrapping panel.
		/// </summary>
		public Orientation WrapOrientation
		{
			get { return (Orientation)GetValue(WrapOrientationProperty); }
			set { SetValue(WrapOrientationProperty, value); }
		}

		/// <summary>
		/// The wrap panel's orientation dependency property.
		/// </summary>
		public static readonly DependencyProperty WrapOrientationProperty =
			DependencyProperty.Register(
			"WrapOrientation",
			typeof(Orientation),
			typeof(ViewDetails),
			new PropertyMetadata(Orientation.Horizontal));

		#endregion

		#endregion

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
			UseAeroHeaders = VisualStyleInformation.DisplayName != "";

			InitHeaderMenu();
			multiColumnGrid.ColumnHeaderContextMenu = headerMenu;

			singleColumnGrid.ColumnHeaderContainerStyle = (Style)TryFindResource("HiddenHeaderStyle");

			listTemplate = ItemsPanel;
			wrapTemplate = (ItemsPanelTemplate)TryFindResource("WrapTemplate");

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
			AcceptFileDrops = true;
			Mode = ViewMode.Details;

			multiColumnGrid.Columns.CollectionChanged += new NotifyCollectionChangedEventHandler(Columns_CollectionChanged);
		}

		#endregion Constructor

		#region Methods

		#region Public

		/// <summary>
		/// Checks if the list is sorted on a specific column field.
		/// </summary>
		/// <param name="field">The name of the columns binded property</param>
		/// <returns>Whether or not the list is sorted on the column</returns>
		public bool IsSortedOn(string field)
		{
			return (config.Sorts.Contains("asc:"+field) || config.Sorts.Contains("desc:"+field));
		}

		/// <summary>
		/// Adds a column to the list
		/// </summary>
		/// <param name="column">The column to be added</param>
		/// <param name="index">The index to insert at (-1 means last)</param>
		/// <param name="addToConfig">Whether the column should be added to the config</param>
		public void AddColumn(ListColumn column, int index = -1, bool addToConfig = true)
		{
			// create header
			GridViewColumnHeader gvch = new GridViewColumnHeader();
			gvch.Tag = column.Binding;
			gvch.Content = column.Text;
			gvch.HorizontalAlignment = ConvertHAlignment(column.Alignment);
			gvch.Click += Column_Click;
			gvch.SizeChanged += Column_SizeChanged;
			gvch.HorizontalAlignment = HorizontalAlignment.Stretch;

			if (VisualStyleInformation.DisplayName != "")
			{
				gvch.SetResourceReference(GridViewColumnHeader.TemplateProperty, "AeroHeaderTemplate");
				gvch.ContentTemplate = (DataTemplate)FindResource("HeaderTemplate");
			}

			// create column
			GridViewColumn gvc = new GridViewColumn();
			gvc.Header = gvch;
			gvc.CellTemplate = CreateDetailsTemplate(column.Binding, column.Alignment, false, (UseIcons && multiColumnGrid.Columns.Count == 0), column.Converter);
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
				multiColumnGrid.Columns.Insert(index, gvc);
			else if (column.IsVisible)
				multiColumnGrid.Columns.Add(gvc);

			if (config != null && addToConfig)
			{
				if (config.Columns == null)
					config.Columns = new ObservableCollection<ListColumn>();
				config.Columns.Add(column);
			}

			RefreshHeaderMenu();

			if (singleColumnGrid.Columns.Count == 0)
			{
				singleColumnGrid.Columns.Add(new GridViewColumn()
				{
					Header = gvch,
					CellTemplate = GetItemTemplate()
				});
				UpdateSingleColumnWidth();
			}

			if (Primary == null)
				Primary = column.Binding;
			else if (Secondary1 == null)
				Secondary1 = column.Binding;
			else if (Secondary2 == null)
				Secondary2 = column.Binding;
			else if (Tertiary1 == null)
				Tertiary1 = column.Binding;
			else if (Tertiary2 == null)
				Tertiary2 = column.Binding;

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
			ListColumn vdc = new ListColumn();
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
		public void RemoveColumn(ListColumn column, bool removeFromConfig = true)
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
				multiColumnGrid.Columns.Remove(gvc);
				columnTable.Remove(column.Text);
			}

			if (columns.ContainsKey(column.Text))
			{
				ListColumn vdc = (ListColumn)columns[column.Text];
				if (config != null && config.Columns != null && removeFromConfig)
					config.Columns.Remove(vdc);
				columns.Remove(column.Text);
			}

			if (columns.Count == 0)
				singleColumnGrid.Columns.Clear();
		}

		/// <summary>
		/// Selects a given list of indices of items
		/// </summary>
		/// <param name="indices">The indices of the items to select</param>
		public void SelectIndices(IEnumerable<uint> indices)
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
		public void SelectItem(ListItem item)
		{
			if (item == null)
				return;

			if (Items.Contains(item))
				SelectedItem = item;

			else
			{
				foreach (ListItem i in Items)
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
						ListColumn vdc = (ListColumn)columns[c.Key];
						GridViewColumn gvc = (GridViewColumn)c.Value;
						gvc.CellTemplate = CreateDetailsTemplate(vdc.Binding, vdc.Alignment, false, (UseIcons && multiColumnGrid.Columns.IndexOf(gvc) == 0), vdc.Converter);
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
		public ListItem GetItemAt(int index)
		{
			return Items[index] as ListItem;
		}

		/// <summary>
		/// Returns the graphical index of an item source
		/// </summary>
		/// <param name="logicalObject">The item source</param>
		/// <returns>The graphical index of <paramref name="logicalObject"/></returns>
		public int IndexOf(ListItem logicalObject)
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
			if (searchOverlay != null)
				searchOverlay.RefreshStrings();
			if (clearSortMenuItem != null)
				clearSortMenuItem.Header = U.T("MenuClearSort");
		}

		/// <summary>
		/// Updates the scroll position according to the
		/// saved values in the configuration.
		/// </summary>
		public void UpdateScrollPosition()
		{
			var sv = GetScrollViewer();
			if (sv != null && config != null)
			{
				double v = config.VerticalScrollOffset;
				double h = config.HorizontalScrollOffset;
				if (!double.IsInfinity(v) && !double.IsNaN(v))
					sv.ScrollToVerticalOffset(v);
				if (!double.IsInfinity(h) && !double.IsNaN(h))
					sv.ScrollToHorizontalOffset(h);
			}
		}

		#endregion Public

		#region Private

		#region Data templates

		/// <summary>
		/// Gets the data template to use for the
		/// icon view according to the icon size.
		/// </summary>
		/// <returns>The proper data template to use</returns>
		private DataTemplate CreateIconTemplate()
		{
			DataTemplate dt = new DataTemplate();

			FrameworkElementFactory img = new FrameworkElementFactory(typeof(Image));
			img.SetValue(Image.MaxHeightProperty, IconSize);
			img.SetValue(Image.MaxWidthProperty, IconSize);
			img.SetValue(Image.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
			img.SetBinding(Image.SourceProperty, new Binding("Image") { Converter = new AlbumArtConverter() });

			FrameworkElementFactory lbl = new FrameworkElementFactory(typeof(TextBlock));
			lbl.SetValue(TextBlock.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
			lbl.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
			lbl.SetValue(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow);
			lbl.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
			lbl.SetValue(TextBlock.MarginProperty, new Thickness(0));
			lbl.SetValue(TextBlock.MinWidthProperty, 60.0);
			lbl.SetBinding(TextBlock.TextProperty, new Binding(Primary) { StringFormat = PrimaryFormat, Converter = primaryConverter });

			FrameworkElementFactory sp = new FrameworkElementFactory(typeof(StackPanel));
			sp.SetValue(StackPanel.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Top);

			if (IconSize > 30)
			{
				// image above label
				double v = IconSize / 80;
				double h = IconSize / 100;
				img.SetValue(Image.MarginProperty, new Thickness(0, 2, 0, 2));
				lbl.SetValue(TextBlock.WidthProperty, IconSize);
				lbl.SetValue(TextBlock.MaxHeightProperty, 60.0);
				lbl.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
				sp.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
				sp.SetValue(StackPanel.MarginProperty, new Thickness(2+v, h, 2+v, h));
			}
			else
			{
				// image to the left of label
				img.SetValue(Image.MarginProperty, new Thickness(0, 0, 5, 0));
				lbl.SetValue(TextBlock.WidthProperty, 200.0);
				lbl.SetValue(TextBlock.MaxHeightProperty, 20.0);
				sp.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
				sp.SetValue(StackPanel.MarginProperty, new Thickness(0, 1, 0, 1));
			}

			sp.AppendChild(img);
			sp.AppendChild(lbl);

			dt.VisualTree = sp;
			return dt;
		}

		/// <summary>
		/// Gets the data template to use for the content view.
		/// </summary>
		/// <returns>The proper data template to use</returns>
		private DataTemplate CreateTilesTemplate()
		{
			DataTemplate dt = new DataTemplate();

			double iconSize = 45.0;
			FrameworkElementFactory img = new FrameworkElementFactory(typeof(Image));
			img.SetValue(Image.MaxHeightProperty, iconSize);
			img.SetValue(Image.MaxWidthProperty, iconSize);
			img.SetValue(Image.MarginProperty, new Thickness(0, 0, 5, 0));
			img.SetValue(Image.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
			img.SetBinding(Image.SourceProperty, new Binding("Image") { Converter = new AlbumArtConverter() });

			FrameworkElementFactory title = new FrameworkElementFactory(typeof(TextBlock));
			title.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
			title.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
			title.SetBinding(TextBlock.TextProperty, new Binding(Primary) { StringFormat = PrimaryFormat, Converter = primaryConverter });

			FrameworkElementFactory line1 = new FrameworkElementFactory(typeof(TextBlock));
			line1.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
			line1.SetValue(TextBlock.ForegroundProperty, Brushes.Gray);
			line1.SetBinding(TextBlock.TextProperty, new Binding(Secondary1) { StringFormat = Secondary1Format, Converter = secondary1Converter });

			FrameworkElementFactory line2 = new FrameworkElementFactory(typeof(TextBlock));
			line2.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
			line2.SetValue(TextBlock.ForegroundProperty, Brushes.Gray);
			line2.SetBinding(TextBlock.TextProperty, new Binding(Secondary2) { StringFormat = Secondary2Format, Converter = secondary2Converter });

			FrameworkElementFactory labels = new FrameworkElementFactory(typeof(StackPanel));
			labels.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
			labels.SetValue(StackPanel.WidthProperty, 180.0);

			labels.AppendChild(title);
			labels.AppendChild(line1);
			labels.AppendChild(line2);

			FrameworkElementFactory sp = new FrameworkElementFactory(typeof(StackPanel));
			sp.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
			sp.SetValue(StackPanel.MarginProperty, new Thickness(0, 2, 0, 2));

			sp.AppendChild(img);
			sp.AppendChild(labels);

			dt.VisualTree = sp;
			return dt;
		}

		/// <summary>
		/// Gets the data template to use for the list view.
		/// </summary>
		/// <returns>The proper data template to use</returns>
		private DataTemplate CreateListTemplate()
		{
			DataTemplate dt = new DataTemplate();
			
			double iconSize = 16.0;
			FrameworkElementFactory icon = new FrameworkElementFactory(typeof(Image));
			icon.SetBinding(Image.SourceProperty, new Binding("Icon") { Converter = new StringToBitmapImageConverter() });
			icon.SetValue(Image.HeightProperty, iconSize);
			icon.SetValue(Image.WidthProperty, iconSize);
			icon.SetValue(Image.MarginProperty, new Thickness(0, 0, 5, 0));

			FrameworkElementFactory tb = new FrameworkElementFactory(typeof(TextBlock));
			tb.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
			tb.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
			tb.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
			tb.SetValue(TextBlock.MarginProperty, new Thickness(0));
			tb.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
			tb.SetValue(TextBlock.MaxWidthProperty, listItemWidth);
			tb.SetValue(TextBlock.WidthProperty, listItemWidth);
			tb.SetBinding(TextBlock.TextProperty, new Binding(Primary) { StringFormat = PrimaryFormat, Converter = primaryConverter });

			FrameworkElementFactory sp = new FrameworkElementFactory(typeof(StackPanel));
			sp.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
			sp.SetValue(StackPanel.MarginProperty, new Thickness(0, 1, 0, 1));

			sp.AppendChild(icon);
			sp.AppendChild(tb);

			dt.VisualTree = sp;
			return dt;
		}

		/// <summary>
		/// Gets the data template to use for the content view.
		/// </summary>
		/// <returns>The proper data template to use</returns>
		private DataTemplate CreateContentTemplate()
		{
			DataTemplate dt = new DataTemplate();

			double iconWidth = 35;
			double iconHeight = 50;
			FrameworkElementFactory img = new FrameworkElementFactory(typeof(Image));
			img.SetValue(Image.MaxHeightProperty, iconHeight);
			img.SetValue(Image.MaxWidthProperty, iconWidth);
			img.SetValue(Image.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
			img.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			img.SetBinding(Image.SourceProperty, new Binding("Image") { Converter = new AlbumArtConverter() });
			img.SetValue(StackPanel.MarginProperty, new Thickness(0, 3, 5, 3));

			FrameworkElementFactory imgPanel = new FrameworkElementFactory(typeof(StackPanel));
			imgPanel.SetValue(StackPanel.WidthProperty, iconWidth);
			imgPanel.AppendChild(img);

			FrameworkElementFactory title = new FrameworkElementFactory(typeof(TextBlock));
			title.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
			title.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
			title.SetValue(TextBlock.MarginProperty, new Thickness(0,0,3,0));
			title.SetValue(TextBlock.WidthProperty, 250.0);
			title.SetValue(TextBlock.FontSizeProperty, 14.0);
			title.SetValue(Image.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Top);
			title.SetBinding(TextBlock.TextProperty, new Binding(Primary) { StringFormat = PrimaryFormat, Converter = primaryConverter });

			FrameworkElementFactory[] labels = new FrameworkElementFactory[4];
			string[] bindings = { Secondary1, Secondary2, Tertiary1, Tertiary2 };
			string[] formats = { Secondary1Format, Secondary2Format, Tertiary1Format, Tertiary2Format };
			IValueConverter[] converters = { secondary1Converter, secondary2Converter, tertiary1Converter, tertiary2Converter };
			for (int i = 0; i < 4; i++)
			{
				FrameworkElementFactory key = new FrameworkElementFactory(typeof(TextBlock));
				key.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(109,109,109)));
				key.SetValue(TextBlock.TextProperty, U.T("Column" + bindings[i])+":");
				key.SetValue(TextBlock.MarginProperty, new Thickness(0, 1, 3, 1));

				FrameworkElementFactory val = new FrameworkElementFactory(typeof(TextBlock));
				val.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
				val.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
				val.SetBinding(TextBlock.TextProperty, new Binding(bindings[i]) { StringFormat = bindings[i], Converter = converters[i] });
				val.SetValue(TextBlock.MarginProperty, new Thickness(0, 1, 0, 1));

				FrameworkElementFactory label = new FrameworkElementFactory(typeof(StackPanel));
				label.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
				label.AppendChild(key);
				label.AppendChild(val);
				labels[i] = label;
			}

			FrameworkElementFactory terLabels = new FrameworkElementFactory(typeof(StackPanel));
			FrameworkElementFactory secLabels = new FrameworkElementFactory(typeof(StackPanel));
			secLabels.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
			terLabels.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
			secLabels.SetValue(StackPanel.WidthProperty, 250.0);
			terLabels.SetValue(StackPanel.WidthProperty, 250.0);
			secLabels.AppendChild(labels[0]);
			secLabels.AppendChild(labels[1]);
			terLabels.AppendChild(labels[2]);
			terLabels.AppendChild(labels[3]);

			FrameworkElementFactory esp = new FrameworkElementFactory(typeof(EnhancedStackPanel));
			esp.SetValue(EnhancedStackPanel.OrientationProperty, Orientation.Horizontal);
			esp.SetValue(EnhancedStackPanel.ToggleChildrenProperty, true);
			esp.SetValue(EnhancedStackPanel.MinimumVisibleChildrenProperty, 2);
			esp.SetValue(EnhancedStackPanel.MarginProperty, new Thickness(18, 8, 0, 8));

			esp.AppendChild(imgPanel);
			esp.AppendChild(title);
			esp.AppendChild(secLabels);
			esp.AppendChild(terLabels);

			dt.VisualTree = esp;
			return dt;
		}

		/// <summary>
		/// Uses certain parameters to create a DataTemplate which can be used as a CellTemplate for a
		/// specific column in the list.
		/// </summary>
		/// <param name="binding">The value to bind to</param>
		/// <param name="alignment">Horizontal alignment of the content</param>
		/// <param name="active">Whether the column is active or not</param>
		/// <param name="rightMost">Whether the column is the right most</param>
		/// <param name="converter">The converter to use for converting the value of the binding</param>
		/// <returns>DataTemplate to use as a CellTemplate for a column</returns>
		private DataTemplate CreateDetailsTemplate(string binding, Alignment alignment, bool active, bool rightMost, string converter)
		{
			FrameworkElementFactory dp = new FrameworkElementFactory(typeof(DockPanel));
			dp.SetValue(DockPanel.LastChildFillProperty, true);
			dp.SetValue(DockPanel.MarginProperty, new Thickness(0, 2, 0, 2));

			if (rightMost && UseIcons)
			{
				double iconSize = 16.0;
				FrameworkElementFactory icon = new FrameworkElementFactory(typeof(Image));
				icon.SetBinding(Image.SourceProperty, new Binding("Icon") { Converter = new StringToBitmapImageConverter() });
				icon.SetValue(Image.WidthProperty, iconSize);
				icon.SetValue(Image.HeightProperty, iconSize);
				icon.SetValue(Image.MarginProperty, new Thickness(15, 0, 5, 0));
				dp.AppendChild(icon);
			}

			Binding b = new Binding(binding);
			b.Converter = GetConverter(converter);

			FrameworkElementFactory tb = new FrameworkElementFactory(typeof(TextBlock));
			tb.SetBinding(TextBlock.TextProperty, b);
			tb.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
			tb.SetValue(TextBlock.HorizontalAlignmentProperty, ConvertHAlignment(alignment));
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
		/// Gets the data template to use for the
		/// icon view according to the icon size.
		/// </summary>
		/// <returns>The proper data template to use</returns>
		private DataTemplate GetItemTemplate()
		{
			DataTemplate dt = null;

			switch (viewMode)
			{
				case ViewMode.Icons:
					dt = CreateIconTemplate();
					break;

				case ViewMode.Content:
					dt = CreateContentTemplate();
					break;

				case ViewMode.List:
					dt = CreateListTemplate();
					break;

				case ViewMode.Tiles:
					dt = CreateTilesTemplate();
					break;
			}

			return dt;
		}

		/// <summary>
		/// Refreshes the data template for the items.
		/// </summary>
		private void RefreshItemTemplate()
		{
			if (singleColumnGrid.Columns.Count == 0)
				return;

			singleColumnGrid.Columns[0].CellTemplate = GetItemTemplate();
			RefreshView();
		}

		/// <summary>
		/// Retrives a converter given is textual name used in the column configurations.
		/// </summary>
		/// <param name="name">The name of the converter</param>
		/// <returns>The proper value converter if found, otherwise null</returns>
		private IValueConverter GetConverter(string name)
		{
			switch (name)
			{
				case "Number":
					return new NumberConverter();

				case "DateTime":
					return new DateTimeConverter();

				case "Duration":
					return new DurationConverter();

				case "SourceType":
					return new SourceTypeConverter();

				case "PluginType":
					return new PluginTypeConverter();
			}
			return null;
		}

		#endregion

		/// <summary>
		/// Updates the width of the single column.
		/// </summary>
		private void UpdateSingleColumnWidth()
		{
			if (singleColumnGrid.Columns.Count > 0)
			{
				if (viewMode == ViewMode.Icons)
				{
					Double v = IconSize;
					if (v <= 30)
						v = 214;
					else if (v < 60)
						v = 60;
					singleColumnGrid.Columns[0].Width = v + 18;
				}
				else if (viewMode == ViewMode.List)
					singleColumnGrid.Columns[0].Width = listItemWidth + 35;
				else if (viewMode == ViewMode.Content)
				{
					singleColumnGrid.Columns[0].Width = Math.Max(ActualWidth - 40, 0);
				}
				else
					singleColumnGrid.Columns[0].Width = double.NaN;
			}
		}

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
			ListColumn vdc = columns[name] as ListColumn;
			item.IsChecked = visible;
			if (visible)
			{
				// calculate the position to insert the column based on the position in the context menu
				int pos = 0;
				foreach (MenuItem mi in multiColumnGrid.ColumnHeaderContextMenu.Items)
				{
					ListColumn c = columns[mi.Tag] as ListColumn;
					if (c == vdc)
						break;
					if (c.IsVisible) // only count visible columns
						pos++;
				}
				if (multiColumnGrid.Columns.Contains(column))
					multiColumnGrid.Columns.Remove(column);
				multiColumnGrid.Columns.Insert(pos, column);
			}
			else
				multiColumnGrid.Columns.Remove(column);

			int i = 0;
			foreach (GridViewColumn gvc in multiColumnGrid.Columns)
			{
				string n = (string)((GridViewColumnHeader)gvc.Header).Content;
				ListColumn col = FindColumn(n);
				gvc.CellTemplate = CreateDetailsTemplate(col.Binding, col.Alignment, (currentSortColumn == (GridViewColumnHeader)gvc.Header), i < 1, col.Converter);
				i++;
			}

			if (HasNumber)
			{
				numberIndex = multiColumnGrid.Columns.IndexOf((GridViewColumn)columnTable["#"]);
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
			clearSortMenuItem.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
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
			ListColumn onlyVisible = numberColumn != null && numberColumn.IsVisible ? numberColumn : null;
			foreach (ListColumn column in columns.Values)
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
		private void Sort(ListColumn vdc, ListSortDirection direction)
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
					ListColumn vdc_ = (ListColumn)columns[key];
					bool active = (key == vdc.Name);
					bool rightMost = multiColumnGrid.Columns.IndexOf(gvc) == 0;
					gvc.CellTemplate = CreateDetailsTemplate(vdc_.Binding, vdc_.Alignment, active, rightMost, vdc_.Converter);
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
		/// Find the corresponding column configuration given the content of the column
		/// </summary>
		/// <param name="content">The displayed text on the column</param>
		/// <returns>The column configuration for the column</returns>
		private ListColumn FindColumn(string content)
		{
			if (content == "#")
				return numberColumn;

			foreach (DictionaryEntry i in columns)
			{
				if (((ListColumn)i.Value).Text == content)
					return (ListColumn)i.Value;
			}
			return null;
		}

		/// <summary>
		/// Gets the ScrollViewer of the list.
		/// </summary>
		/// <param name="o">The object in which to look for the scrollbar </param>
		/// <returns>The ScrollViewer of this list</returns>
		private ScrollViewer GetScrollViewer(DependencyObject o = null)
		{
			if (o == null) o = this;

			// Return the DependencyObject if it is a ScrollViewer
			if (o is ScrollViewer)
			{ return o as ScrollViewer; }

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
		private System.Windows.VerticalAlignment ConvertVAlignment(Alignment alignment)
		{
			switch (alignment)
			{
				default:
				case Alignment.Top:
					return System.Windows.VerticalAlignment.Top;

				case Alignment.Bottom:
					return System.Windows.VerticalAlignment.Bottom;

				case Alignment.Center:
				case Alignment.Middle:
					return System.Windows.VerticalAlignment.Center;
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
			if (double.IsInfinity(constraint.Width))
				constraint.Width = DesiredSize.Width;
			if (double.IsInfinity(constraint.Height))
				constraint.Height = DesiredSize.Height;

			Size size = base.MeasureOverride(constraint);
			if (searchOverlay != null)
				searchOverlay.Measure(constraint);

			return size;
		}

		/// <summary>
		/// Raises the SizeChanged event, using the specified information as part of the eventual event data.
		/// </summary>
		/// <param name="sizeInfo">Details of the old and new size involved in the change</param>
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			UpdateSingleColumnWidth();
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
			try
			{
				// if all numbers are zero we fix the numbers
				bool allZero = true;
				foreach (ListItem item in newValue)
					if (item.Number != 0)
					{
						allZero = false;
						break;
					}
				int i = 1;
				if (allZero)
					foreach (ListItem item in newValue)
						item.Number = i++;
			}
			catch { }

			base.OnItemsSourceChanged(oldValue, newValue);

			UpdateScrollPosition();
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
			dropTarget.Visibility = Visibility.Collapsed;
			if (!(e.Data.GetDataPresent(DataFormats.FileDrop) && AcceptFileDrops) &&
				!(e.Data.GetDataPresent(typeof(List<object>).FullName) && IsDragSortable))
			{
				e.Effects = DragDropEffects.None;
				return;
			}

			bool isH = dropTarget.Orientation == Orientation.Horizontal;

			// locate the item nearest the mouse pointer
			Point p = e.GetPosition(this);
			ListViewItem lvi = ViewDetailsUtilities.TryFindClosestFromPoint<ListViewItem>(this, p, 10);

			// get index of drop position
			int i = Items.Count;
			if (lvi != null)
			{
				Point lt = lvi.TranslatePoint(new Point(0, 0), this);
				i = this.ItemContainerGenerator.IndexFromContainer(lvi);
				if ((isH && p.Y >= lt.Y + lvi.ActualHeight / 2) ||
					(!isH && p.X >= lt.X + lvi.ActualWidth / 2))
					i++;
			}

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] paths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
				if (lvi != null)
					DispatchFilesDropped(paths, i);
				else
					DispatchFilesDropped(paths, Items.Count);
			}

			else if (e.Data.GetDataPresent(typeof(List<object>).FullName))
			{
				if (lvi != null)
				{
					List<object> items = e.Data.GetData(typeof(List<object>).FullName) as List<object>;

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
						foreach (ListItem o in Items)
						{
							o.Number = Items.IndexOf(o) + 1;
						}
					}

					SetSelectedItems(items);
				}
			}
		}

		/// <summary>
		/// Invoked when an item is dragged over the list
		/// </summary>
		/// <param name="e">The event data</param>
		protected override void OnDragOver(DragEventArgs e)
		{
			// only accept files and objects (for internal drag)
			if (!(e.Data.GetDataPresent(DataFormats.FileDrop) && AcceptFileDrops) &&
				!(e.Data.GetDataPresent(typeof(List<object>).FullName) && IsDragSortable))
			{
				e.Effects = DragDropEffects.None;
				return;
			}

			// locate the item nearest the mouse pointer
			Point p = e.GetPosition(this);
			ListViewItem lvi = ViewDetailsUtilities.TryFindClosestFromPoint<ListViewItem>(this, p, 10);

			if (lvi != null)
			{
				Point lt = lvi.TranslatePoint(new Point(0, 0), this);
				bool firstHalf = false;
				bool isH = !(Mode == ViewMode.Icons || Mode == ViewMode.Tiles);
				double pointer = 0; // pointer position
				double start = 0; // item start position
				double length = 0; // item length
				ListViewItem b = null; // item before pointer pos (no wrap)
				ListViewItem a = null; // item after pointer pos (no wrap)
				if (isH)
				{
					pointer = p.Y;
					start = lt.Y;
					length = lvi.ActualHeight;
				}
				else
				{
					pointer = p.X;
					start = lt.X;
					length = lvi.ActualWidth;
				}

				// find items before (b) and after (a) pointer pos (no wrap)
				firstHalf = pointer < start + length / 2;
				int delta = firstHalf ? -1 : 1;
				if (firstHalf) a = lvi;
				else b = lvi;
				Point p1 = new Point(p.X, p.Y);
				double tmp = firstHalf ? start - 1 : start + length + 1;
				if (isH) p1.Y = tmp;
				else p1.X = tmp;
				double maxSteps = Math.Max(lvi.ActualHeight, lvi.ActualWidth);
				for (int i = 0; i < maxSteps && (b == null || a == null); i++)
				{
					if (isH) p1.Y += delta;
					else p1.X += delta;
					if (firstHalf) b = ViewDetailsUtilities.TryFindFromPoint<ListViewItem>(this, p1);
					else a = ViewDetailsUtilities.TryFindFromPoint<ListViewItem>(this, p1);
				}

				// find edges between items a and b
				double _a = 0;
				double _b = 0;
				if (a == null)
				{
					a = b;
					p1 = b.TranslatePoint(new Point(0, 0), this);
					_a = _b = isH ? p1.Y + b.ActualHeight : p1.X + b.ActualWidth;
				}
				else if (b == null)
				{
					b = a;
					p1 = a.TranslatePoint(new Point(0, 0), this);
					_a = _b = isH ? p1.Y : p1.X;
				}
				else
				{
					p1 = b.TranslatePoint(new Point(0, 0), this);
					_b = isH ? p1.Y + b.ActualHeight : p1.X + b.ActualWidth;
					p1 = a.TranslatePoint(new Point(0, 0), this);
					_a = isH ? p1.Y : p1.X;
				}

				// now we can finally express the position of the drop target as
				dropTarget.Position = _b + ((_a - _b) / 2);

				// drop target line should not obscure the scrollbar
				ScrollViewer sv = GetScrollViewer();
				dropTarget.ScrollBar = sv != null && 
					((sv.ComputedVerticalScrollBarVisibility == Visibility.Visible && isH) ||
					(sv.ComputedHorizontalScrollBarVisibility == Visibility.Visible && !isH));

				// adjust the drop target according to view mode
				if (Mode == ViewMode.Icons || Mode == ViewMode.Tiles)
				{
					dropTarget.Length = Math.Max(a.ActualHeight, b.ActualHeight);
					dropTarget.Offset = lt.Y;
				}
				else if (Mode == ViewMode.List)
				{
					dropTarget.Length = Math.Max(a.ActualWidth, b.ActualWidth);
					dropTarget.Offset = lt.X;
				}
				else
				{
					dropTarget.Length = double.NaN;
					dropTarget.Offset = 0;
				}
				dropTarget.Orientation = isH ? Orientation.Horizontal : Orientation.Vertical;

				// perform scrolling if drag is near border
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

				dropTarget.Visibility = Visibility.Visible;
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
					uint i = (uint)Items.IndexOf(o);
					if (!config.SelectedIndices.Contains(i))
						config.SelectedIndices.Add(i);
				}
			}
			base.OnSelectionChanged(e);
		}

		/// <summary>
		/// Causes the object to scroll into view.
		/// </summary>
		/// <param name="item">Object to view</param>
		public new void ScrollIntoView(object item)
		{
			if (Mode != ViewMode.Icons)
				base.ScrollIntoView(item);
			else if (item != null)
			{
				// using virtualized wrap panel so we need special solution
				int i = Items.IndexOf(item);
				ScrollViewer sv = GetScrollViewer();
				if (sv != null)
				{
					ListViewItem lvi = ViewDetailsUtilities.TryFindFromPoint<ListViewItem>(this, new Point(5, 5));
					if (lvi != null)
					{
						int itemsPerRow = (int)(ActualWidth / lvi.ActualWidth);
						int row = i / itemsPerRow;
						sv.ScrollToVerticalOffset(row);
					}
				}
			}
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
					foreach (ListItem o in Items.SourceCollection)
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

			var al = System.Windows.Documents.AdornerLayer.GetAdornerLayer(this);
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
			ListColumn vdc = FindColumn((string)gvch.Content);
			if (vdc != null)
				vdc.Width = gvch.ActualWidth;
		}

		/// <summary>
		/// Invoked when a column is clicked
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Column_Click(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader column = sender as GridViewColumnHeader;
			ListSortDirection direction;
			ListColumn vdc = FindColumn((string)column.Content);
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
			ListColumn vdc = sender as ListColumn;

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
					GridViewColumn oldFirst = multiColumnGrid.Columns[oldIndex];
					GridViewColumn newFirst = multiColumnGrid.Columns[0];

					ListColumn oldVdc = FindColumn((string)((GridViewColumnHeader)oldFirst.Header).Content);
					ListColumn newVdc = FindColumn((string)((GridViewColumnHeader)newFirst.Header).Content);

					bool oldIsActive = IsClickSortable && ((GridViewColumnHeader)oldFirst.Header) == currentSortColumn;
					bool newIsActive = IsClickSortable && ((GridViewColumnHeader)newFirst.Header) == currentSortColumn;

					oldFirst.CellTemplate = CreateDetailsTemplate(oldVdc.Binding, oldVdc.Alignment, oldIsActive, false, oldVdc.Converter);
					newFirst.CellTemplate = CreateDetailsTemplate(newVdc.Binding, newVdc.Alignment, newIsActive, true, newVdc.Converter);
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
						ListColumn vdc = config.Columns[e.OldStartingIndex - oldAdjust];
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
			var sv = GetScrollViewer();
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

		/// <summary>
		/// Dispatches the PropertyChanged event.
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		private void DispatchPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
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

		/// <summary>
		/// Occurs when a property has changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Enums

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
	public delegate bool ViewDetailsSearchDelegate(ListItem item, string filterString);

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

	#endregion

	#region Adorners

	/// <summary>
	/// The graphical drag-n-drop target of ViewDetails
	/// </summary>
	public class ViewDetailsDropTarget : System.Windows.Documents.Adorner
	{
		#region Members

		private double position;
		private double length = double.NaN;
		private double offset = 0;
		private bool scrollbar = false;
		private Orientation orientation = Orientation.Horizontal;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the vertical position of the line.
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
		/// Gets or sets the length of the line.
		/// </summary>
		public double Length
		{
			get
			{
				return length;
			}
			set
			{
				length = value;
				this.InvalidateVisual();
			}
		}

		/// <summary>
		/// Gets or sets the offset before the start of the line.
		/// </summary>
		public double Offset
		{
			get
			{
				return offset;
			}
			set
			{
				offset = value;
				this.InvalidateVisual();
			}
		}

		/// <summary>
		/// Gets or sets whether the line should make space for a scroll bar.
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

		/// <summary>
		/// Gets or sets the orientation of the line.
		/// </summary>
		public Orientation Orientation
		{
			get
			{
				return orientation;
			}
			set
			{
				orientation = value;
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
			double l = length;
			bool isH = Orientation == Orientation.Horizontal;
			Size s = AdornedElement.RenderSize;
			double max = isH ? s.Width : s.Height;
			if (ScrollBar) max -= 18;
			if (double.IsNaN(l))
				l = max - 2;
			else
				l += offset;

			if (offset == 0) offset = 2;

			bool startSerif = offset > 0;
			bool endSerif = l < max;

			if (l > max) l = max;
			else if (l < 0) l = 0;
			if (offset > l) return;
			else if (offset < 0) offset = 0;

			PathFigure pfig = new PathFigure();

			pfig.StartPoint = new Point(offset, position - (startSerif?2:0));
			pfig.Segments.Add(new LineSegment(new Point(offset+2, position), true));
			pfig.Segments.Add(new LineSegment(new Point(l - 2, position), true));
			pfig.Segments.Add(new LineSegment(new Point(l, position - (endSerif?2:0)), true));
			pfig.Segments.Add(new LineSegment(new Point(l, position + (endSerif?3:1)), true));
			pfig.Segments.Add(new LineSegment(new Point(l - 2, position + 1), true));
			pfig.Segments.Add(new LineSegment(new Point(offset+2, position + 1), true));
			pfig.Segments.Add(new LineSegment(new Point(offset, position + (startSerif?3:1)), true));
			if (!isH)
			{
				pfig.StartPoint = new Point(pfig.StartPoint.Y, pfig.StartPoint.X);
				foreach (LineSegment ls in pfig.Segments)
					ls.Point = new Point(ls.Point.Y, ls.Point.X);
			}

			PathGeometry p = new PathGeometry();
			p.Figures.Add(pfig);

			drawingContext.DrawGeometry(Brushes.Black, new Pen(Brushes.Black, 1), p);
		}

		#endregion
	}

	/// <summary>
	/// The overlay displaying a progressbar and a text informing the
	/// user that a search is in progress.
	/// </summary>
	public class ViewDetailsSearchOverlay : System.Windows.Documents.Adorner
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
			text.VerticalAlignment = System.Windows.VerticalAlignment.Center;
			text.Margin = new Thickness(0, 0, 0, 5);

			progress.IsIndeterminate = true;
			progress.Width = 300;
			progress.Height = 10;
			progress.HorizontalAlignment = HorizontalAlignment.Center;
			progress.VerticalAlignment = System.Windows.VerticalAlignment.Center;

			innerPanel.Children.Add(text);
			innerPanel.Children.Add(progress);
			innerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
			innerPanel.VerticalAlignment = System.Windows.VerticalAlignment.Center;

			DockPanel.SetDock(innerPanel, Dock.Top);

			panel.Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
			panel.Children.Add(innerPanel);
			panel.HorizontalAlignment = HorizontalAlignment.Stretch;
			panel.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

			HorizontalAlignment = HorizontalAlignment.Stretch;
			VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

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

	#endregion

	#region Misc

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

		/// <summary>Tries to locate a given item within the visual tree, starting with the dependency object at a given position and then expanding outward.</summary>
		/// <typeparam name="T">The type of the element to be found on the visual tree of the element at the given location.</typeparam>
		/// <param name="iReference">The main element which is used to perform hit testing.</param>
		/// <param name="iPoint">The position to be evaluated on the origin.</param>
		/// <param name="searchArea">The number of pixels to search in each of the eight directions tl,t,tr,r,br,b,bl,l</param>
		public static T TryFindClosestFromPoint<T>(this UIElement iReference, Point iPoint, int searchArea) where T : DependencyObject
		{
			T lvi = TryFindFromPoint<T>(iReference, iPoint);

			// some view modes have spaces between items
			if (lvi == null)
			{
				// directions:
				// up, down, left, right,
				// up-left, up-right, down-left, down-right
				// 
				// true => move point p i steps in that direction
				bool[] dir = { false, false, false, false, false, false, false, false };
				for (int i = 0; lvi == null && i < searchArea; i++)
				{
					for (int j = 0; lvi == null && j < 8; j++)
					{
						dir[j] = true;
						Point p1 = new Point(iPoint.X, iPoint.Y);
						if (dir[0] || dir[4] || dir[5]) p1.Y -= i;
						if (dir[1] || dir[6] || dir[7]) p1.Y += i;
						if (dir[3] || dir[4] || dir[6]) p1.X -= i;
						if (dir[4] || dir[5] || dir[7]) p1.X += i;
						lvi = TryFindFromPoint<T>(iReference, p1);
						dir[j] = false;
					}
				}
			}

			return lvi;
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

	class HighlightConverter : IValueConverter
	{
		public object Convert(object value, Type type, object parameter, CultureInfo culture)
		{
			var textBlock = value as TextBlock;
			string str = textBlock.Tag as string;
			string query = parameter as string;
			int index = str.IndexOf(query);
			if (index >= 0)
			{
				string before = str.Substring(0, index);
				string after = str.Substring(index + query.Length);
				textBlock.Inlines.Clear();
				textBlock.Inlines.Add(new System.Windows.Documents.Run() { Text=before });
				textBlock.Inlines.Add(new System.Windows.Documents.Run() { Text = query, FontWeight = FontWeights.Bold });
				textBlock.Inlines.Add(new System.Windows.Documents.Run() { Text = after });
			}
			return "";
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
	/// Represents a converter for translating a group name.
	/// </summary>
	public class GroupNameConverter : IValueConverter
	{
		/// <summary>
		/// Translates a group name into the current language.
		/// </summary>
		/// <param name="value">The name of the group</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used, we use the culture of the main thread)</param>
		/// <returns>A localized version of the group name</returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || !(value is string)) return value;
			try
			{
				return (object)U.T((string)value);
			}
			catch
			{
				return value;
			}
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
	/// Represents a converter for adjusting an album art path string.
	/// </summary>
	public class AlbumArtConverter : IValueConverter
	{
		/// <summary>
		/// Tunes an album art string by correcting it if needed.
		/// </summary>
		/// <param name="value">The path to the image</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>A bitmap image created from the path given</returns>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || !(value is string)) return Utilities.DefaultAlbumArt;
			try
			{
				string uristring = value as string;
				if (uristring.Substring(uristring.Length - 4) == ".ico")
					return Utilities.GetIcoImage(uristring, 16, 16);
				return new BitmapImage(new Uri(uristring, UriKind.RelativeOrAbsolute));
			}
			catch
			{
				return Utilities.DefaultAlbumArt;
			}
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
			if (value == null) return "";
			string uristring = value as string;
			if (uristring.Length < 4 || uristring[uristring.Length - 3] != '.' || uristring.Substring(uristring.Length - 4) == ".ico")
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
