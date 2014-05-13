/**
 * Filter.cs
 * 
 * All the functionality for a filter plugin
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

using Un4seen.Bass;

namespace Stoffi.Plugins
{
	/// <summary>
	/// A filter plugin.
	/// </summary>
	public class Filter : Plugin
	{
		#region Members
		#endregion

		#region Properties

		/// <summary>
		/// Sets the volume.
		/// </summary>
		protected double Volume
		{
			set
			{
				if (VolumeChanged != null)
					VolumeChanged(this, new GenericEventArgs<double>(value));
			}
		}

		/// <summary>
		/// Sets the chorus.
		/// </summary>
		protected BASS_DX8_CHORUS Chorus
		{
			set
			{
				if (ChorusChanged != null)
					ChorusChanged(this, new GenericEventArgs<BASS_DX8_CHORUS>(value));
			}
		}

		/// <summary>
		/// Sets the compressor.
		/// </summary>
		protected BASS_DX8_COMPRESSOR Compressor
		{
			set
			{
				if (CompressiorChanged != null)
					CompressiorChanged(this, new GenericEventArgs<BASS_DX8_COMPRESSOR>(value));
			}
		}

		/// <summary>
		/// Sets the distortion.
		/// </summary>
		protected BASS_DX8_DISTORTION Distortion
		{
			set
			{
				if (DistortionChanged != null)
					DistortionChanged(this, new GenericEventArgs<BASS_DX8_DISTORTION>(value));
			}
		}

		/// <summary>
		/// Sets the echo.
		/// </summary>
		protected BASS_DX8_ECHO Echo
		{
			set
			{
				if (EchoChanged != null)
					EchoChanged(this, new GenericEventArgs<BASS_DX8_ECHO>(value));
			}
		}

		/// <summary>
		/// Sets the flanger.
		/// </summary>
		protected BASS_DX8_FLANGER Flanger
		{
			set
			{
				if (FlangerChanged != null)
					FlangerChanged(this, new GenericEventArgs<BASS_DX8_FLANGER>(value));
			}
		}

		/// <summary>
		/// Sets the gargle (amplitude modulation).
		/// </summary>
		protected BASS_DX8_GARGLE Gargle
		{
			set
			{
				if (GargleChanged != null)
					GargleChanged(this, new GenericEventArgs<BASS_DX8_GARGLE>(value));
			}
		}

		/// <summary>
		/// Sets the interactive 3D audio level 2 reverb.
		/// </summary>
		protected BASS_DX8_I3DL2REVERB I3DL2Reverb
		{
			set
			{
				if (I3DL2ReverbChanged != null)
					I3DL2ReverbChanged(this, new GenericEventArgs<BASS_DX8_I3DL2REVERB>(value));
			}
		}

		/// <summary>
		/// Sets the parametric equalizer.
		/// </summary>
		protected BASS_DX8_PARAMEQ ParamEq
		{
			set
			{
				if (ParamEqChanged != null)
					ParamEqChanged(this, new GenericEventArgs<BASS_DX8_PARAMEQ>(value));
			}
		}

		/// <summary>
		/// Sets the reverb.
		/// </summary>
		protected BASS_DX8_REVERB Reverb
		{
			set
			{
				if (ReverbChanged != null)
					ReverbChanged(this, new GenericEventArgs<BASS_DX8_REVERB>(value));
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of this class.
		/// </summary>
		/// <param name="id">A string identifying the plugin</param>
		/// <param name="version">The assembly version</param>
		/// <param name="platformVersion">The minimum version required of the plugin platform</param>
		public Filter(string id, Version version, Version platformVersion)
			: base(id, version, platformVersion)
		{
			pluginType = PluginType.Filter;
		}

		#endregion

		#region Methods

		#region Protected

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the filter is trying to change the volume.
		/// </summary>
		public event EventHandler<GenericEventArgs<double>> VolumeChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the chorus.
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_CHORUS>> ChorusChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the compressor.
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_COMPRESSOR>> CompressiorChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the distortion.
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_DISTORTION>> DistortionChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the echo.
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_ECHO>> EchoChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the flanger.
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_FLANGER>> FlangerChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the gargle (amplitude modulation).
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_GARGLE>> GargleChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the interative 3D Audio Level 2 reverb.
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_I3DL2REVERB>> I3DL2ReverbChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the parametric equalizer.
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_PARAMEQ>> ParamEqChanged;

		/// <summary>
		/// Occurs when the filter is trying to change the reverb.
		/// </summary>
		public event EventHandler<GenericEventArgs<BASS_DX8_REVERB>> ReverbChanged;

		#endregion
	}

	public class GenericEventArgs<T> : EventArgs
	{
		T value;
		public T Value { get { return value; } }
		public GenericEventArgs(T value) { this.value = value; }
	}
}