/**
 * TrayNotification.xaml.cs
 * 
 * The notification that is shown in the tray area.
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
using System.IO;
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
	/// Interaction logic for TrayNotification.xaml
	/// </summary>
	public partial class TrayNotification : UserControl
	{
		#region Members

		private StoffiWindow ParentWindow;

		#endregion

		#region Constructors

		/// <summary>
		/// 
		/// </summary>
		/// <param name="numberOfUpgrades"></param>
		/// <param name="parent"></param>
		public TrayNotification(StoffiWindow parent)
		{
			U.L(LogLevel.Debug, "TRAY NOTIFICATION", "Initialize");
			InitializeComponent();
			U.L(LogLevel.Debug, "TRAY NOTIFICATION", "Initialized");
			TrackInformation.Visibility = System.Windows.Visibility.Collapsed;
			NewUpgrades.Visibility = System.Windows.Visibility.Visible;
			UpgradeTitle.Text = "New Upgrade Available";
			UpgradeDescription.Text = "Found new upgrade";
			ParentWindow = parent;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>
		/// <param name="parent"></param>
		public TrayNotification(TrackData track, StoffiWindow parent)
		{
			ParentWindow = parent;
			InitializeComponent();

			TrackArtist.Text = track.Artist;
			TrackTitle.Text = track.Title;
			AlbumArt.Source = Utilities.GetImageTag(track);
		}

		#endregion

		#region Methods

		#region Event handlers

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Close_Click(object sender, RoutedEventArgs e)
		{
			ParentWindow.trayIcon.CloseBalloon();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Upgrade_Click(object sender, RoutedEventArgs e)
		{
			ParentWindow.trayIcon.CloseBalloon();
			ParentWindow.ControlPanel.PrefDoUpgrade_Clicked(null, null);
		}

		#endregion

		#endregion
	}
}
