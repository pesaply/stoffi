/**
 * MainWindow.xaml.cs
 * 
 * The main window of the migration tester.
 * It is used to test the Settings Migrator by providing
 * an interface for loading and migrating settings files.
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
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        #region Members

		private SettingsMigrator migrator = new SettingsMigrator();
		private string inFile = "", outFile = "";

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
		public MainWindow()
		{
			InitializeComponent();
			inFile = @"..\..\user2.config";
			outFile = @"..\..\user.config";
			SettingsFileLabel.Content = inFile;
		}

        #endregion

        #region Methods

        #region Event handlers

        /// <summary>
        /// Invoked when the user clicks "Load"
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data</param>
		private void LoadSettings_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			Nullable<bool> result = dlg.ShowDialog();
			if (result == true)
			{
				inFile = dlg.FileName;
				SettingsFileLabel.Content = dlg.FileName;
			}
		}

        /// <summary>
        /// Invoked when the user clicks "Migrate"
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Migrate_Click(object sender, RoutedEventArgs e)
		{
			if (inFile == "")
				MessageBox.Show("Select a settings file", "Select File", MessageBoxButton.OK, MessageBoxImage.Error);

			else if (!File.Exists(inFile))
				MessageBox.Show("No such settings file", "Select File", MessageBoxButton.OK, MessageBoxImage.Error);

			else
			{
				migrator.Migrate(inFile, outFile);
				MessageBox.Show("Migrated to " + outFile, "Migration Complete", MessageBoxButton.OK, MessageBoxImage.Information);
			}
        }

        #endregion

        #endregion
    }
}
