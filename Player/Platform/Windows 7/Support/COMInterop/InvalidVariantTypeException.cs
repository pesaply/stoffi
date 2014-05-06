/**
 * InvalidVariantTypeException.cs
 * 
 * Exceptions for PropertyVariant access
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
	/// Exception to be thrown if the incorrect Property is used to access a variant
	/// given its VT_TYPE field
	/// </summary>
	public class InvalidVariantTypeException : InvalidCastException
	{
		public InvalidVariantTypeException()
		{
		}

		public InvalidVariantTypeException(string reason)
			: base(reason)
		{
		}
	}
}
