/***
 * ToolbarButton.xaml.cs
 * 
 * A button in the toolbar.
 * The toolbar can have a menu associated with it. This menu will
 * be toggled when the button is clicked. The arrow can either be
 * inline with the content (default) or seperated into its own
 * button with a separated click event.
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
 ***/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms.VisualStyles;

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// Interaction logic for ToolbarButton.xaml
	/// </summary>
	public partial class ToolbarButton : Button, INotifyPropertyChanged
	{
		#region Members

		private ContextMenu arrowMenu = null;
		private bool menuVisible = false;
		private bool pressingArrow = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets whether the arrow section
		/// (which is visible when a menu is present)
		/// should be split into a separate clickable
		/// area.
		/// </summary>
		public bool SplitArrow
		{
			get { return (bool)GetValue(SplitArrowProperty); }
			set { SetValue(SplitArrowProperty, value); OnPropertyChanged("SplitArrow"); }
		}

		/// <summary>
		/// The property describing whether arrow should be split from the
		/// rest of the button if visible.
		/// </summary>
		public static readonly DependencyProperty SplitArrowProperty =
			DependencyProperty.Register(
			"SplitArrow",
			typeof(bool),
			typeof(ToolbarButton),
			new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets the menu to show when the button
		/// (or the arrow if SplitArrow is true) is clicked.
		/// </summary>
		public ContextMenu ArrowMenu
		{
			get { return arrowMenu; }
			set
			{
				arrowMenu = value;
				if (arrowMenu != null)
				{
					arrowMenu.Opened += new RoutedEventHandler(ArrowMenu_Toggled);
					arrowMenu.Closed += new RoutedEventHandler(ArrowMenu_Toggled);
				}
				SetValue(ShowArrowProperty, arrowMenu != null);
				OnPropertyChanged("ArrowMenu");
				OnPropertyChanged("ShowArrow");
			}
		}

		/// <summary>
		/// Gets whether or not to show an arrow.
		/// </summary>
		public bool ShowArrow { get { return arrowMenu != null; } }

		/// <summary>
		/// The ShowArrow property of the control
		/// </summary>
		public static readonly DependencyProperty ShowArrowProperty =
			DependencyProperty.Register(
			"ShowArrow",
			typeof(bool),
			typeof(ToolbarButton),
			new PropertyMetadata(false));

		/// <summary>
		/// Gets whether or not to never show an arrow.
		/// </summary>
		public bool DisableArrow
		{
			get { return (bool)GetValue(DisableArrowProperty); }
			set { SetValue(DisableArrowProperty, value); OnPropertyChanged("DisableArrow"); }
		}

		/// <summary>
		/// The DisableArrow property of the control
		/// </summary>
		public static readonly DependencyProperty DisableArrowProperty =
			DependencyProperty.Register(
			"DisableArrow",
			typeof(bool),
			typeof(ToolbarButton),
			new PropertyMetadata(false));

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of a toolbar button.
		/// </summary>
		public ToolbarButton()
		{
			InitializeComponent();

			Style = (Style)FindResource("AeroStyle");
			if (VisualStyleInformation.DisplayName == "")
				Style = (Style)FindResource("ClassicStyle");
		}

		#endregion

		#region Methods

		#region Overrides

		/// <summary>
		/// Called when the ToolbarButton is clicked.
		/// </summary>
		protected override void OnClick()
		{
			if (arrowMenu != null && (!SplitArrow || pressingArrow))
			{
				arrowMenu.PlacementTarget = this;
				arrowMenu.Placement = PlacementMode.Bottom;
				menuVisible = !menuVisible;
				arrowMenu.IsOpen = menuVisible;
				if (pressingArrow)
					OnArrowClick();
			}
			if (!pressingArrow)
				base.OnClick();
			pressingArrow = false;
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the menu is opened or closed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void ArrowMenu_Toggled(object sender, RoutedEventArgs e)
		{
			menuVisible = arrowMenu.IsOpen;
		}

		/// <summary>
		/// Invoked when user presses the left mouse button over the
		/// separated arrow.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Arrow_Click(object sender, RoutedEventArgs e)
		{
			pressingArrow = true;
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>
		/// Dispatches the ArrowClick event
		/// </summary>
		public void OnArrowClick()
		{
			if (ArrowClick != null)
				ArrowClick(this, new RoutedEventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the arrow section is clicked.
		/// </summary>
		/// <remarks>
		/// Can only occur when SplitArrow is true and
		/// a ArrowMenu has been defined.
		/// </remarks>
		public event RoutedEventHandler ArrowClick;

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
