/**
 * IMMDevice.cs
 * 
 * COM interface binding for the IMMDevice
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
using Stoffi.Win32.PropSysObjects;

namespace Stoffi.Win32.MMDeviceAPILib
{
	[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMMDevice
	{
		#region Methods
		[DispId(1)]
		void Activate([In] Guid iid, [In] int dwClsCtx, [In, Optional, MarshalAs(UnmanagedType.LPStruct)] PropertyVariant propVariant, [Out] out object iInterface);
		[DispId(2)]
		void OpenPropertyStore([In, MarshalAs(UnmanagedType.U4)] UInt32 stgmAccess, [Out] out IPropertyStore propStore);
		[DispId(3)]
		void GetId([Out, MarshalAs(UnmanagedType.LPWStr)] out string id);
		[DispId(4)]
		void GetState([Out, MarshalAs(UnmanagedType.U4)] UInt32 state);
		#endregion
	}

	public static class IMMDeviceProperties
	{
		#region Constant
		// Taken from FunctionDiscoryKeys_devpkey.h
		public static PropertyKey DeviceInterface_FriendlyName = new PropertyKey(

			new Guid(unchecked((int)0x80d81ea6), (short)0x7473, (short)0x4b0c, new byte[] { 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b }),
			3);
		public static PropertyKey Device_DeviceDesc = new PropertyKey(
			new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), (short)0x4efd, new byte[] { 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0 }),
			2
		);
		public static PropertyKey Device_FriendlyName = new PropertyKey(
			new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), (short)0x4efd, new byte[] { 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0 }),
			14
		);
		#endregion
	}
}
