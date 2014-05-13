/**
 * About.xaml.cs
 * 
 * The "About" screen inside the "Control Panel".
 * It shows information regarding Stoffi.
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

using Stoffi.Core;

using SettingsManager = Stoffi.Core.Settings.Manager;
using UpgradeManager = Stoffi.Core.Upgrade.Manager;

namespace Stoffi.Player.GUI.Controls.ControlPanel
{
	/// <summary>
	/// Interaction logic for About.xaml
	/// </summary>
	public partial class About : ScrollViewer
	{
		#region Members

		#endregion

		#region Properties

		#endregion

		#region Constructor
		public About()
		{
			InitializeComponent();
			AboutChannel.Text = U.Capitalize(SettingsManager.Channel);

			AboutVersion.Text = U.Titleize(SettingsManager.Release, false, true); // release name
			AboutRelease.Text = U.Unix2Date(SettingsManager.Version); // release date
			AboutStamp.Text = U.Version2String(SettingsManager.Version); // release version
			AboutArch.Text = SettingsManager.Architecture + "-bit";
			UpgradeManager.Checked += new EventHandler(UpgradeManager_Checked);
			UpdateUpgradeCheck();
		}
		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Updates the strings around the GUI, which are set programmatically, according to the current Language
		/// </summary>
		public void RefreshStrings()
		{
			UpdateUpgradeCheck();
		}

		#endregion

		#region Private

		/// <summary>
		/// Updates the timestamp for the latest check for upgrades
		/// </summary>
		public void UpdateUpgradeCheck()
		{
			try
			{
				string text = U.Unix2Date(SettingsManager.UpgradeCheck);
				AboutUpgradeCheck.Text = text;
				if (UpgradeManager.Pending)
				{
					AboutUpgradePending.Visibility = System.Windows.Visibility.Visible;
				}
			}
			catch
			{
				AboutUpgradeCheck.Text = U.T("UpgradeNotChecked");
			}
			if (SettingsManager.UpgradeCheck == 0)
				AboutUpgradeCheck.Text = U.T("UpgradeNotChecked");
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the control is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void About_Loaded(object sender, RoutedEventArgs e)
		{
			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
				Title.Style = (Style)FindResource("ClassicControlPanelTitleStyle");
		}

		/// <summary>
		/// Invoked when the user expands details.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Details_Expanded(object sender, RoutedEventArgs e)
		{
			AboutRelease.Visibility = System.Windows.Visibility.Visible;
			AboutReleaseLabel.Visibility = System.Windows.Visibility.Visible;
			AboutStamp.Visibility = System.Windows.Visibility.Visible;
			AboutStampLabel.Visibility = System.Windows.Visibility.Visible;
			AboutChannel.Visibility = System.Windows.Visibility.Visible;
			AboutChannelLabel.Visibility = System.Windows.Visibility.Visible;
			AboutArch.Visibility = System.Windows.Visibility.Visible;
			AboutArchLabel.Visibility = System.Windows.Visibility.Visible;
		}

		/// <summary>
		/// Invoked when the user collapses details.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Details_Collapsed(object sender, RoutedEventArgs e)
		{
			AboutRelease.Visibility = System.Windows.Visibility.Collapsed;
			AboutReleaseLabel.Visibility = System.Windows.Visibility.Collapsed;
			AboutStamp.Visibility = System.Windows.Visibility.Collapsed;
			AboutStampLabel.Visibility = System.Windows.Visibility.Collapsed;
			AboutChannel.Visibility = System.Windows.Visibility.Collapsed;
			AboutChannelLabel.Visibility = System.Windows.Visibility.Collapsed;
			AboutArch.Visibility = System.Windows.Visibility.Collapsed;
			AboutArchLabel.Visibility = System.Windows.Visibility.Collapsed;
		}

		/// <summary>
		/// Invoked when a check for upgrades has been performed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void UpgradeManager_Checked(object sender, EventArgs e)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate()
			{
				UpdateUpgradeCheck();
			}));
		}

		#endregion

		#endregion
	}
}
