/***
 * KeyboardShortcut.cs
 * 
 * Describes a keyboard shortcut for an action.
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
	/// Describes a keyboard shortcut.
	/// </summary>
	public class KeyboardShortcut : PropertyChangedBase
	{
		#region Members

		private string name;
		private string category;
		private string keys;
		private bool isGlobal = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the name of the shortcut.
		/// </summary>
		public string Name
		{
			get { return name; }
			set { SetProp<string> (ref name, value, "Name"); }
		}

		/// <summary>
		/// Gets or sets the category of the shortcut.
		/// </summary>
		public string Category
		{
			get { return category; }
			set { SetProp<string> (ref category, value, "Category"); }
		}

		/// <summary>
		/// Get or sets the keys that will trigger the shortcut.
		/// </summary>
		public string Keys
		{
			get { return keys; }
			set { SetProp<string> (ref keys, value, "Keys"); }
		}

		/// <summary>
		/// Gets or sets whether the shortcut should be accessible
		/// when the application doesn't have focus.
		/// </summary>
		public bool IsGlobal
		{
			get { return isGlobal; }
			set { SetProp<bool> (ref isGlobal, value, "IsGlobal"); }
		}

		#endregion
	}
}

