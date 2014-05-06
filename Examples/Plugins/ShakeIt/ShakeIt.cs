/**
 * ShakeIt.cs
 * 
 * A Stoffi filter plugin which adjusts the volume according to the
 * level of dance movements in the room.
 * 
 * Requires a Kinect for Windows device.
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
using System.Linq;
using System.Text;

using Simplare;

namespace Stoffi.Plugins
{
	public class ShakeIt : Filter
	{
		#region Members

		Setting minimumVolume;
        Setting maximumVolume;
        Setting viscosity;
        Setting sensitivity;
		StatusLabel deviceStatus;
        bool isRunning = false;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the ShakeIt plugin class.
		/// </summary>
		/// <param name="id">A unique string identifying the plugin</param>
		/// <param name="version">The version of the plugin, specified in the assembly</param>
		public ShakeIt(string id, Version version)
			: base(id, version, new Version(0, 4))
		{
			Author = "Simplare";
			Website = "http://dev.stoffiplayer.com/wiki/PluginAPI";

			minimumVolume = new Setting
			{
				ID = "MinimumVolume",
				Type = typeof(System.Double),
				Value = (Object)0.0,
				Maximum = (Object)100.0,
				IsVisible = true
			};

			maximumVolume = new Setting
			{
				ID = "MaximumVolume",
				Type = typeof(System.Double),
				Value = (Object)100.0,
				Maximum = (Object)100.0,
				IsVisible = true
            };

            viscosity = new Setting
            {
                ID = "Viscosity",
                Type = typeof(System.Double),
                Value = (Object)6.5,
                Maximum = (Object)10.0,
                IsVisible = true
            };

            sensitivity = new Setting
            {
                ID = "Sensitivity",
                Type = typeof(System.Double),
                Value = (Object)5.0,
                Maximum = (Object)10.0,
                IsVisible = true
            };

			Settings.Add(minimumVolume);
            Settings.Add(maximumVolume);
            Settings.Add(sensitivity);
            Settings.Add(viscosity);

			foreach (Setting s in Settings)
                s.PropertyChanged += new PropertyChangedEventHandler(Setting_PropertyChanged);

			deviceStatus = new StatusLabel
			{
				Label = "KinectStatus",
				Status = DanceAnalyzer.IsConnected ? "Connected" : "NotConnected"
			};

			StatusLabels.Add(deviceStatus);

            DanceAnalyzer.PropertyChanged += new PropertyChangedEventHandler(DanceAnalyzer_PropertyChanged);
            DanceAnalyzer.Connect += new EventHandler(DanceAnalyzer_Connect);
            DanceAnalyzer.Disconnect += new EventHandler(DanceAnalyzer_Disconnect);

            DanceAnalyzer.Viscosity = (Double)viscosity.Value;
            DanceAnalyzer.Sensitivity = (Double)sensitivity.Value;
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
			try
			{
				DanceAnalyzer.StartDeviceDetection();
				DanceAnalyzer.IsEnabled = true;
				if (!DanceAnalyzer.IsEnabled)
					throw new Exception();
				isRunning = true;
				return true;
			}
			catch
			{
				isRunning = false;
				throw new Exception(T("ErrorStarting"));
			}
		}

		/// <summary>
		/// Called when the plugin is deactivated
		/// </summary>
		///  <returns>True if stopping was successfull, otherwise false</returns>
		public override bool OnStop()
		{
            DanceAnalyzer.IsEnabled = false;
            isRunning = false;
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
				case "MinimumVolume":
					if ((Double)maximumVolume.Value < (Double)minimumVolume.Value)
						maximumVolume.Value = minimumVolume.Value;
					break;

				case "MaximumVolume":
					if ((Double)maximumVolume.Value < (Double)minimumVolume.Value)
						minimumVolume.Value = maximumVolume.Value;
					break;

                case "Sensitivity":
                    DanceAnalyzer.Sensitivity = (Double)sensitivity.Value;
                    break;

                case "Viscosity":
                    DanceAnalyzer.Viscosity = (Double)viscosity.Value;
                    break;
			}
		}

        /// <summary>
        /// Invoked when the dance analyzer detects that the Kinect device is connected.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data</param>
        private void DanceAnalyzer_Connect(object sender, EventArgs e)
        {

            Console.WriteLine("");
            deviceStatus.Status = "Connected";
            DanceAnalyzer.IsEnabled = isRunning;
        }

        /// <summary>
        /// Invoked when the dance analyzer detects that the Kinect device is disconnected.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data</param>
        private void DanceAnalyzer_Disconnect(object sender, EventArgs e)
        {
            deviceStatus.Status = "NotConnected";
        }

        /// <summary>
        /// Invoked when a property of the dance analyzer is changed.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data</param>
        private void DanceAnalyzer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Quantity")
            {
				double min = (double)minimumVolume.Value;
				double max = (double)maximumVolume.Value;
				Volume = min + ((DanceAnalyzer.Quantity/10) * (max - min));
            }
        }

		#endregion

		#endregion
	}
}
