/**
 * Keyboard.cs
 * 
 * Various helper classes and method for dealing with keyboard events.
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
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Windows.Input;

namespace Stoffi
{
	/// <summary>
	/// Listens keyboard globally.
	/// 
	/// <remarks>Uses WH_KEYBOARD_LL.</remarks>
	/// </summary>
	public class KeyboardListener : IDisposable
	{
		#region Constructor

		/// <summary>
		/// Creates global keyboard listener.
		/// </summary>
		public KeyboardListener()
		{
			// We have to store the HookCallback, so that it is not garbage collected runtime
			hookedLowLevelKeyboardProc = (InterceptKeys.LowLevelKeyboardProc)LowLevelKeyboardProc;

			// Set the hook
			hookId = InterceptKeys.SetHook(hookedLowLevelKeyboardProc);

			// Assign the asynchronous callback event
			hookedKeyboardCallbackAsync = new KeyboardCallbackAsync(KeyboardListener_KeyboardCallbackAsync);
		}

		#endregion

		#region Destructor

		/// <summary>
		/// Destroys global keyboard listener.
		/// </summary>
		~KeyboardListener()
		{
			Dispose();
		}

		#endregion

		#region Methods
		/// <summary>
		/// Check if the handlers are present
		/// </summary>
		/// <returns>true if both handlers are not null, otherwise false</returns>
		public bool CheckHandlers()
		{
			return (KeyDown != null && KeyUp != null);
		}
		#endregion

		#region Events

		/// <summary>
		/// Fired when any of the keys is pressed down.
		/// </summary>
		public event RawKeyEventHandler KeyDown;

		/// <summary>
		/// Fired when any of the keys is released.
		/// </summary>
		public event RawKeyEventHandler KeyUp;

		#endregion

		#region Inner workings

		/// <summary>
		/// Hook ID
		/// </summary>
		private IntPtr hookId = IntPtr.Zero;

		/// <summary>
		/// Asynchronous callback hook.
		/// </summary>
		/// <param name="keyEvent"></param>
		/// <param name="vkCode"></param>
		private delegate void KeyboardCallbackAsync(InterceptKeys.KeyEvent keyEvent, int vkCode);

		/// <summary>
		/// Actual callback hook.
		/// 
		/// <remarks>Calls asynchronously the asyncCallback.</remarks>
		/// </summary>
		/// <param name="nCode"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
				if (wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_KEYDOWN ||
					wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_KEYUP ||
					wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_SYSKEYDOWN ||
					wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_SYSKEYUP)
					hookedKeyboardCallbackAsync.BeginInvoke((InterceptKeys.KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam), null, null);

			return InterceptKeys.CallNextHookEx(hookId, nCode, wParam, lParam);
		}

		/// <summary>
		/// Event to be invoked asynchronously (BeginInvoke) each time key is pressed.
		/// </summary>
		private KeyboardCallbackAsync hookedKeyboardCallbackAsync;

		/// <summary>
		/// Contains the hooked callback in runtime.
		/// </summary>
		private InterceptKeys.LowLevelKeyboardProc hookedLowLevelKeyboardProc;

		/// <summary>
		/// HookCallbackAsync procedure that calls accordingly the KeyDown or KeyUp events.
		/// </summary>
		/// <param name="keyEvent">Keyboard event</param>
		/// <param name="vkCode">vkCode</param>
		void KeyboardListener_KeyboardCallbackAsync(InterceptKeys.KeyEvent keyEvent, int vkCode)
		{
			switch (keyEvent)
			{
				// KeyDown events
				case InterceptKeys.KeyEvent.WM_KEYDOWN:
					if (KeyDown != null)
						KeyDown(this, new RawKeyEventArgs(vkCode, false));
					break;
				case InterceptKeys.KeyEvent.WM_SYSKEYDOWN:
					if (KeyDown != null)
						KeyDown(this, new RawKeyEventArgs(vkCode, true));
					break;

				// KeyUp events
				case InterceptKeys.KeyEvent.WM_KEYUP:
					if (KeyUp != null)
						KeyUp(this, new RawKeyEventArgs(vkCode, false));
					break;
				case InterceptKeys.KeyEvent.WM_SYSKEYUP:
					if (KeyUp != null)
						KeyUp(this, new RawKeyEventArgs(vkCode, true));
					break;

				default:
					break;
			}
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Disposes the hook.
		/// <remarks>This call is required as it calls the UnhookWindowsHookEx.</remarks>
		/// </summary>
		public void Dispose()
		{
			InterceptKeys.UnhookWindowsHookEx(hookId);
		}

		#endregion
	}

	/// <summary>
	/// Raw KeyEvent arguments.
	/// </summary>
	public class RawKeyEventArgs : EventArgs
	{
		#region Members

		/// <summary>
		/// vkCode of the key.
		/// </summary>
		public int vkCode;

		/// <summary>
		/// WPF key of the key.
		/// </summary>
		public Key key;

		/// <summary>
		/// Is the hitted key system key.
		/// </summary>
		public bool isSysKey;

		#endregion

		#region Constructor

		/// <summary>
		/// Create raw keyevent arguments.
		/// </summary>
		/// <param name="vkCode"></param>
		/// <param name="isSysKey"></param>
		public RawKeyEventArgs(int VKCode, bool isSysKey)
		{
			this.vkCode = VKCode;
			this.isSysKey = isSysKey;
			this.key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(VKCode);
		}

		#endregion
	}

	/// <summary>
	/// Raw keyevent handler.
	/// </summary>
	/// <param name="sender">sender</param>
	/// <param name="args">raw keyevent arguments</param>
	public delegate void RawKeyEventHandler(object sender, RawKeyEventArgs args);

	#region WINAPI Helper class

	/// <summary>
	/// Winapi key interception helper class.
	/// </summary>
	internal static class InterceptKeys
	{
		#region Members

		public static int WH_KEYBOARD_LL = 13;

		#endregion

		#region Delegates

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nCode"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		public delegate IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam);

		#endregion

		#region Enums

		/// <summary>
		/// A code describing the key event.
		/// </summary>
		public enum KeyEvent : int
		{
			/// <summary>
			/// Key was pressed
			/// </summary>
			WM_KEYDOWN = 256,

			/// <summary>
			/// Key was released
			/// </summary>
			WM_KEYUP = 257,

			/// <summary>
			/// System key was released
			/// </summary>
			WM_SYSKEYUP = 261,

			/// <summary>
			/// System key was pressed
			/// </summary>
			WM_SYSKEYDOWN = 260
		}

		#endregion

		#region Statics

		public static IntPtr SetHook(LowLevelKeyboardProc proc)
		{
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule)
			{
				return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
					GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, UIntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		#endregion
	}

	#endregion
}
