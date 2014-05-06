/**
 * SpectrumLine.cs
 * 
 * A Stoffi visualizer plugin which shows spectrum lines.
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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using Stoffi.Plugins;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Stoffi.Plugins
{
	public class SpectrumLine : Visualizer
	{
		#region Members

		Setting showSecondLine;
		Setting colorFirstLine;
		Setting colorSecondLine;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the SpectrumLine plugin class.
		/// </summary>
		/// <param name="id">A unique string identifying the plugin</param>
		/// <param name="version">The version of the plugin, specified in the assembly</param>
		public SpectrumLine(string id, Version version)
			: base(id, version, new Version(0, 3))
		{
			Author = "Simplare";
			Website = "http://dev.stoffiplayer.com/wiki/PluginAPI";

			showSecondLine = new Setting
			{
				ID = "ShowSecondLine",
				Type = typeof(System.Boolean),
				Value = (Object)true,
				IsVisible = true
			};

			colorFirstLine = new Setting
			{
				ID = "ColorFirstLine",
				Type = typeof(Color),
				Value = (Object)Color.Red,
				IsVisible = true
			};

			colorSecondLine = new Setting
			{
				ID = "ColorSecondLine",
				Type = typeof(Color),
				Value = (Object)Color.DarkBlue,
				IsVisible = true
			};

			Settings.Add(showSecondLine);
			Settings.Add(colorFirstLine);
			Settings.Add(colorSecondLine);

			foreach (Setting s in Settings)
				s.PropertyChanged += new PropertyChangedEventHandler(Setting_PropertyChanged);
		}

		#endregion

		#region Methods

		#region Overrides

		/// <summary>
		/// Called when plugin is installed
		/// </summary>
		///  <returns>True if set up was successfull, otherwise false</returns>
		public override bool OnInstall()
		{
			return true;
		}

		/// <summary>
		/// Called when the plugin is activated
		/// </summary>
		///  <returns>True if starting was successfull, otherwise false</returns>
		public override bool OnStart()
		{
			return true;
		}

		/// <summary>
		/// Called when the plugin is deactivated
		/// </summary>
		///  <returns>True if stopping was successfull, otherwise false</returns>
		public override bool OnStop()
		{
			return true;
		}

		/// <summary>
		/// Called when the plugin is uninstalled
		/// </summary>
		///  <returns>True if tear down was successfull, otherwise false</returns>
		public override bool OnUninstall()
		{
			return true;
		}

		/// <summary>
		/// Updates the plugin
		/// </summary>
		public override void Refresh()
		{
            // paint red linestrip
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

			// size
			float width = 100;
			float height = 100;

			// margins
			float bottom = 10;
			float top = 10;
			float left = 10;
			float right = 10;

			float size = 1024;

			if ((System.Boolean)showSecondLine.Value)
			{
				float[] rev = FFTData.Reverse().ToArray<float>();
				GL.Color3((Color)colorSecondLine.Value);
				GL.Begin(BeginMode.LineStrip);
				for (int i = 0; i < rev.Length; i++)
				{
					float p = rev[i];
					float w = width - left - right;
					float h = height - top - bottom;
					float x = left + (i * w / size);
					float y = bottom + (p * h * (float)Math.Sqrt(size - i));
					GL.Vertex2(x, y);
				}
				GL.End();
			}

			GL.Color3((Color)colorFirstLine.Value);
			GL.Begin(BeginMode.LineStrip);
			for (int i=0; i < FFTData.Length; i++)
			{
				float p = FFTData[i];
				float w = width - left - right;
				float h = height - top - bottom;
				float x = left + (i * w / size);
				float e = (float)0.5 + ((float)1.5 * (i / size));
				float y = bottom + (p * h * (float)Math.Sqrt(i));
				GL.Vertex2(x, y);
			}
			GL.End();
		}

		#endregion

		#region Private

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when a setting is changed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Setting_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Setting s = (Setting)sender;
			switch (s.ID)
			{
				case "ShowSecondLine":
					colorSecondLine.IsVisible = (System.Boolean)s.Value;
					break;
			}
		}

		#endregion

		#endregion
	}
}
