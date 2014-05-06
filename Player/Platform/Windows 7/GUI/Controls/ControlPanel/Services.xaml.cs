/**
 * Services.xaml.cs
 * 
 * The "Services" screen inside the "Control Panel".
 * It shows the login/register form when not connected
 * and the dashboard with settings and account links
 * when connected.
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Threading;
using System.Globalization;
using Tomers.WPF.Localization;

using Stoffi;

namespace Stoffi.Platform.Windows7.GUI.Controls.ControlPanel
{
	/// <summary>
	/// Interaction logic for Services.xaml
	/// </summary>
	public partial class Services : ScrollViewer
	{
		#region Members

		private bool loggingOutCloud = false;
		private bool linkingCloud = false;
		private bool cloudBrowserIsReady = true;
		private SortedDictionary<string, UIElement> connectedLinks = new SortedDictionary<string, UIElement>();
		private SortedDictionary<string, UIElement> notConnectedLinks = new SortedDictionary<string, UIElement>();
		private bool browserIsNavigating = false;
		private Dictionary<string, StackPanel> linkNotices = new Dictionary<string, StackPanel>();
		private DispatcherTimer relinkTimer = new DispatcherTimer();
		private bool isReconnecting = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets whether or not the browser for the cloud
		/// services is currently loading or not.
		/// </summary>
		public bool CloudBrowserIsLoading
		{
			get { return BrowserLoadingIndicator.IsVisible; }
			set
			{
				BrowserLoadingIndicator.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
				Cursor = value ? Cursors.Wait : null;
			}
		}

		/// <summary>
		/// Gets or sets whether the cloud controls are visible or not.
		/// </summary>
		public bool CloudControlsAreVisible
		{
			get { return Connected.Children.Contains(Dashboard); }
			set
			{
				if (value && !Connected.Children.Contains(Dashboard))
					Connected.Children.Add(Dashboard);
				else if (!value && Connected.Children.Contains(Dashboard))
					Connected.Children.Remove(Dashboard);

				Visibility v = value ? Visibility.Visible : Visibility.Collapsed;

				Dashboard.Visibility = v;
				Delink.Visibility = v;
				AccountPanel.Visibility = v;
				Browser.Height = value ? 1 : double.NaN;
				Browser.Width = value ? 1 : double.NaN;
				UpdateBrowserBorder();
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates the Services pane in the control panel.
		/// </summary>
		public Services()
		{
			InitializeComponent();
			ServiceManager.PropertyChanged += new PropertyChangedWithValuesEventHandler(ServiceManager_PropertyChanged);
			ServiceManager.Initialized += new EventHandler(ServiceManager_Initialized);
			ServiceManager.RealtimeDisconnected += new EventHandler(ServiceManager_RealtimeDisconnected);
			ServiceManager.RealtimeConnected += new EventHandler(ServiceManager_RealtimeConnected);

			CloudSyncInterface csi = new CloudSyncInterface();
			Browser.ObjectForScripting = csi;

			relinkTimer.Interval = new TimeSpan(0, 15, 0);
			relinkTimer.Tick += new EventHandler(RelinkTimer_Tick);
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Connects to the service via the browser.
		/// </summary>
		/// <param name="reconnect">If true than reconnection will be made if previous tokens found.</param>
		public void Connect(bool reconnect = true)
		{
			Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
			{
				CloudControlsAreVisible = false;
				CloudBrowserIsLoading = true;
				BrowseTo(new Uri(ServiceManager.Linked ? ServiceManager.CallbackURLWithAuthParams : ServiceManager.RequestURL));
			}));
		}

		/// <summary>
		/// Updates the strings around the GUI, which are set programmatically, according to the current Language
		/// </summary>
		public void RefreshStrings()
		{
			try
			{
				if (ConnectedLinks.Children.Count > 0)
				{
					StackPanel sp = ConnectedLinks.Children[0] as StackPanel;
					foreach (UIElement e in sp.Children)
					{
						CheckBox cb = e as CheckBox;
						if (cb != null)
						{
							try
							{
								string[] s = cb.Tag as string[];
								if (s != null && s.Length > 1)
									cb.Content = U.T("ServicesCheck" + s[1], "Content");
							}
							catch { }
							continue;
						}

						Button b = e as Button;
						if (b != null)
						{
							b.Content = U.T("ServicesUnlink", "Content");
							continue;
						}
					}
				}
			}
			catch { }

			try
			{
				foreach (UIElement e in NotConnectedLinks.Children)
				{
					Button b = e as Button;
					if (b != null)
					{
						string[] s = b.Tag as string[];
						if (s != null && s.Length > 0)
							b.Content = String.Format(U.T("ServicesLink", "Content"), s[0]);
					}
				}
			}
			catch { }

			try
			{
				foreach (KeyValuePair<string, StackPanel> notice in linkNotices)
				{
					try
					{
						TextBlock tb = notice.Value.Children[1] as TextBlock;
						Button bu = notice.Value.Children[2] as Button;
						tb.Text = String.Format(U.T("ServicesLinkDisconnected"), notice.Key);
						bu.Content = U.T("ServicesReconnect");
					}
					catch { }
				}
			}
			catch { }
		}

		#endregion

		#region Private

		/// <summary>
		/// Updates the service GUI according to link state of ServiceManager
		/// </summary>
		private void RefreshServiceGUI()
		{
			if (cloudBrowserIsReady)
				CloudBrowserIsLoading = false;
			if (ServiceManager.Linked)
			{
				CloudControlsAreVisible = true;
				BrowseTo(new Uri(ServiceManager.CallbackURLWithAuthParams));

				ConnectedLinks.Children.Clear();
				connectedLinks.Clear();
				linkNotices.Clear();
				NotConnectedLinks.Children.Clear();
				notConnectedLinks.Clear();
				RefreshLinks();
			}
			else
			{
				CloudControlsAreVisible = false;
				DeviceName.Visibility = Visibility.Collapsed;
				DeviceNameLoading.Visibility = Visibility.Visible;
				UserName.Visibility = Visibility.Collapsed;
				UserNameLoading.Visibility = Visibility.Visible;
				LinksLoading.Visibility = Visibility.Visible;
				BrowseTo(new Uri(ServiceManager.LogoutURL));
			}
			Synchronize.IsChecked = ServiceManager.Identity != null &&
				ServiceManager.Identity.ConfigurationID > 0;
			RefreshSyncGUI();
		}

		/// <summary>
		/// Refreshes the synchronization management controls.
		/// 
		/// Visible controls depend on number of profile:
		///     0: Only checkbox is shown
		///     1: Checkbox and expander shown
		///  more: Combo and expander shown
		/// </summary>
		private void RefreshSyncGUI()
		{
			bool c = ServiceManager.Synchronize;

			ServiceManager.PropertyChanged -= ServiceManager_PropertyChanged;

			Synchronize.IsChecked = c;

			if (c)
			{
				if (SynchronizePlaylists.IsChecked != ServiceManager.SynchronizePlaylists)
					SynchronizePlaylists.IsChecked = ServiceManager.SynchronizePlaylists;

				if (SynchronizeConfig.IsChecked != ServiceManager.SynchronizeConfiguration)
					SynchronizeConfig.IsChecked = ServiceManager.SynchronizeConfiguration;
			}


			ServiceManager.PropertyChanged += ServiceManager_PropertyChanged;

			Visibility v = Visibility.Visible;
			Visibility i = Visibility.Collapsed;

			SynchronizeManagement.Visibility = c ? v : i;
		}

		/// <summary>
		/// Refreshes the controls for managing links to third party services.
		/// </summary>
		private void RefreshLinks()
		{
			try
			{
				if (ServiceManager.Identity.Links == null)
					ServiceManager.Identity.Links = new List<Link>();

				notConnectedLinks.Clear();
				connectedLinks.Clear();

				foreach (Link l in ServiceManager.Identity.Links)
					if (l != null)
						BuildLink(l);

				LinksLoading.Visibility = Visibility.Collapsed;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "Services", "Could not refresh links: " + e.Message);
				RefreshLinks();
			}
		}

		/// <summary>
		/// Builds the control for managing a link to a third party.
		/// </summary>
		/// <param name="link">The link to the third party service</param>
		private void BuildLink(Link link)
		{
			try
			{
				if (link.Connected)
				{
					link.PropertyChanged -= Link_PropertyChanged;
					link.PropertyChanged += Link_PropertyChanged;

					// remove from unconnected if it exists there
					if (notConnectedLinks.ContainsKey(link.Provider))
						notConnectedLinks.Remove(link.Provider);

					if (connectedLinks.ContainsKey(link.Provider)) // adapt existing control
					{
						StackPanel sp = connectedLinks[link.Provider] as StackPanel;
						Dictionary<string, CheckBox> checkboxes = GetLinkCheckboxes(link);
						Thickness m = new Thickness(5);

						// update error visibility
						StackPanel spNotice = linkNotices[link.Provider];
						if (spNotice != null)
						{
							if (String.IsNullOrWhiteSpace(link.Error))
								spNotice.Visibility = Visibility.Collapsed;
							else
							{
								spNotice.Visibility = Visibility.Visible;
								spNotice.ToolTip = link.Error;
								relinkTimer.Start();
							}
						}

						// add or modify capabilities on the link controls
						if (link.CanShare && !checkboxes.ContainsKey("Share"))
							sp.Children.Insert(0, CreateLinkCheckBox(link.Provider, "Share", link.DoShare));
						else if (link.CanShare)
							checkboxes["Share"].IsChecked = link.DoShare;

						if (link.CanListen && !checkboxes.ContainsKey("Listen"))
							sp.Children.Insert(1, CreateLinkCheckBox(link.Provider, "Listen", link.DoListen));
						else if (link.CanListen)
							checkboxes["Listen"].IsChecked = link.DoListen;

						if (link.CanCreatePlaylist && !checkboxes.ContainsKey("CreatePlaylist"))
							sp.Children.Insert(3, CreateLinkCheckBox(link.Provider, "CreatePlaylist", link.DoCreatePlaylist));
						else if (link.CanCreatePlaylist)
							checkboxes["CreatePlaylist"].IsChecked = link.DoCreatePlaylist;

						if (link.CanDonate && !checkboxes.ContainsKey("Donate"))
							sp.Children.Insert(2, CreateLinkCheckBox(link.Provider, "Donate", link.DoDonate));
						else if (link.CanDonate)
							checkboxes["Donate"].IsChecked = link.DoDonate;
							
						// remove checkboxes for capabilities the link doesn't have
						if (!link.CanShare && checkboxes.ContainsKey("Share"))
							sp.Children.Remove(checkboxes["Share"]);
						if (!link.CanListen && checkboxes.ContainsKey("Listen"))
							sp.Children.Remove(checkboxes["Listen"]);
						if (!link.CanDonate && checkboxes.ContainsKey("Donate"))
							sp.Children.Remove(checkboxes["Donate"]);
						if (!link.CanCreatePlaylist && checkboxes.ContainsKey("CreatePlaylist"))
							sp.Children.Remove(checkboxes["CreatePlaylist"]);
					}
					else // create new controls
					{
						StackPanel sp = new StackPanel() { Orientation = Orientation.Vertical };
						Thickness m = new Thickness(5);

						DockPanel dp = new DockPanel();
						dp.Children.Add(new TextBlock { Text = link.Provider, FontSize = 14, Margin = new Thickness(0, 5, 0, 0) });
						StackPanel spNotice = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Right, Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
						DockPanel.SetDock(spNotice, Dock.Left);
						spNotice.Children.Add(new Image() { Source = Utilities.GetIcoImage("pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/Error.ico", 16, 16), Width = 16, Height = 16 });
						spNotice.Children.Add(new TextBlock() { Text = String.Format(U.T("ServicesLinkDisconnected"), link.Provider), FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5,0,5,0) });
						Button relink = new Button() { Content = U.T("ServicesReconnect"), FontSize = 10, Padding = new Thickness(10, 0, 10, 1), Tag = link.ConnectURL };
						relink.Click += new RoutedEventHandler(Relink_Click);
						spNotice.Children.Add(relink);
						if (String.IsNullOrWhiteSpace(link.Error))
							spNotice.Visibility = Visibility.Collapsed;
						else
						{
							spNotice.ToolTip = link.Error;
							relinkTimer.Start();
						}
						dp.Children.Add(spNotice);
						sp.Children.Add(dp);
						linkNotices[link.Provider] = spNotice;

						if (link.CanShare)
							sp.Children.Add(CreateLinkCheckBox(link.Provider, "Share", link.DoShare));
						if (link.CanListen)
							sp.Children.Add(CreateLinkCheckBox(link.Provider, "Listen", link.DoListen));
						if (link.CanCreatePlaylist)
							sp.Children.Add(CreateLinkCheckBox(link.Provider, "CreatePlaylist", link.DoCreatePlaylist));
						if (link.CanDonate)
							sp.Children.Add(CreateLinkCheckBox(link.Provider, "Donate", link.DoDonate));

						m = new Thickness(0, 5, 0, 15);
						if (link.CanShare || link.CanListen || link.CanDonate || link.CanCreatePlaylist)
							m.Top = 0;
						Button b = new Button();
						b.MinWidth = 80;
						b.Padding = new Thickness(15, 1, 15, 1);
						b.Margin = m;
						b.Content = U.T("ServicesUnlink", "Content");
						b.Tag = new string[] { link.Provider, link.URL, link.ConnectURL };
						b.Click += new RoutedEventHandler(Unlink_Click);
						b.HorizontalAlignment = HorizontalAlignment.Left;
						sp.Tag = new string[] { link.Provider, link.URL };
						sp.Children.Add(b);

						connectedLinks[link.Provider] = sp;
					}
				}
				else
				{
					// remove from connected if it exists there
					if (connectedLinks.ContainsKey(link.Provider))
						connectedLinks.Remove(link.Provider);

					if (notConnectedLinks.ContainsKey(link.Provider))
					{
						// adapt existing control
						Button b = notConnectedLinks[link.Provider] as Button;
						b.Content = String.Format(U.T("ServicesLink", "Content"), link.Provider);
						b.Tag = new string[] { link.Provider, link.ConnectURL };
					}
					else // create new controls
						notConnectedLinks[link.Provider] = CreateUnconnectedLink(link);
				}

				// make sure all and only existing links are in GUI
				for (int i = 0; i < notConnectedLinks.Count; i++)
				{
					if (NotConnectedLinks.Children.Count > i)
					{
						Button b = (Button)NotConnectedLinks.Children[i];
						string modl = notConnectedLinks.ElementAt(i).Key;
						string view = ((string[])b.Tag)[0];
						int cmpr = String.Compare(modl, view);
						if (cmpr > 0) // view needs to be removed
						{
							NotConnectedLinks.Children.RemoveAt(i--);
							continue;
						}
						if (cmpr < 0) // model needs to be added to view
							NotConnectedLinks.Children.Insert(i, notConnectedLinks.ElementAt(i).Value);
					}
					else
						NotConnectedLinks.Children.Add(notConnectedLinks.ElementAt(i).Value);
				}
				while (NotConnectedLinks.Children.Count > notConnectedLinks.Count)
					NotConnectedLinks.Children.RemoveAt(notConnectedLinks.Count);
				for (int i = 0; i < connectedLinks.Count; i++)
				{
					if (ConnectedLinks.Children.Count > i)
					{
						StackPanel sp = (StackPanel)ConnectedLinks.Children[i];
						string modl = connectedLinks.ElementAt(i).Key;
						string view = ((string[])sp.Tag)[0];
						int cmpr = String.Compare(modl, view);
						while (cmpr > 0) // view needs to be removed
						{
							ConnectedLinks.Children.RemoveAt(i--);
							continue;
						}
						if (cmpr < 0) // model needs to be added to view
							ConnectedLinks.Children.Insert(i, connectedLinks.ElementAt(i).Value);
					}
					else
						ConnectedLinks.Children.Add(connectedLinks.ElementAt(i).Value);
				}
				while (ConnectedLinks.Children.Count > connectedLinks.Count)
					ConnectedLinks.Children.RemoveAt(connectedLinks.Count);
			}
			catch (Exception e)
			{
			    U.L(LogLevel.Error, "CONTROL", "Could not build link controls for " + link.Provider + ": " + e.Message);
			}
		}

		/// <summary>
		/// Updates the border around the browser according to the current URL of the browser.
		/// </summary>
		private void UpdateBrowserBorder()
		{
			bool intSource = Browser.Source == null || Browser.Source.ToString().StartsWith(ServiceManager.Domain);
			BrowserBorder.BorderBrush = CloudControlsAreVisible || intSource ? Brushes.Transparent : Brushes.Black;
			BrowserBorder.Margin = CloudControlsAreVisible || intSource ? new Thickness(45, 5, 5, 5) : new Thickness(5);
		}

		/// <summary>
		/// Creates a checkbox for setting behaviour of a link to a third party.
		/// </summary>
		/// <param name="provider">The name of the provider of the link</param>
		/// <param name="name">The name of the setting which the checkbox should control</param>
		/// <param name="isChecked">Whether or not the checkbox should be checked</param>
		/// <returns>A checkbox which is setup to control the link setting</returns>
		private CheckBox CreateLinkCheckBox(string provider, string name, bool isChecked)
		{
			CheckBox cb = new CheckBox();
			cb.Tag = new string[] { provider, name };
			cb.Margin = new Thickness(5);
			cb.Content = U.T("ServicesCheck" + name, "Content");
			cb.IsChecked = isChecked;
			cb.Click += new RoutedEventHandler(LinkCheckBox_Click);
			return cb;
		}

		/// <summary>
		/// Creates a button for a given link which is not connected.
		/// </summary>
		/// <param name="link">The link object</param>
		/// <returns>A button which is used to represent an unconnected link and allows the user to connect it</returns>
		private Button CreateUnconnectedLink(Link link)
		{
			Button b = new Button();
			b.Content = String.Format(U.T("ServicesLink", "Content"), link.Provider);
			b.HorizontalAlignment = HorizontalAlignment.Left;
			b.MinWidth = 150;
			b.Padding = new Thickness(15, 1, 15, 1);
			b.Margin = new Thickness(0,5,5,5);
			b.Tag = new string[] { link.Provider, link.ConnectURL };
			b.Click += new RoutedEventHandler(Link_Click);
			return b;
		}

		/// <summary>
		/// Creates a list of all checkboxes for a link.
		/// </summary>
		/// <param name="link">The link to the third party</param>
		/// <returns>A list of checkbox controls indexed by their name</returns>
		private Dictionary<string, CheckBox> GetLinkCheckboxes(Link link)
		{
			Dictionary<string, CheckBox> checkboxes = new Dictionary<string, CheckBox>();
			Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
			{
				StackPanel sp = connectedLinks[link.Provider] as StackPanel;
				if (sp != null)
					foreach (FrameworkElement cb in sp.Children)
						if (cb is CheckBox)
							checkboxes[((string[])cb.Tag)[1]] = cb as CheckBox;
			}));
			return checkboxes;
		}

		/// <summary>
		/// Points the integrated browser to a specific URL.
		/// </summary>
		/// <param name="url">The URL to browse to</param>
		/// <param name="reconnecting">Whether or not the request is an automatic attempt to reconnect a link</param>
		private void BrowseTo(Uri url, bool reconnecting = false)
		{
			U.L(LogLevel.Debug, "service", "browse to: " + url.AbsoluteUri);
			string headers = "";

			CultureInfo ci = CultureInfo.GetCultureInfo(SettingsManager.Language);
			headers += String.Format("ACCEPT_LANGUAGE: {0}\r\n", ci.IetfLanguageTag);
			headers += String.Format("X_EMBEDDER: Stoffi Music Player/{0}-{0}\r\n", SettingsManager.Version, SettingsManager.Channel);

			if (ServiceManager.Linked && ServiceManager.Identity != null)
				headers += String.Format("X_DEVICE_ID: {0}\r\n", ServiceManager.Identity.DeviceID);

			isReconnecting = reconnecting;
			Browser.Navigate(url, "", null, headers);
		}

		/// <summary>
		/// Checks whether an URL is the callback of the cloud platform.
		/// </summary>
		/// <param name="url">The URL to check</param>
		/// <returns>Whether or not the URL is the endpoint when the cloud has been connected (when we should show the dashboard)</returns>
		private bool IsCallback(string url)
		{
			return (url.StartsWith(ServiceManager.CallbackURL) || url == ServiceManager.CallbackURLWithAuthParams);
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked at an interval while a link is down and needs to be relinked.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void RelinkTimer_Tick(object sender, EventArgs e)
		{
			if (ServiceManager.Identity == null)
			{
				relinkTimer.Stop();
				return;
			}

			try
			{
				bool allConnected = true;
				foreach (Link l in ServiceManager.Identity.Links)
				{
					if (String.IsNullOrWhiteSpace(l.Error))
						continue;

					if (l.Provider == "Facebook")
					{
						if (l.Error.StartsWith("Session has expired") ||
							l.Error == "Error validating access token: The session has been invalidated because the user has changed the password.")
						{
							BrowseTo(new Uri(l.ConnectURL), true);
							allConnected = false;
							break;
						}
					}
				}

				if (allConnected)
					relinkTimer.Stop();
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "SERVICES", "Could not reconnect link in cloud: " + exc.Message);
			}
		}

		/// <summary>
		/// Invoked when the user clicks "Reconnect" on a third party link.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Relink_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Button b = sender as Button;
				string url = b.Tag as string;
				BrowseTo(new Uri(url));
				CloudControlsAreVisible = false;
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "SERVICES", "Could not reconnect link in cloud: " + exc.Message);
			}
		}

		/// <summary>
		/// Invoked when the user clicks "Unlink" on a third party link.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Unlink_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Button b = sender as Button;
				if (b == null) return;

				string[] tag = b.Tag as string[];
				if (tag == null || tag.Length != 3) return;

				string provider = tag[0];
				string url = tag[1];

				StackPanel sp = connectedLinks[provider] as StackPanel;
				if (sp != null)
					ConnectedLinks.Children.Remove(sp);

				Link link = ServiceManager.GetLink(provider);
				if (link == null) return;

				// insert the link button at the correct lexical position
				bool inserted = false;
				for (int i = 0; i < NotConnectedLinks.Children.Count; i++)
				{
					try
					{
						string[] btag = ((Button)NotConnectedLinks.Children[i]).Tag as string[];
						if (String.Compare(btag[0].ToLower(), provider.ToLower()) > 0)
						{
							Button unlinkButton = CreateUnconnectedLink(link);
							NotConnectedLinks.Children.Insert(i, unlinkButton);
							notConnectedLinks[provider] = unlinkButton;
							inserted = true;
							break;
						}
					}
					catch { }
				}
				if (!inserted)
				{
					Button unlinkButton = CreateUnconnectedLink(link);
					NotConnectedLinks.Children.Add(unlinkButton);
					notConnectedLinks[provider] = unlinkButton;
				}

				ServiceManager.DeleteLink(link);
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "SERVICES", "Could not update link in cloud: " + exc.Message);
			}
		}

		/// <summary>
		/// Invoked when the user clicks the button to create a link to a third party.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Link_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Button b = sender as Button;
				if (b == null) return;

				string[] tag = b.Tag as string[];
				if (tag == null || tag.Length != 2) return;

				string url = tag[1];
				BrowseTo(new Uri(url));
				CloudControlsAreVisible = false;
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "SERVICES", "Could not update link in cloud: " + exc.Message);
			}
		}

		/// <summary>
		/// Invoked when the user clicks a checkbox of a link to a third party.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void LinkCheckBox_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				CheckBox cb = sender as CheckBox;
				if (cb == null) return;

				string[] tag = cb.Tag as string[];
				if (tag == null || tag.Length < 1) return;

				ThreadStart cloudThread = delegate()
				{
					Link link = ServiceManager.GetLink(tag[0]);
					if (link == null)
						U.L(LogLevel.Warning, "SERVICES", "Could not find link object of provider '" + tag[0] + "'");
					else
					{
						Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
						{
							Dictionary<string, CheckBox> checkboxes = GetLinkCheckboxes(link);
							link.DoShare = (bool)checkboxes["Share"].IsChecked;
							link.DoListen = (bool)checkboxes["Listen"].IsChecked;
							link.DoDonate = (bool)checkboxes["Donate"].IsChecked;
							link.DoCreatePlaylist = (bool)checkboxes["CreatePlaylist"].IsChecked;
						}));
						ServiceManager.UpdateLink(link);
					}
				};
				Thread cl_thread = new Thread(cloudThread);
				cl_thread.Name = "Link update";
				cl_thread.Priority = ThreadPriority.Lowest;
				cl_thread.Start();
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "SERVICES", "Could not update link in cloud: " + exc.Message);
			}
		}

		/// <summary>
		/// Invoked when the panel is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Services_Loaded(object sender, RoutedEventArgs e)
		{
		}

		/// <summary>
		/// Invoked when the service manager has been fully initialized.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ServiceManager_Initialized(object sender, EventArgs e)
		{
			Connect();
		}

		/// <summary>
		/// Invoked when the service browser is navigating to a new URL.
		/// Will attempt to start linking at ServiceManager if the browser
		/// is detected to get redirected back to the designated callback URL.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Browser_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			U.L(LogLevel.Debug, "SERVICE", "Navigating: " + e.Uri.AbsoluteUri);

			CloudBrowserIsLoading = true;
			linkingCloud = false;
			cloudBrowserIsReady = false;
			browserIsNavigating = true;

			string uri = e.Uri.AbsoluteUri;

			// callback but WITH query
			if (IsCallback(uri) && uri != ServiceManager.CallbackURL)
			{
				linkingCloud = true;

				// extra parameters, parse them and link accounts
				if (uri != ServiceManager.CallbackURLWithAuthParams)
					ServiceManager.Link(uri);
				else
					linkingCloud = false;
			}
			else if (IsCallback(uri))
			{
				ServiceManager.RetrieveLinkData();
			}
			else if (uri == ServiceManager.LogoutURL)
				loggingOutCloud = true;
		}

		/// <summary>
		/// Invoked when the service browser has navigated to a new URL.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Browser_Navigated(object sender, NavigationEventArgs e)
		{
			U.L(LogLevel.Debug, "SERVICE", "Navigated: " + e.Uri.AbsoluteUri);
			browserIsNavigating = false;

			Utilities.SetSilent(Browser, true);
			if (loggingOutCloud)
			{
				loggingOutCloud = false;
				ServiceManager.InitOAuth();
				BrowseTo(new Uri(ServiceManager.RequestURL));
				return;
			}

			ServiceManager.Ping(e.Uri);
		}

		/// <summary>
		/// Invoked when the service browser has loaded the page.
		/// Will hide the browser loading indicator and show the browser.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
		{
			U.L(LogLevel.Debug, "CONTROL", "Service browser completed loading");
			UpdateBrowserBorder();
			cloudBrowserIsReady = true;
			if (!linkingCloud)
			{
				CloudBrowserIsLoading = false;
				if (ServiceManager.Linked && IsCallback(Browser.Source.ToString()))
					CloudControlsAreVisible = true;
			}

			if (isReconnecting && !IsCallback(Browser.Source.ToString()))
			{
				isReconnecting = false;
				Connect();
				relinkTimer.Start();
			}
		}

		/// <summary>
		/// Invoked when the browser gets focus.
		/// Disables keyboard shortcuts in the application.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Browser_GotFocus(object sender, RoutedEventArgs e)
		{
			U.ListenForShortcut = false;
		}

		/// <summary>
		/// Invoked when the browser loses focus.
		/// Enables keyboard shortcuts in the application.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Browser_LostFocus(object sender, RoutedEventArgs e)
		{
			U.ListenForShortcut = true;
		}

		/// <summary>
		/// Invoked when the user clicks on Delink.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Delink_Click(object sender, RoutedEventArgs e)
		{
			ThreadStart cloudThread = delegate()
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					CloudBrowserIsLoading = true;
					CloudControlsAreVisible = false;
				}));
				ServiceManager.Delink();

				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					for (int i = 0; i < SettingsManager.Playlists.Count; i++)
					{
						var p = SettingsManager.Playlists[i];
						if (p.ID > 0)
						{
							PlaylistManager.RemovePlaylist(p.ID);
							i--;
						}
					}
				}));
			};
			Thread cl_thread = new Thread(cloudThread);
			cl_thread.Name = "Cloud delink thread";
			cl_thread.Priority = ThreadPriority.Lowest;
			cl_thread.Start();
		}

		/// <summary>
		/// Invoked when the user clicks on Reset.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			ThreadStart resetThread = delegate()
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					CloudControlsAreVisible = false;
					CloudBrowserIsLoading = true;
					if (ServiceManager.Linked)
					{
						BrowseTo(new Uri(ServiceManager.CallbackURLWithAuthParams));
					}
					else
					{
						ServiceManager.InitOAuth();
						BrowseTo(new Uri(ServiceManager.RequestURL));
					}
				}));
			};
			Thread rst_thread = new Thread(resetThread);
			rst_thread.Name = "Cloud reset";
			rst_thread.Priority = ThreadPriority.Lowest;
			rst_thread.Start();
		}

		/// <summary>
		/// Invoked when the user changes the device name.
		/// Will send the new name to the server.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void DeviceName_Edited(object sender, EditableTextBlockEventArgs e)
		{
			ServiceManager.DeviceName = e.NewText;
		}

		/// <summary>
		/// Invoked when the user clicks the checkbox to enable synchronization.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Synchronize_Click(object sender, RoutedEventArgs e)
		{
			ServiceManager.Synchronize = (bool)Synchronize.IsChecked;
		}

		/// <summary>
		/// Invoked when a property of a third party link changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Link_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
			{
				try
				{
					if (e.PropertyName == "Error")
					{
						Link link = sender as Link;
						StackPanel notice = linkNotices[link.Provider];
						if (String.IsNullOrWhiteSpace(link.Error))
							notice.Visibility = Visibility.Collapsed;
						else
						{
							notice.Visibility = Visibility.Visible;
							notice.ToolTip = link.Error;
							relinkTimer.Start();
							RelinkTimer_Tick(null, null);
						}
					}
				}
				catch (Exception exc)
				{
					U.L(LogLevel.Warning, "services", "Could not handle property change of link: " + exc.Message);
				}
			}));
		}

		/// <summary>
		/// Invoked when a property changes in the service manager.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ServiceManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				switch (e.PropertyName)
				{
					case "DeviceName":
						DeviceName.Edited -= DeviceName_Edited;
						DeviceName.Text = e.NewValue as string;
						DeviceNameLoading.Visibility = Visibility.Collapsed;
						DeviceName.Visibility = Visibility.Visible;
						DeviceName.Edited += DeviceName_Edited;
						break;

					case "UserName":
						UserName.Text = e.NewValue as string;
						UserNameLoading.Visibility = Visibility.Collapsed;
						UserName.Visibility = Visibility.Visible;
						break;

					case "Linked":
						U.L(LogLevel.Debug, "CONTROL", "Detected change in service link state");
						linkingCloud = false;
						RefreshServiceGUI();
						break;

					case "Links":
						U.L(LogLevel.Debug, "CONTROL", "Detected change in links to third parties");
						RefreshLinks();
						CloudBrowserIsLoading = false;
						break;

					case "Synchronize":
						RefreshSyncGUI();
						break;

					case "Connected":
						Connected.Visibility = ServiceManager.Connected ? Visibility.Visible : Visibility.Collapsed;
						Disconnected.Visibility = ServiceManager.Connected ? Visibility.Collapsed : Visibility.Visible;
						if (ServiceManager.Connected)
							Connect();
						break;
				}
			}));
		}

		/// <summary>
		/// Invoked when the realtime communication channel has been disconnected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ServiceManager_RealtimeDisconnected(object sender, EventArgs e)
		{
			try
			{
				if (!browserIsNavigating && Browser.Source != null && IsCallback(Browser.Source.ToString()))
				{
					U.L(LogLevel.Debug, "SERVICES", "Lost realtime connection so forcing browser to reload");
					BrowseTo(new Uri(ServiceManager.CallbackURLWithAuthParams));
				}
			}
			catch { }
		}

		/// <summary>
		/// Invoked when the realtime communication channel has been connected.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ServiceManager_RealtimeConnected(object sender, EventArgs e)
		{
		}

		#endregion

		#region Dispatchers
		#endregion

		#endregion

		#region Events
		#endregion
	}
}
