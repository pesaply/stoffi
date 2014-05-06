/**
 * Welcome.cs
 * 
 * A task dialog allowing the user to configure file
 * associations.
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
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;

using System.Windows;

namespace Stoffi
{
	/// <summary>
	/// A welcome procedure for new users to the application.
	/// </summary>
	public static class Welcome
	{
		/// <summary>
		/// Shows the dialog.
		/// </summary>
		public static TaskDialogResult Show(IntPtr owner)
		{
			TaskDialog td = new TaskDialog();

			//var cusButton = new TaskDialogCommandLink("cusButton", U.T("AssociationsChoose"), U.T("AssociationsChooseText"));
			//var skipButton = new TaskDialogCommandLink("skipButton", U.T("AssociationsSkip"), U.T("AssociationsSkipText"));
			//var defButton = new TaskDialogCommandLink("defButton", U.T("AssociationsYes"), U.T("AssociationsYesText"));
			var cusButton = new TaskDialogButton("cusButton", U.T("AssociationsChoose"));
			var skipButton = new TaskDialogButton("skipButton", U.T("AssociationsSkip"));
			var defButton = new TaskDialogButton("defButton", U.T("AssociationsYes"));

			td.HyperlinksEnabled = false;
			defButton.UseElevationIcon = true;
			defButton.Default = true;
			defButton.Click += new EventHandler(defButton_Click);
			cusButton.Click += new EventHandler(cusButton_Click);
			skipButton.Click += new EventHandler(skipButton_Click);

			td.Controls.Add(defButton);
			td.Controls.Add(cusButton);
			td.Controls.Add(skipButton);

			td.Caption = U.T("AssociationsCaption");
			td.InstructionText = U.T("AssociationsInstruction");
			//td.Text = U.T("AssociationsText");
			td.StartupLocation = TaskDialogStartupLocation.CenterOwner;
			td.OwnerWindowHandle = owner;
			td.Icon = TaskDialogStandardIcon.Shield;
			return td.Show();
		}

		/// <summary>
		/// Invoked when the user clicks on the default button.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void defButton_Click(object sender, EventArgs e)
		{
			TaskDialog td = ((sender as TaskDialogButton).HostingDialog as TaskDialog);
			td.Close(TaskDialogResult.Yes);
		}

		/// <summary>
		/// Invoked when the user clicks on the Skip button.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void cusButton_Click(object sender, EventArgs e)
		{
			TaskDialog td = ((sender as TaskDialogButton).HostingDialog as TaskDialog);
			td.Close(TaskDialogResult.No); // CustomButtonClicked doesn't work for some unknown reason
		}

		/// <summary>
		/// Invoked when the user clicks on the Skip button.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void skipButton_Click(object sender, EventArgs e)
		{
			TaskDialog td = ((sender as TaskDialogButton).HostingDialog as TaskDialog);
			td.Close(TaskDialogResult.Cancel);
		}
	}
}
