/**
 * Equalizer.xaml.cs
 * 
 * The equalizer window.
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Stoffi.Core;
using Stoffi.Core.Settings;

using SettingsManager = Stoffi.Core.Settings.Manager;
using MediaManager = Stoffi.Core.Media.Manager;

namespace Stoffi.Player.GUI.Windows
{
	/// <summary>
	/// Interaction logic for Equalizer.xaml
	/// </summary>
	public partial class Equalizer : Window
	{
		#region Members

		/// <summary>
		/// Holds the values which are currently shown in the equalizer window (may not yet have been saved).
		/// </summary>
		private EqualizerProfile currentValues = new EqualizerProfile();

		/// <summary>
		/// The sliders ordered by same index as in the profiles.
		/// </summary>
		Slider[] sliders = null;

		/// <summary>
		/// A timer used for delaying setting effects on the media. This is to prevent the GUI from
		/// freezing while moving the slider.
		/// </summary>
		private Timer fxDelay = null;

		/// <summary>
		/// Whether or not a change in the slides should be sent to the media for updating of the effects.
		/// </summary>
		private bool setFX = true;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the Equalizer window
		/// </summary>
		public Equalizer()
		{
			InitializeComponent();
			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);
			foreach (EqualizerProfile profile in SettingsManager.EqualizerProfiles)
				Profiles.Items.Add(profile.IsProtected ? U.T("EqualizerProfile" + profile.Name) : profile.Name);

			// make sure this is the same order as the values in the profiles!
			sliders = new Slider[] { F32, F64, F125, F250, F500, F1K, F2K, F4K, F8K, F16K };

			Refresh();
			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName != "")
				Background = Brushes.WhiteSmoke;
			else
				Background = new SolidColorBrush(Color.FromRgb(212, 208, 200));
		}

		#endregion

		#region Methods

		#region Private

		/// <summary>
		/// Loads all values from the current profile.
		/// </summary>
		private void Refresh()
		{
			setFX = false;
			for (int i = 0; i < sliders.Count(); i++)
				sliders[i].Value = SettingsManager.CurrentEqualizerProfile.Levels[i] * 10f;
			Echo.Value = SettingsManager.CurrentEqualizerProfile.EchoLevel;

			Profiles.SelectedItem = SettingsManager.CurrentEqualizerProfile.IsProtected ? 
				U.T("EqualizerProfile" + SettingsManager.CurrentEqualizerProfile.Name) :
				SettingsManager.CurrentEqualizerProfile.Name;

			RefreshButtons();
			setFX = true;
		}

		/// <summary>
		/// Checks if any values has been changed.
		/// </summary>
		/// <param name="profile">The profile to compare to</param>
		/// <returns>True if any values has been changed, otherwise false</returns>
		private bool AnyChanged(EqualizerProfile profile = null)
		{
			if (profile == null)
				profile = SettingsManager.CurrentEqualizerProfile;

			float[] levels = new float[]
			{
				(float)F32.Value / 10f,
				(float)F64.Value / 10f,
				(float)F125.Value / 10f,
				(float)F250.Value / 10f,
				(float)F500.Value / 10f,
				(float)F1K.Value / 10f,
				(float)F2K.Value / 10f,
				(float)F4K.Value / 10f,
				(float)F8K.Value / 10f,
				(float)F16K.Value / 10f,
			};

			for (int i = 0; i < levels.Count(); i++)
			{
				float a = profile.Levels[i];
				float b = levels[i];
				if (Math.Round(a, 2) != Math.Round(b, 2)) return true;
			}

			return (Math.Round(profile.EchoLevel, 2) != Math.Round(Echo.Value, 2));
		}

		/// <summary>
		/// Refreshes the buttons depending on whether anything has changed or not
		/// </summary>
		private void RefreshButtons()
		{
			bool changed = AnyChanged();
			bool protect = SettingsManager.CurrentEqualizerProfile.IsProtected;

			OK.Visibility    = changed ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
			Apply.Visibility = changed ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
			Cancel.Content = changed ? U.T("ButtonCancel", "Content") : U.T("ButtonClose", "Content");

			Remove.Visibility = protect ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
			Rename.Visibility = protect ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
		}

		/// <summary>
		/// Save the settings
		/// </summary>
		private void Save()
		{
			EqualizerProfile profile = SettingsManager.CurrentEqualizerProfile;

			var levels = new ObservableCollection<float>(new float[]
			{
				(float)F32.Value / 10f,
				(float)F64.Value / 10f,
				(float)F125.Value / 10f,
				(float)F250.Value / 10f,
				(float)F500.Value / 10f,
				(float)F1K.Value / 10f,
				(float)F2K.Value / 10f,
				(float)F4K.Value / 10f,
				(float)F8K.Value / 10f,
				(float)F16K.Value / 10f,
			});

			float echo = (float)Echo.Value;

			// create new profile
			if (profile.IsProtected)
			{
				List<string> occupied = new List<string>();
				foreach (EqualizerProfile p in SettingsManager.EqualizerProfiles)
				{
					occupied.Add(p.Name);
					if (p.IsProtected)
						occupied.Add(U.T("EqualizerProfile" + p.Name));
				}
				NameDialog dialog = new NameDialog(occupied);
				dialog.ShowDialog();
				if (dialog.DialogResult == true)
				{
					profile = EqualizerProfile.Create(U.CleanXMLString(dialog.ProfileName.Text), levels, echo);
					SettingsManager.EqualizerProfiles.Add(profile);
					Profiles.Items.Add(profile.Name);
					SettingsManager.CurrentEqualizerProfile = profile;
				}
			}

			// update profile
			else
			{
				profile.Levels = levels;
				profile.EchoLevel = echo;
			}

			Refresh();
		}

		/// <summary>
		/// Asks the user if she wants to save the current values
		/// </summary>
		private void AskToSave()
		{
			if (AnyChanged(SettingsManager.CurrentEqualizerProfile))
			{
				if (MessageBox.Show(
					U.T("MessageSaveProfile", "Message"),
					U.T("MessageSaveProfile", "Title"),
					MessageBoxButton.YesNo,
					MessageBoxImage.Question) == MessageBoxResult.Yes)
					Save();
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the value of a slider changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			try
			{
				Slider s = sender as Slider;
				int i = Array.IndexOf(sliders, s);
				currentValues.Levels[i] = (float)s.Value / 10f;
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "equalizer", "Could not update equalizer value: " + exc.Message);
			}
			if (setFX)
			{
				int dueTime = 200;
				if (fxDelay == null)
					fxDelay = new Timer(PerformRefreshFX, null, dueTime, Timeout.Infinite);
				else
					fxDelay.Change(dueTime, Timeout.Infinite);
			}
			RefreshButtons();
		}

		/// <summary>
		/// Invoked when the user scrolls the mousewheel over a slider.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Slider_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			Slider s = sender as Slider;
			int divider = s == Echo ? 48 : 12;
			s.Value += e.Delta / divider;
		}

		/// <summary>
		/// Invoked when the user changes the echo level
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Echo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			currentValues.EchoLevel = (float)Echo.Value;
			if (setFX)
			{
				int dueTime = 200;
				if (fxDelay == null)
					fxDelay = new Timer(PerformRefreshFX, null, dueTime, Timeout.Infinite);
				else
					fxDelay.Change(dueTime, Timeout.Infinite);
			}
			RefreshButtons();
		}

		/// <summary>
		/// Invoked when the user resizes the window
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void EqualizerDialog_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SettingsManager.EqualizerWidth = Width;
			SettingsManager.EqualizerHeight = Height;
		}

		/// <summary>
		/// Invoked when the user moves the window
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void EqualizerDialog_LocationChanged(object sender, EventArgs e)
		{
			SettingsManager.EqualizerTop = Top;
			SettingsManager.EqualizerLeft = Left;
		}

		/// <summary>
		/// Invoked when the user changes profile
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			EqualizerProfile profile = SettingsManager.GetEqualizerProfile(Profiles.SelectedItem as string);
			AskToSave();
			SettingsManager.CurrentEqualizerProfile = profile;
		}

		/// <summary>
		/// Invoked when the user clicks "Apply"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Apply_Click(object sender, RoutedEventArgs e)
		{
			Save();
		}

		/// <summary>
		/// Invoked when the user clicks "Cancel" or "Close"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (AnyChanged())
				Refresh();
			else
				Close();
		}

		/// <summary>
		/// Invoked when the user clicks "OK"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void OK_Click(object sender, RoutedEventArgs e)
		{
			Save();
			Close();
		}

		/// <summary>
		/// Invoked when the user clicks "New"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void New_Click(object sender, RoutedEventArgs e)
		{
			List<string> occupied = new List<string>();
			foreach (EqualizerProfile p in SettingsManager.EqualizerProfiles)
			{
				occupied.Add(p.Name);
				if (p.IsProtected)
					occupied.Add(U.T("EqualizerProfile" + p.Name));
			}
			NameDialog dialog = new NameDialog(occupied);
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
			{
				var profile = EqualizerProfile.Create(U.CleanXMLString(dialog.ProfileName.Text), new ObservableCollection<float>(new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }), 0);
				SettingsManager.EqualizerProfiles.Add(profile);
				Profiles.Items.Add(profile.Name);
				AskToSave();
				SettingsManager.CurrentEqualizerProfile = profile;
			}
		}

		/// <summary>
		/// Invoked when the user clicks "Remove"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Remove_Click(object sender, RoutedEventArgs e)
		{
			EqualizerProfile profile = SettingsManager.CurrentEqualizerProfile;
			string itemToRemove = Profiles.SelectedItem as string;
			int index = Profiles.Items.IndexOf(itemToRemove) - 1;
			if (index < 0) index = 0;
			SettingsManager.EqualizerProfiles.Remove(profile);
			Profiles.SelectedIndex = index;
			Profiles.Items.Remove(itemToRemove);
		}

		/// <summary>
		/// Invoked when the user clicks "Rename"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Rename_Click(object sender, RoutedEventArgs e)
		{
			List<string> occupied = new List<string>();
			foreach (EqualizerProfile p in SettingsManager.EqualizerProfiles)
			{
				occupied.Add(p.Name);
				if (p.IsProtected)
					occupied.Add(U.T("EqualizerProfile" + p.Name));
			}
			EqualizerProfile profile = SettingsManager.CurrentEqualizerProfile;
			NameDialog dialog = new NameDialog(occupied, profile.Name);
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
			{
				string oldName = profile.Name;
				string newName = U.CleanXMLString(dialog.ProfileName.Text);

				for (int i = 0; i < Profiles.Items.Count; i++)
					if (Profiles.Items[i] as string == oldName)
						Profiles.Items[i] = newName;

				foreach (EqualizerProfile p in SettingsManager.EqualizerProfiles)
					if (p.Name == oldName)
						p.Name = newName;

				profile.Name = newName;
				SettingsManager.CurrentEqualizerProfile = profile;
			}
		}

		/// <summary>
		/// Invoked when a property of SettingsManager changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			if (e.PropertyName == "CurrentEqualizerProfile")
			{
				Refresh();
			}
		}

		/// <summary>
		/// Invoked when the dialog is about to be closed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>
		/// Will reset the equalizer levels on the media.
		/// </remarks>
		private void EqualizerDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			MediaManager.SetFX();
		}

		/// <summary>
		/// Sets the effects of the current equalizer values on the media.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private void PerformRefreshFX(object state)
		{
			MediaManager.RefreshFX(currentValues);
		}

		#endregion

		#endregion
	}
}
