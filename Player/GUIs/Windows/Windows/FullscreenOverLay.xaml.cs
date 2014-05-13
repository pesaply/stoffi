/**
 * TrayToolTip.xaml.cs
 * 
 * The tooltip that is shown when the tray icon is hovered.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Shapes;

using Stoffi;
using Stoffi.Core;
using Stoffi.Core.Settings;
using Stoffi.Core.Media;
using MediaManager = Stoffi.Core.Media.Manager;
using SettingsManager = Stoffi.Core.Settings.Manager;

namespace Stoffi.Player.GUI.Windows
{
	/// <summary>
	/// Interaction logic for FullscreenOverLay.xaml
	/// </summary>
	public partial class FullscreenOverLay : Window
	{
		#region Members

		private Visibility controlsVisibility = Visibility.Visible;
		private Color overlayBackground = Color.FromArgb(8 * 16, 0, 0, 0);

		#endregion

		#region Properties

		/// <summary>
		/// Gets whether the overlay should be kept open or not.
		/// </summary>
		/// <remarks>
		/// The overlay can still be hidden, this property indicates whether
		/// the user may be interacting with the overlay even though there
		/// is no mouse activity.
		/// </remarks>
		public bool PreventHiding { get; private set; }

		/// <summary>
		/// Sets or sets the visibility of the overlay controls.
		/// </summary>
		public Visibility ControlsVisibility
		{
			get { return controlsVisibility; }
			set
			{
				if (value != controlsVisibility)
				{
					if (value == Visibility.Visible)
					{
						var da = new DoubleAnimation();
						da.Duration = new Duration(TimeSpan.FromMilliseconds(150));
						da.From = 0.0;
						da.To = 1.0;

						var ta = new ThicknessAnimation();
						ta.Duration = da.Duration;
						ta.From = new Thickness(0, 0, 0, -100);
						ta.To = new Thickness(0, 0, 0, 0);

						var ca = new ColorAnimation();
						ca.Duration = da.Duration;
						ca.From = Color.FromArgb(0,0,0,0);
						ca.To = overlayBackground;
						var overlayBackgroundBrush = this.FindResource("OverlayBackground") as SolidColorBrush;

						overlayBackgroundBrush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
						TrackInfo.BeginAnimation(UIElement.OpacityProperty, da);
						BottomOverlay.BeginAnimation(UIElement.OpacityProperty, da);
						BottomOverlay.BeginAnimation(FrameworkElement.MarginProperty, ta);
					}
					else
					{
						var da = new DoubleAnimation();
						da.Duration = new Duration(TimeSpan.FromMilliseconds(800));
						da.From = 1.0;
						da.To = 0.0;

						var ta = new ThicknessAnimation();
						ta.Duration = da.Duration;
						ta.From = new Thickness(0, 0, 0, 0);
						ta.To = new Thickness(0, 0, 0, -100);

						var ca = new ColorAnimation();
						ca.Duration = da.Duration;
						ca.From = overlayBackground;
						ca.To = Color.FromArgb(0, 0, 0, 0);
						var overlayBackgroundBrush = this.FindResource("OverlayBackground") as SolidColorBrush;

						overlayBackgroundBrush.BeginAnimation(SolidColorBrush.ColorProperty, ca);
						TrackInfo.BeginAnimation(UIElement.OpacityProperty, da);
						BottomOverlay.BeginAnimation(UIElement.OpacityProperty, da);
						BottomOverlay.BeginAnimation(FrameworkElement.MarginProperty, ta);
					}
				}
				controlsVisibility = value;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a fullscreen overlay window.
		/// </summary>
		public FullscreenOverLay()
		{
			InitializeComponent();
			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);

			Seek.Value = SettingsManager.Seek;
			Seek.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Seek_ValueChanged);

			Volume.Value = SettingsManager.Volume;
			Volume.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Volume_ValueChanged);
			Volume.AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft;
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Updates the strings around the GUI, which are set programmatically, according to the current Language.
		/// </summary>
		public void RefreshStrings()
		{
			foreach (MenuItem mi in QualityMenu.Items)
			{
				switch ((string)mi.Tag)
				{
					case "default":
						mi.Header = U.T("QualityDefault", "Content");
						break;

					case "highres":
						mi.Header = U.T("QualityHighres", "Content");
						break;

					case "hd1080":
						mi.Header = U.T("Quality1080", "Content");
						break;

					case "hd720":
						mi.Header = U.T("Quality720", "Content");
						break;

					case "large":
						mi.Header = U.T("QualityHigh", "Content");
						break;

					case "medium":
						mi.Header = U.T("QualityMedium", "Content");
						break;

					case "small":
						mi.Header = U.T("QualityLow", "Content");
						break;
				}
			}
		}

		/// <summary>
		/// Updates the information about the currently loaded track
		/// </summary>
		public void UpdateInfo()
		{
			var t = SettingsManager.CurrentTrack;
			if (t == null || (t.Type == TrackType.File && !File.Exists(t.Path)))
			{
				Artist.Text = U.T("PlaybackEmpty");
				Title.Text = U.T("PlaybackEmptyDescription");
				TimeMinus.Content = "N/A";
				TimePlus.Content = "N/A";
				Seek.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Seek_ValueChanged);
				Seek.Value = 0;
				Seek.SecondValueWidth = 0;
				Seek.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Seek_ValueChanged);
			}
			else
			{
				if (t.Type == TrackType.WebRadio)
				{
					Artist.Text = t.URL;
					Title.Text = t.Title;
					TimeMinus.Content = "N/A";
					TimePlus.Content = "N/A";
					Seek.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Seek_ValueChanged);
					Seek.Value = 0;
					Seek.SecondValueWidth = 0;
					Seek.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Seek_ValueChanged);
				}
				else
				{
					Artist.Text = t.Artist;
					Title.Text = t.Title;
					double pos = MediaManager.Position;
					double len = MediaManager.Length;

					if (pos < 0) pos = 0;
					if (len < 0) len = 0;

					TimeSpan timePlus = new TimeSpan(0, 0, (int)pos);
					TimeSpan timeMinus = new TimeSpan(0, 0, (int)(len - pos));

					if (timePlus.TotalSeconds < 0)
						TimePlus.Content = "N/A";
					else if (timePlus.TotalSeconds >= 0)
						TimePlus.Content = U.TimeSpanToString(timePlus);

					if (timeMinus.TotalSeconds < 0)
						TimeMinus.Content = "N/A";
					else if (timeMinus.TotalSeconds > 0)
						TimeMinus.Content = "-" + U.TimeSpanToString(timeMinus);

					double seek = SettingsManager.Seek * (Seek.Maximum / 10.0);
					if (timePlus.Seconds < 0 || Double.IsNaN(seek) || Double.IsInfinity(seek)) seek = 0;

					if (seek > 0 || SettingsManager.MediaState != Core.Settings.MediaState.Playing)
					{
						Seek.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Seek_ValueChanged);
						Seek.Value = seek;
						Seek.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Seek_ValueChanged);
					}
				}
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Updates the album art image.
		/// </summary>
		private void UpdateArt()
		{
			Dispatcher.Invoke(DispatcherPriority.SystemIdle, new Action(delegate()
			{
				AlbumArt.Source = Utilities.GetImageTag(SettingsManager.CurrentTrack);
			}));
		}

		/// <summary>
		/// Updates the shuffle button to reflect the current Shuffle state
		/// </summary>
		private void UpdateShuffle()
		{
			switch (SettingsManager.Shuffle)
			{
				case true:
					Shuffle.Style = (Style)FindResource("ShuffleButtonStyle");
					break;

				case false:
				default:
					Shuffle.Style = (Style)FindResource("ShuffleGrayButtonStyle");
					break;
			}
		}

		/// <summary>
		/// Updates the repeat button to reflect the current Repeat state
		/// </summary>
		private void UpdateRepeat()
		{
			switch (SettingsManager.Repeat)
			{
				case RepeatState.RepeatAll:
					Repeat.Style = (Style)FindResource("RepeatAllButtonStyle");
					break;

				case RepeatState.RepeatOne:
					Repeat.Style = (Style)FindResource("RepeatOneButtonStyle");
					break;

				case RepeatState.NoRepeat:
				default:
					Repeat.Style = (Style)FindResource("RepeatGrayButtonStyle");
					break;
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user clicks on pause/play.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PlayPause_Click(object sender, RoutedEventArgs e)
		{
			if (SettingsManager.MediaState == Core.Settings.MediaState.Playing)
				MediaManager.Pause();
			else
				MediaManager.Play();
		}

		/// <summary>
		/// Invoked when the user clicks on next.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Next_Click(object sender, RoutedEventArgs e)
		{
			MediaManager.Next(true);
		}

		/// <summary>
		/// Invoked when the user clicks on previous.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Previous_Click(object sender, RoutedEventArgs e)
		{
			MediaManager.Previous();
		}

		/// <summary>
		/// Invoked when the user clicks on repeat.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Repeat_Click(object sender, RoutedEventArgs e)
		{
			switch (SettingsManager.Repeat)
			{
				case RepeatState.RepeatAll:
					SettingsManager.Repeat = RepeatState.RepeatOne;
					break;
				case RepeatState.RepeatOne:
					SettingsManager.Repeat = RepeatState.NoRepeat;
					break;
				case RepeatState.NoRepeat:
				default:
					SettingsManager.Repeat = RepeatState.RepeatAll;
					break;
			}
			UpdateRepeat();
		}

		/// <summary>
		/// Invoked when the user clicks on shuffle.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Shuffle_Click(object sender, RoutedEventArgs e)
		{
			SettingsManager.Shuffle = !SettingsManager.Shuffle;
			UpdateShuffle();
		}

		/// <summary>
		/// Invoked when the user changes the volume slider.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			SettingsManager.Volume = Volume.Value;
		}

		/// <summary>
		/// Invoked when the user scrolls over the volume slider.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Volume_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
				Volume.Value += 5;
			else
				Volume.Value -= 5;
		}

		/// <summary>
		/// Invoked when the user changes the seek slider.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Seek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (MediaManager.IsInitialized)
				MediaManager.Seek = (Seek.Value * 10.0) / Seek.Maximum;
		}

		/// <summary>
		/// Invoked when the user enters fullscreen mode.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateInfo();
			UpdateArt();
			RefreshStrings();
			foreach (var item in QualityMenu.Items)
			{
				var menu = item as MenuItem;
				var tag = menu.Tag as string;
				if (tag == SettingsManager.YouTubeQuality)
				{
					menu.Checked -= new RoutedEventHandler(Quality_Checked);
					menu.IsChecked = true;
					menu.Checked += new RoutedEventHandler(Quality_Checked);
					break;
				}
			}
		}

		/// <summary>
		/// Invoked when the user changes quality.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Quality_Checked(object sender, RoutedEventArgs e)
		{
			var item = sender as MenuItem;
			var quality = item.Tag as string;

			foreach (MenuItem mi in QualityMenu.Items)
				mi.IsChecked = mi == item;

			SettingsManager.YouTubeQuality = quality;
		}

		/// <summary>
		/// Invoked when the user clicks on the quality icon.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Quality_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			QualityMenu.PlacementTarget = Quality;
			QualityMenu.IsOpen = true;
		}

		/// <summary>
		/// Invoked when the user moves the mouse cursor onto the overlay.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_MouseLeave(object sender, MouseEventArgs e)
		{
			PreventHiding = false;
		}

		/// <summary>
		/// Invoked when the user moves the mouse cursor off the overlay.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_MouseEnter(object sender, MouseEventArgs e)
		{
			PreventHiding = true;
		}

		/// <summary>
		/// Invoked when the user openes the quality menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void QualityMenu_Opened(object sender, RoutedEventArgs e)
		{
			PreventHiding = true;
		}

		/// <summary>
		/// Invoked when the user closes the quality menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void QualityMenu_Closed(object sender, RoutedEventArgs e)
		{
			PreventHiding = false;
		}

		/// <summary>
		/// Invoked when a property of the settings manager changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				switch (e.PropertyName)
				{
					case "Seek":
						UpdateInfo();
						break;

					case "BufferSize":
						Seek.SecondValue = SettingsManager.BufferSize * (Seek.Maximum / 10.0);
						break;

					case "Volume":
						Volume.Value = SettingsManager.Volume;
						break;

					case "Shuffle":
						UpdateShuffle();
						break;

					case "Repeat":
						UpdateRepeat();
						break;

					case "CurrentTrack":
						UpdateInfo();
						UpdateArt();
						//ClearBookmarks();
						//if (SettingsManager.CurrentTrack != null)
						//{
						//    Track libraryTrack = MediaManager.GetLibrarySourceTrack(SettingsManager.CurrentTrack);
						//    if (libraryTrack.Bookmarks != null)
						//        foreach (double b in libraryTrack.Bookmarks)
						//            AddBookmark(b);
						//}
						break;

					case "MediaState":
						switch (SettingsManager.MediaState)
						{
							case Core.Settings.MediaState.Playing:
								PlayPause.Style = (Style)FindResource("PauseButtonStyle");
								break;

							case Core.Settings.MediaState.Paused:
							case Core.Settings.MediaState.Stopped:
								PlayPause.Style = (Style)FindResource("PlayButtonStyle");
								break;
						}
						break;
				}
			}));
		}

		#endregion

		#endregion
	}
}
