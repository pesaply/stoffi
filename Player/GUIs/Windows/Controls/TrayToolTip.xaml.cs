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
using System.Linq;
using System.IO;
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
using System.Windows.Shapes;
using System.Windows.Threading;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Settings;
using Stoffi.Player.GUI;
using MediaManager = Stoffi.Core.Media.Manager;
using SettingsManager = Stoffi.Core.Settings.Manager;

using GlassLib;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for TrayToolTip.xaml
	/// </summary>
	public partial class TrayToolTip : Window
	{
		#region Members

		private Timer hideDelay = null;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets whether the control should be hidden
		/// when the user moves the mouse off the control.
		/// </summary>
		public bool HideWhenMouseOut { get; set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the tooltip widget.
		/// </summary>
		public TrayToolTip()
		{
			InitializeComponent();
			MinWidth = MaxWidth = Width;
			MinHeight = MaxHeight = Height;
			hideDelay = new Timer(PerformHide, null, Timeout.Infinite, Timeout.Infinite);
			HideWhenMouseOut = false;

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
		/// Hides the widget after a delay. This will be aborted if the mouse is over the widget
		/// but restarted when the mouse leaves the widget.
		/// </summary>
		public void DelayedHide()
		{
			HideWhenMouseOut = true;
			hideDelay.Change(1000, Timeout.Infinite);
		}

		/// <summary>
		/// Shows the control.
		/// </summary>
		public new void Show()
		{
			base.Show();
			Activate();
			Focus();
		}

		/// <summary>
		/// Toggle the visibility of the widget.
		/// </summary>
		public void Toggle()
		{
			if (IsVisible)
				Hide();
			else
			{
				Show();
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
		/// Will repaint the glass effect behind the playback pane.
		/// </summary>
		private void RefreshGlassEffect()
		{
			try
			{
				Point lt = Info.TranslatePoint(new Point(0, Info.Height), this);
				var source = PresentationSource.FromVisual(this);
				Matrix transformToDevice = source.CompositionTarget.TransformToDevice;
				Dwm.Glass[this].Enabled = true;
				Thickness glassMargin = new Thickness(1, 1, 1, transformToDevice.Transform(lt).Y);
				Dwm.Glass[this].Margins = glassMargin;
				Background = Brushes.Transparent;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "MAIN", "Could not set glass effect: " + e.Message);
				if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
					Background = SystemColors.ControlBrush;
				else
					Background = SystemColors.GradientActiveCaptionBrush;
			}
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
		/// Invoked when the control is about to close.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TrayWidget_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}

		/// <summary>
		/// Invoked when the control is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TrayWidget_Loaded(object sender, RoutedEventArgs e)
		{
			System.Drawing.Rectangle wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
			var source = PresentationSource.FromVisual(this);
			Matrix transformFromDevice = source.CompositionTarget.TransformFromDevice;
			Point corner = transformFromDevice.Transform(new Point(wa.Right, wa.Bottom));
			Left = corner.X - Width - Margin.Right;
			Top = corner.Y - Height - Margin.Bottom;
			RefreshGlassEffect();
			UpdateInfo();
			UpdateShuffle();
			UpdateRepeat();
		}

		/// <summary>
		/// Invoked when the control looses focus.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TrayWidget_LostFocus(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		/// <summary>
		/// Invoked when the control is deactivated.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TrayWidget_Deactivated(object sender, EventArgs e)
		{
			Hide();
		}

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
						Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(delegate()
						{
							AlbumArt.Source = Utilities.GetImageTag(SettingsManager.CurrentTrack);
						}));
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
		
		/// <summary>
		/// Hides the widget.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private void PerformHide(object state)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				Hide();
			}));
		}

		/// <summary>
		/// Invoked when the user moves the mouse pointer off the control.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TrayWidget_MouseLeave(object sender, MouseEventArgs e)
		{
			if (HideWhenMouseOut)
				DelayedHide();
		}

		/// <summary>
		/// Invoked when the user moves the mouse pointer onto the control.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TrayWidget_MouseEnter(object sender, MouseEventArgs e)
		{
			hideDelay.Change(Timeout.Infinite, Timeout.Infinite);
		}

		/// <summary>
		/// Invoked when the user moves the mouse pointer over the control.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TrayWidget_MouseMove(object sender, MouseEventArgs e)
		{
			hideDelay.Change(Timeout.Infinite, Timeout.Infinite);
		}

		#endregion

		#endregion
	}
}
