/***
 * ListItem.cs
 * 
 * Describes an item inside a list (columned, icon grid, etc).
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

namespace Stoffi.Core.Settings
{
	/// <summary>
	/// Describes the data source of an item inside the enhanced list view
	/// </summary>
	public class ListItem : PropertyChangedBase
	{
		#region Members

		protected int number;
		protected bool isActive;
		protected string icon;
		protected string image;
		protected bool strike;
		protected bool disabled = false;
		protected bool isVisible = false;
		protected string group;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the index number of the item
		/// </summary>
		public int Number
		{
			get { return number; }
			set { SetProp<int> (ref number, value, "Number"); }
		}

		/// <summary>
		/// Gets or sets whether the item is marked as active or not
		/// </summary>
		public bool IsActive
		{
			get { return isActive; }
			set { SetProp<bool> (ref isActive, value, "IsActive"); }
		}

		/// <summary>
		/// Gets or sets the icon of the item
		/// </summary>
		public string Icon
		{
			get { return icon; }
			set { SetProp<string> (ref icon, value, "Icon"); }
		}

		/// <summary>
		/// Gets or sets the image of the item
		/// </summary>
		public string Image
		{
			get { return image; }
			set { SetProp<string> (ref image, value, "Image"); }
		}

		/// <summary>
		/// Gets or sets whether the items should feature a strikethrough
		/// </summary>
		public bool Strike
		{
			get { return strike; }
			set { SetProp<bool> (ref strike, value, "Strike"); }
		}

		/// <summary>
		/// Gets or sets whether the items should be viewed as disabled (for example grayed out)
		/// </summary>
		public bool Disabled
		{
			get { return disabled; }
			set { SetProp<bool> (ref disabled, value, "Disabled"); }
		}

		/// <summary>
		/// Gets or sets whether or not the item is visible
		/// and should be rendered.
		/// </summary>
		/// <remarks>
		/// Used to implement virtualization for controls that
		/// don't support it (like the WrapPanel).
		/// </remarks>
		public bool IsVisible
		{
			get { return isVisible; }
			set { SetProp<bool> (ref isVisible, value, "IsVisible"); }
		}

		/// <summary>
		/// Gets or sets the group of the item.
		/// </summary>
		/// <remarks>
		/// This is used when grouping is enabled.
		/// </remarks>
		public string Group
		{
			get { return group; }
			set { SetProp<string> (ref group, value, "Group"); }
		}

		#endregion
	}
}

