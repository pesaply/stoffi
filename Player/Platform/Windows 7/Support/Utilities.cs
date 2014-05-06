/**
 * Utilities.cs
 * 
 * An extension of the Utilities class in Core that provides
 * various GUI related utilities for the Windows 7 GUI.
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
using System.Windows.Controls;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Media;

namespace Stoffi
{
	/// <summary>
	/// A utilities class with helper method for GUI related stuff
	/// </summary>
	static partial class Utilities
	{
		#region Properties

		/// <summary>
		/// The default album art path
		/// </summary>
		public static String DefaultAlbumArt { get; set; }

		#endregion

		#region Methods

		/// <summary>Finds a parent of a given item on the visual tree.</summary>
		/// <typeparam name="T">The type of the queried item.</typeparam>
		/// <param name="iChild">A direct or indirect child of the queried item.</param>
		/// <returns>The first parent item that matches the submitted type parameter. If not matching item can be found, a null reference is being returned.</returns>
		public static T TryFindParent<T>(this DependencyObject iChild)
		  where T : DependencyObject
		{
			// Get parent item.
			DependencyObject parentObject = GetParentObject(iChild);

			// We've reached the end of the tree.
			if (parentObject == null)
				return null;

			// Check if the parent matches the type we're looking for.
			// Else use recursion to proceed with next level.
			T parent = parentObject as T;
			return parent ?? TryFindParent<T>(parentObject);
		}

		/// <summary>
		/// This method is an alternative to WPF's <see cref="VisualTreeHelper.GetParent"/> method, which also
		/// supports content elements. Keep in mind that for content element, this method falls back to the logical tree of the element!
		/// </summary>
		/// <param name="iChild">The item to be processed.</param>
		/// <returns>The submitted item's parent, if available. Otherwise null.</returns>
		public static DependencyObject GetParentObject(this DependencyObject iChild)
		{
			if (iChild == null)
			{
				return null;
			}

			// Handle content elements separately.
			ContentElement contentElement = iChild as ContentElement;
			if (contentElement != null)
			{
				DependencyObject parent = ContentOperations.GetParent(contentElement);
				if (parent != null) return parent;

				FrameworkContentElement frameworkContentElement = contentElement as FrameworkContentElement;
				return frameworkContentElement != null ? frameworkContentElement.Parent : null;
			}

			// Also try searching for parent in framework elements (such as DockPanel, etc).
			FrameworkElement frameworkElement = iChild as FrameworkElement;
			if (frameworkElement != null)
			{
				DependencyObject parent = frameworkElement.Parent;
				if (parent != null) return parent;
			}

			// If it's not a ContentElement/FrameworkElement, rely on VisualTreeHelper.
			return VisualTreeHelper.GetParent(iChild);
		}

		/// <summary>Tries to locate a given item within the visual tree, starting with the dependency object at a given position.</summary>
		/// <typeparam name="T">The type of the element to be found on the visual tree of the element at the given location.</typeparam>
		/// <param name="iReference">The main element which is used to perform hit testing.</param>
		/// <param name="iPoint">The position to be evaluated on the origin.</param>
		public static T TryFindFromPoint<T>(this UIElement iReference, Point iPoint) where T : DependencyObject
		{
			DependencyObject element = iReference.InputHitTest(iPoint) as DependencyObject;
			if (element == null)
			{
				return null;
			}
			else if (element is T)
				return (T)element;
			else
				return TryFindParent<T>(element);
		}

		/// <summary>
		/// Tries to locate a child of a given item within the visual tree
		/// </summary>
		/// <typeparam name="T">The type of the element to be found on the visual tree of the parent to the element</typeparam>
		/// <param name="referenceVisual">A direct or indirect parent of the element to be found</param>
		/// <returns>The first child item that matches the submitted type parameter. If not matching item can be found, a null reference is being returned.</returns>
		public static T GetVisualChild<T>(Visual referenceVisual) where T : Visual
		{
			Visual child = null;
			for (Int32 i = 0; i < VisualTreeHelper.GetChildrenCount(referenceVisual); i++)
			{
				child = VisualTreeHelper.GetChild(referenceVisual, i) as Visual;
				if (child != null && (child.GetType() == typeof(T)))
				{
					break;
				}
				else if (child != null)
				{
					child = GetVisualChild<T>(child);
					if (child != null && (child.GetType() == typeof(T)))
					{
						break;
					}
				}
			}
			return child as T;
		}

		/// <summary>
		/// Sets a WebBrowser in silent mode so no script errors can cause
		/// interrupting popups.
		/// </summary>
		/// <param name="browser">The browser control</param>
		/// <param name="silent">Whether or not silent mode should be on</param>
		public static void SetSilent(WebBrowser browser, bool silent)
		{
			if (browser == null)
				throw new ArgumentNullException("browser");

			// get an IWebBrowser2 from the document
			IOleServiceProvider sp = browser.Document as IOleServiceProvider;
			if (sp != null)
			{
				Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
				Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

				object webBrowser;
				sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
				if (webBrowser != null)
				{
					webBrowser.GetType().InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty, null, webBrowser, new object[] { silent });
				}
			}
		}

		[ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IOleServiceProvider
		{
			[PreserveSig]
			int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
		}

		#endregion
	}

	#region Description converters

	/// <summary>
	/// 
	/// </summary>
	class OpenFileAddDescriptionConverter : IValueConverter
	{
		private string OpenFile_NoAdd = U.T("GeneralAddPolicyDont", "Content");
		private string OpenFile_Add2Lib = U.T("GeneralAddPolicyLibrary", "Content");
		private string OpenFile_Add2Pl = U.T("GeneralAddPolicyBoth", "Content");

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			OpenAddPolicy source = (OpenAddPolicy)Enum.Parse(typeof(OpenAddPolicy), value.ToString());
			string target;

			switch (source)
			{
				case OpenAddPolicy.DoNotAdd:
					target = OpenFile_NoAdd;
					break;

				case OpenAddPolicy.Library:
				default:
					target = OpenFile_Add2Lib;
					break;

				case OpenAddPolicy.LibraryAndPlaylist:
					target = OpenFile_Add2Pl;
					break;
			}

			return target;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			string source = value.ToString();

			if (source == OpenFile_NoAdd)
				return OpenAddPolicy.DoNotAdd;

			else if (source == OpenFile_Add2Pl)
				return OpenAddPolicy.LibraryAndPlaylist;

			else
				return OpenAddPolicy.Library;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	class OpenFilePlayDescriptionConverter : IValueConverter
	{
		private string OpenFile_Play = U.T("GeneralPlayPolicyPlay", "Content");
		private string OpenFile_DoNotPlay = U.T("GeneralPlayPolicyDont", "Content");
		private string OpenFile_BackOfQueue = U.T("GeneralPlayPolicyBack", "Content");
		private string OpenFile_FrontOfQueue = U.T("GeneralPlayPolicyFront", "Content");

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			OpenPlayPolicy source = (OpenPlayPolicy)Enum.Parse(typeof(OpenPlayPolicy), value.ToString());
			string target;

			switch (source)
			{
				case OpenPlayPolicy.BackOfQueue:
				default:
					target = OpenFile_BackOfQueue;
					break;

				case OpenPlayPolicy.FrontOfQueue:
					target = OpenFile_FrontOfQueue;
					break;

				case OpenPlayPolicy.DoNotPlay:
					target = OpenFile_DoNotPlay;
					break;

				case OpenPlayPolicy.Play:
					target = OpenFile_Play;
					break;
			}

			return target;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			string source = value.ToString();

			if (source == OpenFile_Play)
				return OpenPlayPolicy.Play;

			else if (source == OpenFile_FrontOfQueue)
				return OpenPlayPolicy.FrontOfQueue;

			else if (source == OpenFile_DoNotPlay)
				return OpenPlayPolicy.DoNotPlay;

			else
				return OpenPlayPolicy.BackOfQueue;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	class UpgradePolicyDescriptionConverter : IValueConverter
	{
		private string UpgradePolicy_Automatic = U.T("GeneralUpgradePolicyAuto", "Content");
		private string UpgradePolicy_Notify = U.T("GeneralUpgradePolicyNotify", "Content");
		private string UpgradePolicy_Manual = U.T("GeneralUpgradePolicyManual", "Content");

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			UpgradePolicy source = (UpgradePolicy)Enum.Parse(typeof(UpgradePolicy), value.ToString());
			string target;

			switch (source)
			{
				case UpgradePolicy.Automatic:
				default:
					target = UpgradePolicy_Automatic;
					break;

				case UpgradePolicy.Notify:
					target = UpgradePolicy_Notify;
					break;

				case UpgradePolicy.Manual:
					target = UpgradePolicy_Manual;
					break;
			}

			return target;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			string source = value.ToString();

			if (source == UpgradePolicy_Notify)
				return UpgradePolicy.Notify;

			else if (source == UpgradePolicy_Manual)
				return UpgradePolicy.Manual;

			else
				return UpgradePolicy.Automatic;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	class SearchPolicyDescriptionConverter : IValueConverter
	{
		private string SearchPolicy_Global = U.T("GeneralSearchPolicyGlobal", "Content");
		private string SearchPolicy_Partial = U.T("GeneralSearchPolicyPartial", "Content");
		private string SearchPolicy_Individual = U.T("GeneralSearchPolicyIndividual", "Content");

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			SearchPolicy source = (SearchPolicy)Enum.Parse(typeof(SearchPolicy), value.ToString());
			string target;

			switch (source)
			{
				case SearchPolicy.Global:
				default:
					target = SearchPolicy_Global;
					break;

				case SearchPolicy.Partial:
					target = SearchPolicy_Partial;
					break;

				case SearchPolicy.Individual:
					target = SearchPolicy_Individual;
					break;
			}

			return target;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			string source = value.ToString();

			if (source == SearchPolicy_Partial)
				return SearchPolicy.Partial;

			else if (source == SearchPolicy_Individual)
				return SearchPolicy.Individual;

			else
				return SearchPolicy.Global;
		}
	}

	#endregion

	/// <summary>
	/// Exception for an unknown column
	/// </summary>
	class UnknownColumnException : ApplicationException
	{
		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public UnknownColumnException(string name)
			: base("The list view doesn't contain the column '" + name + "'")
		{
		}

		#endregion
	}
}
