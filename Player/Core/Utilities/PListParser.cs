/***
 * PListParser.cs
 * 
 * A parser for Apple's XML based Property List (plist) format.
 * 
 * This code is originally from http://www.codeproject.com/Tips/406235/A-Simple-PList-Parser-in-Csharp
 *	
 * * * * * * * * *
 * 
 * Copyright 2012 paladin_t, 2014 Simplare
 * 
 * This code is part of the Stoffi Music Player Project.
 * Visit our website at: stoffiplayer.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the Code Project Open License
 * as published at http://www.codeproject.com/info/cpol10.aspx.
 * 
 * See stoffiplayer.com/license for more information.
 ***/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Stoffi.Core
{
	public class PList : Dictionary<string, dynamic>
	{
		public PList()
		{
		}

		public PList(string file)
		{
			Load(file);
		}

		public PList(StreamReader reader)
		{
			var data = reader.ReadToEnd();
			var settings = new XmlReaderSettings();
			settings.DtdProcessing = DtdProcessing.Ignore;
			settings.IgnoreWhitespace = true;
			using (var xmlReader = XmlReader.Create (new StringReader (data), settings))
			{
				Load(XDocument.Load(xmlReader));
			}
		}

		public void Load(string file)
		{
			Clear();
			Load(XDocument.Load(file));
		}

		public void Load(XDocument doc)
		{
			XElement plist = doc.Element("plist");
			XElement dict = plist.Element("dict");

			var dictElements = dict.Elements();
			Parse(this, dictElements);
		}

		private void Parse(PList dict, IEnumerable<XElement> elements)
		{
			for (int i = 0; i < elements.Count(); i += 2)
			{
				XElement key = elements.ElementAt(i);
				XElement val = elements.ElementAt(i + 1);

				dict[key.Value] = ParseValue(val);
			}
		}

		private List<dynamic> ParseArray(IEnumerable<XElement> elements)
		{
			List<dynamic> list = new List<dynamic>();
			foreach (XElement e in elements)
			{
				dynamic one = ParseValue(e);
				list.Add(one);
			}

			return list;
		}

		private dynamic ParseValue(XElement val)
		{
			switch (val.Name.ToString())
			{
			case "string":
				return val.Value;
			case "integer":
				return int.Parse(val.Value);
			case "real":
				return float.Parse(val.Value);
			case "date":
				return DateTime.Parse(val.Value);
			case "true":
				return true;
			case "false":
				return false;
			case "dict":
				PList plist = new PList();
				Parse(plist, val.Elements());
				return plist;
			case "array":
				List<dynamic> list = ParseArray(val.Elements());
				return list;
			default:
				throw new ArgumentException("Unsupported value type: " + val.Name.ToString());
			}
		}
	}
}