/**
 * JamendoTracks.cs
 * 
 * The list which contains and displays tracks from Jamendo.
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Threading;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Settings;
using SettingsManager = Stoffi.Core.Settings.Manager;
using SourceManager = Stoffi.Core.Sources.Manager;

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// The list which contains the tracks at Jamendo.
	/// </summary>
	public class JamendoTracks : ViewDetails
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

				Items.Filter = delegate(object item)
				{
					return U.TrackMatchesQuery((ListItem)item, value);
				};

				if (Config != null && Config.Filter != value)
					Config.Filter = value;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the JamendoTracks class
		/// </summary>
		public JamendoTracks()
		{
			//U.L(LogLevel.Debug, "Jamendo", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "Jamendo", "Initialized");

			searchDelay.Tick += new EventHandler(SearchDelay_Tick);
			searchDelay.Interval = new TimeSpan(0, 0, 0, 0, 500);
			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);
			if (SettingsManager.JamendoListConfig != null)
				SettingsManager.JamendoListConfig.PropertyChanged += new PropertyChangedEventHandler(Config_PropertyChanged);

			//U.L(LogLevel.Debug, "Jamendo", "Created");
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Perform a search on Jamendo
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
				if (ysThread != null && ysThread.IsAlive)
					ysThread.Abort();
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
			if (ysThread != null && ysThread.IsAlive)
				ysThread.Abort();

			ThreadStart JamendoThread = delegate()
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
				{
					SearchOverlay = Visibility.Visible;
				}));

				try
				{
					ObservableCollection<Track> tracks = SourceManager.Jamendo.TopFeed();

					Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
					{
						//ClearSort();
						ItemsSource = tracks;
					}));
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "Jamendo", "Could not populate TopRated: " + e.Message);
				}

				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
				{
					SearchOverlay = Visibility.Collapsed;
				}));
			};
			ysThread = new Thread(JamendoThread);
			ysThread.Name = "Jamendo default";
			ysThread.Priority = ThreadPriority.BelowNormal;
			ysThread.Start();
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the search delay is hit
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SearchDelay_Tick(object sender, EventArgs e)
		{
			searchDelay.Stop();

			if (ysThread != null && ysThread.IsAlive)
				ysThread.Abort();

			ThreadStart searchThread = delegate()
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
				{
					SearchOverlay = Visibility.Visible;
				}));

				try
				{
					ObservableCollection<Track> tracks = SourceManager.Jamendo.Search(searchText);

					Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
					{
						//ClearSort();
						ItemsSource = tracks;
					}));
				}
				catch (Exception exc)
				{
					U.L(LogLevel.Warning, "Jamendo", "Could not search: " + exc.Message);
				}

				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
				{
					SearchOverlay = Visibility.Collapsed;
				}));
			};
			ysThread = new Thread(searchThread);
			ysThread.Name = "Jamendo search";
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
			switch (e.PropertyName)
			{
				case "JamendoListConfig":
					Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate()
					{
						Config = SettingsManager.JamendoListConfig;
						SettingsManager.JamendoListConfig.PropertyChanged += new PropertyChangedEventHandler(Config_PropertyChanged);
					}));
					break;
			}
		}

		/// <summary>
		/// Invoked when a property of the Jamendo list configuration changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		protected override void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Filter")
				Search(SettingsManager.JamendoListConfig.Filter);
		}

		#endregion

		#endregion
	}
}
