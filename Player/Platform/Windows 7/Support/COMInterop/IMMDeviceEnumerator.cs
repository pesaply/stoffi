/**
 * IMMDeviceEnumeration.cs
 * 
 * COM interface binding for the IMMDeviceEnumeration
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
	/// An enumerator for system multimedia devices
	/// </summary>
	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMMDeviceEnumerator
	{
		[DispId(1)]
		void EnumAudioEndpoints([In] DataFlow dataFlow, [In] Role role, [Out] out IMMDeviceCollection devices);
		[DispId(2)]
		void GetDefaultAudioEndpoint([In] DataFlow dataFlow, [In] Role role, [Out] out IMMDevice endpoint);
		[DispId(3)]
		void GetDevice([In, MarshalAs(UnmanagedType.LPWStr)] string id, [Out] out IMMDevice device);
		[DispId(4)]
		void RegisterEndpointNotificationCallback([In, MarshalAs(UnmanagedType.Interface)] IMMNotificationClient client);
		[DispId(5)]
		void UnregisterEndpointNtoficationCallback([In, MarshalAs(UnmanagedType.Interface)] IMMNotificationClient client);
	}

	public enum DataFlow : uint
	{
		eRender,
		eCapture,
		eAll,
		DataFlow_enum_count
	}

	public enum Role : uint
	{
		eConsole,
		eMultimedia,
		eCommunications,
		Role_enum_count
	}
}
