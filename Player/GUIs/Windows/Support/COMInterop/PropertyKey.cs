/**
 * PropertyKey.cs
 * 
 * Managed PROPERTKEY struct
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

namespace Stoffi.Win32.Wtypes
{
	[StructLayout(LayoutKind.Sequential, Pack=4)]
	public struct PropertyKey
	{
		public PropertyKey(Guid guid, UInt32 pid)
		{
			fmtid = guid;
			this.pid = pid;
		}

		[MarshalAs(UnmanagedType.Struct)]
		public Guid fmtid;
		public uint pid;
	}
}
