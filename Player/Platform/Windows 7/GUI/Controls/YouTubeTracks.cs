/**
 * YouTubeTracks.cs
 * 
 * The list which contains and displays tracks from YouTube.
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Threading;

namespace Stoffi.Platform.Windows7.GUI.Controls
{
	/// <summary>
	/// The list which contains the tracks at YouTube.
	/// </summary>
	public class YouTubeTracks : ViewDetails
	{
		#region Members

		private DispatcherTimer searchDelay = new DispatcherTimer();
		private string searchText = "";
		private Thread ysThread;
		private string filter = "";

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the string that is used to filter items.
		/// </summary>
		public string Filter
		{
			get { return filter; }
			set
			{
				filter = value;

				if (value == "")
					Items.Filter = null;

				else if (FilterMatch != null)
				{
					Items.Filter = delegate(object item)
					{
						return FilterMatch((ViewDetailsItemData)item, value);
					};
				}

				if (Config != null && Config.Filter != value)
					Config.Filter = value;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the YouTube class
		/// </summary>
		public YouTubeTracks()
		{
			//U.L(LogLevel.Debug, "YouTube", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "YouTube", "Initialized");

			searchDelay.Tick += new EventHandler(SearchDelay_Tick);
			searchDelay.Interval = new TimeSpan(0, 0, 0, 0, 500);
			SettingsManager.PropertyChanged += new PropertyChangedWithValuesEventHandler(SettingsManager_PropertyChanged);
			YouTubeManager.Reconnected += new EventHandler(YouTubeManager_Reconnected);
			if (SettingsManager.YouTubeListConfig != null)
				SettingsManager.YouTubeListConfig.PropertyChanged += new PropertyChangedEventHandler(Config_PropertyChanged);

			//U.L(LogLevel.Debug, "YouTube", "Created");
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Perform a search on YouTube
		/// </summary>
		/// <param name="text">The text to search for</param>
		public void Search(string text)
		{
			if (text == null || text == "")
			{
				FillDefaultTracks();
			}
			else
			{
				searchText = text;
				searchDelay.Stop();
				try
				{
					if (ysThread != null && ysThread.IsAlive)
						ysThread.Abort();
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "YOUTUBE", "Could not abort search thread: " + e.Message);
				}
				searchDelay.Start();
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Fills the list with default tracks
		/// </summary>
		private void FillDefaultTracks()
		{
			try
			{
				if (ysThread != null && ysThread.IsAlive)
					ysThread.Abort();
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Error, "YOUTUBE", "Could not abort search thread: " + exc.Message);
			}

			ThreadStart YouTubeThread = delegate()
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					SearchOverlay = Visibility.Visible;
				}));

				try
				{
					ObservableCollection<TrackData> tracks = YouTubeManager.TopFeed("top_rated");

					Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
					{
						//ClearSort();
						ItemsSource = tracks;
					}));
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "YouTube", "Could not populate TopRated: " + e.Message);
				}

				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					SearchOverlay = Visibility.Collapsed;
				}));
			};
			ysThread = new Thread(YouTubeThread);
			ysThread.Name = "YouTube default";
			ysThread.Priority = ThreadPriority.BelowNormal;
			ysThread.Start();
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the connectivity to the YouTube server is re-established.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void YouTubeManager_Reconnected(object sender, EventArgs e)
		{
			if (String.IsNullOrWhiteSpace(SettingsManager.YouTubeListConfig.Filter))
				FillDefaultTracks();
			else
				Search(SettingsManager.YouTubeListConfig.Filter);
		}

		/// <summary>
		/// Invoked when the search delay is hit
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SearchDelay_Tick(object sender, EventArgs e)
		{
			searchDelay.Stop();

			try
			{
				if (ysThread != null && ysThread.IsAlive)
					ysThread.Abort();
			}
			catch { }

			ThreadStart searchThread = delegate()
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					SearchOverlay = Visibility.Visible;
				}));

				try
				{
					ObservableCollection<TrackData> tracks = YouTubeManager.Search(searchText);

					Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
					{
						//ClearSort();
						ItemsSource = tracks;
					}));
				}
				catch (Exception exc)
				{
					U.L(LogLevel.Warning, "YouTube", "Could not search: " + exc.Message);
				}

				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					SearchOverlay = Visibility.Collapsed;
				}));
			};
			ysThread = new Thread(searchThread);
			ysThread.Name = "YouTube search";
			ysThread.Priority = ThreadPriority.BelowNormal;
			ysThread.Start();
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
					case "YouTubeListConfig":
						Config = SettingsManager.YouTubeListConfig;
						SettingsManager.YouTubeListConfig.PropertyChanged += new PropertyChangedEventHandler(Config_PropertyChanged);
						break;

					case "YouTubeFilter":
						if (String.IsNullOrWhiteSpace(SettingsManager.YouTubeListConfig.Filter))
							FillDefaultTracks();
						else
							Search(SettingsManager.YouTubeListConfig.Filter);
						break;
				}
			}));
		}

		/// <summary>
		/// Invoked when a property of the YouTube list configuration changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		protected override void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Filter")
				Search(SettingsManager.YouTubeListConfig.Filter);
		}

		#endregion

		#endregion
	}
}
