/***
 * ListColumn.cs
 * 
 * Describes a column of a columned list displaying content.
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
	/// Represents a column of a details list
	/// </summary>
	public class ListColumn : PropertyChangedBase
	{
		#region Members

		private string name;
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
		public string Name
		{
			get { return name; }
			set { SetProp<string> (ref name, value, "Name"); }
		}

		/// <summary>
		/// Gets or sets the displayed text
		/// </summary>
		public string Text
		{
			get { return text; }
			set { SetProp <string>(ref text, value, "Text"); }
		}

		/// <summary>
		/// Gets or sets the value to bind to
		/// </summary>
		public string Binding
		{
			get { return binding; }
			set { SetProp <string>(ref binding, value, "Binding"); }
		}

		/// <summary>
		/// Gets or sets the converter that should be used to present the value of the binding.
		/// </summary>
		public string Converter
		{
			get { return converter; }
			set { SetProp <string>(ref converter, value, "Converter"); }
		}

		/// <summary>
		/// Gets or sets the value to sort on
		/// </summary>
		public string SortField
		{
			get { return sortField; }
			set { SetProp <string>(ref sortField, value, "SortField"); }
		}

		/// <summary>
		/// Gets or sets whether the column is always visible
		/// </summary>
		public bool IsAlwaysVisible
		{
			get { return isAlwaysVisible; }
			set { SetProp <bool>(ref isAlwaysVisible, value, "IsAlwaysVisible"); }
		}

		/// <summary>
		/// Gets or sets whether the column is sortable
		/// </summary>
		public bool IsSortable
		{
			get { return isSortable; }
			set { SetProp <bool>(ref isSortable, value, "IsSortable"); }
		}

		/// <summary>
		/// Gets or sets the width of the column
		/// </summary>
		public double Width
		{
			get { return width; }
			set { SetProp <double>(ref width, value, "Width"); }
		}

		/// <summary>
		/// Gets or sets whether the column is visible (only effective if IsAlwaysVisible is false)
		/// </summary>
		public bool IsVisible
		{
			get { return isVisible; }
			set { SetProp <bool>(ref isVisible, value, "IsVisible"); }
		}

		/// <summary>
		/// Gets or sets the text alignment of the displayed text
		/// </summary>
		public Alignment Alignment
		{
			get { return alignment; }
			set { SetProp <Alignment>(ref alignment, value, "Alignment"); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Create a column.
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The displayed text</param>
		/// <param name="width">The width</param>
		/// <param name="converter">The converter used to convert the value of the binding</param>
		/// <param name="isVisible">Whether the column is visible</param>
		/// <param name="alignment">The alignment of the text</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <returns>The newly created column</returns>
		public static ListColumn Create(string name, string text, int width, string converter,
			Alignment alignment = Alignment.Left,
			bool isVisible = true,
			bool isAlwaysVisible = false,
			bool isSortable = true)
		{
			return Create(name, text, name, name, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Create a column.
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The displayed text</param>
		/// <param name="width">The width</param>
		/// <param name="isVisible">Whether the column is visible</param>
		/// <param name="alignment">The alignment of the text</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <param name="converter">The converter used to convert the value of the binding</param>
		/// <returns>The newly created column</returns>
		public static ListColumn Create(string name, string text, int width,
			Alignment alignment = Alignment.Left,
			bool isVisible = true,
			bool isAlwaysVisible = false,
			bool isSortable = true,
			string converter = null)
		{
			return Create(name, text, name, name, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Create a column.
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The displayed text</param>
		/// <param name="binding">The value to bind to</param>
		/// <param name="width">The width</param>
		/// <param name="isVisible">Whether the column is visible</param>
		/// <param name="alignment">The alignment of the text</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <param name="converter">The converter used to convert the value of the binding</param>
		/// <returns>The newly created column</returns>
		public static ListColumn Create(string name, string text, string binding, int width,
			Alignment alignment = Alignment.Left,
			bool isVisible = true,
			bool isAlwaysVisible = false,
			bool isSortable = true,
			string converter = null)
		{
			return Create(name, text, binding, binding, width, alignment, isVisible, isAlwaysVisible, isSortable, converter);
		}

		/// <summary>
		/// Create a column.
		/// </summary>
		/// <param name="name">The name of the column</param>
		/// <param name="text">The displayed text</param>
		/// <param name="binding">The value to bind to</param>
		/// <param name="sortField">The column to sort on</param>
		/// <param name="width">The width</param>
		/// <param name="isVisible">Whether the column is visible</param>
		/// <param name="alignment">The alignment of the text</param>
		/// <param name="isAlwaysVisible">Whether the column is always visible</param>
		/// <param name="isSortable">Whether the column is sortable</param>
		/// <param name="converter">The converter used to convert the value of the binding</param>
		/// <returns>The newly created column</returns>
		public static ListColumn Create(string name, string text, string binding, string sortField, int width,
			Alignment alignment = Alignment.Left,
			bool isVisible = true,
			bool isAlwaysVisible = false,
			bool isSortable = true,
			string converter = null)
		{
			ListColumn column = new ListColumn();
			column.Name = name;
			column.Text = text;
			column.Binding = binding;
			column.Width = width;
			column.Alignment = alignment;
			column.IsAlwaysVisible = isAlwaysVisible;
			column.IsSortable = isSortable;
			column.IsVisible = isVisible;
			column.SortField = sortField;
			column.Converter = converter;
			return column;
		}

		#endregion
	}
}

