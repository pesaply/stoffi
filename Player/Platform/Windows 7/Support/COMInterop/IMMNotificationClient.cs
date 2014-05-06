/**
 * IMMNotificationClient.cs
 * 
 * IMMNotificationClient implementation
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

namespace Stoffi.Win32.MMDeviceAPILib
{
	/// <summary>
	/// Interface for IMMNotificationClient com event sink
	/// </summary>
	[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMNotificationClient
    {
		[DispId(1)]
		void OnDeviceStateChanged([In, MarshalAs(UnmanagedType.LPWStr)] string deviceId, [In, MarshalAs(UnmanagedType.U4)] UInt32 newState);
		[DispId(2)]
		void OnDeviceAdded([In, MarshalAs(UnmanagedType.LPWStr)] string deviceId);
		[DispId(3)]
		void OnDeviceRemoved([In, MarshalAs(UnmanagedType.LPWStr)] string deviceId);
		[DispId(4)]
		void OnDefaultDeviceChanged([In] DataFlow flow, [In] Role role, [In, MarshalAs(UnmanagedType.LPWStr)] string defaultDeviceId);
		[DispId(5)]
		void OnPropertyValueChanged([In, MarshalAs(UnmanagedType.LPWStr)] string deviceId, [In] PropertyKey key);
    }
}
