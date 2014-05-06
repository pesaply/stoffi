/**
 * TrayToolTip.xaml.cs
 * 
 * The tooltip that is shown when the tray icon is hovered.
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
	/// Interaction logic for TrayToolTip.xaml
	/// </summary>
	public partial class TrayToolTip : UserControl
	{
		public StoffiWindow ParentWindow;
		public TrayToolTip(StoffiWindow parent)
		{
			ParentWindow = parent;
			//U.L(LogLevel.Debug, "TRAY TOOLTIP", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "TRAY TOOLTIP", "Initialized");
		}

		public void SetTrack(TrackData track)
		{
			TrackArtist.Text = track.Artist;
			TrackTitle.Text = track.Title;
			AlbumArt.Source = Utilities.GetImageTag(track);
		}

		public void Clear()
		{
			TrackArtist.Text = "";
			TrackTitle.Text = "Nothing is playing";
			AlbumArt.Source = new BitmapImage(new Uri("/Platform/Windows 7/GUI/Images/AlbumArt/Default.jpg", UriKind.RelativeOrAbsolute));
		}
	}
}
