using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public partial class NavigationView : MonoMac.AppKit.NSView
	{
		#region Constructors
		// Called when created from unmanaged code
		public NavigationView (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public NavigationView (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion
	}

	public partial class CustomTextField : MonoMac.AppKit.NSTextField
	{
		#region Constructors

		// Called when created from unmanaged code
		public CustomTextField (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public CustomTextField (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		public override void RightMouseUp (NSEvent theEvent)
		{
			//base.RightMouseUp (theEvent);
			NextResponder.RightMouseUp (theEvent);
		}

		public override void RightMouseDown (NSEvent theEvent)
		{
			NextResponder.RightMouseDown (theEvent);
		}
	}
}

