/**
 * IPropertyStore.cs
 * 
 * COM Interop for IPropertyStore
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
using System.Runtime.InteropServices;
using System.Text;

using Stoffi.Win32.Wtypes;

namespace Stoffi.Win32.PropSysObjects
{
	/// <summary>
	/// COM property store implementing type
	/// </summary>
	[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPropertyStore
	{
		#region Methods
		/// <summary>
		/// Get the number of elements in this property store.
		/// </summary>
		/// <param name="cProps">out param for count</param>
		void GetCount([Out, MarshalAs(UnmanagedType.U4)] UInt32 cProps);
		/// <summary>
		/// Get a proprety key at a given index
		/// </summary>
		/// <param name="propIdx">the index of the property</param>
		/// <param name="propKey">the key of the property</param>
		void GetAt([Out, MarshalAs(UnmanagedType.U4)] UInt32 propIdx, [Out] PropertyKey propKey);
		/// <summary>
		/// Get the property value for a given key
		/// </summary>
		/// <param name="propKey">the key</param>
		/// <param name="propVar">the out param for value</param>
		void GetValue([In] ref PropertyKey propKey, [Out] out PropertyVariant propVar);
		/// <summary>
		/// Set the value of a given key
		/// </summary>
		/// <param name="propKey"></param>
		/// <param name="propVar"></param>
		void SetValue([In] ref PropertyKey propKey, [In, MarshalAs(UnmanagedType.LPStruct)] PropertyVariant propVar);
		/// <summary>
		/// Commit changes to the property store
		/// </summary>
		void Commit();
		#endregion
	}
}
