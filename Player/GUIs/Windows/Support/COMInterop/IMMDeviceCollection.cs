/**
 * IMMDeviceCollection.cs
 * 
 * COM interface binding for the IMMDeviceCollection
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

namespace Stoffi.Win32.MMDeviceAPILib
{
	/// <summary>
	/// COM interface for the IMMDeviceCollection type
	/// </summary>
	[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMMDeviceCollection
	{
		#region Methods
		/// <summary>
		/// Get the number of elements in this collection
		/// </summary>
		/// <param name="cDev">out param for count</param>
		[DispId(1)]
		void GetCount([Out, MarshalAs(UnmanagedType.U4)] out uint cDev);
		/// <summary>
		/// Get an item in this collection at the specified idx
		/// </summary>
		/// <param name="nDev">the idx of the item</param>
		/// <param name="device">out parameter to contain the specified item</param>
		[DispId(2)]
		void Item([In, MarshalAs(UnmanagedType.U4)] uint nDev, [Out, MarshalAs(UnmanagedType.Interface)] out IMMDevice device);
		#endregion
	}
}
