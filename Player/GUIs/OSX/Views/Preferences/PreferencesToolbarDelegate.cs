using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public class PreferencesToolbarDelegate : NSToolbarDelegate
	{
		internal event EventHandler SelectionChanged;
		IEnumerable<IPreferencesTab> tabs;

		internal PreferencesToolbarDelegate (IEnumerable<IPreferencesTab> tabs)
		{
			this.tabs = tabs;
		}

		public override NSToolbarItem WillInsertItem (NSToolbar toolbar, string itemIdentifier, bool willBeInserted)
		{
			var tab = tabs.Single (s => s.Name.Equals (itemIdentifier));
			var item = new NSToolbarItem (tab.Name) { Image = tab.Icon, Label = tab.Name };
			item.Activated += ToolbarItem_Activated;
			return item;
		}

		private void ToolbarItem_Activated(object sender, EventArgs e)
		{
			if (SelectionChanged != null)
				SelectionChanged (sender, e);
		}

		public override string[] DefaultItemIdentifiers (NSToolbar toolbar)
		{
			return TabNames;
		}

		public override string[] AllowedItemIdentifiers (NSToolbar toolbar)
		{
			return TabNames;
		}

		public override string[] SelectableItemIdentifiers (NSToolbar toolbar)
		{
			return TabNames;
		}

		public override void WillAddItem (MonoMac.Foundation.NSNotification notification)
		{
		}

		public override void DidRemoveItem (MonoMac.Foundation.NSNotification notification)
		{
		}

		private string[] TabNames { get { return tabs.Select (s => s.Name).ToArray (); } }
	}
}

