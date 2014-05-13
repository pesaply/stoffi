/**
 * Fullscreen.xaml.cs
 * 
 * The "Fullscreen" screen.
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

using Stoffi.Player.GUI.Controls;
using Stoffi.Core;

namespace Stoffi.Player.GUI.Windows
{
	/// <summary>
	/// Interaction logic for Fullscreen.xaml
	/// </summary>
	public partial class Fullscreen : Window
	{
		#region Members

		private Timer mouseHider;
		private Video video = null;
		private FullscreenOverLay overlay;

		#endregion

		#region Properties

		/// <summary>
		/// Sets the visibility of the overlay controls.
		/// </summary>
		public Visibility ControlsVisibility
		{
			set
			{
				if (overlay != null)
					overlay.ControlsVisibility = value;
			}
		}

		/// <summary>
		/// Gets or sets the video control.
		/// </summary>
		public Video Video
		{
			get { return video; }
			set
			{
				if (value == null && video != null)
					this.Grid.Children.Remove(video);
				else
				{
					this.Grid.Children.Insert(0, value);
				}
				video = value;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a fullscreen window.
		/// </summary>
		public Fullscreen()
		{
			InitializeComponent();
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Updates the strings around the GUI, which are set programmatically, according to the current Language.
		/// </summary>
		public void RefreshStrings()
		{
			overlay.RefreshStrings();
		}

		/// <summary>
		/// Shows the mouse cursor.
		/// </summary>
		public void ShowCursor()
		{
			if (Cursor != Cursors.Arrow)
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					Cursor = Cursors.Arrow;
					overlay.ControlsVisibility = Visibility.Visible;
					Mouse.OverrideCursor = null;
					//System.Windows.Forms.Cursor.Show();
				}));
			}
		}

		/// <summary>
		/// Hides the mouse cursor.
		/// </summary>
		public void HideCursor()
		{
			if (overlay.PreventHiding)
			{
				ResetMouseHider();
			}
			else
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					Cursor = Cursors.None;
					overlay.ControlsVisibility = Visibility.Collapsed;
					Mouse.OverrideCursor = Cursors.None;
					//System.Windows.Forms.Cursor.Hide();
				}));
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Stops the timer for hiding the mouse cursor.
		/// </summary>
		private void StopMouseHider()
		{
			if (mouseHider != null)
				mouseHider.Dispose();
		}

		/// <summary>
		/// Resets the timer for hiding the mouse cursor.
		/// </summary>
		private void ResetMouseHider()
		{
			StopMouseHider();
			mouseHider = new Timer(PerformMouseHiding, null, 3000, Timeout.Infinite);
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user presses a key.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Close();
		}

		/// <summary>
		/// Invoked when the user double clicks.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			Close();
		}

		/// <summary>
		/// Invoked when the user moves the mouse.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_MouseMove(object sender, MouseEventArgs e)
		{
			ShowCursor();
			ResetMouseHider();
		}

		/// <summary>
		/// Invoked when the user is leaving the fullscreen mode.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			StopMouseHider();
			ShowCursor();
		}

		/// <summary>
		/// Invoked when the user enters fullscreen mode.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				ResetMouseHider();

				overlay = new FullscreenOverLay();
				overlay.Closing += new CancelEventHandler(Overlay_Closing);
				overlay.KeyDown += new KeyEventHandler(Window_KeyDown);
				overlay.MouseDoubleClick += new MouseButtonEventHandler(Window_MouseDoubleClick);
				overlay.MouseMove += new MouseEventHandler(Window_MouseMove);
				overlay.MouseLeftButtonUp += new MouseButtonEventHandler(Window_MouseLeftButtonUp);
				overlay.Exit.MouseLeftButtonDown += new MouseButtonEventHandler(Exit_MouseLeftButtonDown);
				overlay.Show();
				overlay.Owner = this;
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "Fullscreen", "Could not enter fullscreen: " + exc.Message);
				MessageBox.Show(exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Close();
			}
		}

		/// <summary>
		/// Invoked when the user clicks the fullscreen exit icon.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Exit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Close();
		}

		/// Invoked when the user leaves fullscreen mode.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Overlay_Closing(object sender, CancelEventArgs e)
		{
			Close();
		}

		/// <summary>
		/// Called by the mouse hider timer.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private void PerformMouseHiding(object state)
		{
			HideCursor();
		}

		#endregion

		private void Window_Activated(object sender, EventArgs e)
		{
			//Console.WriteLine("Activated");
			//if (overlay != null)
			//{
			//    overlay.Focus();
			//    overlay.Activate();
			//}
		}

		#endregion

		/// <summary>
		/// Invoked when the user double clicks.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
		}
	}
}
