/**
 * Plugins.xaml.cs
 * 
 * The "Plugins" screen inside the "Control Panel".
 * It shows a list of all installed plugins with the
 * option to install additional plugins as well has
 * manage plugin configurations.
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

using Xceed.Wpf.Toolkit;

using Stoffi.Core;
using Stoffi.Core.Plugins;
using Stoffi.Plugins;
using PluginManager = Stoffi.Core.Plugins.Plugins;
using SettingsManager = Stoffi.Core.Settings.Manager;

namespace Stoffi.Player.GUI.Controls.ControlPanel
{
	/// <summary>
	/// Interaction logic for Plugins.xaml
	/// </summary>
	public partial class Plugins : ScrollViewer
	{
		#region Members

		private MenuItem enable;
		private MenuItem disable;
		private Dictionary<StatusLabel, TextBlock> currentStatusLabels = new Dictionary<StatusLabel, TextBlock>();

		#endregion

		#region Properties

		/// <summary>
		/// Gets the currently selected Plugin.
		/// </summary>
		private Plugin SelectedPlugin
		{
			get
			{
				var p = PluginList.SelectedItem as PluginItem;
				if (p == null)
					return null;
				else
					return PluginManager.Get(p.ID);
			}
		}

		#endregion

		#region Constructor

				/// <summary>
				/// Creates an instance of the Control Panel screen Plugins.
				/// </summary>
				public Plugins()
				{
					InitializeComponent();
					
					PluginList.Config = SettingsManager.PluginListConfig;
					PluginList.ItemsSource = SettingsManager.Plugins;
					SettingsManager.Plugins.CollectionChanged += PluginList.ItemsSource_CollectionChanged;
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Refreshes the strings according to current Culture language.
		/// </summary>
		public void RefreshStrings()
		{
			PluginList_SelectionChanged(null, null);
			if (PluginList != null)
				PluginList.RefreshView();
		}

		#endregion

		#region Private

		/// <summary>
		/// Populates the status label panel with the status labels of a given plugin.
		/// </summary>
		/// <param name="plugin">The plugin whose status labels to display</param>
		private void PopulateLabels(Plugin plugin)
		{
			StatusLabelGrid.RowDefinitions.Clear();
			for (int i=0; i < 5; i++)
				StatusLabelGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

			while (StatusLabelGrid.Children.Count > 10)
				StatusLabelGrid.Children.RemoveAt(10);

			PluginName.Text = plugin.T("Name");
			PluginDescription.Text = plugin.T("Description");
			PluginAuthor.Text = plugin.Author;
			PluginVersion.Text = plugin.Version.ToString();
			PluginType.Text = U.T(plugin.Type);
			PluginEnabled.Click -= PluginEnabled_Click;
			PluginEnabled.IsChecked = PluginManager.IsActive(plugin);
			PluginEnabled.Click += PluginEnabled_Click;

			if (plugin.StatusLabels != null)
				for (int i = 0; i < plugin.StatusLabels.Count; i++)
				{
					StatusLabel s = plugin.StatusLabels[i];

					// add row
					StatusLabelGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

					// create label
					TextBlock label = new TextBlock()
					{
						Text = plugin.T(s.Label),
						Margin = new Thickness(5, 5, 10, 5),
						VerticalAlignment = VerticalAlignment.Center
					};

					Grid.SetRow(label, i + 5);
					Grid.SetColumn(label, 0);
					StatusLabelGrid.Children.Add(label);

					// create status
					TextBlock status = new TextBlock()
					{
						Text = plugin.T(s.Status),
						Margin = new Thickness(0, 5, 0, 5),
						VerticalAlignment = VerticalAlignment.Center
					};

					Grid.SetRow(status, i + 5);
					Grid.SetColumn(status, 1);
					StatusLabelGrid.Children.Add(status);

					currentStatusLabels.Add(s, status);

					s.PropertyChanged += PluginStatus_PropertyChanged;
				}
		}

		/// <summary>
		/// Populates the settings panel with the settings of a given plugin.
		/// </summary>
		/// <param name="plugin">The plugin whose settings to display</param>
		private void PopulateSettings(Plugin plugin)
		{
			SettingsGrid.RowDefinitions.Clear();
			SettingsGrid.Children.Clear();

			for (int i = 0; i < plugin.Settings.Count; i++)
			{
				Setting s = plugin.Settings[i];

				// add row
				SettingsGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

				// create label
				TextBlock tb = new TextBlock()
				{
					Text = plugin.T(s.ID),
					Margin = new Thickness(5, 5, 10, 5),
					VerticalAlignment = VerticalAlignment.Center
				};

				tb.SetBinding(TextBlock.VisibilityProperty, new Binding("IsVisible")
				{
					Source = s,
					Mode = BindingMode.OneWay,
					Converter = new BooleanToVisibilityConverter()
				});

				Grid.SetRow(tb, i);
				Grid.SetColumn(tb, 0);
				SettingsGrid.Children.Add(tb);

				FrameworkElement control = null;

				// create control
				if (s.Type == typeof(Boolean))
				{
					// checkbox
					control = new CheckBox() { Height = 15 };
					control.SetBinding(CheckBox.IsCheckedProperty, new Binding("Value")
					{
						Source = s,
						Mode = BindingMode.TwoWay
					});
				}
				else if (s.Type == typeof(Color))
				{
					// color selector
					control = new ColorPicker()
					{
						ShowAvailableColors = false,
						ShowStandardColors = true,
						Width = 50,
					};
					if (s.PossibleValues != null)
					{
						ColorConverter converter = new ColorConverter();
						((ColorPicker)control).AvailableColors.Clear();
						foreach (Color c in s.PossibleValues)
						{
							System.Windows.Media.Color color = (System.Windows.Media.Color)converter.Convert(c, null, null, null);
							((ColorPicker)control).AvailableColors.Add(new ColorItem(color, c.Name));
						}
					}
					control.SetBinding(ColorPicker.SelectedColorProperty, new Binding("Value")
					{
						Source = s,
						Mode = BindingMode.TwoWay,
						Converter = new ColorConverter()
					});
				}
				else if (s.PossibleValues != null)
				{
					// dropdown
					control = new ComboBox();
					foreach (Object val in s.PossibleValues)
					{
						try
						{
							String content = val.ToString();
							if (s.Type == typeof(String))
								content = plugin.T(val.ToString());
							((ComboBox)control).Items.Add(new ComboBoxItem
							{
								Content = content,
								Name = val.ToString()
							});
						}
						catch (Exception exc)
						{
							U.L(LogLevel.Warning, "PLUGIN", "Could not add combobox item in plugin settings: " + exc.Message);
						}
					}
					((ComboBox)control).SelectedValuePath = "Name";
					control.SetBinding(ComboBox.SelectedValueProperty, new Binding("Value")
					{
						Source = s,
						Mode = BindingMode.TwoWay
					});
				}
				else if (s.Type == typeof(String))
				{
					// text input
					control = new TextBox()
					{
						MaxWidth = 400,
						MinWidth = 250
					};
					control.SetBinding(TextBox.TextProperty, new Binding("Value")
					{
						Source = s,
						Mode = BindingMode.TwoWay
					});
				}
				else if (s.Type == typeof(Int32))
				{
					if (s.Maximum != null)
					{
						// slider
						control = new Slider()
						{
							Maximum = (Int32)s.Maximum,
							AutoToolTipPlacement = AutoToolTipPlacement.TopLeft,
							Width = 200,
						};
						if (s.Minimum != null)
							((Slider)control).Minimum = (Int32)s.Minimum;
						control.SetBinding(Slider.ValueProperty, new Binding("Value")
						{
							Source = s,
							Mode = BindingMode.TwoWay
						});
					}
					else
					{
						// spinner
						control = new IntegerUpDown();
						control.SetBinding(IntegerUpDown.ValueProperty, new Binding("Value")
						{
							Source = s,
							Mode = BindingMode.TwoWay
						});
					}
				}
				else if (s.Type == typeof(Double))
				{
					if (s.Maximum != null)
					{
						// slider
						control = new Slider()
						{
							Maximum = (Double)s.Maximum,
							AutoToolTipPlacement = AutoToolTipPlacement.TopLeft,
							Width = 200,
						};
						if (s.Minimum != null)
							((Slider)control).Minimum = (Double)s.Minimum;
						control.SetBinding(Slider.ValueProperty, new Binding("Value")
						{
							Source = s,
							Mode = BindingMode.TwoWay
						});
					}
					else
					{
						// spinner
						control = new DoubleUpDown();
						control.SetBinding(DoubleUpDown.ValueProperty, new Binding("Value")
						{
							Source = s,
							Mode = BindingMode.TwoWay
						});
					}
				}

				if (control != null)
				{
					control.Margin = new Thickness(0, 5, 0, 5);
					control.VerticalAlignment = VerticalAlignment.Center;
					control.HorizontalAlignment = HorizontalAlignment.Left;

					control.SetBinding(FrameworkElement.VisibilityProperty, new Binding("IsVisible")
					{
						Source = s,
						Mode = BindingMode.OneWay,
						Converter = new BooleanToVisibilityConverter()
					});

					Grid.SetRow(control, i);
					Grid.SetColumn(control, 1);
					SettingsGrid.Children.Add(control);
				}
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the control is loaded.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Plugins_Loaded(object sender, RoutedEventArgs e)
		{
			foreach (MenuItem m in PluginList.ContextMenu.Items)
			{
				if ((string)m.Tag == "Enable")
					enable = m;
				else if ((string)m.Tag == "Disable")
					disable = m;
			}

			if (System.Windows.Forms.VisualStyles.VisualStyleInformation.DisplayName == "")
				Title.Style = (Style)FindResource("ClassicControlPanelTitleStyle");
		}

		/// <summary>
		/// Invoked when the user right-clicks the plugin list.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PluginList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			Plugin p = SelectedPlugin;
			bool b = p != null && PluginManager.IsActive(p);
			if (enable != null)
				enable.Visibility = b ? Visibility.Collapsed : Visibility.Visible;
			if (disable != null)
				disable.Visibility = b ? Visibility.Visible : Visibility.Collapsed;
				
		}

		/// <summary>
		/// Invoked when the user presses a key in the plugin list.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PluginList_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Delete:
					Plugin p = SelectedPlugin;
					if (p != null)
						PluginManager.Uninstall(p);
					break;
			}
		}

		/// <summary>
		/// Invoked when the user selects a plugin.
		/// </summary>
		/// <remarks>
		/// Shows the selected plugins settings.
		/// </remarks>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PluginList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (KeyValuePair<StatusLabel,TextBlock> l in currentStatusLabels)
				l.Key.PropertyChanged -= PluginStatus_PropertyChanged;
			currentStatusLabels.Clear();

			Plugin p = SelectedPlugin;

			if (p == null)
				Settings.Visibility = Visibility.Collapsed;

			else
			{
				PopulateLabels(p);
				PopulateSettings(p);
				Settings.Visibility = Visibility.Visible;
			}
		}

		/// <summary>
		/// Invoked when the user clicks Install.
		/// </summary>
		/// <remarks>
		/// Will install the plugin file that the user selects.
		/// </remarks>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Install_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.Title = U.T("DialogInstallAppTitle");
			dialog.Filter = String.Format("{0}|*.spp", U.T("FileAssociationSPP"));
			dialog.DefaultExt = ".spp";
			bool result = (bool)dialog.ShowDialog();
			if (result == true)
			{
				try
				{
					PluginManager.Install(dialog.FileName, true);
				}
				catch (Exception exc)
				{
					System.Windows.MessageBox.Show(
						exc.Message, U.T("MessageErrorInstallingApp", "Title"),
						MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Invoked when the user clicks Enable in context menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Enable_Click(object sender, RoutedEventArgs e)
		{
			Plugin p = SelectedPlugin;
			if (p != null && !PluginManager.IsActive(p))
			{
				if (p.Type == Stoffi.Plugins.PluginType.Visualizer)
					SettingsManager.CurrentVisualizer = p.ID;
				else
				{
					try
					{
						PluginManager.Start(p);
					}
					catch (Exception exc)
					{
						System.Windows.MessageBox.Show(
							exc.Message, U.T("MessageErrorStartingApp", "Title"), 
							MessageBoxButton.OK, MessageBoxImage.Error);
						CheckBox cb = sender as CheckBox;
						if (cb != null)
							cb.IsChecked = false;
					}
				}
			}
		}

		/// <summary>
		/// Invoked when the user clicks Disable in context menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Disable_Click(object sender, RoutedEventArgs e)
		{
			Plugin p = SelectedPlugin;
			if (p != null && PluginManager.IsActive(p))
			{
				if (p.Type == Stoffi.Plugins.PluginType.Visualizer)
					SettingsManager.CurrentVisualizer = null;
				else
					PluginManager.Stop(p);
			}
		}

		/// <summary>
		/// Invoked when the user clicks "Visit website" in context menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void VisitWebsite_Click(object sender, RoutedEventArgs e)
		{
			Plugin p = SelectedPlugin;
			if (p != null)
				Process.Start(p.Website);
		}

		/// <summary>
		/// Invoked when the user clicks Uninstall in context menu.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Uninstall_Click(object sender, RoutedEventArgs e)
		{
			Plugin p = SelectedPlugin;
			if (p != null)
				PluginManager.Uninstall(p);
		}

		/// <summary>
		/// Invoked when the user checks or unchecks the Active checkbox for a plugin.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PluginEnabled_Click(object sender, RoutedEventArgs e)
		{
			if ((bool)PluginEnabled.IsChecked)
				Enable_Click(sender, e);
			else
				Disable_Click(sender, e);
		}

		/// <summary>
		/// Invoked when the property of a plugin status changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void PluginStatus_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate()
			{
				Plugin p = SelectedPlugin;
				if (p != null)
				{
					StatusLabel s = sender as StatusLabel;
					if (s != null)
					{
						foreach (KeyValuePair<StatusLabel, TextBlock> l in currentStatusLabels)
						{
							if (l.Key.Label == s.Label)
								l.Value.Text = p.T(s.Status);
						}
					}
				}
			}));
		}

		#endregion

		#endregion
	}

	#region Converters

	/// <summary>
	/// Converts between System.Drawing.Color and System.Windows.Media.Color.
	/// </summary>
	[ValueConversion(typeof(Color), typeof(System.Windows.Media.Color))]
	public class ColorConverter : IValueConverter
	{
		/// <summary>
		/// Convert a System.Drawing.Color to a System.Windows.Media.Color.
		/// </summary>
		/// <param name="value">The System.Drawing.Color value</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Any additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>The corresponding System.Windows.Media.Color</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Color color = (Color)value;
			return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		/// <summary>
		/// Convert a System.Windows.Media.Color to a System.Drawing.Color.
		/// </summary>
		/// <param name="value">The System.Windows.Media.Color value</param>
		/// <param name="targetType">The type of the target (not used)</param>
		/// <param name="parameter">Any additional parameters (not used)</param>
		/// <param name="culture">The current culture (not used)</param>
		/// <returns>The corresponding System.Drawing.Color</returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			System.Windows.Media.Color color = (System.Windows.Media.Color)value;
			return Color.FromArgb(color.A, color.R, color.G, color.B);
		}
	}

	#endregion
}
