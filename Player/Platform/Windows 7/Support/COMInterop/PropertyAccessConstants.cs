/**
 * StgmConstants.cs
 * 
 * Constants for IPropertyStore access
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
using System.Text;

namespace Stoffi.Win32.PropSysObjects
{
	/// <summary>
	/// Class containing constants for IPropertyStore access modes
	/// </summary>
	public static class PropertyAccessConstants
	{
		#region Constants
		public const long READ = 0x00000000L;
		public const long WRITE = 0x00000001L;
		public const long READWRITE = 0x00000002L;
		#endregion
	}
}
