/**
 * PropertyVariant.cs
 * 
 * Interop type for the PROPVARIANT structure
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

namespace Stoffi.Win32.PropSysObjects
{
	/// <summary>
	/// A partial implementation of the PROPVARIANT native struct.
	/// </summary>
	/// This will probably leak memory if used to retrieve a type not yet supported
	[StructLayout(LayoutKind.Explicit)]
	public struct PropertyVariant
	{

		#region Fields
		// Reserved
		[FieldOffset(0)]
		private VarType varType;
		[FieldOffset(2)]
		private ushort wReserved1;
		[FieldOffset(4)]
		private ushort wReserved2;
		[FieldOffset(6)]
		private ushort wReserved3;

		// Variant fields
		// TODO Free string types on close. This probably leaks
		[FieldOffset(8)]
		long lVal;
		[FieldOffset(8)]
		IntPtr ptr;
		#endregion

		#region Properties
		public string StringValue
		{
			get
			{
				switch (varType)
				{
					case VarType.LPSTR:
						return Marshal.PtrToStringAnsi(ptr);
					case VarType.LPWSTR:
						return Marshal.PtrToStringUni(ptr);
					case VarType.BSTR:
						return Marshal.PtrToStringBSTR(ptr);
					default:
						throw new InvalidVariantTypeException();
				}
			}
		}
		#endregion

		#region Methods

		#endregion

		#region Enums
		// From http://msdn.microsoft.com/en-us/library/aa380072%28VS.85%29.aspx
		enum VarType : ushort
		{
			EMPTY = 0,	
			NULL = 1,  
			I1 = 16,
			UI1 = 17,
			I2 = 2,
			UI2 = 18,
			I4 = 3,
			UI4 = 19,
			INT = 22,
			UINT = 23,
			I8 = 20,
			UI8 = 21,
			R4 = 4,
			R8 = 5,
			BOOL = 11,
			ERROR = 10,
			CY = 6,
			DATE	= 7,
			FILETIME = 64,
			CLSID = 72,
			CF = 71,
			BSTR	= 8,
			BSTR_BLOB = 0xfff,
			BLOB = 65,
			BLOBOBJECT = 70,
			LPSTR = 30,
			LPWSTR = 31,
			UNKNOWN = 13,
			DISPATCH	= 9,	
			STREAM = 66,
			STREAMED_OBJECT = 68,
			STORAGE	= 67,
			STORED_OBJECT = 69,
			VERSIONED_STREAM = 73,
			DECIMAL = 14,
			VECTOR = 0x1000,
			ARRAY = 0x2000,
			VARIANT = 12,
			TYPEMASK = 0xFFF,
		};

		#endregion

		#region Constants
		const ushort RESERVED = 0x8000;
		const ushort BYREF = 0x4000;
		const ushort ARRAY = 0x2000;
		#endregion
	}	
}
