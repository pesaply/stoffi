/**
 * EnhancedSlider.xaml.cs
 * 
 * Contains the logic for an enhanced slider.
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

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for EnhancedSlider.xaml
	/// </summary>
	public partial class EnhancedSlider : Slider
	{

		#region Members

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the second value of the slider.
		/// It will only show if it is higher than Value.
		/// </summary>
		public double SecondValue
		{
			get
			{
				return (SecondValueWidth / ActualWidth) * Maximum;
			}
			set
			{
				SecondValueWidth = ActualWidth * (value / Maximum);
			}
		}

		/// <summary>
		/// Gets or sets the actual width (in pixels) of the
		/// second value indicator.
		/// </summary>
		public double SecondValueWidth
		{
			get
			{
				return (double)GetValue(SecondValueWidthProperty);
			}
			set
			{
				SetValue(SecondValueWidthProperty, value);
			}
		}

		/// <summary>
		/// The DependencyProperty for SecondValueWidth
		/// </summary>
		public static readonly DependencyProperty SecondValueWidthProperty =
			DependencyProperty.Register("SecondValueWidth", typeof(double), typeof(EnhancedSlider), new UIPropertyMetadata(0.0));

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of a Slider.
		/// </summary>
		public EnhancedSlider()
		{
			//U.L(LogLevel.Debug, "ENHANCED SLIDER", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "ENHANCED SLIDER", "Initialized");
			DataContext = this;
		}

		#endregion

		#region Methods

		#endregion
	}
}
