using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public partial class PreferencesWindowController : MonoMac.AppKit.NSWindowController
	{
		#region Constructors
		// Called when created from unmanaged code
		public PreferencesWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public PreferencesWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public PreferencesWindowController () : base ("PreferencesWindow")
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion

		#region Members

		List<IPreferencesTab> tabs = new List<IPreferencesTab>();
		PreferencesToolbarDelegate toolbarDelegate;

		#endregion

		#region Properties

		//strongly typed window accessor
		public new PreferencesWindow Window {
			get {
				return (PreferencesWindow)base.Window;
			}
		}

		#endregion

		#region Methods

		#region Private

		private void InitializeToolbar()
		{
			toolbarDelegate = new PreferencesToolbarDelegate (tabs);
			toolbarDelegate.SelectionChanged += Toolbar_SelectionChanged;
			Window.Toolbar = CreateToolbar ();
			Toolbar_SelectionChanged (this, null);
		}

		private NSToolbar CreateToolbar()
		{
			var tb = new NSToolbar ("PreferencesToolbar");
			tb.AllowsUserCustomization = false;
			tb.Delegate = toolbarDelegate;
			tb.SelectedItemIdentifier = tabs.First ().Name;
			return tb;
		}

		private void ShowSelectedTab(IPreferencesTab selectedTab)
		{
			float delta = Window.ContentView.Frame.Height - selectedTab.View.Frame.Height;
			RemoveCurrentTabView ();
			Window.SetFrame (CalculateNewFrameForWindow(delta), true, true);
			Window.ContentView.AddSubview (selectedTab.View);
		}

		private void RemoveCurrentTabView()
		{
			if (Window.ContentView.Subviews.Any())
				Window.ContentView.Subviews.Single ().RemoveFromSuperview ();
		}

		private RectangleF CalculateNewFrameForWindow(float delta)
		{
			return new RectangleF (Window.Frame.X, Window.Frame.Y + delta, Window.Frame.Width, Window.Frame.Height - delta);
		}

		#endregion

		#region Event handlers

		private void Toolbar_SelectionChanged(object sender, EventArgs e)
		{
			var selectedTab = tabs.Single (s => s.Name.Equals (Window.Toolbar.SelectedItemIdentifier));
			Window.Title = selectedTab.Name;
			ShowSelectedTab (selectedTab);
		}

		#endregion

		#region Overrides

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			tabs.Add (new GeneralViewController ());
			tabs.Add (new SourcesViewController ());
			tabs.Add (new ShortcutsViewController ());
			tabs.Add (new CloudViewController ());
			tabs.Add (new AppsViewController ());
			InitializeToolbar ();
		}

		#endregion

		#endregion
	}
}

