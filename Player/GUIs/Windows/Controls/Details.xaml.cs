/**
 * Details.xaml.cs
 * 
 * The details pane which shows an image, a title, a description
 * and a number of key-value fields.
 * It automatically hides fields that cannot be shown and supports
 * for in-place edit of certain fields.
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

using Stoffi.Core;

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// Interaction logic for Details.xaml
	/// </summary>
	public partial class Details : StatusBar
	{
		#region Members
		private List<TextBlock> labels = new List<TextBlock>();
		private List<FrameworkElement> fields = new List<FrameworkElement>();
		private double cellWidth = 120;
		private double labelWidth = 110;
		private double cellHeight = 20;
		private List<ImageSource> images = new List<ImageSource>();
		#endregion

		#region Properties

		/// <summary>
		/// The title of the item
		/// </summary>
		public string Title
		{
			get { return TitleBlock.Text; }
			set { TitleBlock.Text = value; }
		}

		/// <summary>
		/// A short description of the item
		/// </summary>
		public string Description
		{
			get { return DescrBlock.Text; }
			set { DescrBlock.Text = value; }
		}

		/// <summary>
		/// Sets the thumbnail image.
		/// </summary>
		public ImageSource Image
		{
			get { return Thumbnail.Source; }
			set
			{
				images.Clear();
				images.Add(value);
				RefreshImage();
			}
		}

		/// <summary>
		/// Gets or sets the list of thumbnail images.
		/// </summary>
		public List<ImageSource> Images
		{
			get { return images; }
			set { images = value; RefreshImage(); }
		}

		#endregion

		#region Constructor
		/// <summary>
		/// Details Pane
		/// </summary>
		public Details()
		{
			//U.L(LogLevel.Debug, "DETAILS", "Initialize");
			InitializeComponent();
			//U.L(LogLevel.Debug, "DETAILS", "Initialized");
			FieldsGrid.RowDefinitions[0].Height = new GridLength(cellHeight);
			FieldsGrid.RowDefinitions[1].Height = new GridLength(cellHeight);
			FieldsGrid.ColumnDefinitions[0].Width = new GridLength(labelWidth);
			FieldsGrid.ColumnDefinitions[1].Width = new GridLength(cellWidth);

			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
				DescrBlock.Foreground = Brushes.Black;
			//U.L(LogLevel.Debug, "DETAILS", "Created");
		}
		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Adds a text field
		/// </summary>
		/// <param name="name">The name of the field</param>
		/// <param name="value">The value of the field</param>
		/// <param name="editable">Whether the field can be edited</param>
		public void AddField(string name, string value, bool editable = false)
		{
			EditableTextBlock etb = new EditableTextBlock();
			etb.IsEditable = editable;
			etb.ClickToEdit = editable;
			etb.Text = value;
			etb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
			etb.Edited += new EditableTextBlockDelegate(Field_Edited);
			etb.EnteredEditMode += new EventHandler(Field_EnteredEditMode);
			AddField(name, etb);
		}

		/// <summary>
		/// Adds a text field with a binding to an object.
		/// </summary>
		/// <param name="name">The text to display as the label of the field</param>
		/// <param name="source">The object source which holds the value of the field</param>
		/// <param name="field">The name of the property which should be displayed in the field</param>
		/// <param name="editable">Whether or not the field can be edited</param>
		/// <param name="converter">A converter to use to convert the value of the binding</param>
		/// <remarks>TODO: Appearantly not working for some reason... :(</remarks>
		public void AddTextField(string name, object source, string field, bool editable = false, IValueConverter converter = null)
		{
			Binding binding = new Binding(field);
			binding.Source = source;
			binding.Mode = BindingMode.OneWay;
			if (converter != null)
				binding.Converter = converter;

			EditableTextBlock etb = new EditableTextBlock();
			etb.IsEditable = editable;
			etb.ClickToEdit = editable;
			etb.DataContext = source;
			etb.SetBinding(EditableTextBlock.TextProperty, binding);
			etb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
			etb.Edited += new EditableTextBlockDelegate(Field_Edited);
			etb.EnteredEditMode += new EventHandler(Field_EnteredEditMode);
			AddField(name, etb);
		}

		/// <summary>
		/// Adds a rating field
		/// </summary>
		/// <param name="name">The name of the field</param>
		/// <param name="i">The rate value (0-10)</param>
		public void AddField(string name, int i)
		{
			// TODO: create rate control
		}

		/// <summary>
		/// Adds a field
		/// </summary>
		/// <param name="name">The name of the field</param>
		/// <param name="e">The object representing the value</param>
		public void AddField(string name, FrameworkElement e)
		{
			e.Tag = name;

			int col = (int)Math.Floor((double)fields.Count + 2) / FieldsGrid.RowDefinitions.Count;
			int row = fields.Count + 2 - col * FieldsGrid.RowDefinitions.Count;
			col *= 2;

			TextBlock label = new TextBlock();
			label.Text = name + ':';
			label.TextAlignment = TextAlignment.Right;
			label.VerticalAlignment = System.Windows.VerticalAlignment.Center;
			label.Margin = new Thickness(0, 0, 2, 0);
			label.Padding = new Thickness(0);
			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName != "")
				label.Foreground = new SolidColorBrush(Color.FromRgb(90, 103, 121));
			label.Tag = name;
			Grid.SetRow(label, row);
			Grid.SetColumn(label, col);

			e.Margin = new Thickness(2);
			e.Tag = name;
			Grid.SetRow(e, row);
			Grid.SetColumn(e, col + 1);

			if (col >= FieldsGrid.ColumnDefinitions.Count || row >= FieldsGrid.RowDefinitions.Count)
			{
				label.Visibility = System.Windows.Visibility.Collapsed;
				e.Visibility = System.Windows.Visibility.Collapsed;
			}

			FieldsGrid.Children.Add(label);
			labels.Add(label);
			FieldsGrid.Children.Add(e);
			fields.Add(e);
			RefreshGrid();
		}

		/// <summary>
		/// Removes a field
		/// </summary>
		/// <param name="name">The name of the field to remove</param>
		public void RemoveField(string name)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				FrameworkElement e = fields[i];
				if ((string)e.Tag == name)
				{
					FieldsGrid.Children.Remove(e);
					fields.RemoveAt(i);
					i--;
				}
			}
			for (int i = 0; i < labels.Count; i++)
			{
				FrameworkElement e = fields[i];
				if ((string)e.Tag == name)
				{
					FieldsGrid.Children.Remove(e);
					labels.RemoveAt(i);
					i--;
				}
			}
			RefreshGrid();
		}

		/// <summary>
		/// Changes the value of a field
		/// </summary>
		/// <param name="name">The name of the field</param>
		/// <param name="value">The new value of the field</param>
		public void SetField(string name, string value)
		{
			foreach (FrameworkElement e in fields)
			{
				if (e.Tag as string == name && e is EditableTextBlock)
					(e as EditableTextBlock).Text = value;
				else if (e.Tag as string == name && e is TextBlock)
					(e as TextBlock).Text = value;
			}
		}

		/// <summary>
		/// Removes all fields and resets control
		/// </summary>
		public void Clear()
		{
			Thumbnail.Visibility = System.Windows.Visibility.Hidden;

			Title = "";
			Description = "";

			ClearFields();
		}

		/// <summary>
		/// Removes all fields.
		/// </summary>
		public void ClearFields()
		{
			fields.Clear();
			labels.Clear();
			FieldsGrid.Children.Clear();

			FieldsGrid.Children.Add(TitleBlock);
			FieldsGrid.Children.Add(DescrBlock);

			RefreshGrid();
		}

		#endregion

		#region Private

		/// <summary>
		/// Refreshes the content inside the grid
		/// </summary>
		private void RefreshFields()
		{
			int i = 0;
			for (int col = 0; i < fields.Count && col < FieldsGrid.ColumnDefinitions.Count; col += 2) // double increase
			{
			    for (int row = col == 0 ? 2 : 0; i < fields.Count && row < FieldsGrid.RowDefinitions.Count; row++, i++)
			    {
			        fields[i].Visibility = System.Windows.Visibility.Visible;
			        labels[i].Visibility = System.Windows.Visibility.Visible;
			        Grid.SetColumn(labels[i], col);
			        Grid.SetRow(labels[i], row);
			        Grid.SetColumn(fields[i], col + 1);
			        Grid.SetRow(fields[i], row);
			    }
			}
			while (i < fields.Count)
			{
			    fields[i].Visibility = System.Windows.Visibility.Collapsed;
			    labels[i].Visibility = System.Windows.Visibility.Collapsed;
			    i++;
			}
		}

		/// <summary>
		/// Refreshes the grids column and row configuration
		/// </summary>
		private void RefreshGrid()
		{
			#region Add columns

			int allowed = (int)Math.Floor(RightPart.RenderSize.Width / (cellWidth + labelWidth));
			int actual = FieldsGrid.ColumnDefinitions.Count / 2;

			if (allowed > actual)
			{
				int add = allowed - actual;

				for (int i = 0; i < add; i++)
				{
					FieldsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(labelWidth) });
					FieldsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(cellWidth) });
					RefreshFields();
				}
			}
			#endregion

			#region Remove columns
			if (actual > allowed)
			{
				int remove = actual - allowed;
				FieldsGrid.ColumnDefinitions.RemoveRange(
					FieldsGrid.ColumnDefinitions.Count - (remove*2), remove*2);
			}
			#endregion

			allowed = (int)Math.Floor(RightPart.RenderSize.Height / cellHeight);
			actual = FieldsGrid.RowDefinitions.Count;

			#region Add rows
			if (allowed > actual)
			{
				for (int i = 0; i < allowed - actual; i++)
				{
					int index = actual + i;
					double add = allowed;
					FieldsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(cellHeight) });

					for (int j = 0; j < FieldsGrid.ColumnDefinitions.Count; j += 2)
					{
						TextBlock tb1 = new TextBlock()
						{
							Text = "Foo:",
							Background = Brushes.Blue,
							Margin = new Thickness(2),
							Width = labelWidth
						};

						TextBlock tb2 = new TextBlock()
						{
							Text = "Bar",
							Background = Brushes.Red,
							Margin = new Thickness(2),
							Width = cellWidth
						};

						Grid.SetColumn(tb1, j);
						Grid.SetColumn(tb2, j + 1);

						Grid.SetRow(tb1, FieldsGrid.RowDefinitions.Count - 1);
						Grid.SetRow(tb2, FieldsGrid.RowDefinitions.Count - 1);
					}
				}
			}
			#endregion

			#region Remove rows
			else if (allowed < actual)
			{
				int remove = actual - allowed;
				FieldsGrid.RowDefinitions.RemoveRange(
					FieldsGrid.RowDefinitions.Count - remove, remove);

				if (FieldsGrid.RowDefinitions.Count == 0)
					FieldsGrid.Children.Clear();
			}
			#endregion

			RefreshFields();
		}

		/// <summary>
		/// Refreshes the image according to size of the control.
		/// </summary>
		private void RefreshImage()
		{
			if (images.Count == 0)
			{
				Thumbnail.Source = null;
				Thumbnail.Visibility = Visibility.Hidden;
			}
			else if (images.Count == 1)
			{
				Thumbnail.Source = images[0];
				Thumbnail.Visibility = Visibility.Visible;
			}
			else
			{
				// find the two images that are nearest in size (both larger and smaller than allowed size)
				double nlSize, nsSize;
				double allowedSize = ThumbnailContainer.RenderSize.Height;
				ImageSource nearestLarger = images[0];
				ImageSource nearestSmaller = images[0];
				foreach (ImageSource img in images)
				{
					try
					{
						double size = Math.Max(img.Width, img.Height);
						nlSize = Math.Max(nearestLarger.Width, nearestLarger.Height);
						nsSize = Math.Max(nearestSmaller.Width, nearestSmaller.Height);

						if ((nlSize < allowedSize && size > nlSize) ||
							(nlSize > allowedSize && size >= allowedSize && nlSize > size))
							nearestLarger = img;

						if ((nsSize > allowedSize && size < nsSize) ||
							(nsSize < allowedSize && size <= allowedSize && nsSize < size))
							nearestSmaller = img;
							
					}
					catch { }
				}

				// choose the image that is closest in area to the
				// allowed area, using a weighted distance measure
				nlSize = Math.Max(nearestLarger.Width, nearestLarger.Height);
				nsSize = Math.Max(nearestSmaller.Width, nearestSmaller.Height);
				
				// 0.0 = always choose larger
				// 0.5 = unbiased
				// 1.0 = always choose smaller
				double weight = 0.25;

				double nlDist = Math.Abs(nlSize - allowedSize) * weight;
				double nsDist = Math.Abs(nsSize - allowedSize) * (1.0 - weight);
				Thumbnail.Source = nlDist > nsDist ? nearestSmaller : nearestLarger;
				Thumbnail.Visibility = Visibility.Visible;
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the size changes.
		/// Will add or remove rows or columns
		/// and call Refresh().
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Details_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RefreshGrid();
			RefreshImage();
		}

		/// <summary>
		/// Invoked when a field has been edited
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Field_Edited(object sender, EditableTextBlockEventArgs e)
		{
			EditableTextBlock etb = sender as EditableTextBlock;
			OnFieldEdited(etb.Tag as string, e.NewText);
		}

		/// <summary>
		/// Invoked when a field enters edit mode
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Field_EnteredEditMode(object sender, EventArgs e)
		{
			OnEnteredEditMode(sender);
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the FieldEdited event
		/// </summary>
		/// <param name="name">The name of the field</param>
		/// <param name="value">The new value of the field</param>
		private void OnFieldEdited(string name, string value)
		{
			if (FieldEdited != null)
				FieldEdited(this, new FieldEditedEventArgs(name, value));
		}

		/// <summary>
		/// Dispatches the EnteredEditMode event
		/// </summary>
		/// <param name="etb">The object that sent the event</param>
		private void OnEnteredEditMode(object etb)
		{
			if (EnteredEditMode != null)
				EnteredEditMode(etb, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when a field was edited
		/// </summary>
		public event FieldEditedEventHandler FieldEdited;


		/// <summary>
		/// Occurs when the control enters edit mode
		/// </summary>
		public event EventHandler EnteredEditMode;

		#endregion

		#region Delegates

		/// <summary>
		/// Represents the method that will handle the FieldEdited event.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		public delegate void FieldEditedEventHandler(object sender, FieldEditedEventArgs e);

		#endregion
	}

	#region Event arguments

	/// <summary>
	/// Provides data for the <see cref="Details.FieldEdited"/> event
	/// </summary>
	public class FieldEditedEventArgs : EventArgs
	{
		#region Properties

		/// <summary>
		/// Gets the name of the field
		/// </summary>
		public string Field { get; private set; }

		/// <summary>
		/// Gets the new value of the field
		/// </summary>
		public string Value { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldEditedEventArgs"/> class
		/// </summary>
		/// <param name="field">The name of the field</param>
		/// <param name="value">The new value of the field</param>
		public FieldEditedEventArgs(string field, string value)
		{
			Field = field;
			Value = value;
		}

		#endregion
	}

	#endregion
}
