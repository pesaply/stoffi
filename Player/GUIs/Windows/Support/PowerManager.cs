/**
 * PowerManager.cs
 * 
 * Handle machine power management logic.
 * 
 * * * * * * * * *
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
using System.Windows.Threading;
using System.Threading;
using System.Text;

using Stoffi.Core;
using Stoffi.Core.Settings;
using SettingsManager = Stoffi.Core.Settings.Manager;
using ServiceManager = Stoffi.Core.Services.Manager;

namespace Stoffi.Player.GUI
{
	/// <summary>
	/// A class that interfaces with the OS power management api.
	/// </summary>
	public static class PowerManager
	{
		#region Members
		private static object lockObj = new object();
		private static int tasks = 0;
		private static bool preventing = false;
		private static Dispatcher uiDispatcher = null;
		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the dispatcher of the UI thread.
		/// </summary>
		public static Dispatcher UIDispatcher
		{
			get { return uiDispatcher; }
			set { uiDispatcher = value; }
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initialize the power manager
		/// </summary>
		public static void Initialize()
		{
			SettingsManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(SettingsManager_PropertyChanged);
			ServiceManager.PropertyChanged += new EventHandler<PropertyChangedWithValuesEventArgs>(ServiceManager_PropertyChanged);
		}

		/// <summary>
		/// Signal that a task which should prevent computer sleep has started.
		/// </summary>
		/// <returns>the number of such tasks running, including this one</returns>
		public static void SleepPreventTaskStart()
		{
			if (preventing) return;
			if (uiDispatcher == null) return;

			uiDispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
			{
				Interlocked.Increment(ref tasks);
				if (tasks > 0)
				{
					EXECUTION_STATE state = EXECUTION_STATE.ES_CONTINUOUS;
					state |= EXECUTION_STATE.ES_SYSTEM_REQUIRED;
					state |= EXECUTION_STATE.ES_AWAYMODE_REQUIRED;
					if (0 == SetThreadExecutionState(state))
					{
						U.L(LogLevel.Warning, "POWER", "Failed to set ES_SYSTEM_REQUIRED ThreadState");
					}
					else
					{
						preventing = true;
						U.L(LogLevel.Information, "POWER", "Set ES_SYSTEM_REQUIRED ThreadState");
					}
				}
			}));
		}

		/// <summary>
		/// Signal that a task which should prevent the computer from sleeping has ended
		/// </summary>
		/// <returns>the number of such tasks still running after this call</returns>
		public static void SleepPreventTaskEnd()
		{
			if (!preventing) return;
			if (uiDispatcher == null) return;

			uiDispatcher.Invoke(DispatcherPriority.Background, new Action(delegate()
			{
				Interlocked.Decrement(ref tasks);
				if (tasks == 0)
				{
					if (0 == SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS))
					{
						U.L(LogLevel.Warning, "POWER", "Failed to clear ES_SYSTEM_REQUIRED ThreadState");
					}
					else
					{
						preventing = false;
						U.L(LogLevel.Information, "POWER", "Cleared ES_SYSTEM_REQUIRED ThreadState");
					}
				}
				if (tasks < 0)
				{
					tasks = 0;
					U.L(LogLevel.Error, "POWER", "Mismatched calls to SleepPreventTaskStart|End");
				}
			}));
		}

		#region Private

		/// <summary>
		/// Starts or stops the sleep prevention mechanism according to current state of the player.
		/// </summary>
		private static void RefreshSleepPrevention()
		{
			bool dueToSynchronize = ServiceManager.Linked && ServiceManager.Identity != null && ServiceManager.Identity.SynchronizeConfig;
			bool dueToMediaState = SettingsManager.MediaState == MediaState.Playing;
			if (dueToMediaState || dueToSynchronize)
				SleepPreventTaskStart();
			else
				SleepPreventTaskEnd();
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the property of the Settings manager changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			if (e.PropertyName == "MediaState")
				RefreshSleepPrevention();
		}

		/// <summary>
		/// Invoked when the property of the Service manager changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void ServiceManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			if (e.PropertyName == "Linked" || e.PropertyName == "Synchronize")
				RefreshSleepPrevention();
		}

		#endregion

		#endregion

		#region Native
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
		#endregion

		#endregion

		#region Enums
		/// <summary>
		/// EXECUTION_STATE enum for P/Invoke
		/// </summary>
		[FlagsAttribute]
		public enum EXECUTION_STATE : uint
		{
			ES_AWAYMODE_REQUIRED = 0x00000040,
			ES_CONTINUOUS = 0x80000000,
			ES_DISPLAY_REQUIRED = 0x00000002,
			ES_SYSTEM_REQUIRED = 0x00000001,
			// Should not be used according to documentation
			ES_USER_PRESENT = 0x00000004
		}

		#endregion
	}
}
