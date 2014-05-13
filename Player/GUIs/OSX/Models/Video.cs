using System;
using MonoMac.AppKit;
using MonoMac.Foundation;

using Stoffi.Core;
using Stoffi.Core.Settings;

using SettingsManager = Stoffi.Core.Settings.Manager;

namespace Stoffi.GUI.Models
{
	public class VideoInterface : NSObject
	{
		public VideoInterface ()
		{
		}

		// This shows you how to expose an Objective-C function that is not present.
		// The function isSelectorExcludedFromWebScript: is part of the WebScripting Protocol
		[Export ("isSelectorExcludedFromWebScript:")]
		public static bool IsSelectorExcludedFromWebScript(MonoMac.ObjCRuntime.Selector aSelector)
		{
			if (aSelector.Name == "OnVideoError")
				return false;
			if (aSelector.Name == "OnNoFlash")
				return false;
			if (aSelector.Name == "OnStateChanged")
				return false;
			if (aSelector.Name == "OnErrorOccured")
				return false;
			if (aSelector.Name == "OnNoFlashDetected")
				return false;
			if (aSelector.Name == "OnPlayerReady")
				return false;
			if (aSelector.Name == "OnDoubleClick")
				return false;
			if (aSelector.Name == "OnSingleClick")
				return false;
			if (aSelector.Name == "OnShowCursor")
				return false;
			if (aSelector.Name == "OnHideCursor")
				return false;

			return true; // disallow everything else
		}
		
		/// <summary>
		/// Invoked when an error occurs within the YouTube player
		/// </summary>
		/// <param name="errorCode">The error code</param>
		[Export("OnVideoError")]
		public void OnVideoError(int errorCode)
		{
			switch (errorCode)
			{
				case 2:
				U.L(LogLevel.Error, "YOUTUBE", "Player reported that we used bad parameters");
				break;

				case 100:
				U.L(LogLevel.Error, "YOUTUBE", "Player reported that the track has either been removed or marked as private");
				break;

				case 101:
				case 150:
				U.L(LogLevel.Error, "YOUTUBE", "Player reported that the track is restricted");
				break;

				default:
				U.L(LogLevel.Error, "YOUTUBE", "Player reported an unknown error code: " + errorCode);
				break;
			}
			OnErrorOccured(errorCode.ToString());
		}

		/// <summary>
		/// Invoked when user tries to play a youtube track but doesn't have flash installed
		/// </summary>
		[Export("OnNoFlash")]
		public void OnNoFlash()
		{
			OnNoFlashDetected();
		}

		/// <summary>
		/// Invoked when the player changes state
		/// </summary>
		/// <param name="state">The new state of the player</param>
		[Export("OnStateChanged")]
		public void OnStateChanged(int state)
		{
			switch (state)
			{
				case -1: // unstarted
				break;

				case 0: // ended
				SettingsManager.MediaState = MediaState.Ended;
				break;

				case 1: // playing
				SettingsManager.MediaState = MediaState.Playing;
				break;

				case 2: // paused
				SettingsManager.MediaState = MediaState.Paused;
				break;

				case 3: // buffering
				break;

				case 5: // cued
				break;
			}
		}

		/// <summary>
		/// Dispatches the ErrorOccured event
		/// </summary>
		/// <param name="message">The error message</param>
		[Export("OnErrorOccured")]
		public void OnErrorOccured(string message)
		{
			if (ErrorOccured != null)
				ErrorOccured(this, message);
		}

		/// <summary>
		/// Dispatches the NoFlashDetected event
		/// </summary>
		[Export("OnNoFlashDetected")]
		public void OnNoFlashDetected()
		{
			if (NoFlashDetected != null)
				NoFlashDetected(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the PlayerReady event
		/// </summary>
		[Export("OnPlayerReady")]
		public void OnPlayerReady()
		{
			if (PlayerReady != null)
				PlayerReady(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the DoubleClick event
		/// </summary>
		[Export("OnDoubleClick")]
		public void OnDoubleClick()
		{
			if (DoubleClick != null)
				DoubleClick(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the SingleClick event
		/// </summary>
		[Export("OnSingleClick")]
		public void OnSingleClick()
		{
			if (SingleClick != null)
				SingleClick(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the HideCursor event
		/// </summary>
		[Export("OnHideCursor")]
		public void OnHideCursor()
		{
			if (HideCursor != null)
				HideCursor(this, new EventArgs());
		}

		/// <summary>
		/// Dispatches the ShowCursor event
		/// </summary>
		[Export("OnShowCursor")]
		public void OnShowCursor()
		{
			if (ShowCursor != null)
				ShowCursor(this, new EventArgs());
		}

		/// <summary>
		/// Occurs when there's an error from the player
		/// </summary>
		public event ErrorEventHandler ErrorOccured;

		/// <summary>
		/// Occurs when the user tries to play a youtube track but there's no flash installed
		/// </summary>
		public event EventHandler NoFlashDetected;

		/// <summary>
		/// Occurs when the player is ready
		/// </summary>
		public event EventHandler PlayerReady;

		/// <summary>
		/// Occurs when the user double clicks the video.
		/// </summary>
		public event EventHandler DoubleClick;

		/// <summary>
		/// Occurs when the user clicks the video.
		/// </summary>
		public event EventHandler SingleClick;

		/// <summary>
		/// Occurs when the mouse cursor is hidden.
		/// </summary>
		public event EventHandler HideCursor;

		/// <summary>
		/// Occurs when the mouse cursor becomes visible.
		/// </summary>
		public event EventHandler ShowCursor;
	}
}

