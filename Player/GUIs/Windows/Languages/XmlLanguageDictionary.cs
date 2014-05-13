/**
 * XmlLanguageDictionary.cs
 * 
 * The class responsible for translating strings using
 * an XML language file. It supports translation in both
 * C# and XAML.
 * 
 * * * * * * * * *
 * 
 * Copyright 2013 Tomer Shamam, Simplare
 * 
 * This code was taken from blogs.microsoft.co.il/blogs/tomershamam
 * with permission and is part of the Stoffi Music Player Project.
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
using System.Text;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.ComponentModel;

namespace Tomers.WPF.Localization
{
	/// <summary>
	/// A langauge dictionary loaded from an XML language file.
	/// </summary>
	public class XmlLanguageDictionary : LanguageDictionary
	{
		private Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();
		private string path;
		private string cultureName;
		private string englishName;

		/// <summary>
		/// Gets or sets the path to the language file.
		/// </summary>
		public string Path
		{
			get { return path;  }
			set { path = value; }
		}

		/// <summary>
		/// Gets the native name of the language in the language file.
		/// </summary>
		public override string CultureName
		{
			get { return cultureName; }
		}

		/// <summary>
		/// Gets the English name of the language in the langauge file.
		/// </summary>
		public override string EnglishName
		{
			get { return englishName; }
		}

		/// <summary>
		/// Creates a new language dictionary.
		/// </summary>
		/// <param name="path">The path to the language file</param>
		public XmlLanguageDictionary(string path)
		{
			if (!File.Exists(path))
			{
				throw new ArgumentException(string.Format("File {0} doesn't exist", path));
			}
			this.path = path;
		}

		/// <summary>
		/// Reads the language file and loads all translations into the dictionary.
		/// </summary>
		protected override void OnLoad()
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(path);
			if (xmlDocument.DocumentElement.Name != "Dictionary")
			{
				throw new XmlException("Invalid root element. Must be Dictionary");
			}
			XmlAttribute englishNameAttribute = xmlDocument.DocumentElement.Attributes["EnglishName"];
			if (englishNameAttribute != null)
			{
				englishName = englishNameAttribute.Value;
			}
			XmlAttribute cultureNameAttribute = xmlDocument.DocumentElement.Attributes["CultureName"];
			if (cultureNameAttribute != null)
			{
				cultureName = cultureNameAttribute.Value;
			}
			foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
			{
				if (node.Name == "Value")
				{
					Dictionary<string, string> innerData = new Dictionary<string, string>();
					foreach (XmlAttribute attribute in node.Attributes)
					{
						if (attribute.Name == "Id")
						{
							if (!data.ContainsKey(attribute.Value))
							{
								data[attribute.Value] = innerData;
							}
						}
						else
						{
							innerData[attribute.Name] = attribute.Value;
						}
					}
				}
			}
		}

		/// <summary>
		/// Clears the dictionary.
		/// </summary>
		protected override void OnUnload()
		{
			data.Clear();
		}

		/// <summary>
		/// Translates a string by using the dictionary.
		/// </summary>
		/// <param name="uid">The ID of the string to be translated</param>
		/// <param name="vid">The name of the property for the string</param>
		/// <param name="defaultValue">The value to return if no tralsation is found</param>
		/// <param name="type">The type of the translation</param>
		/// <returns>The translation if found, otherwise the default value</returns>
		protected override object OnTranslate(string uid, string vid, object defaultValue, Type type)
		{
			if (string.IsNullOrEmpty(uid))
			{
				#region Trace
				Debug.WriteLine(string.Format("Uid must not be null or empty"));
				#endregion
				return null;
			}
			if (string.IsNullOrEmpty(vid))
			{
				#region Trace
				Debug.WriteLine(string.Format("Vid must not be null or empty"));
				#endregion
				return null;
			}
			if (!data.ContainsKey(uid))
			{
				#region Trace
				Debug.WriteLine(string.Format("Uid {0} was not found in the {1} dictionary", uid, EnglishName));
				#endregion
				return null;
			}
			Dictionary<string, string> innerData = data[uid];

			if (!innerData.ContainsKey(vid))
			{
				#region Trace
				Debug.WriteLine(string.Format("Vid {0} was not found for Uid {1}, in the {2} dictionary", vid, uid, EnglishName));
				#endregion
				return null;
			}
			string textValue = innerData[vid];
			try
			{
				if (type == typeof(object))
				{
					return textValue;
				}
				else
				{
					TypeConverter typeConverter = TypeDescriptor.GetConverter(type);
					object translation = typeConverter.ConvertFromString(textValue);
					return translation;
				}						
			}
			catch (Exception ex)
			{
				#region Trace
				Debug.WriteLine(string.Format("Failed to translate text {0} in dictionary {1}:\n{2}", textValue, EnglishName, ex.Message));
				#endregion
				return null;
			}			
		}		
	}
}
