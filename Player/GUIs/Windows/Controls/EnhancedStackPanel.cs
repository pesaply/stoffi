/***
 * EnhancedStackPanel.cs
 * 
 * A modified StackPanel that can hide children if they don't fit.
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

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// A stack panel that can hide children which do not fit.
	/// </summary>
	public class EnhancedStackPanel : StackPanel
	{
		#region Properties

		/// <summary>
		/// Gets the currently hidden children.
		/// </summary>
		public List<UIElement> HiddenChildren { get; private set; }

		/// <summary>
		/// Gets or sets whether children which do not fit should be hidden.
		/// </summary>
		public bool ToggleChildren
		{
			get { return (bool)GetValue(ToggleChildrenProperty); }
			set { SetValue(ToggleChildrenProperty, value); }
		}

		/// <summary>
		/// The icon size dependency property.
		/// </summary>
		public static readonly DependencyProperty ToggleChildrenProperty =
			DependencyProperty.Register(
			"ToggleChildren",
			typeof(bool),
			typeof(EnhancedStackPanel),
			new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets the margin to the left or bottom which will be
		/// subtracted from the maximum allowed size.
		/// </summary>
		public double ToggleMargin { get; set; }

		/// <summary>
		/// Gets or sets whether the last child is a "show more" button.
		/// </summary>
		public bool LastIsMore { get; set; }

		/// <summary>
		/// Gets or sets whether any children are currently being hidden.
		/// </summary>
		public bool IsHiding
		{
			get { return (bool)GetValue(IsHidingProperty); }
			set { SetValue(IsHidingProperty, value); }
		}

		/// <summary>
		/// The icon size dependency property.
		/// </summary>
		public static readonly DependencyProperty IsHidingProperty =
			DependencyProperty.Register(
			"IsHiding",
			typeof(bool),
			typeof(EnhancedStackPanel),
			new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets the minimum number of children that should be visible.
		/// </summary>
		public int MinimumVisibleChildren
		{
			get { return (int)GetValue(MinimumVisibleChildrenProperty); }
			set { SetValue(MinimumVisibleChildrenProperty, value); }
		}

		/// <summary>
		/// The icon size dependency property.
		/// </summary>
		public static readonly DependencyProperty MinimumVisibleChildrenProperty =
			DependencyProperty.Register(
			"MinimumVisibleChildren",
			typeof(int),
			typeof(EnhancedStackPanel),
			new PropertyMetadata(1));

		#endregion

		#region Constructor

		/// <summary>
		/// Initiaties the EnhancedStackPanel class.
		/// </summary>
		static EnhancedStackPanel()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(EnhancedStackPanel), new FrameworkPropertyMetadata(typeof(EnhancedStackPanel)));
		}

		/// <summary>
		/// Creates an instance of the enhanced stack panel.
		/// </summary>
		public EnhancedStackPanel()
		{
			ToggleMargin = 0;
			LastIsMore = false;
			HiddenChildren = new List<UIElement>();
		}

		#endregion

		#region Methods

		#region Overrides

		/// <summary>
		/// Measures the child elements in anticipation of arranging them during the ArrangeOverride pass.
		/// </summary>
		/// <remarks>
		/// TODO: Consider margins.
		/// </remarks>
		/// <param name="constraint">An upper limit size that should not be exceeded</param>
		/// <returns>The desired size</returns>
		protected override Size MeasureOverride(Size constraint)
		{
			foreach (UIElement child in Children)
				child.Visibility = Visibility.Visible;

			Size desiredSize = base.MeasureOverride(constraint);

			int prevHidden = HiddenChildren.Count;
			HiddenChildren.Clear();
			double size = 0;
			bool isHor = Orientation == Orientation.Horizontal;
			double maxSize = isHor ? constraint.Width : constraint.Width;
			int i = 0;

			UIElement last = null;

			if (LastIsMore)
			{
				if (Children.Count > 0)
					last = Children[Children.Count - 1];
				maxSize -= isHor ? last.DesiredSize.Width : last.DesiredSize.Height;
			}

			foreach (UIElement child in Children)
			{
				if (LastIsMore && child == last)
					continue;

				size += isHor ? child.DesiredSize.Width : child.DesiredSize.Height;
				if (ToggleChildren && size > maxSize - ToggleMargin && i >= MinimumVisibleChildren)
				{
					child.Visibility = Visibility.Collapsed;
					HiddenChildren.Add(child);
				}
				i++;
			}
			if (HiddenChildren.Count != prevHidden) // we avoid firing the dependency property
			{
				IsHiding = HiddenChildren.Count > 0;
				OnHidingChanged();
			}

			if (LastIsMore)
				last.Visibility = IsHiding ? Visibility.Visible : Visibility.Collapsed;

			return desiredSize;
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the HidingChanged event.
		/// </summary>
		private void OnHidingChanged()
		{
			if (HidingChanged != null)
				HidingChanged(this, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the number of hidden children changes.
		/// </summary>
		public event EventHandler HidingChanged;

		#endregion
	}
}
