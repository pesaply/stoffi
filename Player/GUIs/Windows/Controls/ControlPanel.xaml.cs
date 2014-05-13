/**
 * ControlPanel.xaml.cs
 * 
 * The "Control Panel" screen used to show all the preferences
 * of Stoffi.
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using System.Threading;
using System.Globalization;
using Tomers.WPF.Localization;

using Stoffi.Core;
using ServiceManager = Stoffi.Core.Services.Manager;

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// Interaction logic for ControlPanel.xaml
	/// </summary>
	public partial class ControlPanelView : UserControl
	{
		#region Members

		private Hashtable tabs = new Hashtable();
		private Hashtable tabLinks = new Hashtable();
		private bool initialized = false;

		#endregion

		#region Properties

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a control panel
		/// </summary>
		public ControlPanelView()
		{
			//U.L(LogLevel.Debug, "CONTROL PANEL", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "CONTROL PANEL", "Initialized");

			this.DataContext = this;

			tabs.Add(Tab.General, General);
			tabs.Add(Tab.Sources, Sources);
            tabs.Add(Tab.Services, Services);
			tabs.Add(Tab.Plugins, Plugins);
			tabs.Add(Tab.Shortcuts, Shortcuts);
			tabs.Add(Tab.About, About);
			tabLinks.Add(Tab.General, ControlPanelLink_General);
			tabLinks.Add(Tab.Sources, ControlPanelLink_Sources);
			tabLinks.Add(Tab.Services, ControlPanelLink_Services);
			tabLinks.Add(Tab.Plugins, ControlPanelLink_Plugins);
			tabLinks.Add(Tab.Shortcuts, ControlPanelLink_Shortcuts);
			tabLinks.Add(Tab.About, ControlPanelLink_About);

			//U.L(LogLevel.Debug, "CONTROL PANEL", "Created");
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Changes to a given tab.
		/// </summary>
		/// <param name="tab">The tab to switch to</param>
		public void SwitchTab(Tab tab)
		{
			foreach (DictionaryEntry c in tabs)
				((UIElement)c.Value).Visibility = ((Tab)c.Key) == tab ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
			foreach (DictionaryEntry c in tabLinks)
				((Button)c.Value).Style = ((Tab)c.Key) == tab ? (Style)FindResource("ControlPanelLinkActiveStyle") : (Style)FindResource("ControlPanelLinkStyle");
		}

		/// <summary>
		/// Updates the strings around the GUI, which are set programmatically, according to the current Language.
		/// </summary>
		public void RefreshStrings()
		{
			Shortcuts.RefreshStrings();
			Sources.RefreshStrings();
			Plugins.RefreshStrings();
			Services.RefreshStrings();
			About.RefreshStrings();
		}

		#endregion

		#region Private

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user changes size of the control panel
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ControlPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//ScrollBarVisibility vis = e.NewSize.Width < 600 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
			//ControlPanelAbout.HorizontalScrollBarVisibility = vis;
			//ControlPanelGeneral.HorizontalScrollBarVisibility = vis;
			//ControlPanelShortcuts.HorizontalScrollBarVisibility = vis;
			//ControlPanelSources.HorizontalScrollBarVisibility = vis;
		}

		/// <summary>
		/// Invoked when the control panel is loaded
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ControlPanel_Loaded(object sender, RoutedEventArgs e)
		{
			if (!initialized)
			{
				if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
				{
					ControlPanelMain.Background = SystemColors.WindowBrush;
					ControlPanelLeft.Background = SystemColors.WindowBrush;
				}

				initialized = true;
			}
		}

		/// <summary>
		/// Invoked when the user clicks on "Back to music"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Back_Click(object sender, RoutedEventArgs e)
		{
			OnBackClick(e);
		}

		/// <summary>
		/// Invoked when the user clicks on General
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void General_Click(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.General);
		}

		/// <summary>
		/// Invoked when the user clicks on Sources
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Sources_Click(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.Sources);
		}

		/// <summary>
		/// Invoked when the user clicks on Services
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Services_Click(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.Services);
		}

        /// <summary>
        /// Invoked when the user clicks on Plugins
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data</param>
        private void Plugins_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab(Tab.Plugins);
        }

		/// <summary>
		/// Invoked when the user clicks on Shortcuts
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Shortcuts_Click(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.Shortcuts);
		}

		/// <summary>
		/// Invoked when the user clicks on About
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void About_Click(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.About);
		}

		/// <summary>
		/// Invoked when the user clicks on Website
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Website_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.stoffiplayer.com/?ref=stoffi");
		}

		/// <summary>
		/// Invoked when the user clicks on Blog
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Blog_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://blog.stoffiplayer.com/?ref=stoffi");
		}

		/// <summary>
		/// Invoked when the user clicks on Project
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Project_Click(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://dev.stoffiplayer.com/?ref=stoffi");
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the BackClick event.
		/// </summary>
		/// <param name="e">The event data</param>
		private void OnBackClick(RoutedEventArgs e)
		{
			if (BackClick != null)
				BackClick(this, e);
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks Back.
		/// </summary>
		public event RoutedEventHandler BackClick;

		#endregion

		#region Enum

		/// <summary>
		/// The tabs of the control panel
		/// </summary>
		public enum Tab
		{
			/// <summary>
			/// General settings
			/// </summary>
			General,

			/// <summary>
			/// Music sources
			/// </summary>
			Sources,

			/// <summary>
			/// The Stoffi services
			/// </summary>
            Services,

            /// <summary>
            /// Plugins
            /// </summary>
            Plugins,

			/// <summary>
			/// Keyboard shortcuts
			/// </summary>
			Shortcuts,

			/// <summary>
			/// About Stoffi
			/// </summary>
			About
		}

		#endregion
	}
}
