/**
 * General.xaml.cs
 * 
 * The "General" screen inside the "Control Panel".
 * It shows general settings for appearance and behavior.
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

using Stoffi.Core;
using Stoffi.Core.Settings;
using Stoffi.Core.Upgrade;
using SettingsManager = Stoffi.Core.Settings.Manager;
using UpgradeManager = Stoffi.Core.Upgrade.Manager;

namespace Stoffi.Player.GUI.Controls.ControlPanel
{
	/// <summary>
	/// Interaction logic for General.xaml
	/// </summary>
	public partial class General : ScrollViewer
	{
		#region Members
		
		private UpgradePolicy upgradePolicy = SettingsManager.UpgradePolicy;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the policy for adding items opened with Stoffi.
		/// </summary>
		public OpenAddPolicy ExternalClickAdd
		{
			get { return SettingsManager.OpenAddPolicy; }
			set { SettingsManager.OpenAddPolicy = value; }
		}

		/// <summary>
		/// Gets or sets the policy for playing items opened with Stoffi.
		/// </summary>
		public OpenPlayPolicy ExternalClickPlay
		{
			get { return SettingsManager.OpenPlayPolicy; }
			set { SettingsManager.OpenPlayPolicy = value; }
		}

		/// <summary>
		/// Gets or sets the policy for downloading and installing upgrades of Stoffi.
		/// </summary>
		public UpgradePolicy UpgradePolicy
		{
			get { return upgradePolicy; }
			set
			{
				upgradePolicy = value;
				SettingsManager.UpgradePolicy = value;
				DoUpgrade.Visibility = System.Windows.Visibility.Collapsed;

				if (value == UpgradePolicy.Manual)
					EnableUpgradeCheck();

				else // Notify or Automatic
				{
					CheckForUpgrades.Visibility = System.Windows.Visibility.Collapsed;
					UpgradeProgress.Visibility = System.Windows.Visibility.Collapsed;
				}
			}
		}

		/// <summary>
		/// Gets or sets the policy for applying search filters to various lists in Stoffi.
		/// </summary>
		public SearchPolicy SearchPolicy
		{
			get { return SettingsManager.SearchPolicy; }
			set { SettingsManager.SearchPolicy = value; }
		}

		/// <summary>
		/// Gets or sets whether or not to show a notification when a new song is played.
		/// </summary>
		public bool ShowOSD
		{
			get { return SettingsManager.ShowOSD; }
			set { SettingsManager.ShowOSD = value; }
		}

		/// <summary>
		/// Gets or sets whether to preload Stoffi into memory when it's not running.
		/// </summary>
		public bool FastStart
		{
			get { return SettingsManager.FastStart; }
			set { SettingsManager.FastStart = value; }
		}

		/// <summary>
		/// Gets or sets whether to pause playback while computer is locked
		/// </summary>
		public bool PauseWhenLocked
		{
			get { return SettingsManager.PauseWhenLocked; }
			set { SettingsManager.PauseWhenLocked = value; }
		}

		/// <summary>
		/// Gets or sets whether to pause playback when the currently playing song reaches the end.
		/// </summary>
		public bool PauseWhenSongEnds
		{
			get { return SettingsManager.PauseWhenSongEnds; }
			set { SettingsManager.PauseWhenSongEnds = value; }
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the Control Panel screen Plugins.
		/// </summary>
		public General()
		{
			InitializeComponent();

			if (SettingsManager.UpgradePolicy == UpgradePolicy.Manual)
				CheckForUpgrades.Visibility = System.Windows.Visibility.Visible;

			// create languages
			foreach (ComboBoxItem cbi in Language.Items)
			{
				if ((string)cbi.Tag == SettingsManager.Language)
				{
					Language.SelectedItem = cbi;
					break;
				}
			}

			UpgradePolicyDescriptionConverter uconv = new UpgradePolicyDescriptionConverter();
			string upolicy = (string)uconv.Convert(this.UpgradePolicy, null, null, null);

			SearchPolicyDescriptionConverter sconv = new SearchPolicyDescriptionConverter();
			string spolicy = (string)sconv.Convert(SearchPolicy, null, null, null);

			OpenFileAddDescriptionConverter aconv = new OpenFileAddDescriptionConverter();
			string apolicy = (string)aconv.Convert(ExternalClickAdd, null, null, null);

			OpenFilePlayDescriptionConverter pconv = new OpenFilePlayDescriptionConverter();
			string ppolicy = (string)pconv.Convert(ExternalClickPlay, null, null, null);

			foreach (ComboBoxItem cbi in SearchPolicyCombo.Items)
			{
				if ((string)cbi.Content == spolicy)
				{
					SearchPolicyCombo.SelectedItem = cbi;
					break;
				}
			}

			foreach (ComboBoxItem cbi in UpgradePolicyCombo.Items)
			{
				if ((string)cbi.Content == upolicy)
				{
					UpgradePolicyCombo.SelectedItem = cbi;
					break;
				}
			}

			foreach (ComboBoxItem cbi in AddPolicyCombo.Items)
			{
				if ((string)cbi.Content == apolicy)
				{
					AddPolicyCombo.SelectedItem = cbi;
					break;
				}
			}

			foreach (ComboBoxItem cbi in PlayPolicyCombo.Items)
			{
				if ((string)cbi.Content == ppolicy)
				{
					PlayPolicyCombo.SelectedItem = cbi;
					break;
				}
			}

			SearchPolicyCombo.SelectionChanged += new SelectionChangedEventHandler(SearchPolicyCombo_SelectionChanged);
			UpgradePolicyCombo.SelectionChanged += new SelectionChangedEventHandler(UpgradePolicyCombo_SelectionChanged);
			AddPolicyCombo.SelectionChanged += new SelectionChangedEventHandler(AddPolicyCombo_SelectionChanged);
			PlayPolicyCombo.SelectionChanged += new SelectionChangedEventHandler(PlayPolicyCombo_SelectionChanged);
			Language.SelectionChanged += new SelectionChangedEventHandler(Language_SelectionChanged);
			UpgradeManager.Checked += new EventHandler(UpgradeManager_Checked);
			UpgradeManager.ErrorOccured += new ErrorEventHandler(UpgradeManager_ErrorOccured);
			UpgradeManager.ProgressChanged += new ProgressChangedEventHandler(UpgradeManager_ProgressChanged);
			UpgradeManager.Upgraded += new EventHandler(UpgradeManager_Upgraded);
			UpgradeManager.UpgradeFound += new EventHandler(UpgradeManager_UpgradeFound);
			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Refreshes the strings according to current Culture language.
		/// </summary>
		public void RefreshStrings()
		{
		}

		/// <summary>
		/// Enables the button to perform an upgrade.
		/// </summary>
		public void EnableUpgradeDo()
		{
			U.L(LogLevel.Debug, "CONTROL", "Enabling upgrade button");
			DoUpgrade.Visibility = System.Windows.Visibility.Visible;
			DoUpgrade.IsEnabled = true;
			DoUpgrade.Content = U.T("GeneralDoUpgrade", "Content");

			CheckForUpgrades.Visibility = System.Windows.Visibility.Collapsed;
			Restart.Visibility = System.Windows.Visibility.Collapsed;
			UpgradeProgress.Visibility = System.Windows.Visibility.Collapsed;
			OnCanDoUpgrade();
		}

		/// <summary>
		/// Enables the button to check for an upgrade.
		/// </summary>
		public void EnableUpgradeCheck()
		{
			U.L(LogLevel.Debug, "CONTROL", "Enabling upgrade check button");
			CheckForUpgrades.Visibility = System.Windows.Visibility.Visible;
			CheckForUpgrades.IsEnabled = true;
			CheckForUpgrades.Content = U.T("GeneralDoCheck", "Content");


			DoUpgrade.Visibility = System.Windows.Visibility.Collapsed;
			Restart.Visibility = System.Windows.Visibility.Collapsed;
			UpgradeProgress.Visibility = System.Windows.Visibility.Collapsed;
			OnCanCheckUpgrade();

		}

		/// <summary>
		/// Enables the button to restart Stoffi.
		/// </summary>
		public void EnableRestart()
		{
			U.L(LogLevel.Debug, "CONTROL", "Enabling restart button");
			Restart.Visibility = System.Windows.Visibility.Visible;

			CheckForUpgrades.Visibility = System.Windows.Visibility.Collapsed;
			DoUpgrade.Visibility = System.Windows.Visibility.Collapsed;
			UpgradeProgress.Visibility = System.Windows.Visibility.Collapsed;
			OnCanRestart();
		}

		#endregion

		#region Private

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the control is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void General_Loaded(object sender, RoutedEventArgs e)
		{
			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
				Title.Style = (Style)FindResource("ClassicControlPanelTitleStyle");
		}

		/// <summary>
		/// Invoked when theuser clicks to check for upgrades
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void CheckForUpgrades_Click(object sender, RoutedEventArgs e)
		{
			UpgradeMessageClose_MouseLeftButtonDown(null, null);

			CheckForUpgrades.IsEnabled = false;
			CheckForUpgrades.Content = U.T("UpgradeWait");

			UpgradeProgressLabel.Text = U.T("UpgradeConnecting");
			UpgradeProgressBar.IsIndeterminate = true;
			UpgradeProgress.Visibility = System.Windows.Visibility.Visible;

			ThreadStart InitUpgradeManagerThread = delegate()
			{
				UpgradeManager.Probe(null);
			};
			Thread um_thread = new Thread(InitUpgradeManagerThread);
			um_thread.Priority = ThreadPriority.BelowNormal;
			um_thread.Start();
		}

		/// <summary>
		/// Invoked when the user clicks to perform an upgrade
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void DoUpgrade_Click(object sender, RoutedEventArgs e)
		{
			UpgradeMessageClose_MouseLeftButtonDown(null, null);

			DoUpgrade.IsEnabled = false;
			DoUpgrade.Content = U.T("UpgradeWait");

			UpgradeProgressLabel.Text = U.T("UpgradeWait");
			UpgradeProgressBar.IsIndeterminate = false;
			UpgradeProgressBar.Value = 0;
			UpgradeProgress.Visibility = System.Windows.Visibility.Visible;
			UpgradeProgressInfo.Visibility = System.Windows.Visibility.Visible;

			ThreadStart InitUpgradeManagerThread = delegate()
			{
				UpgradeManager.ForceDownload = true;
				UpgradeManager.Probe(null);
			};
			Thread um_thread = new Thread(InitUpgradeManagerThread);
			um_thread.Priority = ThreadPriority.BelowNormal;
			um_thread.Start();
		}

		/// <summary>
		/// Invoked when the user clicks on the restart button
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Restart_Click(object sender, RoutedEventArgs e)
		{
			OnRestartClick(e);
		}

		/// <summary>
		/// Invoked when the user changes search policy
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SearchPolicyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SearchPolicyDescriptionConverter conv = new SearchPolicyDescriptionConverter();
			ComboBoxItem cbi = (ComboBoxItem)SearchPolicyCombo.SelectedValue;
			SearchPolicy = (SearchPolicy)conv.ConvertBack((string)cbi.Content, null, null, null);
		}

		/// <summary>
		/// Invoked when the user changes search policy
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void UpgradePolicyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpgradeMessage.Visibility = System.Windows.Visibility.Collapsed;
			UpgradePolicyDescriptionConverter conv = new UpgradePolicyDescriptionConverter();
			ComboBoxItem cbi = (ComboBoxItem)UpgradePolicyCombo.SelectedValue;
			UpgradePolicy = (UpgradePolicy)conv.ConvertBack((string)cbi.Content, null, null, null);
		}

		/// <summary>
		/// Invoked when the user changes search policy
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void AddPolicyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			OpenFileAddDescriptionConverter conv = new OpenFileAddDescriptionConverter();
			ComboBoxItem cbi = (ComboBoxItem)AddPolicyCombo.SelectedValue;
			ExternalClickAdd = (OpenAddPolicy)conv.ConvertBack((string)cbi.Content, null, null, null);
		}

		/// <summary>
		/// Invoked when the user changes search policy
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PlayPolicyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			OpenFilePlayDescriptionConverter conv = new OpenFilePlayDescriptionConverter();
			ComboBoxItem cbi = (ComboBoxItem)PlayPolicyCombo.SelectedValue;
			ExternalClickPlay = (OpenPlayPolicy)conv.ConvertBack((string)cbi.Content, null, null, null);
		}

		/// <summary>
		/// Invoked when the user selects a different language
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBoxItem cbi = Language.SelectedItem as ComboBoxItem;
			SettingsManager.Language = (string)cbi.Tag;
		}

		/// <summary>
		/// Invoked when the user clicks close on a upgrade message
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void UpgradeMessageClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			UpgradeMessage.Visibility = System.Windows.Visibility.Collapsed;
		}

		/// <summary>
		/// Invoked when the progress of an upgrade changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void UpgradeManager_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate()
			{
				ProgressState ps = e.UserState as ProgressState;
				UpgradeProgressInfo.Text = ps.Message;
				UpgradeProgressBar.Value = e.ProgressPercentage;
				UpgradeProgressBar.IsIndeterminate = ps.IsIndetermined;
			}));
		}

		/// <summary>
		/// Invoked when an error occurs during an upgrade
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="message">The error message</param>
		private void UpgradeManager_ErrorOccured(object sender, string message)
		{
			Dispatcher.BeginInvoke(new Action(delegate
			{

				if (SettingsManager.UpgradePolicy == UpgradePolicy.Manual || UpgradeManager.ForceDownload)
				{
					UpgradeMessageText.Text = message;
					UpgradeMessageIcon.Source = new BitmapImage(new Uri("/Images/Icons/Error.ico", UriKind.RelativeOrAbsolute));
					UpgradeMessage.Visibility = System.Windows.Visibility.Visible;

					if (UpgradeManager.Policy == UpgradePolicy.Manual)
						EnableUpgradeCheck();
					else
						EnableUpgradeDo();
				}
			}));
		}

		/// <summary>
		/// Invoked when an upgrade is found
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void UpgradeManager_UpgradeFound(object sender, EventArgs e)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate()
			{
				UpgradeMessageText.Text = U.T("UpgradeFound");
				UpgradeMessageIcon.Source = new BitmapImage(new Uri("/Images/Icons/Info.ico", UriKind.RelativeOrAbsolute));
				UpgradeMessage.Visibility = System.Windows.Visibility.Visible;
				EnableUpgradeDo();

				if (SettingsManager.UpgradePolicy == UpgradePolicy.Notify)
					DoUpgrade.Visibility = Visibility.Visible;
			}));
		}

		/// <summary>
		/// Invoked when the application has been upgraded
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void UpgradeManager_Upgraded(object sender, EventArgs e)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate()
			{
				DoUpgrade.Visibility = Visibility.Collapsed;
				UpgradeProgress.Visibility = Visibility.Collapsed;
				if (SettingsManager.UpgradePolicy == UpgradePolicy.Manual)
				{
					EnableRestart();
					UpgradeMessageText.Text = U.T("UpgradePending");
					UpgradeMessageIcon.Source = new BitmapImage(new Uri("/Images/Icons/Info.ico", UriKind.RelativeOrAbsolute));
					UpgradeMessage.Visibility = System.Windows.Visibility.Visible;
				}
				else
					UpgradeMessage.Visibility = Visibility.Collapsed;
			}));
		}

		/// <summary>
		/// Invoked when a check for upgrades has been performed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void UpgradeManager_Checked(object sender, EventArgs e)
		{
			U.L(LogLevel.Debug, "MAIN", "Upgrade manager completed check");
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate()
			{
				if (UpgradeManager.Policy == UpgradePolicy.Manual && !UpgradeManager.Found)
				{
					EnableUpgradeCheck();
					UpgradeMessageIcon.Source = new BitmapImage(new Uri("/Images/Icons/Info.ico", UriKind.RelativeOrAbsolute));
					UpgradeMessageText.Text = U.T("UpgradeNotFound");
					UpgradeMessage.Visibility = System.Windows.Visibility.Visible;
				}
			}));
		}

		/// <summary>
		/// Invoked when a property of the settings manager changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate()
			{
				switch (e.PropertyName)
				{
					case "FastStart":
						DoFastStart.IsChecked = SettingsManager.FastStart;
						break;

					case "ShowOSD":
						OSD.IsChecked = SettingsManager.ShowOSD;
						break;

					case "OpenAddPolicy":
						OpenFileAddDescriptionConverter aconv = new OpenFileAddDescriptionConverter();
						string apolicy = (string)aconv.Convert(SettingsManager.OpenAddPolicy, null, null, null);

						foreach (ComboBoxItem cbi in AddPolicyCombo.Items)
						{
							if ((string)cbi.Content == apolicy)
							{
								AddPolicyCombo.SelectedItem = cbi;
								break;
							}
						}
						break;

					case "OpenPlayPolicy":
						OpenFilePlayDescriptionConverter pconv = new OpenFilePlayDescriptionConverter();
						string ppolicy = (string)pconv.Convert(SettingsManager.OpenPlayPolicy, null, null, null);

						foreach (ComboBoxItem cbi in PlayPolicyCombo.Items)
						{
							if ((string)cbi.Content == ppolicy)
							{
								PlayPolicyCombo.SelectedItem = cbi;
								break;
							}
						}
						break;

					case "SearchPolicy":
						SearchPolicyDescriptionConverter sconv = new SearchPolicyDescriptionConverter();
						string spolicy = (string)sconv.Convert(SettingsManager.SearchPolicy, null, null, null);

						foreach (ComboBoxItem cbi in SearchPolicyCombo.Items)
						{
							if ((string)cbi.Content == spolicy)
							{
								SearchPolicyCombo.SelectedItem = cbi;
								break;
							}
						}
						break;

					case "UpgradePolicy":
						UpgradePolicyDescriptionConverter uconv = new UpgradePolicyDescriptionConverter();
						string upolicy = (string)uconv.Convert(SettingsManager.UpgradePolicy, null, null, null);

						foreach (ComboBoxItem cbi in UpgradePolicyCombo.Items)
						{
							if ((string)cbi.Content == upolicy)
							{
								UpgradePolicyCombo.SelectedItem = cbi;
								break;
							}
						}
						break;
				}
			}));
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the CanDoUpgrade event.
		/// </summary>
		private void OnCanDoUpgrade()
		{
			if (CanDoUpgrade != null)
				CanDoUpgrade(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the CanCheckUpgrade event.
		/// </summary>
		private void OnCanCheckUpgrade()
		{
			if (CanCheckUpgrade != null)
				CanCheckUpgrade(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the CanRestart event.
		/// </summary>
		private void OnCanRestart()
		{
			if (CanRestart != null)
				CanRestart(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the RestartClick event.
		/// </summary>
		private void OnRestartClick(RoutedEventArgs e)
		{
			if (RestartClick != null)
				RestartClick(this, e);
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the button to perform an upgrade appears.
		/// </summary>
		public event EventHandler CanDoUpgrade;

		/// <summary>
		/// Occurs when the button to check for an upgrade appears.
		/// </summary>
		public event EventHandler CanCheckUpgrade;

		/// <summary>
		/// Occurs when the button to restart after an upgrade appears.
		/// </summary>
		public event EventHandler CanRestart;

		/// <summary>
		/// Occurs when the button to restart after an upgrade is clicked.
		/// </summary>
		public event RoutedEventHandler RestartClick;

		#endregion

		#endregion
	}

	#region Converters

	#endregion
}
