/**
 * Navigation.cs
 * 
 * Represents an item in the navigation tree.
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
using System.IO;
using MonoMac.AppKit;
using MonoMac.Foundation;

using Stoffi.Core.Playlists;
using PlaylistManager = Stoffi.Core.Playlists.Manager;

namespace Stoffi.GUI.Models
{
	/// <summary>
	/// Represents an item in the navigation tree.
	/// </summary>
	public class Navigation : NSObject
	{
		#region Members

		private string name;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get { return name; }
			set {
				bool changed = name != value;
				var oldName = name;
				name = value;
				if (EditOnSelect && changed) {
					OnCreateNew (this);
					name = oldName;
				}
				else if (Editable && changed) {
					PlaylistManager.Rename (oldName, name);
				}
			}
		}

		/// <summary>
		/// Gets or sets the unique identitifer for the navigation.
		/// </summary>
		/// <value>The identifier.</value>
		public string ID { get; set; }

		/// <summary>
		/// Gets or sets the icon.
		/// </summary>
		/// <value>The icon.</value>
		public string Icon { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is header.
		/// </summary>
		/// <value><c>true</c> if this instance is header; otherwise, <c>false</c>.</value>
		public bool IsHeader { get; set; }

		/// <summary>
		/// Gets or sets whether the item can be edited by the user.
		/// </summary>
		/// <value><c>true</c> if this instance is editable; otherwise, <c>false</c>.</value>
		public bool Editable { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a special item which turns editable when selected.
		/// </summary>
		/// <value><c>true</c> if this instance is a "Create new" item; otherwise, <c>false</c>.</value>
		public bool EditOnSelect { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a playlist.
		/// </summary>
		/// <value><c>true</c> if this instance is playlist; otherwise, <c>false</c>.</value>
		public bool IsPlaylist { get; set; }

		/// <summary>
		/// Gets or sets the children.
		/// </summary>
		/// <value>The children.</value>
		public List<Navigation> Children { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.OSX.Models.Navigation"/> class as a data item.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="icon">Icon.</param>
		/// <param name="id">Identifier.</param>
		/// <param name="editable">Whether or not the item can be edited by the user.</param>
		/// <param name="editOnSelect">Whether or not this item turns editable when selected.</param>
		public Navigation(string name, string icon, string id, bool editable, bool editOnSelect) : this(name, icon, id, editable)
		{
			EditOnSelect = editOnSelect;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.OSX.Models.Navigation"/> class as a data item.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="icon">Icon.</param>
		/// <param name="editable">Whether or not the item can be edited by the user.</param>
		/// <param name="editOnSelect">Whether or not this item turns editable when selected.</param>
		public Navigation(string name, string icon, bool editable, bool editOnSelect) : this(name, icon, editable)
		{
			EditOnSelect = editOnSelect;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.OSX.Models.Navigation"/> class as a data item.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="icon">Icon.</param>
		/// <param name="id">Identifier.</param>
		/// <param name="editable">Whether or not the item can be edited by the user.</param>
		public Navigation(string name, string icon, string id, bool editable = false) : this(name, icon, editable)
		{
			ID = id;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.OSX.Models.Navigation"/> class as a data item.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="icon">Icon.</param>
		/// <param name="editable">Whether or not the item can be edited by the user.</param>
		public Navigation(string name, string icon, bool editable = false) : this(name)
		{
			Editable = editable;
			Icon = icon;
			IsHeader = false;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.OSX.Models.Navigation"/> class as a header.
		/// </summary>
		/// <param name="name">Name.</param>
		public Navigation(string name)
		{
			this.name = name;
			ID = name;
			IsHeader = true;
			Children = new List<Navigation> ();
			EditOnSelect = false;
			Editable = false;
			IsPlaylist = false;
		}

		#endregion

		#region Methods

		#region Dispatchers

		private void OnCreateNew(object sender)
		{
			if (CreateNew != null)
				CreateNew (sender, new EventArgs ());
		}

		#endregion

		#endregion

		#region Events

		public event EventHandler CreateNew;

		#endregion
	}

	/// <summary>
	/// Navigation delegate.
	/// </summary>
	public class NavigationDelegate : NSOutlineViewDelegate
	{
		#region Members
		
		private NavigationItemDelegate itemDelegate = new NavigationItemDelegate();
		private NSMenu playlistMenu = null;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.GUI.Models.NavigationDelegate"/> class.
		/// </summary>
		public NavigationDelegate(NSMenu playlistMenu) : base()
		{
			itemDelegate.Edited += OnItemEdited;
			this.playlistMenu = playlistMenu;
		}

		#endregion
		
		#region Methods
		
		#region Overrides

		/// <summary>
		/// Determines whether this instance is group item the specified outlineView item.
		/// </summary>
		/// <returns><c>true</c> if this instance is group item the specified outlineView item; otherwise, <c>false</c>.</returns>
		/// <param name="outlineView">Outline view.</param>
		/// <param name="item">Item.</param>
		public override bool IsGroupItem(NSOutlineView outlineView, NSObject item)
		{
			return item != null && (item as Navigation).IsHeader;
		}

		/// <summary>
		/// Gets the height of the row.
		/// </summary>
		/// <returns>The row height.</returns>
		/// <param name="outlineView">Outline view.</param>
		/// <param name="item">Item.</param>
		public override float GetRowHeight(NSOutlineView outlineView, NSObject item)
		{
			if (((Navigation)item).IsHeader) {
				return 23f;
			}
			return 20f;
		}

		/// <summary>
		/// Gets the view.
		/// </summary>
		/// <returns>The view.</returns>
		/// <param name="outlineView">Outline view.</param>
		/// <param name="tableColumn">Table column.</param>
		/// <param name="item">Item.</param>
		public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			var navigation = item as Navigation;
			NSView view;
			if (IsGroupItem (outlineView, item)) {
				view = outlineView.MakeView ("HeaderCell", this);
				((NSTextField)view.Subviews [0]).StringValue = navigation.Name.ToUpper();
			} else {
				view = outlineView.MakeView ("DataCell", this);
				var tf = (NSTextField)view.Subviews [1];
				((NSImageView)view.Subviews [0]).Image = NSImage.ImageNamed (navigation.Icon);
				tf.StringValue = navigation.Name;
				tf.Identifier = navigation.Name;
				tf.Editable = navigation.Editable || navigation.EditOnSelect;
				tf.Delegate = itemDelegate;
				if (navigation.IsPlaylist && playlistMenu != null)
					view.Menu = playlistMenu;
			}

			return view;
		}

		/// <summary>
		/// Determines if this instance should be selected.
		/// </summary>
		/// <returns><c>true</c>, if item should be selected, <c>false</c> otherwise.</returns>
		/// <param name="outlineView">Outline view.</param>
		/// <param name="item">Item.</param>
		public override bool ShouldSelectItem (NSOutlineView outlineView, NSObject item)
		{
			return !((Navigation)item).IsHeader;
		}

		#endregion
		
		#region Dispatchers

		/// <summary>
		/// Raises the item edited event.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void OnItemEdited(object sender, RenamedEventArgs e)
		{
			if (ItemEdited != null)
				ItemEdited (sender, e);
		}

		#endregion

		#endregion
		
		#region Events

		/// <summary>
		/// Occurs when an item was edited.
		/// </summary>
		public event RenamedEventHandler ItemEdited;

		#endregion
	}

	/// <summary>
	/// Represents the data source of an item in the OutlineView that is the navigation tree.
	/// </summary>
	public class NavigationDataSource : NSOutlineViewDataSource
	{
		#region Properties

		/// <summary>
		/// Gets or sets the top level navigations.
		/// </summary>
		/// <value>The navigations.</value>
		public List<Navigation> Navigations { get; set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Models.NavigationDataSource"/> class.
		/// </summary>
		public NavigationDataSource ()
		{
			Navigations = new List<Navigation> ();
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Gets the row of a playlist at a given index (in the playlist list).
		/// </summary>
		/// <returns>The row of playlist.</returns>
		/// <param name="index">Index.</param>
		public int GetRowOfPlaylist(int index)
		{
			var row = 0;
			var i = 0;
			foreach (var item in Navigations) {
				if (item.IsPlaylist && item.EditOnSelect)
					return row;
				if (item.IsPlaylist) {
					if (i == index)
						break;
					i++;
				}
				row++;
			}
			return row;
		}

		/// <summary>
		/// Translate between a playlist and a navigation item.
		/// </summary>
		/// <returns>The navigation item.</returns>
		/// <param name="playlist">Playlist.</param>
		public Navigation GetPlaylistItem(Playlist playlist)
		{
			return GetPlaylistItem (playlist.Name);
		}

		/// <summary>
		/// Translate between a playlist and a navigation item.
		/// </summary>
		/// <returns>The navigation item.</returns>
		/// <param name="name">Playlist name.</param>
		public Navigation GetPlaylistItem(string name)
		{
			foreach (var item in Navigations)
				if (item.IsPlaylist && item.Name == name)
						return item;
			return null;
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Gets the children count.
		/// </summary>
		/// <returns>The children count.</returns>
		/// <param name="outlineView">Outline view.</param>
		/// <param name="item">Item.</param>
		public override int GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			if (item != null)
				return (item as Navigation).Children.Count;
			return Navigations.Count;
		}

		/// <summary>
		/// Gets the child.
		/// </summary>
		/// <returns>The child.</returns>
		/// <param name="outlineView">Outline view.</param>
		/// <param name="childIndex">Child index.</param>
		/// <param name="item">Item.</param>
		public override NSObject GetChild (NSOutlineView outlineView, int childIndex, NSObject item)
		{
			if (item == null)
				return Navigations [childIndex];
			return (NSObject)(item as Navigation).Children [childIndex];
		}

		/// <summary>
		/// Checks if an item can be expanded.
		/// </summary>
		/// <returns><c>true</c>, if item was expandable, <c>false</c> otherwise.</returns>
		/// <param name="outlineView">Outline view.</param>
		/// <param name="item">Item.</param>
		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			var n = item as Navigation;
			return n != null && n.Children.Count > 0;
		}

		#endregion

		#endregion
	}

	/// <summary>
	/// Delegate of the text fields of an item in the navigation tree.
	/// </summary>
	public class NavigationItemDelegate : NSTextFieldDelegate
	{
		#region Members
		private string valueBeforeEdit = null;
		#endregion

		#region Methods

		#region Overrides

		/// <summary>
		/// Invoked when editing begins.
		/// </summary>
		/// <param name="notification">Notification.</param>
		public override void EditingBegan (NSNotification notification)
		{
			var textField = notification.Object as NSTextField;
			if (textField == null)
				return;

			valueBeforeEdit = textField.StringValue;
		}

		/// <summary>
		/// Invoked when editing ends.
		/// </summary>
		/// <param name="notification">Notification.</param>
		public override void EditingEnded (NSNotification notification)
		{
			var textField = notification.Object as NSTextField;
			if (textField == null || valueBeforeEdit == null)
				return;

			var newValue = textField.StringValue;
			var oldValue = valueBeforeEdit;
			valueBeforeEdit = null;
			OnEdited (textField, oldValue, newValue);
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Raises the edited event.
		/// </summary>
		/// <param name="field">Field.</param>
		/// <param name="oldValue">Old value.</param>
		/// <param name="newValue">New value.</param>
		private void OnEdited(NSTextField field, string oldValue, string newValue)
		{
			if (Edited != null)
				Edited(field as object, new RenamedEventArgs(WatcherChangeTypes.Renamed, "", newValue, oldValue));
		}

		#endregion

		#endregion
		
		#region Events
		
		/// <summary>
		/// Occurs when edited.
		/// </summary>
		public event RenamedEventHandler Edited;

		#endregion

	}

	[Register("NavigationTree")]
	public class NavigationTree : NSOutlineView
	{
		public NSMenu PlaylistMenu { get; set; }

		public NavigationTree(NSCoder coder) : base(coder) { }
		public NavigationTree(NSObjectFlag t) : base(t) { }
		public NavigationTree(IntPtr handle) : base(handle) { }
		public NavigationTree() : base() { }

		public override NSMenu MenuForEvent (NSEvent theEvent)
		{
			var ds = DataSource as NavigationDataSource;
			var point = ConvertPointFromView (theEvent.LocationInWindow, null);
			var row = GetRow (point);
			if (row < 0 || row >= ds.Navigations.Count)
				return null;

			var item = ds.Navigations [row];
			if (item.IsPlaylist && PlaylistMenu != null) {
				SelectRow (row, false);
				return PlaylistMenu;
			}

			return null;
		}
	}
}

