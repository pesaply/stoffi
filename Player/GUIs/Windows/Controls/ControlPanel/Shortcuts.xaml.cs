/**
 * Shortcuts.xaml.cs
 * 
 * The "Shortcuts" screen inside the "Control Panel".
 * It shows the shortcut profiles and keyboard shortcuts
 * for navigating Stoffi.
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
using System.Collections;
using System.Collections.Generic;
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

using Stoffi.Core;
using Stoffi.Core.Settings;
using Stoffi.Core.Media;
using Stoffi.Player.GUI.Controls;
using Stoffi.Player.GUI.Windows;

using MediaManager = Stoffi.Core.Media.Manager;
using SettingsManager = Stoffi.Core.Settings.Manager;

namespace Stoffi.Player.GUI.Controls.ControlPanel
{
	/// <summary>
	/// Interaction logic for Shortcuts.xaml
	/// </summary>
	public partial class Shortcuts : ScrollViewer
	{
		#region Members

		private Boolean waitingForShortcut = false;
		private Button currentShortcutButton = null;
		private KeyboardShortcut currentShortcut = null;
		private List<Key> currentPressedKeys = new List<Key>();
		private List<TextBlock> ShortcutTitles = new List<TextBlock>();
		private List<TextBlock> ShortcutDescriptions = new List<TextBlock>();
		private List<Label> ShortcutLabels = new List<Label>();
		private Hashtable shortcutButtons = new Hashtable();
		private Hashtable shortcutCheckBoxes = new Hashtable();
		private Label globalLabel = new Label();

		#endregion

		#region Properties

		#endregion

		#region Constructor
		public Shortcuts()
		{
			InitializeComponent();
			//InitShortcuts();
		}
		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Updates the strings around the GUI, which are set programmatically, according to the current Language
		/// </summary>
		public void RefreshStrings()
		{
			// change shortcuts
			globalLabel.Content = U.T("ShortcutGlobal");
			foreach (TextBlock t in ShortcutTitles)
				t.Text = U.T((string)t.Tag);
			foreach (TextBlock t in ShortcutDescriptions)
				t.Text = U.T((string)t.Tag);
			foreach (Label l in ShortcutLabels)
				l.Content = U.T((string)l.Tag);
		}

		#endregion

		#region Private

		/// <summary>
		/// Initializes the controls for the shortcuts
		/// </summary>
		private void InitShortcuts()
		{
			String[,] categories = new String[4, 3]; // name, translation id (title), translation id (text
			categories[0, 0] = "Application";
			categories[0, 1] = "ShortcutApplicationTitle";
			categories[0, 2] = "ShortcutApplicationText";

			categories[1, 0] = "MainWindow";
			categories[1, 1] = "ShortcutMainWindowTitle";
			categories[1, 2] = "ShortcutMainWindowText";

			categories[2, 0] = "MediaManager.Commands";
			categories[2, 1] = "ShortcutMediaManager.CommandsTitle";
			categories[2, 2] = "ShortcutMediaManager.CommandsText";

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
				KeyboardShortcutProfile profile = SettingsManager.CurrentShortcutProfile;
				for (j = 0; j < profile.Shortcuts.Count; j++)
					g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

				if (categories[i, 0] == "MediaManager.Commands")
				{
					globalLabel.Content = U.T("ShortcutGlobal");
					globalLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
					Grid.SetColumn(globalLabel, 2);
					Grid.SetRow(globalLabel, 0);
					g.Children.Add(globalLabel);
				}

				j = 1;
				foreach (KeyboardShortcut sc in profile.Shortcuts)
				{
					// skip now playing
					if (sc.Name == "Now playing") continue;

					if (sc.Category != categories[i, 0]) continue;
					Label l = new Label() { Content = U.T("Shortcut_" + sc.Name.Replace(" ", "_")) };
					l.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
					l.Tag = "Shortcut_" + sc.Name.Replace(" ", "_");
					Grid.SetColumn(l, 0);
					Grid.SetRow(l, j);
					g.Children.Add(l);
					ShortcutLabels.Add(l);

					Button b = new Button() { Content = sc.Keys, MinWidth = 100 };
					b.Name = sc.Category + "_" + sc.Name.Replace(" ", "_");
					b.LostFocus += ShortcutButton_LostFocus;
					b.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
					b.Margin = new Thickness(2);
					b.Click += ShortcutButton_Click;
					shortcutButtons.Add(b.Name, b);
					Grid.SetColumn(b, 1);
					Grid.SetRow(b, j);
					g.Children.Add(b);

					if (categories[i, 0] == "MediaManager.Commands")
					{
						CheckBox cb = new CheckBox() { IsChecked = sc.IsGlobal, Name = b.Name, ToolTip = U.T("ShortcutGlobal", "ToolTip") };
						cb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						cb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
						cb.Margin = new Thickness(10, 0, 10, 0);
						cb.Click += new RoutedEventHandler(Global_Click);
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

			string selTxt = SettingsManager.CurrentShortcutProfile.Name;
			int sel = 0;
			foreach (KeyboardShortcutProfile p in SettingsManager.ShortcutProfiles)
			{
				if (selTxt == p.Name)
					sel = ShortcutProfile.Items.Count;
				ShortcutProfile.Items.Add(new ComboBoxItem() { Content = p.Name });
			}
			ShortcutProfile.SelectedIndex = sel;
		}

		/// <summary>
		/// Resets a shortcut button to it's previous state.
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
		/// Create a new shortcut profile.
		/// </summary>
		/// <param name="name">The name of the new shortcut profile. If empty then the name will be "CustomX" where X is a number</param>
		/// <returns>The newly created shortcut profile</returns>
		private KeyboardShortcutProfile CreateShortcutProfile(String name = "")
		{
			KeyboardShortcutProfile profile = new KeyboardShortcutProfile();
			bool copy = false;

			if (name == "")
			{
				// create a "CustomX" name
				int i = 0;
				bool found;
				while (true)
				{
					i++;
					found = false;
					foreach (KeyboardShortcutProfile scp in SettingsManager.ShortcutProfiles)
						if (scp.Name == U.T("ShortcutsProfileCustom") + i.ToString())
							found = true;
					if (found) continue;
					name = U.T("ShortcutsProfileCustom") + i.ToString();
					break;
				}

				copy = true;
			}

			profile.Initialize(name, false);

			if (copy) // copy shortcuts from current profile
			{
				KeyboardShortcutProfile currentProfile = SettingsManager.CurrentShortcutProfile;
				foreach (KeyboardShortcut sc in currentProfile.Shortcuts)
				{
					KeyboardShortcut newSc = profile.GetShortcut(sc.Category, sc.Name);
					newSc.Keys = sc.Keys;
				}
			}

			SettingsManager.ShortcutProfiles.Add(profile);
			ShortcutProfile.Items.Add(new ComboBoxItem() { Content = profile.Name });
			ShortcutProfile.SelectedIndex = ShortcutProfile.Items.Count - 1;
			return profile;
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the control is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Shortcuts_Loaded(object sender, RoutedEventArgs e)
		{
			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
				Title.Style = (Style)FindResource("ClassicControlPanelTitleStyle");
		}

		/// <summary>
		/// Invoked when the user depresses a key.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Shortcuts_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!waitingForShortcut) return;

			switch (e.Key)
			{
				// convert right shift to left shift
				case Key.RightShift:
					if (!currentPressedKeys.Contains(Key.LeftShift)) currentPressedKeys.Add(Key.LeftShift);
					currentShortcutButton.Content = Utilities.GetModifiersAsText(currentPressedKeys);
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
					currentShortcutButton.Content = Utilities.GetModifiersAsText(currentPressedKeys);
					return;

				// catch alt/left ctrl key when disguised as system key
				case Key.System:
					if (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt || e.SystemKey == Key.LeftCtrl)
					{
						if (!currentPressedKeys.Contains(e.SystemKey)) currentPressedKeys.Add(e.SystemKey);
						currentShortcutButton.Content = Utilities.GetModifiersAsText(currentPressedKeys);
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
			String currentKey = e.Key == Key.System ? Utilities.KeyToString(e.SystemKey) : Utilities.KeyToString(e.Key);
			String txt = Utilities.GetModifiersAsText(currentPressedKeys);
			if (txt.Length > 0) txt += "+" + currentKey;
			else txt = currentKey;

			KeyboardShortcutProfile profile = SettingsManager.CurrentShortcutProfile;

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
							currentShortcut = profile.GetShortcut(currentShortcut.Keys);
							_sc = profile.GetShortcut(sc.Keys);
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
				currentShortcut = profile.GetShortcut(currentShortcut.Keys);
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
		/// Invoked when the user releases a key.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Shortcuts_PreviewKeyUp(object sender, KeyEventArgs e)
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
				currentShortcutButton.Content = Utilities.GetModifiersAsText(currentPressedKeys);
		}

		/// <summary>
		/// Invoked when the user clicks on a shortcut
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ShortcutButton_Click(object sender, RoutedEventArgs e)
		{
			currentShortcutButton = sender as Button;
			string[] name = currentShortcutButton.Name.Split(new char[] { '_' }, 2);
			currentShortcut = SettingsManager.CurrentShortcutProfile.GetShortcut(name[0], name[1].Replace("_", " "));

			waitingForShortcut = true;
			U.ListenForShortcut = false;
			currentShortcutButton.Content = U.T("ShortcutPress");
			currentShortcutButton.FontStyle = FontStyles.Italic;
			currentPressedKeys.Clear();
		}

		/// <summary>
		/// Invoked when a shortcut button loses focus.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ShortcutButton_LostFocus(object sender, RoutedEventArgs e)
		{
			ResetShortcutButton();
		}

		/// <summary>
		/// Invoked when the user selects a different shortcut profile.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ShortcutProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!(ShortcutProfile.SelectedItem is ComboBoxItem)) return;
			if (!(((ComboBoxItem)ShortcutProfile.SelectedItem).Content is string)) return;
			var name = ((ComboBoxItem)ShortcutProfile.SelectedItem).Content as string;
			foreach (var p in SettingsManager.ShortcutProfiles)
			{
				if (name == p.Name)
				{
					SettingsManager.CurrentShortcutProfile = p;
				}
			}
			var profile = SettingsManager.CurrentShortcutProfile;

			foreach (KeyboardShortcut sc in profile.Shortcuts)
			{
				if (sc.Name == "Now playing") continue;

				Button b = shortcutButtons[sc.Category + "_" + sc.Name.Replace(" ", "_")] as Button;
				b.Content = sc.Keys == "" ? U.T("ShortcutNotUsed") : sc.Keys;
				b.FontStyle = sc.Keys == "" ? FontStyles.Italic : FontStyles.Normal;

				if (sc.Category == "MediaManager.Commands")
				{
					CheckBox cb = shortcutCheckBoxes[sc.Category + "_" + sc.Name.Replace(" ", "_")] as CheckBox;
					cb.IsChecked = sc.IsGlobal;
				}
			}

			System.Windows.Visibility vis = profile.IsProtected ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
			Rename.Visibility = vis;
			Delete.Visibility = vis;
		}

		/// <summary>
		/// Invoked when the user clicks "Create new" under shortcut profiles.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Create_Click(object sender, RoutedEventArgs e)
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
		/// Invoked when the user clicks "Rename" under shortcut profiles.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Rename_Click(object sender, RoutedEventArgs e)
		{
			var profile = SettingsManager.CurrentShortcutProfile;
			List<string> occupied = new List<string>();
			foreach (KeyboardShortcutProfile p in SettingsManager.ShortcutProfiles)
				occupied.Add(p.Name);
			NameDialog dialog = new NameDialog(occupied, profile.Name);
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
			{
				foreach (ComboBoxItem item in ShortcutProfile.Items)
					if (profile.Name == (string)item.Content)
						item.Content = dialog.ProfileName.Text;
				profile.Name = U.CleanXMLString(dialog.ProfileName.Text);
			}
		}

		/// <summary>
		/// Invoked when the user clicks "Delete" under shortcut profiles.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			var profile = SettingsManager.CurrentShortcutProfile;
			ComboBoxItem itemToRemove = ShortcutProfile.SelectedItem as ComboBoxItem;
			int index = ShortcutProfile.Items.IndexOf(itemToRemove) - 1;
			if (index == -1) index = 0;
			SettingsManager.ShortcutProfiles.Remove(profile);
			ShortcutProfile.SelectedIndex = index;
			ShortcutProfile.Items.Remove(itemToRemove);
		}

		/// <summary>
		/// Invoked when the user clicks on the Global checkbox for a shortcut.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Global_Click(object sender, RoutedEventArgs e)
		{
			CheckBox cb = sender as CheckBox;
			string[] name = cb.Name.Split(new char[] { '_' }, 2);
			KeyboardShortcut sc = SettingsManager.CurrentShortcutProfile.GetShortcut(name[0], name[1].Replace("_", " "));
			sc.IsGlobal = (bool)cb.IsChecked;
		}

		#endregion

		#endregion
	}
}
