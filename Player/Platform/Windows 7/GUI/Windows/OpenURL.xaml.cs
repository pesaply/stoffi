/**
 * OpenURL.cs
 * 
 * Presents a dialog where the user can load a URL pointing
 * to a web radio stream.
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for OpenURL.xaml
	/// </summary>
	public partial class OpenURL : Window
	{
		#region Members

		private DispatcherTimer inputDelay = new DispatcherTimer();
		private bool callCallback = false;
		private URLParsed callback;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the URLs that should be added
		/// </summary>
		public List<TrackData> URLs { get; private set; }

		/// <summary>
		/// Gets whether the dialog is currently parsing a URL.
		/// </summary>
		public bool IsParsing { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Create a dialog where the user can add a URL.
		/// It will parse pls and m3u files and extract the URLs used
		/// for streaming audio.
		/// </summary>
		/// <param name="callback">
		/// The function to be called when the URL has been parsed in case
		/// the window was closed before parsing was finished
		/// </param>
		public OpenURL(URLParsed finishCallback)
		{
			callback = finishCallback;
			InitializeComponent();
			inputDelay.Interval = new TimeSpan(0, 0, 1);
			inputDelay.Tick += new EventHandler(inputDelay_Tick);
			URLs = new List<TrackData>();
			IsParsing = false;
		}

		#endregion 

		#region Methods

		#region Private

		/// <summary>
		/// Sets whether the progressbar is visible or not.
		/// </summary>
		/// <param name="loading">If true the progressbar will be shown, otherwise the text labels</param>
		private void ToggleProgressbar(bool loading)
		{
			Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
			{
				bool showMeta = true;
				if (!loading)
				{
					if (URLs.Count > 0)
						MetaTitle.Text = URLs[0].Title;
					else
						showMeta = false;
				}

				MetaTitle.Visibility = loading || !showMeta ? Visibility.Collapsed : Visibility.Visible;
				MetaTitleLabel.Visibility = loading || !showMeta ? Visibility.Collapsed : Visibility.Visible;
				Loading.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
				IsParsing = loading;
				//Add.IsEnabled = !loading;
			}));
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user clicks on "Add".
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Add_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			callCallback = true;
			Close();
		}

		/// <summary>
		/// Invoked when the user clicks on "Cancel".
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		/// <summary>
		/// Invoked when the user changes the URL text.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void URL_TextChanged(object sender, TextChangedEventArgs e)
		{
			IsParsing = true;
			//Add.IsEnabled = false;
			inputDelay.Stop();
			inputDelay.Start();
		}

		/// <summary>
		/// Invoked when the URL has been parsed by the MediaManager.
		/// </summary>
		/// <param name="track">The track representing the audio at the URL</param>
		private void URL_Parsed(TrackData track)
		{
			URLs.Clear();
			URLs.Add(track);
			Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
			{
				ToggleProgressbar(false);
			}));
			if (callCallback && callback != null)
				callback(URLs);
		}

		/// <summary>
		/// Invoked after a short delay after the user has changed the URL text.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void inputDelay_Tick(object sender, EventArgs e)
		{
			inputDelay.Stop();

			ToggleProgressbar(true);

			string url = URL.Text;

			if (PlaylistManager.IsSupported(url))
			{
				ThreadStart ParseThread = delegate()
				{
					HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
					HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
					Stream str = resp.GetResponseStream();
					Encoding enc = Encoding.GetEncoding("utf-8");
					StreamReader read = new StreamReader(str, enc);

					URLs.Clear();
					foreach (TrackData track in PlaylistManager.ParsePlaylist(read, PlaylistManager.GetType(url)))
					{
						track.Icon = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/Radio.ico";
						URLs.Add(track);
					}

					resp.Close();
					read.Close();

					ToggleProgressbar(false);

					if (callCallback && callback != null)
						callback(URLs);
				};
				Thread parse_thread = new Thread(ParseThread);
				parse_thread.Name = "URL Parser";
				parse_thread.IsBackground = true;
				parse_thread.Priority = ThreadPriority.Lowest;
				parse_thread.Start();
			}
			else
				MediaManager.ParseURLAsync(URL.Text, URL_Parsed, true);
		}

		#endregion

		#endregion

		#region Delegates

		public delegate void URLParsed(List<TrackData> tracks);

		#endregion
	}
}
