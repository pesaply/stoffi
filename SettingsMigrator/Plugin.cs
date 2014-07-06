/**
 * Plugin.cs
 * 
 * All the functionality for a plugin.
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Stoffi.Plugins
{
	/// <summary>
	/// The plugin interface which will make available stoffi functionality for the plugin programmer.
	/// </summary>
	public class Plugin
	{
		#region Members
		protected PluginType pluginType = PluginType.Unknown;
		#endregion

		#region Properties

		/// <summary>
		/// Gets the type of the plugin.
		/// </summary>
		public PluginType Type { get { return pluginType; } }

		/// <summary>
		/// Gets the name of the author of the plugin.
		/// </summary>
		public virtual string Author { get; protected set; }

		/// <summary>
		/// Gets the website of the plugin.
		/// </summary>
		public virtual string Website { get; protected set; }

		/// <summary>
		/// Gets or sets the version of the plugin.
		/// </summary>
		public virtual Version Version { get; protected set; }

		/// <summary>
		/// Gets the version of the plugin platform that the
		/// plugin was built for.
		/// </summary>
		public Version PlatformVersion { get; private set; }

		/// <summary>
		/// Gets the identifier of the plugin.
		/// </summary>
		public string ID { get; private set; }

		/// <summary>
		/// Gets or sets the list of supported languages.
		/// </summary>
		public List<Language> Languages { get; set; }

		/// <summary>
		/// Gets or sets the IETF tag of the currently active culture.
		/// </summary>
		public string CurrentCulture { get; set; }

		/// <summary>
		/// Gets the currently active language.
		/// </summary>
		public Language CurrentLanguage
		{
			get
			{
				foreach (Language l in Languages)
					if (l.Culture == CurrentCulture)
						return l;
				foreach (Language l in Languages)
					if (l.Culture == "en-US")
						return l;
				return null;
			}
		}

		/// <summary>
		/// The default language (English if found, otherwise first in list).
		/// </summary>
		public Language DefaultLanguage
		{
			get
			{
				foreach (Language l in Languages)
					if (l.Culture == "en-US")
						return l;
				if (Languages.Count > 0)
					return Languages[0];
				return null;
			}
		}

		/// <summary>
		/// Gets the list of settings used to configure the plugin.
		/// </summary>
		public List<Setting> Settings { get; private set; }

		/// <summary>
		/// Gets the list of status labels of the plugin.
		/// </summary>
		public List<StatusLabel> StatusLabels { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of a plugin.
		/// </summary>
		/// <param name="id">A string identifying the plugin</param>
		/// <param name="version">The assembly version</param>
		/// <param name="platformVersion">The minimum version required of the plugin platform</param>
		public Plugin(string id, Version version, Version platformVersion)
		{
			this.ID = id;
			this.Version = version;
			this.PlatformVersion = platformVersion;
			PlatformVersion = new Version(0, 4);
			Languages = new List<Language>();
			Settings = new List<Setting>();
			StatusLabels = new List<StatusLabel>();
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Translates a string.
		/// 
		/// It first tries to find the string in the current language,
		/// if not found it will search in the default language,
		/// if still not found it will return the ID of the string.
		/// </summary>
		/// <param name="id">The ID of the translation string</param>
		/// <returns>The string localized according to the current culture</returns>
		public string T(string id)
		{
			Language l = CurrentLanguage;
			if (l != null)
				foreach (Translation t in l.Translations)
					if (t.ID == id)
						return t.Text.Replace("\\n", "\n");

			l = DefaultLanguage;
			if (l != null)
				foreach (Translation t in l.Translations)
					if (t.ID == id)
						return t.Text.Replace("\\n", "\n");

			return id;
		}
 
		/// <summary>
		/// Called when the plugin is installed.
		/// </summary>
		///  <returns>True if set up was successful, otherwise false</returns>
		public virtual bool OnInstall()
		{
			return true;
		}
		
		/// <summary>
		/// Called when the plugin is activated
		/// </summary>
		///  <returns>True if set up was successful, otherwise false</returns>
		public virtual bool OnStart()
		{
			return true;
		}

		
		/// <summary>
		/// Called when the plugin is deactivated
		/// </summary>
		///  <returns>True if tear down was successful, otherwise false</returns>
		public virtual bool OnStop()
		{
			return true;
		}
		
		/// <summary>
		/// Called when the plugin is uninstalled.
		/// </summary>
		///  <returns>True if tear down was successful, otherwise false</returns>
		public virtual bool OnUninstall()
		{
			return true;
		}
		
		/// <summary>
		/// Updates the plugin.
		/// </summary>
		public virtual void Refresh()
		{
		}

		/// <summary>
		/// Updates the plugin.
		/// </summary>
		/// /// <param name="deltaTime">Time elapsed since last tick</param>
		public virtual void Refresh(float deltaTime)
		{
		}

		#endregion

		#region Protected

		#endregion

		#endregion
	}

	#region Enums

	/// <summary>
	/// A type of a plugin.
	/// </summary>
	public enum PluginType
	{
		/// <summary>
		/// A plugin providing a source of music.
		/// </summary>
		Source,

		/// <summary>
		/// A plugin manipulating the sound.
		/// </summary>
		Filter,

		/// <summary>
		/// A plugin providing a visualization of the sound.
		/// </summary>
		Visualizer,

		/// <summary>
		/// A plugin of unknown type.
		/// </summary>
		Unknown
	}

	#endregion

	#region Data structures

	/// <summary>
	/// Describes a language containing a collection of localized strings.
	/// </summary>
	public class Language
	{
		/// <summary>
		/// Gets or sets the IETF culture tag of the language.
		/// </summary>
		public string Culture { get; set; }

		/// <summary>
		/// Gets or sets the collection of localized strings.
		/// </summary>
		public List<Translation> Translations { get; set; }
	}

	/// <summary>
	/// Describes a single localized string.
	/// </summary>
	public class Translation
	{
		/// <summary>
		/// Gets or sets the ID of the translation string.
		/// </summary>
		[XmlAttribute("ID")]
		public string ID { get; set; }

		/// <summary>
		/// Gets or sets the localized string.
		/// </summary>
		[XmlAttribute("Text")]
		public string Text { get; set; }
	}

	/// <summary>
	/// Describes a status of the plugin.
	/// </summary>
	public class StatusLabel : INotifyPropertyChanged
	{
		#region Members

		private String label;
		private String status;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the string of the label.
		/// </summary>
		public String Label
		{
			get { return label; }
			set { label = value; OnPropertyChanged("Label"); }
		}

		/// <summary>
		/// Gets or sets the string describing the current status.
		/// </summary>
		public String Status
		{
			get { return status; }
			set { status = value; OnPropertyChanged("Status"); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	/// <summary>
	/// A plugin setting.
	/// </summary>
	public class Setting : INotifyPropertyChanged
	{
		#region Members

		private String id;
		private Object value;
		private Object maximum;
		private Object minimum;
		private List<Object> possibleValues;
		private Type type;
		private Boolean isVisible;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the ID of the setting.
		/// </summary>
		public String ID
		{
			get { return id; }
			set { id = value; OnPropertyChanged("ID"); }
		}

		/// <summary>
		/// Gets or sets the type of the setting's value.
		/// </summary>
		[XmlIgnore()]
		public Type Type
		{
			get { return type; }
			set { type = value; OnPropertyChanged("Type"); }
		}

		/// <summary>
		/// Gets or sets the value of the setting.
		/// </summary>
		[XmlIgnore()]
		public Object Value
		{
			get { return value; }
			set { this.value = value; OnPropertyChanged("Value"); }
		}

		/// <summary>
		/// Gets or sets the maximum value possible.
		/// </summary>
		/// <remarks>
		/// Used with numerical types to create a slider.
		/// </remarks>
		public Object Maximum
		{
			get { return maximum; }
			set { maximum = value; OnPropertyChanged("Maximum"); }
		}

		/// <summary>
		/// Gets or sets the minimum value possible.
		/// </summary>
		/// <remarks>
		/// Used with numerical types to create a slider along with the Maximum property.
		/// </remarks>
		public Object Minimum
		{
			get { return minimum; }
			set { minimum = value; OnPropertyChanged("Minimum"); }
		}

		/// <summary>
		/// Gets or sets the list of possible values for the setting.
		/// </summary>
		/// <remarks>
		/// Used to create a dropdown menu.
		/// </remarks>
		public List<Object> PossibleValues
		{
			get { return possibleValues; }
			set { possibleValues = value; OnPropertyChanged("PossibleValues"); }
		}

		/// <summary>
		/// Gets or sets whether or not the setting should be visible to the user.
		/// </summary>
		public Boolean IsVisible
		{
			get { return isVisible; }
			set { isVisible = value; OnPropertyChanged("IsVisible"); }
		}

		/// <summary>
		/// Gets or sets a serialized representation of the Type property.
		/// </summary>
		/// <remarks>
		/// Enables the class to be serialized for saving in XML file.
		/// </remarks>
		[XmlElement(ElementName = "Type")]
		public String SerializedType
		{
			get
			{
				return type == null ? null : type.AssemblyQualifiedName;
			}
			set
			{
				type = Type.GetType(value);
			}
		}

		/// <summary>
		/// Gets or sets a serialized representation of the Value property.
		/// </summary>
		/// <remarks>
		/// Enables the class to be serialized for saving in XML file.
		/// </remarks>
		[XmlElement(ElementName = "Value")]
		public String SerializedValue
		{
			get
			{
				if (SerializedType == typeof(Color).AssemblyQualifiedName)
				{
					try
					{
						Color c = (Color)value;
						return c.IsNamedColor ? c.Name : "#" + c.Name.ToUpper();
					}
					catch
					{
						return null;
					}
				}
				else
					return value == null ? null : value.ToString();
			}
			set
			{
				string t = SerializedType;
				if (t == typeof(Color).AssemblyQualifiedName)
				{
					try
					{
						this.value = ColorTranslator.FromHtml(value);
					}
					catch
					{
						this.value = value;
					}
				}
				else if (t == typeof(Boolean).AssemblyQualifiedName)
					this.value = Boolean.Parse(value);

				else if (t == typeof(Int32).AssemblyQualifiedName)
					this.value = Int32.Parse(value);

				else if (t == typeof(Double).AssemblyQualifiedName)
					this.value = Double.Parse(value);

				else
					this.value = value;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Dispatches the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the property of the item is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	#endregion
}
