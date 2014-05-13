/**
 * Visualizer.xaml.cs
 * 
 * Contains the logic for the "Visualizer" screen.
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
using System.Windows.Threading;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Stoffi.Core;
using Stoffi.Core.Plugins;
using Stoffi.Core.Settings;
using SettingsManager = Stoffi.Core.Settings.Manager;
using PluginManager = Stoffi.Core.Plugins.Plugins;

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// Interaction logic for Visualizer.xaml
	/// </summary>
	public partial class Visualizer : DockPanel
	{
		#region Members
		private bool loaded = false;
		private GLControl canvas = new GLControl();
		#endregion

		#region Properties

		/// <summary>
		/// Gets the title of the visualizer or a string indicating
		/// that no visualizer is active.
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// Gets the description of the visualizer or a string indicating
		/// that no visualizer is active.
		/// </summary>
		public string Description { get; private set; }

		public bool VisualizerVisible
		{
			set
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					if (value)
					{
						NoVisualizerMessage.Visibility = Visibility.Collapsed;
						VisualizerHost.Visibility = Visibility.Visible;
						if (!Children.Contains(VisualizerHost))
							Children.Add(VisualizerHost);
					}
					else
					{
						NoVisualizerMessage.Visibility = Visibility.Visible;
						VisualizerHost.Visibility = Visibility.Collapsed;
						if (Children.Contains(VisualizerHost))
							Children.Remove(VisualizerHost);
					}
				}));
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of a visualizer control.
		/// </summary>
		public Visualizer()
		{
			canvas.Paint += new System.Windows.Forms.PaintEventHandler(Canvas_Paint);
			canvas.Load += new EventHandler(Canvas_Load);
			canvas.Resize += new EventHandler(Canvas_Resize);
			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);
			InitializeComponent();

			Title = U.T("NavigationVisualizerTitle");
			Description = U.T("NavigationVisualizerDescription");

			VisualizerVisible = false;

			VisualizerHost.Child = canvas;

			PluginManager.Refresh += new EventHandler<PluginEventArgs>(PluginManager_Refresh);
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Refreshes the name, description according to the current visualizer
		/// or hides the canvas if no visualizer is active.
		/// </summary>
		public void RefreshMeta()
		{
			Plugins.Visualizer plugin = null;
			if (SettingsManager.CurrentVisualizer != null &&
				SettingsManager.CurrentVisualizer != "" &&
				(plugin = PluginManager.Get(SettingsManager.CurrentVisualizer) as Plugins.Visualizer) != null)
			{
				VisualizerVisible = true;
				Title = plugin.T("Name");
				Description = plugin.T("Description");
			}
			else
			{
				VisualizerVisible = false;
				Title = U.T("NavigationVisualizerTitle");
				Description = U.T("NavigationVisualizerDescription");
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Sets the viewport of the canvas.
		/// </summary>
		private void SetupViewport()
		{
			int w = canvas.Width;
			int h = canvas.Height;
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Viewport(0, 0, w, h); // Use all of the glControl painting area
			GL.Ortho(0, 100, 0, 100, -1, 1); // Bottom-left corner pixel has coordinate (0, 0)
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when a property of the settings manager changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "CurrentVisualizer":
					RefreshMeta();
					break;
			}
		}

		/// <summary>
		/// Invoked when the canvas is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Canvas_Load(object sender, EventArgs e)
		{
			loaded = true;
			GL.ClearColor(System.Drawing.Color.Black);
			SetupViewport();
		}

		/// <summary>
		/// Invoked when the canvas is resized.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Canvas_Resize(object sender, EventArgs e)
		{
			SetupViewport();
		}

		/// <summary>
		/// Invoked when a plugin should be refreshed and repainted.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PluginManager_Refresh(object sender, PluginEventArgs e)
		{
			if (e.Plugin.Type == Plugins.PluginType.Visualizer)
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					canvas.Invalidate();
				}));
			}
		}

		/// <summary>
		/// Invoked when the canvas is being painted.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Canvas_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			if (!loaded)
				return;

			if (!canvas.Context.IsCurrent)
				canvas.MakeCurrent();

			var p = PluginManager.Get(SettingsManager.CurrentVisualizer);
			if (p == null)
				return;

			Plugins.Visualizer vis = p as Plugins.Visualizer;
			if (vis == null)
				return;

			vis.Refresh();
			canvas.SwapBuffers();
		}

		#endregion

		#endregion
	}
}
