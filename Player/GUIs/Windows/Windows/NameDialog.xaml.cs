/**
 * NameDialog.xaml.cs
 * 
 * The small dialog shown when creating or renaming a keyboard
 * shortcut profile.
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Stoffi.Core;

namespace Stoffi.Player.GUI.Windows
{
	/// <summary>
	/// Interaction logic for NameDialog.xaml
	/// </summary>
	public partial class NameDialog : Window
	{
		#region Members

		private List<string> occupiedNames;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the name we are renaming from.
		/// If set to "" the dialog will act as if we are creating a name.
		/// </summary>
		public String RenameFrom { get; set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the name dialog
		/// </summary>
		/// <param name="occupied">A list of all occupied names</param>
		/// <param name="renameFrom">The original name. If set to "" dialog will act as creating a name</param>
		public NameDialog(List<string> occupied, String renameFrom = "")
		{
			U.L(LogLevel.Debug, "NAME DIALOG", "Initialize");
			InitializeComponent();
			U.L(LogLevel.Debug, "NAME DIALOG", "Initialized");
			occupiedNames = occupied;
			RenameFrom = renameFrom;
			if (RenameFrom != "") Title = U.T("DialogNameRenameTitle");
			else Title = U.T("DialogNameCreateTitle");
			ProfileName.Text = RenameFrom;
			ProfileName.Focus();
			ProfileName.SelectAll();
		}

		#endregion

		#region Methods

		#region Event handlers

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OK_Click(object sender, RoutedEventArgs e)
		{
			// allow rename to same name
			if (ProfileName.Text == RenameFrom && RenameFrom != "")
			{
				DialogResult = false;
				Close();
			}

			// check for duplicate names
			foreach (string name in occupiedNames)
			{
				if (name == ProfileName.Text)
				{
					Error.Text = U.T("DialogNameExistsError");
					Error.Visibility = System.Windows.Visibility.Visible;
					return;
				}
			}

			// check for invalid name
			// TODO: Pattern is too restrictive!
			Regex alphaNumPattern = new Regex("[^a-zA-Z0-9 ]");
			if (alphaNumPattern.IsMatch(ProfileName.Text))
			{
				Error.Text = U.T("DialogNameInvalidError");
				Error.Visibility = System.Windows.Visibility.Visible;
				return;
			}

			// check for empty name
			if (ProfileName.Text.Replace(" ", "").Replace("\t", "") == "")
			{
				Error.Text = U.T("DialogNameEmptyError");
				Error.Visibility = System.Windows.Visibility.Visible;
				return;
			}

			DialogResult = true;
			Close();
		}

		#endregion

		private void ShortcutProfileDialog_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				DialogResult = false;
				Close();
			}
		}

		#endregion
	}
}
