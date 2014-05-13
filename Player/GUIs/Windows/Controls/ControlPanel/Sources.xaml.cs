/**
 * Sources.xaml.cs
 * 
 * The "Sources" screen inside the "Control Panel".
 * It shows a list of the sources where Stoffi looks for music.
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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Shapes;

using Stoffi.Core;
using Stoffi.Core.Settings;
using Stoffi.Core.Sources;
using SettingsManager = Stoffi.Core.Settings.Manager;

namespace Stoffi.Player.GUI.Controls.ControlPanel
{
	/// <summary>
	/// Interaction logic for Sources.xaml
	/// </summary>
	public partial class Sources : DockPanel
	{
		#region Members
		private MenuItem menuToggle;
		private MenuItem menuRemove;
		private ContextMenu sourceMenu;
		#endregion

		#region Properties
		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the Control Panel screen Sources.
		/// </summary>
		public Sources()
		{
			InitializeComponent();

			menuRemove = new MenuItem();
			menuRemove.Header = U.T("MenuRemove");
			menuRemove.Click += new RoutedEventHandler(Remove_Click);

			menuToggle = new MenuItem();
			menuToggle.Header = U.T("MenuIgnore");
			menuToggle.Click += new RoutedEventHandler(Toggle_Click);

			sourceMenu = new ContextMenu();
			sourceMenu.Items.Add(menuToggle);
			sourceMenu.Items.Add(menuRemove);

			List.Config = SettingsManager.SourceListConfig;
			List.ContextMenu = sourceMenu;
			List.ItemsSource = SettingsManager.ScanSources;
			SettingsManager.ScanSources.CollectionChanged += List.ItemsSource_CollectionChanged;
			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Refreshes the strings according to current Culture language.
		/// </summary>
		public void RefreshStrings()
		{
			menuRemove.Header = U.T("MenuRemove");
			if (List != null)
				List.RefreshView();
		}

		#endregion

		#region Private
		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the control is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Sources_Loaded(object sender, RoutedEventArgs e)
		{
			List.Config = SettingsManager.SourceListConfig;
			List.ItemsSource = SettingsManager.ScanSources;
			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
				Title.Style = (Style)FindResource("ClassicControlPanelTitleStyle");
		}

		/// <summary>
		/// Invoked when the user adds a folder
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void AddFolder_Click(object sender, RoutedEventArgs e)
		{
			DispatchAddFolderClick(e);
		}

		/// <summary>
		/// Invoked when the user adds a file
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void AddFile_Click(object sender, RoutedEventArgs e)
		{
			DispatchAddFileClick(e);
		}

		/// <summary>
		/// Invoked when the user ignores a folder
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void IgnoreFolder_Click(object sender, RoutedEventArgs e)
		{
			DispatchIgnoreFolderClick(e);
		}

		/// <summary>
		/// Invoked when the user ignores a file
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void IgnoreFile_Click(object sender, RoutedEventArgs e)
		{
			DispatchIgnoreFileClick(e);
		}

		/// <summary>
		/// Invoked when the user right-click on a source and clicks on Remove
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Remove_Click(object sender, RoutedEventArgs e)
		{
			int index = List.SelectedIndex;
			ThreadStart RemoveThread = delegate()
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
				{
					int keep = 0;
					while (List.SelectedItems.Count > keep)
					{
						var source = List.SelectedItems[keep] as Location;
						if (source.Automatic)
						{
							Files.ToggleSource(source);
							keep++;
						}
						else
							Files.RemoveSource(source);
					}

					if (index >= List.Items.Count)
						index = List.Items.Count - 1;

					if (keep == 0)
						List.SelectedIndex = index;
				}));
			};
			Thread rb_thread = new Thread(RemoveThread);
			rb_thread.Name = "Remove Sources";
			rb_thread.Priority = ThreadPriority.BelowNormal;
			rb_thread.Start();
		}

		/// <summary>
		/// Invoked when the user right-click on a source and clicks on Ignore/Include
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Toggle_Click(object sender, RoutedEventArgs e)
		{
			ThreadStart ToggleThread = delegate()
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
				{
					foreach (Location source in List.SelectedItems)
						Files.ToggleSource(source);
				}));
			};
			Thread tog_thread = new Thread(ToggleThread);
			tog_thread.Name = "Toggle Sources";
			tog_thread.Priority = ThreadPriority.BelowNormal;
			tog_thread.Start();
		}

		/// <summary>
		/// Invoked when the context menu of the source list is opening
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void List_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			bool hasOnlyAutomatic = true;
			bool hasIgnored = false;

			foreach (Location s in List.SelectedItems)
			{
				if (!s.Automatic)
					hasOnlyAutomatic = false;
				if (s.Ignore)
					hasIgnored = true;
			}

			menuRemove.Visibility = (hasOnlyAutomatic ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible);
			menuToggle.Header = (hasIgnored ? U.T("MenuInclude") : U.T("MenuIgnore"));
		}

		/// <summary>
		/// Invoked when the user presses a key
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void List_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				List<Source> sources = new List<Source>();
				foreach (Source s in ((ListView)sender).SelectedItems)
					sources.Add(s);
				Remove_Click(null, null);
			}
		}

		/// <summary>
		/// Invoked when a property of the settings manager changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate()
			{
				switch (e.PropertyName)
				{
					case "SourceListConfig":
						List.Config = SettingsManager.SourceListConfig;
						break;
				}
			}));
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the AddFileClick event.
		/// </summary>
		/// <param name="e">The event data</param>
		private void DispatchAddFileClick(RoutedEventArgs e)
		{
			if (AddFileClick != null)
				AddFileClick(this, e);
		}

		/// <summary>
		/// Dispatches the AddFolderClick event.
		/// </summary>
		/// <param name="e">The event data</param>
		private void DispatchAddFolderClick(RoutedEventArgs e)
		{
			if (AddFolderClick != null)
				AddFolderClick(this, e);
		}

		/// <summary>
		/// Dispatches the IgnoreFileClick event.
		/// </summary>
		/// <param name="e">The event data</param>
		private void DispatchIgnoreFileClick(RoutedEventArgs e)
		{
			if (IgnoreFileClick != null)
				IgnoreFileClick(this, e);
		}

		/// <summary>
		/// Dispatches the IgnoreFolderClick event.
		/// </summary>
		/// <param name="e">The event data</param>
		private void DispatchIgnoreFolderClick(RoutedEventArgs e)
		{
			if (IgnoreFolderClick != null)
				IgnoreFolderClick(this, e);
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the user clicks "Add file".
		/// </summary>
		public event RoutedEventHandler AddFileClick;

		/// <summary>
		/// Occurs when the user clicks "Add folder".
		/// </summary>
		public event RoutedEventHandler AddFolderClick;

		/// <summary>
		/// Occurs when the user clicks "Ignore file".
		/// </summary>
		public event RoutedEventHandler IgnoreFileClick;

		/// <summary>
		/// Occurs when the user clicks "Ignore folder".
		/// </summary>
		public event RoutedEventHandler IgnoreFolderClick;

		#endregion
	}
}
