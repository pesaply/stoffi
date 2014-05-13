/**
 * MMDeviceEnumerator.cs
 * 
 * COM coclass binding for the MMDeviceCollection type
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
	/// COM imported class for MMDeviceEnumerator.
	/// Cast to IMMDeviceEnumerator to use.
	/// </summary>
	[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	public class MMDeviceEnumerator
	{
	}
}
