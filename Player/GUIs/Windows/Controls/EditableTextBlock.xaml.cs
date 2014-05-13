/***
 * EditableTextBlock.xaml.cs
 * 
 * A custom textblock that can be turned into a textbox for
 * editing.
 * 
 * * * * * * * * *
 * 
 * Copyright 2013 Jesper Borgtrup, Simplare
 * 
 * This code is based on the work by Jesper Borgtrup which can
 * be found at
 * www.codeproject.com/KB/WPF/editabletextblock.aspx
 * It is part of the Stoffi Music Player Project.
 * Visit our website at: stoffiplayer.com
 *
 * This code is licensed under the terms of the Code Project
 * Open License as published by the Code Project; only version
 * 1.02 of the License.
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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Stoffi.Core;

namespace Stoffi.Player.GUI.Controls
{
	/// <summary>
	/// 
	/// </summary>
	public partial class EditableTextBlock : UserControl, INotifyPropertyChanged
	{
		#region Members

		// We keep the old text when we go into editmode
		// in case the user aborts with the escape key
		private string oldText;

		private string customText = "";

		private bool clickToEdit = false;

		private TextBox box = null;

		#endregion

		#region Properties

		/// <summary>
		/// Gets current text of the text box
		/// </summary>
		public string CurrentText { get; private set; }

		/// <summary>
		/// Gets or sets the text value
		/// </summary>
		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set
			{
				if (IsInEditMode)
				{
					customText = "";
					if (box != null)
						box.Text = value;
					else
						customText = value;
				}
				else
				{
					SetValue(TextProperty, value);
					OnPropertyChanged("Text");
					OnPropertyChanged("FormattedText");
				}
			}
		}

		/// <summary>
		/// The text property of the control
		/// </summary>
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
			"Text",
			typeof(string),
			typeof(EditableTextBlock),
			new PropertyMetadata(""));

		/// <summary>
		/// Gets or sets whether the control is editable
		/// </summary>
		public bool IsEditable
		{
			get { return (bool)GetValue(IsEditableProperty); }
			set { SetValue(IsEditableProperty, value); }
		}

		/// <summary>
		/// The property describing whether the control is editable
		/// </summary>
		public static readonly DependencyProperty IsEditableProperty =
			DependencyProperty.Register(
			"IsEditable",
			typeof(bool),
			typeof(EditableTextBlock),
			new PropertyMetadata(true));

		/// <summary>
		/// Gets or sets whether the control is in edit mode
		/// Note: Must be editable for this to take effect
		/// </summary>
		public bool IsInEditMode
		{
			get
			{
				if (IsEditable)
					return (bool)GetValue(IsInEditModeProperty);
				else
					return false;
			}
			set
			{
				if (IsEditable)
				{
					if (value)
					{
						oldText = Text;
						DispatchEnteredEditMode();
					}
					U.ListenForShortcut = !value;
					SetValue(IsInEditModeProperty, value);
				}
			}
		}

		/// <summary>
		/// The property describing whether the control is in edit mode
		/// </summary>
		public static readonly DependencyProperty IsInEditModeProperty =
			DependencyProperty.Register(
			"IsInEditMode",
			typeof(bool),
			typeof(EditableTextBlock),
			new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets the format of the text
		/// </summary>
		public string TextFormat
		{
			get { return (string)GetValue(TextFormatProperty); }
			set
			{
				if (value == "") value = "{0}";
				SetValue(TextFormatProperty, value);
			}
		}

		/// <summary>
		/// The property describing the format of the text value
		/// </summary>
		public static readonly DependencyProperty TextFormatProperty =
			DependencyProperty.Register(
			"TextFormat",
			typeof(string),
			typeof(EditableTextBlock),
			new PropertyMetadata("{0}"));

		/// <summary>
		/// Gets the formatted text value
		/// </summary>
		public string FormattedText
		{
			get { return String.Format(TextFormat, Text); }
		}

		/// <summary>
		/// Gets or sets whether the user can initiate
		/// edit mode by clicking on the text.
		/// </summary>
		public bool ClickToEdit
		{
			get { return clickToEdit; }
			set { clickToEdit = value; }
		}

		/// <summary>
		/// Gets or sets whether the hover effect is a simple black
		/// line (true) or if it looks similar to a textbox (false).
		/// </summary>
		public bool SimpleHover { get; set; }

		/// <summary>
		/// Gets or sets whether the cursor should change into
		/// a hand when the mouse is hovered over the control.
		/// </summary>
		public bool HandHover { get; set; }

		#endregion PropertiesWindow

		#region Constructor

		/// <summary>
		/// Creates an editable text block
		/// </summary>
		public EditableTextBlock()
		{
			ClickToEdit = false;
			SimpleHover = true;
			InitializeComponent();
			base.Focusable = true;
			base.FocusVisualStyle = null;
		}

		#endregion Constructor

		#region Methods

		#region Public

		/// <summary>
		/// Cancels the edit
		/// </summary>
		public void Cancel()
		{
			Text = oldText;
			this.IsInEditMode = false;
			DispatchCanceledEvent();
		}

		/// <summary>
		/// Ends the edit successfully
		/// </summary>
		public void Done()
		{
			this.IsInEditMode = false;
			if (CurrentText != oldText)
				DispatchEditedEvent(CurrentText, oldText);
			else
				DispatchCanceledEvent();
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when we enter edit mode.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TextBox_Loaded(object sender, RoutedEventArgs e)
		{
			TextBox txt = sender as TextBox;
			box = txt;
			if (!String.IsNullOrWhiteSpace(customText))
			{
				txt.Text = customText;
				customText = "";
			}
			else
				txt.Text = Text;
			CurrentText = txt.Text;

			// Give the TextBox input focus
			txt.Focus();

			txt.SelectAll();
		}

		/// <summary>
		///  Invoked when we exit edit mode.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (this.IsInEditMode)
				Done();
		}

		/// <summary>
		/// Invoked when the user edits the annotation.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Done();
				e.Handled = true;
			}
			else if (e.Key == Key.Escape)
			{
				Cancel();
				e.Handled = true;
			}
			else
			{
				TextBox txt = sender as TextBox;
				if (txt != null)
					CurrentText = txt.Text;
			}
		}

		/// <summary>
		/// Invoked when the user presses a key in the text box.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		/// <remarks>
		/// If the key is numpad minus the event will be prevented
		/// from further propagation. This is because otherwise a
		/// parent TreeViewItem might collapse.
		/// </remarks>
		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Subtract)
			{
				e.Handled = true;
				TextBox txt = sender as TextBox;
				if (txt != null)
				{
					string t = txt.Text;
					int p = txt.SelectionStart;
					int l = txt.SelectionLength;
					txt.Text = String.Format("{0}-{1}", t.Substring(0, p), t.Substring(p + l));
					CurrentText = txt.Text;
					txt.SelectionStart = p + 1;
				}
			}
		}

		/// <summary>
		/// Invoked when the user clicks on the control.
		/// Will enter edit mode if ClickToEdit is true.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			IsInEditMode = (!IsInEditMode && ClickToEdit);
		}

		#endregion Event Handlers

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the control is edited succesfully
		/// </summary>
		public event EditableTextBlockDelegate Edited;
		
		/// <summary>
		/// Occurs when the edit is canceled
		/// </summary>
		public event EditableTextBlockCanceledDelegate Canceled;

		/// <summary>
		/// Occurs when the control enters edit mode
		/// </summary>
		public event EventHandler EnteredEditMode;

		/// <summary>
		/// The dispatcher for the EnteredEditMode event
		/// </summary>
		private void DispatchEnteredEditMode()
		{
			if (EnteredEditMode != null)
			{
				EnteredEditMode(this, null);
			}
		}

		/// <summary>
		/// The subscriber for the edited event
		/// </summary>
		/// <param name="eventHandler">The event handler</param>
		private void SubscribeEdited(EditableTextBlockDelegate eventHandler)
		{
			Edited += eventHandler;
		}

		/// <summary>
		/// The unsubscriber for the edited event
		/// </summary>
		/// <param name="eventHandler">The event handler</param>
		private void UnsubsribeEdited(EditableTextBlockDelegate eventHandler)
		{
			Edited -= eventHandler;
		}

		/// <summary>
		/// The dispatcher for the edit event
		/// </summary>
		/// <param name="ntxt">The new text value</param>
		/// <param name="otxt">The old text value</param>
		private void DispatchEditedEvent(string ntxt, string otxt)
		{
			if (Edited != null)
			{
				Edited(this, new EditableTextBlockEventArgs(ntxt,otxt));
			}
		}

		/// <summary>
		/// The subscriber for the canceled event
		/// </summary>
		/// <param name="eventHandler">The event handler</param>
		private void SubscribeCanceled(EditableTextBlockDelegate eventHandler)
		{
			Edited += eventHandler;
		}

		/// <summary>
		/// The unsubscriber for the canceled event
		/// </summary>
		/// <param name="eventHandler">The event handler</param>
		private void UnsubsribeCanceled(EditableTextBlockDelegate eventHandler)
		{
			Edited -= eventHandler;
		}

		/// <summary>
		/// The dispatcher for the canceled event
		/// </summary>
		private void DispatchCanceledEvent()
		{
			if (Canceled != null)
			{
				Canceled(this, new EventArgs());
			}
		}

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// 
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion
	}

	/// <summary>
	/// Provides data for the <see cref="EditableTextBlock.Edited"/> event
	/// </summary>
	public class EditableTextBlockEventArgs : EventArgs
	{
		#region Members

		private string newText;
		private string oldText;

		#endregion

		#region Properties

		/// <summary>
		/// The new text value
		/// </summary>
		public string NewText { get { return newText; } }
		
		/// <summary>
		/// The old text value
		/// </summary>
		public string OldText { get { return oldText; } }

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="EditableTextBlockEventArgs"/> class
		/// </summary>
		/// <param name="ntxt">The new text value</param>
		/// <param name="otxt">The old text value</param>
		public EditableTextBlockEventArgs(string ntxt, string otxt)
		{
			newText = ntxt;
			oldText = otxt;
		}

		#endregion
	}

	#region Events

	/// <summary>
	/// Represents the method that will handle the <see cref="EditableTextBlock.Edited"/> event.
	/// </summary>
	/// <param name="e">The event arguments</param>
	/// <param name="sender">The sender of the event</param>
	public delegate void EditableTextBlockDelegate(object sender, EditableTextBlockEventArgs e);

	/// <summary>
	/// Represents the method that will handle the <see cref="EditableTextBlock.Canceled"/> event.
	/// </summary>
	/// <param name="e">The event arguments</param>
	public delegate void EditableTextBlockCanceledDelegate(object sender, EventArgs e);

	#endregion
}
