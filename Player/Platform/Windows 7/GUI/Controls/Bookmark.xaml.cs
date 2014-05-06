/**
 * Bookmark.xaml.cs
 * 
 * A bookmark shown over the track timeline.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for Bookmark.xaml
	/// </summary>
	public partial class Bookmark : UserControl
	{
		#region Members

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public String Text { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public Brush Color
		{
			get { return Box.Fill; }
			set { Box.Fill = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public double Position { get; set; }
		#endregion

		#region Constructor

		/// <summary>
		/// Creates a bookmark
		/// </summary>
		public Bookmark()
		{
			U.L(LogLevel.Debug, "BOOKMARK", "Initialize");
			InitializeComponent();
			U.L(LogLevel.Debug, "BOOKMARK", "Initialized");
		}

		#endregion

		#region Methods

		#region Event handlers

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Remove_Click(object sender, RoutedEventArgs e)
		{
			DispatchRemoveClicked();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Bookmark_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DispatchClicked(this.Position);
		}

		#endregion

		#region Dispatchers
		
		public void DispatchRemoveClicked()
		{
			if (RemoveClicked != null)
			{
				RemoveClicked(this, new EventArgs());
			}
		}

		public void DispatchClicked(double pos)
		{
			if (Clicked != null)
			{
				Clicked(this, new BookmarkEventArgs(pos));
			}
		}

		#endregion

		#endregion

		#region Events

		public event EventHandler RemoveClicked;
		public event BookmarkEventHandler Clicked;

		#endregion
	}

	/// <summary>
	/// Holds data for a bookmark event
	/// </summary>
	public class BookmarkEventArgs : EventArgs
	{
		#region Properties
		public double Position;
		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pos"></param>
		public BookmarkEventArgs(double pos)
		{
			Position = pos;
		}

		#endregion
	}

	/// <summary>
	/// Describes the method called when en event occurs for a bookmark
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void BookmarkEventHandler(object sender, BookmarkEventArgs e);
}
