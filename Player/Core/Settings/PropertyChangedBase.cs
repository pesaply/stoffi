/***
 * PropertyChangedBase.cs
 * 
 * Describes an item which has properties whose changes are announced using the
 * PropertyChanged event.
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
using System.Collections.Generic;
using System.ComponentModel;

namespace Stoffi.Core.Settings
{
	/// <summary>
	/// Base class for classes which sends out PropertyChanged events.
	/// </summary>
	public abstract class PropertyChangedBase : INotifyPropertyChanged
	{

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		protected void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>
		/// Set the value of a property's underlying variable.
		/// </summary>
		/// <param name="variable">Variable.</param>
		/// <param name="value">Value.</param>
		/// <param name="name">Name of the property.</param>
		/// <returns>true if the value was changed, otherwise false.</returns>
		protected bool SetProp<T> (ref T variable, T value, string name)
		{
			if (!EqualityComparer<T>.Default.Equals (variable, value)) {
				variable = value;
				OnPropertyChanged (name);
				return true;
			}
			return false;
		}
	}
}

