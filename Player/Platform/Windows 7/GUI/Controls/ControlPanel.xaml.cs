/**
 * ControlPanel.xaml.cs
 * 
 * The "Control Panel" screen used to show all the preferences
 * of Stoffi.
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
using System.Reflection;
using System.Runtime.InteropServices;
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

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for ControlPanel.xaml
	/// </summary>
	public partial class ControlPanel : UserControl
	{
		#region Members

		public StoffiWindow ParentWindow;
		private OpenAddPolicy openAddPolicy;
		private OpenPlayPolicy openFilePlay;
		private UpgradePolicy upgradePolicy;
		private SearchPolicy searchPolicy;
		private Boolean waitingForShortcut = false;
		private Button currentShortcutButton = null;
		private KeyboardShortcut currentShortcut = null;
		private List<Key> currentPressedKeys = new List<Key>();
		private List<TextBlock> ShortcutTitles = new List<TextBlock>();
		private List<TextBlock> ShortcutDescriptions = new List<TextBlock>();
		private List<Label> ShortcutLabels = new List<Label>();
		private Hashtable shortcutButtons = new Hashtable();
		private Hashtable shortcutCheckBoxes = new Hashtable();
		private Hashtable tabs = new Hashtable();
		private Hashtable tabLinks = new Hashtable();
		private MenuItem menuToggle;
		private MenuItem menuRemove;
		private ContextMenu sourceMenu;
		private Label GlobalLabel = new Label();
		private bool initialized = false;

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public OpenAddPolicy ExternalClickAdd
		{
			get { return openAddPolicy; }
			set
			{
				openAddPolicy = value;
				SettingsManager.OpenAddPolicy = value;
				Save();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public OpenPlayPolicy ExternalClickPlay
		{
			get { return openFilePlay; }
			set
			{
				openFilePlay = value;
				SettingsManager.OpenPlayPolicy = value;
				Save();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public UpgradePolicy UpgradePolicy
		{
			get { return upgradePolicy; }
			set
			{
				upgradePolicy = value;
				SettingsManager.UpgradePolicy = value;
				PrefDoUpgrade.Visibility = System.Windows.Visibility.Collapsed;
				Save();

				if (value == Stoffi.UpgradePolicy.Manual)
					EnableUpgradeCheck();

				else // Notify or Automatic
				{
					PrefCheckForUpgrades.Visibility = System.Windows.Visibility.Collapsed;
					UpgradeProgress.Visibility = System.Windows.Visibility.Collapsed;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public SearchPolicy SearchPolicy
		{
			get { return searchPolicy; }
			set
			{
				searchPolicy = value;
				SettingsManager.SearchPolicy = value;
				Save();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool ShowOSD
		{
			get { return SettingsManager.ShowOSD; }
			set { SettingsManager.ShowOSD = value; Save(); }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool MinimizeToTray
		{
			get { return SettingsManager.MinimizeToTray; }
			set { SettingsManager.MinimizeToTray = value; Save(); }
		}

		/// <summary>
		/// Gets or sets whether to pause playback while computer is locked
		/// </summary>
		public bool PauseWhenLocked
		{
			get { return SettingsManager.PauseWhenLocked; }
			set { SettingsManager.PauseWhenLocked = value; Save(); }
		}

		/// <summary>
		/// Gets or sets whether to pause playback when the currently playing song reaches the end.
		/// </summary>
		public bool PauseWhenSongEnds
		{
			get { return SettingsManager.PauseWhenSongEnds; }
			set { SettingsManager.PauseWhenSongEnds = value; Save(); }
		}

		/// <summary>
		/// Gets or sets whether to submit songs when they are played
		/// </summary>
		public bool SubmitSongs
		{
			get { return SettingsManager.SubmitSongs; }
			set { SettingsManager.SubmitSongs = value; }
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize configuration.
		/// </summary>
		public bool SyncConfig
		{
			get { return ServiceManager.SynchronizeConfiguration; }
			set { ServiceManager.SynchronizeConfiguration = value; }
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize playlists.
		/// </summary>
		public bool SyncPlaylists
		{
			get { return ServiceManager.SynchronizePlaylists; }
			set { ServiceManager.SynchronizePlaylists = value; }
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a control panel
		/// </summary>
		public ControlPanel()
		{
			U.L(LogLevel.Debug, "CONTROL PANEL", "Initialize");
			InitializeComponent();
			U.L(LogLevel.Debug, "CONTROL PANEL", "Initialized");

			openAddPolicy = SettingsManager.OpenAddPolicy;
			openFilePlay = SettingsManager.OpenPlayPolicy;
			upgradePolicy = SettingsManager.UpgradePolicy;
			searchPolicy = SettingsManager.SearchPolicy;

			this.DataContext = this;

			tabs.Add(Tab.General, ControlPanelGeneral);
			tabs.Add(Tab.Sources, ControlPanelSources);
            tabs.Add(Tab.Services, Services);
			tabs.Add(Tab.Plugins, Plugins);
			tabs.Add(Tab.Shortcuts, ControlPanelShortcuts);
			tabs.Add(Tab.About, ControlPanelAbout);
			tabLinks.Add(Tab.General, ControlPanelLink_General);
			tabLinks.Add(Tab.Sources, ControlPanelLink_Sources);
            tabLinks.Add(Tab.Services, ControlPanelLink_Services);
            tabLinks.Add(Tab.Plugins, ControlPanelLink_Plugins);
			tabLinks.Add(Tab.Shortcuts, ControlPanelLink_Shortcuts);
			tabLinks.Add(Tab.About, ControlPanelLink_About);

			menuRemove = new MenuItem();
			menuRemove.Header = U.T("MenuRemove");
			menuRemove.Click += new RoutedEventHandler(menuRemove_Click);

			menuToggle = new MenuItem();
			menuToggle.Header = U.T("MenuIgnore");
			menuToggle.Click += new RoutedEventHandler(menuToggle_Click);

			sourceMenu = new ContextMenu();
			sourceMenu.Items.Add(menuToggle);
			sourceMenu.Items.Add(menuRemove);

			SourceList.ContextMenu = sourceMenu;

			SettingsManager.PropertyChanged += new PropertyChangedWithValuesEventHandler(SettingsManager_PropertyChanged);

			U.L(LogLevel.Debug, "CONTROL PANEL", "Created");
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initializes the controls for the shortcuts
		/// </summary>
		public void InitShortcuts()
		{
			String[,] categories = new String[4, 3]; // name, translation id (title), translation id (text
			categories[0, 0] = "Application";
			categories[0, 1] = "ShortcutApplicationTitle";
			categories[0, 2] = "ShortcutApplicationText";

			categories[1, 0] = "MainWindow";
			categories[1, 1] = "ShortcutMainWindowTitle";
			categories[1, 2] = "ShortcutMainWindowText";

			categories[2, 0] = "MediaCommands";
			categories[2, 1] = "ShortcutMediaCommandsTitle";
			categories[2, 2] = "ShortcutMediaCommandsText";

			categories[3, 0] = "Track";
			categories[3, 1] = "ShortcutTrackTitle";
			categories[3, 2] = "ShortcutTrackText";

			for (int i = 0; i < 4; i++)
			{
				DockPanel d = new DockPanel() { Margin = new Thickness(25, 15, 0, 5), LastChildFill = true };

				TextBlock t = new TextBlock() { Text = U.T(categories[i, 1]) };
				t.Tag = categories[i, 1];
				DockPanel.SetDock(t, Dock.Left);
				ShortcutTitles.Add(t);
				d.Children.Add(t);

				Separator s = new Separator();
				s.Background = Brushes.LightGray;
				s.Height = 1;
				s.Margin = new Thickness(5, 0, 5, 0);
				DockPanel.SetDock(s, Dock.Left);
				d.Children.Add(s);

				DockPanel.SetDock(d, Dock.Top);
				ShortcutPanel.Children.Add(d);

				TextBlock tb = new TextBlock();
				tb.Margin = new Thickness(50, 5, 0, 5);
				tb.TextWrapping = TextWrapping.Wrap;
				tb.Inlines.Add(U.T(categories[i, 2]));
				tb.Tag = categories[i, 2];
				ShortcutDescriptions.Add(tb);
				DockPanel.SetDock(tb, Dock.Top);
				ShortcutPanel.Children.Add(tb);

				GridLengthConverter conv = new GridLengthConverter();
				Grid g = new Grid();
				g.Margin = new Thickness(50, 5, 0, 5);
				g.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)conv.ConvertFrom(170) });
				g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
				g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
				g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

				int j;
				KeyboardShortcutProfile profile = SettingsManager.GetCurrentShortcutProfile();
				for (j = 0; j < profile.Shortcuts.Count; j++)
					g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

				if (categories[i, 0] == "MediaCommands")
				{
					GlobalLabel.Content = U.T("ShortcutGlobal");
					GlobalLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
					Grid.SetColumn(GlobalLabel, 2);
					Grid.SetRow(GlobalLabel, 0);
					g.Children.Add(GlobalLabel);
				}

				j = 1;
				foreach (KeyboardShortcut sc in profile.Shortcuts)
				{
					// skip now playing
					if (sc.Name == "Now playing") continue;

					if (sc.Category != categories[i, 0]) continue;
					Label l = new Label() { Content = U.T("Shortcut_" + sc.Name.Replace(" ","_")) };
					l.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
					l.Tag = "Shortcut_" + sc.Name.Replace(" ", "_");
					Grid.SetColumn(l, 0);
					Grid.SetRow(l, j);
					g.Children.Add(l);
					ShortcutLabels.Add(l);

					Button b = new Button() { Content = sc.Keys, MinWidth = 100 };
					b.Name = sc.Category + "_" + sc.Name.Replace(" ", "_");
					b.LostFocus += PrefShortcutButton_LostFocus;
					b.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
					b.Margin = new Thickness(2);
					b.Click += PrefShortcutButton_Clicked;
					shortcutButtons.Add(b.Name, b);
					Grid.SetColumn(b, 1);
					Grid.SetRow(b, j);
					g.Children.Add(b);

					if (categories[i, 0] == "MediaCommands")
					{
						CheckBox cb = new CheckBox() { IsChecked = sc.IsGlobal, Name = b.Name, ToolTip = U.T("ShortcutGlobal", "ToolTip") };
						cb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						cb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
						cb.Margin = new Thickness(10, 0, 10, 0);
						cb.Click += PrefShortcutGlobal_Clicked;
						shortcutCheckBoxes.Add(b.Name, cb);
						Grid.SetColumn(cb, 2);
						Grid.SetRow(cb, j);
						g.Children.Add(cb);
					}

					j++;
				}

				DockPanel.SetDock(g, Dock.Top);
				ShortcutPanel.Children.Add(g);
			}

			((Grid)ShortcutPanel.Children[ShortcutPanel.Children.Count - 1]).Margin = new Thickness(50, 5, 0, 25);

			string selTxt = SettingsManager.CurrentShortcutProfile;
			int sel = 0;
			foreach (KeyboardShortcutProfile p in SettingsManager.ShortcutProfiles)
			{
				if (selTxt == p.Name)
					sel = PrefShortcutProfile.Items.Count;
				PrefShortcutProfile.Items.Add(new ComboBoxItem() { Content = p.Name });
			}
			PrefShortcutProfile.SelectedIndex = sel;
		}

		/// <summary>
		/// Updates the timestamp for the latest check for upgrades
		/// </summary>
		public void UpdateUpgradeCheck()
		{
			try
			{
				string text = Unix2Date(SettingsManager.UpgradeCheck);
				AboutUpgradeCheck.Text = text;
				if (UpgradeManager.Pending)
				{
					AboutUpgradePending.Visibility = System.Windows.Visibility.Visible;
					ParentWindow.UpgradeButton.Visibility = Visibility.Visible;
				}
			}
			catch
			{
				AboutUpgradeCheck.Text = U.T("UpgradeNotChecked");
			}
			if (SettingsManager.UpgradeCheck == 0)
				AboutUpgradeCheck.Text = U.T("UpgradeNotChecked");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timestamp"></param>
		/// <returns></returns>
		public String Unix2Date(long timestamp)
		{
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
			dt = dt.AddSeconds(timestamp);
			dt = dt.AddSeconds((DateTime.Now - DateTime.UtcNow).TotalSeconds);
			return String.Format("{0:ddd, MMM d, yyy HH:mm}", dt);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timestamp"></param>
		/// <returns></returns>
		public String Version2String(long timestamp)
		{
			int v1 = (int)(timestamp / 1000000000);
			int v2 = (int)(timestamp / 10000000) % 100;
			int v3 = (int)(timestamp / 10000) % 1000;
			int v4 = (int)(timestamp % 10000);
			return v1 + "." + v2 + "." + v3 + "." + v4;
		}

		/// <summary>
		/// 
		/// </summary>
		public void EnableUpgradeDo()
		{
			U.L(LogLevel.Debug, "CONTROL", "Enabling upgrade button");
			PrefDoUpgrade.Visibility = System.Windows.Visibility.Visible;
			PrefDoUpgrade.IsEnabled = true;
			PrefDoUpgrade.Content = U.T("GeneralDoUpgrade", "Content");

			PrefCheckForUpgrades.Visibility = System.Windows.Visibility.Collapsed;
			Restart.Visibility = System.Windows.Visibility.Collapsed;
			UpgradeProgress.Visibility = System.Windows.Visibility.Collapsed;
			if (ParentWindow.TaskbarItemInfo != null)
				ParentWindow.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
		}

		/// <summary>
		/// 
		/// </summary>
		public void EnableUpgradeCheck()
		{
			U.L(LogLevel.Debug, "CONTROL", "Enabling upgrade check button");
			PrefCheckForUpgrades.Visibility = System.Windows.Visibility.Visible;
			PrefCheckForUpgrades.IsEnabled = true;
			PrefCheckForUpgrades.Content = U.T("GeneralDoCheck", "Content");


			PrefDoUpgrade.Visibility = System.Windows.Visibility.Collapsed;
			Restart.Visibility = System.Windows.Visibility.Collapsed;
			UpgradeProgress.Visibility = System.Windows.Visibility.Collapsed;
			if (ParentWindow.TaskbarItemInfo != null)
				ParentWindow.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

		}

		/// <summary>
		/// 
		/// </summary>
		public void EnableRestart()
		{
			U.L(LogLevel.Debug, "CONTROL", "Enabling restart button");
			Restart.Visibility = System.Windows.Visibility.Visible;

			PrefCheckForUpgrades.Visibility = System.Windows.Visibility.Collapsed;
			PrefDoUpgrade.Visibility = System.Windows.Visibility.Collapsed;
			UpgradeProgress.Visibility = System.Windows.Visibility.Collapsed;
			if (ParentWindow.TaskbarItemInfo != null)
				ParentWindow.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="tab"></param>
		public void SwitchTab(Tab tab)
		{
			foreach (DictionaryEntry c in tabs)
				((ScrollViewer)c.Value).Visibility = ((Tab)c.Key) == tab ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
			foreach (DictionaryEntry c in tabLinks)
				((Button)c.Value).Style = ((Tab)c.Key) == tab ? (Style)FindResource("ControlPanelLinkActiveStyle") : (Style)FindResource("ControlPanelLinkStyle");
		}

		/// <summary>
		/// Updates the strings around the GUI, which are set programmatically, according to the current Language
		/// </summary>
		public void RefreshStrings()
		{
			// change shortcuts
			GlobalLabel.Content = U.T("ShortcutGlobal");
			foreach (TextBlock t in ShortcutTitles)
				t.Text = U.T((string)t.Tag);
			foreach (TextBlock t in ShortcutDescriptions)
				t.Text = U.T((string)t.Tag);
			foreach (Label l in ShortcutLabels)
				l.Content = U.T((string)l.Tag);

			menuRemove.Header = U.T("MenuRemove");

			if (SourceList != null)
				SourceList.RefreshView();

			UpdateUpgradeCheck();

			Plugins.RefreshStrings();
			Services.RefreshStrings();
		}

		#endregion

		#region Private

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <param name="everyWord"></param>
		/// <returns></returns>
		private String Capitalize(String str, bool everyWord = false)
		{
			// TODO: move to and enhance U.Capitalize()
			if (everyWord)
			{
				string[] r = str.Split(' ');
				for (int i = 0; i < r.Count(); i++)
					r[i] = Capitalize(r[i], false);
				return String.Join(" ", r);
			}

			return str[0].ToString().ToUpper() + str.Substring(1).ToLower();
		}

		/// <summary>
		/// 
		/// </summary>
		private void ResetShortcutButton()
		{
			if (!waitingForShortcut) return;
			waitingForShortcut = false;
			currentShortcutButton.Content = currentShortcut.Keys == "" ? U.T("ShortcutNotUsed") : currentShortcut.Keys;
			currentShortcutButton.FontStyle = currentShortcut.Keys == "" ? FontStyles.Italic : FontStyles.Normal;
			currentShortcut = null;
			currentShortcutButton = null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private KeyboardShortcutProfile CreateShortcutProfile(String name = "")
		{
			KeyboardShortcutProfile profile = new KeyboardShortcutProfile();
			bool copy = false;

			if (name == "")
			{
				// create a "CustomX" name
				int i=0;
				bool found;
				while (true)
				{
					i++;
					found = false;
					foreach (KeyboardShortcutProfile scp in SettingsManager.ShortcutProfiles)
						if (scp.Name == U.T("ShortcutCustom") + i.ToString())
							found = true;
					if (found) continue;
					name = U.T("ShortcutCustom") + i.ToString();
					break;
				}

				copy = true;
			}

			SettingsManager.InitShortcutProfile(profile, name, false);

			if (copy) // copy shortcuts from current profile
			{
				KeyboardShortcutProfile currentProfile = SettingsManager.GetCurrentShortcutProfile();
				foreach (KeyboardShortcut sc in currentProfile.Shortcuts)
				{
					KeyboardShortcut newSc = SettingsManager.GetKeyboardShortcut(profile, sc.Category, sc.Name);
					newSc.Keys = sc.Keys;
				}
			}

			SettingsManager.ShortcutProfiles.Add(profile);
			PrefShortcutProfile.Items.Add(new ComboBoxItem() { Content = profile.Name });
			PrefShortcutProfile.SelectedIndex = PrefShortcutProfile.Items.Count - 1;
			return profile;
		}

		/// <summary>
		/// 
		/// </summary>
		private void Save()
		{
			// TODO: Try to save the settings in the background without any hickups
			/*
			ThreadStart SaveThread = delegate()
			{
				SettingsManager.Save();
			};
			Thread save_thread = new Thread(SaveThread);
			save_thread.Name = "Save Settings Thread";
			save_thread.Priority = ThreadPriority.Lowest;
			save_thread.Start();
			*/
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user changes size of the control panel
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ControlPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ScrollBarVisibility vis = e.NewSize.Width < 600 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
			//ControlPanelAbout.HorizontalScrollBarVisibility = vis;
			ControlPanelGeneral.HorizontalScrollBarVisibility = vis;
			ControlPanelShortcuts.HorizontalScrollBarVisibility = vis;
			ControlPanelSources.HorizontalScrollBarVisibility = vis;
		}

		/// <summary>
		/// Invoked when the control panel is loaded
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ControlPanel_Loaded(object sender, RoutedEventArgs e)
		{
			if (!initialized)
			{
				if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
				{
					ControlPanelMain.Background = SystemColors.WindowBrush;
					ControlPanelLeft.Background = SystemColors.WindowBrush;
				}
				AboutChannel.Text = Capitalize(SettingsManager.Channel);
				UpdateUpgradeCheck();

				AboutVersion.Text = Capitalize(SettingsManager.Release, true); // release name
				AboutRelease.Text = Unix2Date(SettingsManager.Version); // release date
				AboutStamp.Text = Version2String(SettingsManager.Version); // release version
				AboutArch.Text = SettingsManager.Architecture + "-bit";

				if (SettingsManager.UpgradePolicy == UpgradePolicy.Manual)
					PrefCheckForUpgrades.Visibility = System.Windows.Visibility.Visible;

				// create languages
				foreach (ComboBoxItem cbi in PrefLanguage.Items)
				{
					if ((string)cbi.Tag == SettingsManager.Language)
					{
						PrefLanguage.SelectedItem = cbi;
						break;
					}
				}

				UpgradePolicyDescriptionConverter uconv = new UpgradePolicyDescriptionConverter();
				string upolicy = (string)uconv.Convert(UpgradePolicy, null, null, null);

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
				PrefLanguage.SelectionChanged += new SelectionChangedEventHandler(PrefLanguage_SelectionChanged);

				initialized = true;
			}
		}

		/// <summary>
		/// Invoked when the control panel has been initialized.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ControlPanel_Initialized(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Invoked when the user clicks on "Back to music"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Back_Clicked(object sender, RoutedEventArgs e)
		{
			ParentWindow.MainContainer.Children.Remove(ParentWindow.ControlPanel);

			ParentWindow.MusicPanel.Visibility = System.Windows.Visibility.Visible;
			ParentWindow.SwitchNavigation();

			//ParentWindow.PlaybackControls.Search.IsEnabled = true;
		}

		/// <summary>
		/// Invoked when the user clicks on General
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void General_Clicked(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.General);
		}

		/// <summary>
		/// Invoked when the user clicks on Sources
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Sources_Clicked(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.Sources);
		}

		/// <summary>
		/// Invoked when the user clicks on Services
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Services_Clicked(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.Services);
		}

        /// <summary>
        /// Invoked when the user clicks on Plugins
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data</param>
        private void Plugins_Clicked(object sender, RoutedEventArgs e)
        {
            SwitchTab(Tab.Plugins);
        }

		/// <summary>
		/// Invoked when the user clicks on Shortcuts
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Shortcuts_Clicked(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.Shortcuts);
		}

		/// <summary>
		/// Invoked when the user clicks on About
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void About_Clicked(object sender, RoutedEventArgs e)
		{
			SwitchTab(Tab.About);
		}

		/// <summary>
		/// Invoked when the user clicks on Website
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Website_Clicked(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.stoffiplayer.com/?ref=stoffi");
		}

		/// <summary>
		/// Invoked when the user clicks on Blog
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Blog_Clicked(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://blog.stoffiplayer.com/?ref=stoffi");
		}

		/// <summary>
		/// Invoked when the user clicks on Project
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Project_Clicked(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://dev.stoffiplayer.com/?ref=stoffi");
		}

		/// <summary>
		/// Invoked when the user adds a folder
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void AddFolder_Clicked(object sender, RoutedEventArgs e)
		{
			ParentWindow.AddFolder_Clicked(sender, e);
		}

		/// <summary>
		/// Invoked when the user adds a file
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public void AddFile_Clicked(object sender, RoutedEventArgs e)
		{
			ParentWindow.AddFile_Clicked(sender, e);
		}

		/// <summary>
		/// Invoked when the user ignores a folder
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void IgnoreFolder_Clicked(object sender, RoutedEventArgs e)
		{
			ParentWindow.IgnoreFolder_Clicked(sender, e);
		}

		/// <summary>
		/// Invoked when the user ignores a file
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void IgnoreFile_Clicked(object sender, RoutedEventArgs e)
		{
			ParentWindow.IgnoreFile_Clicked(sender, e);
		}

		/// <summary>
		/// Invoked when the user right-click on a source and clicks on Remove
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void menuRemove_Click(object sender, RoutedEventArgs e)
		{
			int index = SourceList.SelectedIndex;
			ThreadStart RemoveThread = delegate()
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					int keep = 0;
					while (SourceList.SelectedItems.Count > keep)
					{
						SourceData source = SourceList.SelectedItems[keep] as SourceData;
						if (source.Automatic)
						{
							FilesystemManager.ToggleSource(source);
							keep++;
						}
						else
							FilesystemManager.RemoveSource(source);
					}

					if (index >= SourceList.Items.Count)
						index = SourceList.Items.Count - 1;

					if (keep == 0)
						SourceList.SelectedIndex = index;
				}));
			};
			Thread rb_thread = new Thread(RemoveThread);
			rb_thread.Name = "Remove Sources";
			rb_thread.Priority = ThreadPriority.BelowNormal;
			rb_thread.Start();
		}

		/// <summary>
		/// Invoked when the user right-click on a source and clicks on Ignore/Include
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void menuToggle_Click(object sender, RoutedEventArgs e)
		{
			ThreadStart ToggleThread = delegate()
			{
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
				{
					foreach (SourceData source in SourceList.SelectedItems)
						FilesystemManager.ToggleSource(source);
				}));
			};
			Thread tog_thread = new Thread(ToggleThread);
			tog_thread.Name = "Toggle Sources";
			tog_thread.Priority = ThreadPriority.BelowNormal;
			tog_thread.Start();
		}

		/// <summary>
		/// Invoked when the context menu of the source list is opening
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SourceList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			bool hasOnlyAutomatic = true;
			bool hasIgnored = false;

			foreach (SourceData s in SourceList.SelectedItems)
			{
				if (!s.Automatic)
					hasOnlyAutomatic = false;
				if (s.Ignore)
					hasIgnored = true;
			}

			menuRemove.Visibility = (hasOnlyAutomatic ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible);
			menuToggle.Header = (hasIgnored ? U.T("MenuInclude") : U.T("MenuIgnore"));
		}

		/// <summary>
		/// Invoked when the user presses a key
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SourceList_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				List<SourceData> sources = new List<SourceData>();
				foreach (SourceData s in ((ListView)sender).SelectedItems)
					sources.Add(s);
				menuRemove_Click(null, null);
			}
		}

		/// <summary>
		/// Invoked when theuser clicks to check for upgrades
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void CheckForUpgrades_Clicked(object sender, RoutedEventArgs e)
		{
			UpgradeMessageClose_MouseLeftButtonDown(null, null);

			PrefCheckForUpgrades.IsEnabled = false;
			PrefCheckForUpgrades.Content = U.T("UpgradeWait");

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
		public void PrefDoUpgrade_Clicked(object sender, RoutedEventArgs e)
		{
			UpgradeMessageClose_MouseLeftButtonDown(null, null);

			PrefDoUpgrade.IsEnabled = false;
			PrefDoUpgrade.Content = U.T("UpgradeWait");

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
			ParentWindow.Restart();
		}

		/// <summary>
		/// Invoked when the user clicks on a shortcut
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PrefShortcutButton_Clicked(object sender, RoutedEventArgs e)
		{
			currentShortcutButton = sender as Button;
			string[] name = currentShortcutButton.Name.Split(new char[] { '_' }, 2);
			currentShortcut = SettingsManager.GetKeyboardShortcut(SettingsManager.GetCurrentShortcutProfile(), name[0], name[1].Replace("_", " "));

			waitingForShortcut = true;
			U.ListenForShortcut = false;
			currentShortcutButton.Content = U.T("ShortcutPress");
			currentShortcutButton.FontStyle = FontStyles.Italic;
			currentPressedKeys.Clear();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PrefShortcutButton_LostFocus(object sender, RoutedEventArgs e)
		{
			ResetShortcutButton();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PrefShortcutProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!(PrefShortcutProfile.SelectedItem is ComboBoxItem)) return;
			if (!(((ComboBoxItem)PrefShortcutProfile.SelectedItem).Content is string)) return;
			SettingsManager.CurrentShortcutProfile = ((ComboBoxItem)PrefShortcutProfile.SelectedItem).Content as string;
			KeyboardShortcutProfile profile = SettingsManager.GetCurrentShortcutProfile();

			foreach (KeyboardShortcut sc in profile.Shortcuts)
			{
				if (sc.Name == "Now playing") continue;

				Button b = shortcutButtons[sc.Category + "_" + sc.Name.Replace(" ", "_")] as Button;
				b.Content = sc.Keys == "" ? U.T("ShortcutNotUsed") : sc.Keys;
				b.FontStyle = sc.Keys == "" ? FontStyles.Italic : FontStyles.Normal;

				if (sc.Category == "MediaCommands")
				{
					CheckBox cb = shortcutCheckBoxes[sc.Category + "_" + sc.Name.Replace(" ", "_")] as CheckBox;
					cb.IsChecked = sc.IsGlobal;
				}
			}

			System.Windows.Visibility vis = profile.IsProtected ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
			PrefRenameShortcutProfile.Visibility = vis;
			PrefDeleteShortcutProfile.Visibility = vis;
		}

		/// <summary>
		/// Invoked when the user changes search policy
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SearchPolicyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SettingsManager.FileListConfig.Filter = "";
			SettingsManager.HistoryListConfig.Filter = "";
			SettingsManager.QueueListConfig.Filter = "";
			SettingsManager.YouTubeListConfig.Filter = "";
			foreach (PlaylistData p in SettingsManager.Playlists)
				p.ListConfig.Filter = "";
			ParentWindow.PlaybackControls.Search.Clear();

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
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PrefCreateShortcutProfile_Clicked(object sender, RoutedEventArgs e)
		{
			List<string> occupied = new List<string>();
			foreach (KeyboardShortcutProfile p in SettingsManager.ShortcutProfiles)
				occupied.Add(p.Name);
			NameDialog dialog = new NameDialog(occupied);
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
				CreateShortcutProfile(U.CleanXMLString(dialog.ProfileName.Text));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PrefRenameShortcutProfile_Clicked(object sender, RoutedEventArgs e)
		{
			KeyboardShortcutProfile profile = SettingsManager.GetCurrentShortcutProfile();
			List<string> occupied = new List<string>();
			foreach (KeyboardShortcutProfile p in SettingsManager.ShortcutProfiles)
				occupied.Add(p.Name);
			NameDialog dialog = new NameDialog(occupied, profile.Name);
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
			{
				foreach (ComboBoxItem item in PrefShortcutProfile.Items)
					if (profile.Name == (string)item.Content)
						item.Content = dialog.ProfileName.Text;
				profile.Name = U.CleanXMLString(dialog.ProfileName.Text);
				SettingsManager.CurrentShortcutProfile = dialog.ProfileName.Text;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PrefDeleteShortcutProfile_Clicked(object sender, RoutedEventArgs e)
		{
			KeyboardShortcutProfile profile = SettingsManager.GetCurrentShortcutProfile();
			ComboBoxItem itemToRemove = PrefShortcutProfile.SelectedItem as ComboBoxItem;
			int index = PrefShortcutProfile.Items.IndexOf(itemToRemove) - 1;
			if (index == -1) index = 0;
			SettingsManager.ShortcutProfiles.Remove(profile);
			PrefShortcutProfile.SelectedIndex = index;
			PrefShortcutProfile.Items.Remove(itemToRemove);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PrefShortcutGlobal_Clicked(object sender, RoutedEventArgs e)
		{
			CheckBox cb = sender as CheckBox;
			string[] name = cb.Name.Split(new char[] { '_' }, 2);
			KeyboardShortcut sc = SettingsManager.GetKeyboardShortcut(SettingsManager.GetCurrentShortcutProfile(), name[0], name[1].Replace("_", " "));
			sc.IsGlobal = (bool)cb.IsChecked;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ControlPanel_KeyDown(object sender, KeyEventArgs e)
		{
			if (!waitingForShortcut) return;

			switch (e.Key)
			{
				// convert right shift to left shift
				case Key.RightShift:
					if (!currentPressedKeys.Contains(Key.LeftShift)) currentPressedKeys.Add(Key.LeftShift);
					currentShortcutButton.Content = ParentWindow.GetModifiersAsText(currentPressedKeys);
					return;
				// catch modifier keys
				case Key.LeftShift:
				case Key.LeftCtrl:
				case Key.LeftAlt:
				case Key.LWin:
				case Key.RightCtrl:
				case Key.RightAlt:
				case Key.RWin:
					if (!currentPressedKeys.Contains(e.Key)) currentPressedKeys.Add(e.Key);
					currentShortcutButton.Content = ParentWindow.GetModifiersAsText(currentPressedKeys);
					return;

				// catch alt/left ctrl key when disguised as system key
				case Key.System:
					if (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt || e.SystemKey == Key.LeftCtrl)
					{
						if (!currentPressedKeys.Contains(e.SystemKey)) currentPressedKeys.Add(e.SystemKey);
						currentShortcutButton.Content = ParentWindow.GetModifiersAsText(currentPressedKeys);
						return;
					}
					break;
					
				// ignore these keys
				case Key.None:
				case Key.DeadCharProcessed:
					return;
				default:
					break;
			}

			// TODO: Convert Oem keys to nice strings
			String currentKey = e.Key == Key.System ? ParentWindow.KeyToString(e.SystemKey) : ParentWindow.KeyToString(e.Key);
			String txt = ParentWindow.GetModifiersAsText(currentPressedKeys);
			if (txt.Length > 0) txt += "+" + currentKey;
			else txt = currentKey;

			KeyboardShortcutProfile profile = SettingsManager.GetCurrentShortcutProfile();

			// see if shortcut already exists
			bool createdNew = false;
			foreach (KeyboardShortcut sc in profile.Shortcuts)
			{
				if (sc.Keys == txt && sc != currentShortcut)
				{
					string title = U.T("MessageShortcutClash", "Title");
					string message = U.T("MessageShortcutClash", "Message");
					string name = U.T("Shortcut_" + sc.Name.Replace(" ", "_"));
					string category = U.T("Shortcut" + sc.Category);
					message = message.Replace("%name", name);
					message = message.Replace("%category", category);

					if (MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						// if current profile is protected we create a copy of it
						KeyboardShortcut _sc = sc;
						if (profile.IsProtected)
						{
							profile = CreateShortcutProfile();
							currentShortcut = SettingsManager.GetKeyboardShortcut(profile, currentShortcut.Keys);
							_sc = SettingsManager.GetKeyboardShortcut(profile, sc.Keys);
							createdNew = true;
						}

						Button b = (Button)shortcutButtons[sc.Category + "_" + sc.Name.Replace(" ", "_")];
						if (b != null)
						{
							b.Content = U.T("ShortcutNotUsed");
							b.FontStyle = FontStyles.Italic;
						}
						_sc.Keys = "";
						break;
					}
					else
					{
						ResetShortcutButton();
						e.Handled = true;
						return;
					}
				}
			}

			// if current profile is protected we create a copy of it (if we haven't already)
			if (profile.IsProtected && !createdNew)
			{
				profile = CreateShortcutProfile();
				currentShortcut = SettingsManager.GetKeyboardShortcut(profile, currentShortcut.Keys);
			}

			// set shortcut and button text
			currentShortcutButton.FontStyle = FontStyles.Normal;
			waitingForShortcut = false;
			currentShortcutButton.Content = txt;
			if (currentShortcut != null) currentShortcut.Keys = txt;
			currentShortcut = null;
			currentShortcutButton = null;
			e.Handled = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ControlPanel_KeyUp(object sender, KeyEventArgs e)
		{
			if (!waitingForShortcut)
			{
				U.ListenForShortcut = true;
				return;
			}

			// catch alt/left ctrl when disguised as system key
			if (e.Key == Key.System && (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt || e.SystemKey == Key.LeftCtrl))
			{
				if (currentPressedKeys.Contains(e.SystemKey)) currentPressedKeys.Remove(e.SystemKey);
			}
			else if (e.Key == Key.RightShift)
			{
				if (currentPressedKeys.Contains(Key.LeftShift)) currentPressedKeys.Remove(Key.LeftShift);
			}
			else
			{
				if (currentPressedKeys.Contains(e.Key)) currentPressedKeys.Remove(e.Key);
			}

			// update button text
			if (currentPressedKeys.Count == 0)
				currentShortcutButton.Content = U.T("ShortcutPress");
			else
				currentShortcutButton.Content = ParentWindow.GetModifiersAsText(currentPressedKeys);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Details_Expanded(object sender, RoutedEventArgs e)
		{
			AboutRelease.Visibility = System.Windows.Visibility.Visible;
			AboutReleaseLabel.Visibility = System.Windows.Visibility.Visible;
			AboutStamp.Visibility = System.Windows.Visibility.Visible;
			AboutStampLabel.Visibility = System.Windows.Visibility.Visible;
			AboutChannel.Visibility = System.Windows.Visibility.Visible;
			AboutChannelLabel.Visibility = System.Windows.Visibility.Visible;
			AboutArch.Visibility = System.Windows.Visibility.Visible;
			AboutArchLabel.Visibility = System.Windows.Visibility.Visible;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Details_Collapsed(object sender, RoutedEventArgs e)
		{
			AboutRelease.Visibility = System.Windows.Visibility.Collapsed;
			AboutReleaseLabel.Visibility = System.Windows.Visibility.Collapsed;
			AboutStamp.Visibility = System.Windows.Visibility.Collapsed;
			AboutStampLabel.Visibility = System.Windows.Visibility.Collapsed;
			AboutChannel.Visibility = System.Windows.Visibility.Collapsed;
			AboutChannelLabel.Visibility = System.Windows.Visibility.Collapsed;
			AboutArch.Visibility = System.Windows.Visibility.Collapsed;
			AboutArchLabel.Visibility = System.Windows.Visibility.Collapsed;
		}

		/// <summary>
		/// Invoked when the user selects a different language
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PrefLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBoxItem cbi = PrefLanguage.SelectedItem as ComboBoxItem;
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
					case "SourceListConfig":
						SourceList.Config = SettingsManager.SourceListConfig;
						break;

					case "MinimizeToTray":
						Console.WriteLine("M2T changed to: " + SettingsManager.MinimizeToTray);
						PrefMin2Tray.IsChecked = SettingsManager.MinimizeToTray;
						break;

					case "ShowOSD":
						PrefOSD.IsChecked = SettingsManager.ShowOSD;
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

		#endregion

		#region Enum

		/// <summary>
		/// The tabs of the control panel
		/// </summary>
		public enum Tab
		{
			/// <summary>
			/// General settings
			/// </summary>
			General,

			/// <summary>
			/// Music sources
			/// </summary>
			Sources,

			/// <summary>
			/// The Stoffi services
			/// </summary>
            Services,

            /// <summary>
            /// Plugins
            /// </summary>
            Plugins,

			/// <summary>
			/// Keyboard shortcuts
			/// </summary>
			Shortcuts,

			/// <summary>
			/// About Stoffi
			/// </summary>
			About
		}

		#endregion
	}
}
