/**
 * Video.xaml.cs
 * 
 * The "Video" screen.
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
using System.IO;
using System.Linq;
using System.Net;
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

namespace Stoffi.Platform.Windows7.GUI.Controls
{
	/// <summary>
	/// Interaction logic for Video.xaml
	/// </summary>
	public partial class Video : DockPanel
	{
		#region Members
		private static bool browserLoaded = false;
		private static object[] invokeAtLoad = null;
		#endregion

		#region Properties

		/// <summary>
		/// Changes whether the browser is visible or not
		/// </summary>
		public Visibility BrowserVisibility
		{
			get
			{
				if (Children.Contains(Browser))
					return Visibility.Visible;
				else
					return Visibility.Collapsed;
			}
			set
			{
				try
				{
					if (value == Visibility.Visible)
					{
						if (Children.Contains(NoVideoMessage))
							Children.Remove(NoVideoMessage);
						if (!Children.Contains(Browser))
							Children.Add(Browser);
					}
					else
					{
						if (Children.Contains(Browser))
							Children.Remove(Browser);
						if (!Children.Contains(NoVideoMessage))
							Children.Add(NoVideoMessage);
					}
					Browser.Visibility = Visibility.Visible;
					NoVideoMessage.Visibility = Visibility.Visible;
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "VIDEO", "Could not set browser visibility to " + value.ToString() + ": " + e.Message);
				}
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a Now Playing control.
		/// </summary>
		public Video()
		{
			//U.L(LogLevel.Debug, "NOW PLAYING", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "NOW PLAYING", "Initialized");
			InitializeBrowser();
			MediaManager.InvokeScriptCallback = InvokeScript;
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Invokes a script function inside the browser
		/// </summary>
		/// <param name="function">The function name</param>
		/// <param name="param">The function parameters</param>
		/// <returns>The return value of the function (or null if the invokation failed)</returns>
		/// <remarks>
		/// If the invokation fails the method will return null but an attempt to reinitialize
		/// the browser will be made. If the browser loads successfully the invokation will
		/// reoccur but its return value will be lost since the reinitialization is not
		/// synchronous and the method has already returned.
		/// </remarks>
		public object InvokeScript(string function, object[] param = null)
		{
			while (!browserLoaded) ; // wait for the browser to load

			if (!YouTubeManager.HasFlash)
			{
				InitializeBrowser();
				invokeAtLoad = new object[] { function, param };
				return null;
			}

			object ret = null;
			Browser.Dispatcher.Invoke(new Action(delegate()
			{
				try
				{
					ret = Browser.InvokeScript(function, param);
				}
				catch
				{
					InitializeBrowser();
					invokeAtLoad = new object[] { function, param };
				}
			}));
			return ret;
		}

		#endregion

		#region Private


		/// <summary>
		/// Will load the current track into the youtube player if it is a youtube track
		/// </summary>
		public void LoadYouTube()
		{
			YouTubeManager.HasFlash = true;
			if (invokeAtLoad != null)
			{
				string function = invokeAtLoad[0] as string;
				object[] param = invokeAtLoad[1] as object[];
				invokeAtLoad = null;
				InvokeScript(function, param);
			}

			if (YouTubeManager.IsYouTube(SettingsManager.CurrentTrack))
			{
				string vid = YouTubeManager.GetYouTubeID(SettingsManager.CurrentTrack.Path);
				InvokeScript("setVolume", new object[] { SettingsManager.Volume });
				InvokeScript("cueNewVideo", new object[] { vid, 0 });
				SettingsManager.Seek = 0;
			}
		}

		/// <summary>
		/// Initializes the web browser by navigating to the youtube player
		/// </summary>
		private void InitializeBrowser()
		{
			ThreadStart InitThread = delegate()
			{
				string src = "http://static.stoffiplayer.com/youtube/";
				//string src = "http://beta.stoffiplayer.com/youtube/player.embedded";
				//string src = "http://www.jovianvoid.com/yplayer.html";

				// there's a bug which makes WebResponse in NavigationEventArgs null every time
				// so we can't detect a 404, that's why we need to use this workaround to check
				// if we can connect to the server before navigating there with the browser
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create(src);
				req.Timeout = 30000;
				try
				{
					HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
					//NavigateBrowser(resp.GetResponseStream());
					resp.Close();
					NavigateBrowser(new Uri(src));
					Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
					{
						Utilities.SetSilent(Browser, true);
					}));
				}
				catch (Exception e)
				{
					invokeAtLoad = null;
					browserLoaded = false;
					U.L(LogLevel.Error, "MEDIA", "Problem initializing browser: " + e.Message);
					DispatchConnectionProblem();
				}
			};

			Thread init_thread = new Thread(InitThread);
			init_thread.Name = "Initialize youtube browser";
			init_thread.Priority = ThreadPriority.BelowNormal;
			init_thread.Start();
		}

		/// <summary>
		/// Navigate a browser to a URL.
		/// </summary>
		/// <param name="source">The URI to navigate to</param>
		private void NavigateBrowser(Uri source)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				Browser.Source = source;
			}));
		}

		/// <summary>
		/// Navigate a browser to a stream.
		/// </summary>
		/// <param name="source">The URI to navigate to</param>
		private void NavigateBrowser(Stream stream)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				Browser.NavigateToStream(stream);
			}));
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user clicks on the YouTube video
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Browser_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Console.WriteLine("Left: " + e.ClickCount);
		}

		/// <summary>
		/// Invoked when the browser finishes loading of the web page.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
		{
			browserLoaded = true;
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// The dispatcher of the <see cref="ConnectionProblem"/> event.
		/// </summary>
		private void DispatchConnectionProblem()
		{
			if (ConnectionProblem != null)
				ConnectionProblem(null, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when there's a problem with the Internet connection.
		/// </summary>
		public event EventHandler ConnectionProblem;

		#endregion
	}
}
