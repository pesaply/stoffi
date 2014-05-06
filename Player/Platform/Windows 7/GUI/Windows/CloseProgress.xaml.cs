/**
 * CloseProgress.cs
 * 
 * A window that performs the closing progress
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
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for UpgradeProgress.xaml
	/// </summary>
	public partial class CloseProgress : Window
	{
		/// <summary>
		/// Creates an instance of the UpgradeProgress control
		/// </summary>
		public CloseProgress()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Invoked when the control is loaded
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			FilesystemManager.ProgramIsClosed = true;
			progressBar.Maximum = 2000;
			progressBar.Value = 0;
			progressBar.IsIndeterminate = true;
			double max = progressBar.Maximum;

			ThreadStart CloseThread = delegate()
			{
				UpgradeManager.Stop();
				if (UpgradeManager.Pending)
				{
					Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
					{
						progressLabel.Content = U.T("SavingSettings", "Content");
					}));
					SettingsManager.Save();
					Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
					{
						progressLabel.Content = U.T("UpgradeInProgress");
					}));
					UpgradeManager.Finish();
				}
				UpgradeManager.Clean();
				MediaManager.Clean();

				Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
				{
					progressLabel.Content = U.T("Closing", "Content");
				}));
				Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
				{
					this.Close();
				}));
			};
			Thread closeThread = new Thread(CloseThread);
			closeThread.Name = "Close thread";
			closeThread.Priority = ThreadPriority.Normal;
			closeThread.Start();
		}
	}
}
