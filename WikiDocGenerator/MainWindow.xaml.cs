using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;
using Microsoft.Win32;

namespace WikiDocGenerator
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        #region Members

        List<Class> classes = new List<Class>();
		List<Enum> enums = new List<Enum>();
		List<Delegate> delegates = new List<Delegate>();

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        public MainWindow()
		{
			//InitializeComponent();
			//Match m2 = r.Match(text2);
			//Match m3 = r.Match(text3);

			//MessageBox.Show("Match on 1: " + m1.Success + "\nMatch on 2: " + m2.Success + "\nMatch on 3: " + m3.Success);

			//Close();

			string file = @"H:\Development\Stoffi\trunk\Application\bin\Debug\Stoffi.XML";
			Read(file);
			Close();
		}

        #endregion

        #region Methods

        #region Private

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        private void Read(string filename)
		{
			XmlTextReader xmlReader = new XmlTextReader(filename);
			xmlReader.WhitespaceHandling = WhitespaceHandling.None;

			xmlReader.Read();
			xmlReader.Read();
			xmlReader.Read();
			xmlReader.Skip();

			int i = 0;

			if (xmlReader.Name == "members")
			{
				List<Entity> entities = new List<Entity>();
				Hashtable classTable = new Hashtable();
				xmlReader.Read();
				while (!(xmlReader.Name == "members" && xmlReader.NodeType == XmlNodeType.EndElement))
				{
					i++;
					ReadMember(xmlReader, entities);
					xmlReader.Read();
					//if (i > 3) break;
				}
				foreach (Entity e in entities)
				{
					if (e.Name.StartsWith("XamlGeneratedNamespace") ||
						e.Name.StartsWith("Stoffi.Properties.")) continue;

					if (e is Class)
					{
						string[] name = ParseName(e.Name);
						e.Name = name[0];
						classTable[name[0]] = e;
					}
				}
				foreach (Entity e in entities)
				{
					if (e.Name.StartsWith("XamlGeneratedNamespace") ||
						e.Name.StartsWith("Stoffi.Properties.")) continue;

					if (e is Method)
					{
						string[] name = ParseName(e.Name, true);
						e.Name = name[1];
						Class c = classTable[name[0]] as Class;
						c.Methods.Add(e as Method);
					}
					else if (e is Delegate)
					{
						string[] name = ParseName(e.Name, true);
						if (name[1] == null)
						{
							e.Name = name[0];
							delegates.Add(e as Delegate);
						}
						else
						{
							e.Name = name[1];
							Class c = classTable[name[0]] as Class;
							c.Delegates.Add(e as Delegate);
						}
					}
					else if (e is Event)
					{
						string[] name = ParseName(e.Name, true);
						e.Name = name[1];
						Class c = classTable[name[0]] as Class;
						c.Events.Add(e as Event);
					}
					else if (e is Field)
					{
						string[] name = ParseName(e.Name, true);
						e.Name = name[1];
						Class c = classTable[name[0]] as Class;
						c.Fields.Add(e as Field);
					}
					else if (e is Property)
					{
						string[] name = ParseName(e.Name, true);
						e.Name = name[1];
						Class c = classTable[name[0]] as Class;
						c.Properties.Add(e as Property);
					}
					else if (!(e is Class))
					{
						Console.WriteLine("Unknown type of entity: " + e.Name);
					}
				}

				foreach (DictionaryEntry o in classTable)
				{
					Class c = o.Value as Class;

					if (c.Fields.Count > 0 && c.Methods.Count == 0 && c.Events.Count == 0 && c.Properties.Count == 0)
					{
						Enum e = new Enum();
						e.Name = c.Name;
						e.Summary = c.Summary;
						e.SeeAlsos = c.SeeAlsos;
						e.Remarks = c.Remarks;

						foreach (Field f in c.Fields)
						{
							Value v = new Value();
							v.Name = f.Name;
							v.Summary = f.Summary;
							v.SeeAlsos = f.SeeAlsos;
							v.Remarks = f.Remarks;
							e.Values.Add(v);
						}
						enums.Add(e);
					}
					else
					{
						foreach (Method m in c.Methods)
						{
							if (m.Name.StartsWith("#cctor") || m.Name.StartsWith("#ctor"))
								c.Constructor = m;
							else if (m.Name == "Finalize")
								c.Destructor = m;
						}
						c.Methods.Remove(c.Constructor);
						c.Methods.Remove(c.Destructor);
						classes.Add(c);
					}
				}

				Generate();
			}
			else
			{
				Console.WriteLine("Expected element <members> but got <" + xmlReader.Name + "> instead");
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="hasParent"></param>
        /// <returns></returns>
		private string[] ParseName(string name, bool hasParent = false)
		{
			//if (name.StartsWith("Stoffi.Utilities."))
			//    name = name.Remove(0, "Stoffi.Utilities.".Length);

			if (name.StartsWith("Stoffi."))
				name = name.Remove(0, "Stoffi.".Length);

			string[] ret = new string[2];
			int p = name.IndexOf('.');
			if (hasParent && p >= 0)
			{
				ret[0] = name.Substring(0, p);
				ret[1] = name.Substring(p+1);
			}
			else ret[0] = name;
			return ret;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="entities"></param>
		private void ReadMember(XmlTextReader xmlReader, List<Entity> entities)
		{
			if (xmlReader.Name == "member" && xmlReader.NodeType == XmlNodeType.Element)
			{
				string[] nameFields = xmlReader.GetAttribute(0).Split(':');

				Entity e;
				Entity e1 = new Delegate();

				if (nameFields[0] == "F")
					e = new Field();
				else if (nameFields[0] == "T")
				{
					e = new Class();
				}
				else if (nameFields[0] == "E")
					e = new Event();
				else if (nameFields[0] == "M")
					e = new Method();
				else if (nameFields[0] == "P")
					e = new Property();
				else
				{
					Console.WriteLine("Member '" + nameFields[1] + "' is of unknown type: " + nameFields[0]);
					return;
				}

				e.Name = nameFields[1];
				//Console.WriteLine("####### MEMBER: " + e.Name + " ###############");

				xmlReader.Read();
				while (!(xmlReader.Name == "member" && xmlReader.NodeType == XmlNodeType.EndElement))
				{
					if (xmlReader.NodeType == XmlNodeType.Element)
					{
						string tag = xmlReader.Name;
						string value = "";
						string name = "";

						if (xmlReader.HasAttributes)
							name = xmlReader.GetAttribute(0);

						value = ReadTag(xmlReader);

						if (tag.ToLower() == "summary")
							e.Summary = value;
						else if (tag.ToLower() == "seealso")
							e.SeeAlsos.Add(value);
						else if (tag.ToLower() == "remarks")
							e.Remarks.Add(value);

						else if (tag.ToLower() == "param" && e is Method)
							((Method)e).Params[name] = value;
						else if (tag.ToLower() == "typeparam" && e is Method)
							((Method)e).TypeParam = value;
						else if (tag.ToLower() == "returns" && e is Method)
							((Method)e).Return = value;

						else if (tag.ToLower() == "param" && e is Delegate)
							((Delegate)e).Params[name] = value;
						else if (tag.ToLower() == "typeparam" && e is Delegate)
							((Delegate)e).TypeParam = value;
						else if (tag.ToLower() == "returns" && e is Delegate)
							((Delegate)e).Return = value;

						else if (tag.ToLower() == "param" && e is Class)
						{
							e1.Name = e.Name;
							e1.Summary = e.Summary;
							e1.SeeAlsos = e.SeeAlsos;
							e1.Remarks = e.Remarks;
							e = e1;
							((Delegate)e).Params[name] = value;
						}
						else if (tag.ToLower() == "typeparam" && e is Class)
						{
							e1.Name = e.Name;
							e1.Summary = e.Summary;
							e1.SeeAlsos = e.SeeAlsos;
							e1.Remarks = e.Remarks;
							e = e1;
							((Delegate)e).TypeParam = value;
						}
						else if (tag.ToLower() == "returns" && e is Class)
						{
							e1.Name = e.Name;
							e1.Summary = e.Summary;
							e1.SeeAlsos = e.SeeAlsos;
							e1.Remarks = e.Remarks;
							e = e1;
							((Delegate)e).Return = value;
						}
						else
							Console.WriteLine(nameFields[1] + " has invalid member: " + tag + " = " + value);
					}
					xmlReader.Read();
				}

				entities.Add(e);
			}
			else
			{
				Console.WriteLine("Expected element <member> but got <" + xmlReader.Name + "> instead");
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <returns></returns>
		private string ReadTag(XmlTextReader xmlReader)
		{
			string name = xmlReader.Name;
			string value = "";
			//Console.WriteLine("Reading tag: " + name + "\t\tEmpty: " + xmlReader.IsEmptyElement);

			if (xmlReader.IsEmptyElement)
			{
				if (xmlReader.HasAttributes)
					value = xmlReader.GetAttribute(0);
			}
			else
			{
				xmlReader.Read();
				while (!(xmlReader.Name == name && xmlReader.NodeType == XmlNodeType.EndElement))
				{
					if (xmlReader.NodeType == XmlNodeType.Text)
						value += xmlReader.Value;
					else if (xmlReader.NodeType == XmlNodeType.Element)
					{
						if (xmlReader.IsEmptyElement)
						{
							value += "<" + xmlReader.Name;
							if (xmlReader.HasAttributes)
								while (xmlReader.MoveToNextAttribute())
									value += String.Format(" {0}=\"{1}\"", xmlReader.Name, xmlReader.Value);
							value += "/>";
						}
						else
						{
							value += "<" + xmlReader.Name + ">";
							value += ReadTag(xmlReader);
							value += "</" + xmlReader.Name + ">";
						}
					}
					else if (xmlReader.NodeType == XmlNodeType.EndElement)
					{
						value += "</" + xmlReader.Name + ">";
					}

					xmlReader.Read();
				}
			}
			return value;
		}

        /// <summary>
        /// 
        /// </summary>
		private void Generate()
		{
			foreach (Class c in classes)
			{
				Console.WriteLine("Go: " + c.Name);

				if (c.Name == "YouTubePlayerInterface")
					Console.WriteLine("Stop!");

				Directory.CreateDirectory("output");
				StreamWriter sw = File.CreateText(Path.Combine("output", c.Name + "Class.wiki"));
				
				sw.WriteLine("#summary Class specification of " + c.Name);
				sw.WriteLine("#labels Doc-Class");

				sw.WriteLine("= Overview =");
				sw.WriteLine(FixInformation(c, c, false));
				sw.WriteLine("<wiki:toc/>");
				sw.WriteLine("----");

				if (c.Constructor != null)
				{
					sw.WriteLine("= Constructor =");
					sw.WriteLine("||`" + FixMethodName(c.Name, c.Constructor.Name, c.Constructor.Params) + "`||");
					sw.WriteLine("");
					foreach (DictionaryEntry o in c.Constructor.Params)
						sw.WriteLine(" _" + (o.Key as string) + ": " + (o.Value as string) + "_\n");
					sw.WriteLine(FixInformation(c, c.Constructor));
				}

				if (c.Destructor != null)
				{
					sw.WriteLine("= Destructor =");
					sw.WriteLine("||`" + FixMethodName(c.Name, c.Destructor.Name, c.Destructor.Params) + "`||");
					foreach (DictionaryEntry o in c.Destructor.Params)
						sw.WriteLine(" _" + (o.Key as string) + ": " + (o.Value as string) + "_\n");
					sw.WriteLine(FixInformation(c, c.Destructor));
				}

				if (c.Fields.Count > 0 || c.Properties.Count > 0)
				{
					sw.WriteLine("= Members =");

					if (c.Fields.Count > 0)
					{
						sw.WriteLine("== Fields ==");
						sw.WriteLine("");
						foreach (Field f in c.Fields)
						{
							sw.WriteLine("===" + EscapeCamelLink(f.Name) + "===");
							sw.WriteLine("");
							sw.WriteLine(FixInformation(c, f));
						}
					}

					if (c.Properties.Count > 0)
					{
						sw.WriteLine("== Properties ==");
						sw.WriteLine("");
						foreach (Property p in c.Properties)
						{
							sw.WriteLine("===" + EscapeCamelLink(p.Name) + "===");
							sw.WriteLine("");
							sw.WriteLine(FixInformation(c, p));
						}
					}
				}

				if (c.Methods.Count > 0)
				{
					sw.WriteLine("= Methods =");
					sw.WriteLine("");
					foreach (Method m in c.Methods)
					{
						sw.WriteLine("===" + EscapeCamelLink(StripMethodName(m.Name)) + "===");
						sw.WriteLine("");
						sw.WriteLine("||`" + FixMethodName(c.Name, m.Name, m.Params) + "`||");
						sw.WriteLine("");
						foreach (DictionaryEntry o in m.Params)
							sw.WriteLine(" _" + (o.Key as string) + ": " + (o.Value as string) + "_\n");
						sw.WriteLine(FixInformation(c, m));
					}
				}

				if (c.Delegates.Count > 0)
				{
					sw.WriteLine("= Delegates =");
					sw.WriteLine("");
					foreach (Delegate d in c.Delegates)
					{
						sw.WriteLine("===" + EscapeCamelLink(StripMethodName(d.Name)) + "===");
						sw.WriteLine("");
						sw.WriteLine("||`" + FixMethodName(c.Name, d.Name, d.Params) + "`||");
						sw.WriteLine("");
						foreach (DictionaryEntry o in d.Params)
							sw.WriteLine(" _" + (o.Key as string) + ": " + (o.Value as string) + "_\n");
						sw.WriteLine(FixInformation(c, d));
					}
				}

				if (c.Events.Count > 0)
				{
					sw.WriteLine("= Events =");
					sw.WriteLine("");
					foreach (Event e in c.Events)
					{
						sw.WriteLine("===" + EscapeCamelLink(e.Name) + "===");
						sw.WriteLine("");
						sw.WriteLine(FixInformation(c, e));
					}
				}
				sw.Close();
			}

			foreach (Enum e in enums)
			{
				Directory.CreateDirectory("output");
				StreamWriter sw = File.CreateText(Path.Combine("output", e.Name + "Enum.wiki"));

				sw.WriteLine("#summary Enum specification of " + e.Name);
				sw.WriteLine("#labels Doc-Enum");
				sw.WriteLine("= Overview =");
				sw.WriteLine(FixInformation(e, e, false));
				sw.WriteLine("<wiki:toc/>");
				sw.WriteLine("----");

				if (e.Values.Count > 0)
				{
					sw.WriteLine("= Values =");
					sw.WriteLine("");
					foreach (Value v in e.Values)
					{
						sw.WriteLine("==" + v.Name + "==");
						sw.WriteLine(FixInformation(e, v));
					}
				}
				sw.Close();
			}

			foreach (Delegate e in delegates)
			{
				Directory.CreateDirectory("output");
				StreamWriter sw = File.CreateText(Path.Combine("output", e.Name + "Delegate.wiki"));

				sw.WriteLine("#summary Delegate specification of " + e.Name);
				sw.WriteLine("#labels Doc-Delegate");
				sw.WriteLine("= Overview =");
				sw.WriteLine(FixInformation(e, e, false));
				sw.WriteLine("<wiki:toc/>");
				sw.WriteLine("----");

				sw.WriteLine("");
				sw.WriteLine("||`" + FixMethodName("", e.Name, e.Params) + "`||");
				sw.WriteLine("");
				foreach (DictionaryEntry o in e.Params)
					sw.WriteLine(" _" + (o.Key as string) + ": " + (o.Value as string) + "_\n");
				sw.Close();
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
		private string EscapeCamelLink(string str)
		{
			if (HasCamelCase(str)) return "!" + str;
			else return str;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="e"></param>
        /// <param name="returnToTop"></param>
        /// <returns></returns>
		private string FixInformation(Entity p, Entity e, bool returnToTop = true)
		{
			string txt = ParseForLinks(FixSummary(e.Summary)) + "\n\n";

			if (e.Remarks.Count > 0) txt += "\n";
			foreach (string remark in e.Remarks)
				txt += " _Remark: " + ParseForLinks(remark) + "_\n\n";

			if (e.SeeAlsos.Count > 0) txt += "\n";
			foreach (string seealso in e.SeeAlsos)
				txt += " _See also: " + ParseSeeAlso(seealso) + "_\n\n";

			if (returnToTop)
			{
				txt += "[" + p.Name + (p is Class ? "Class":"Enum") + "#Overview Return to top]\n\n<br/>";
			}

			return txt;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
		private bool HasCamelCase(string str)
		{
			string pat = @"^[A-Z]+.*[a-z]+.*[A-Z].*[a-z]+";

			Regex r = new Regex(pat, RegexOptions.None);

			Match m = r.Match(str);
			return m.Success;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="summary"></param>
        /// <returns></returns>
		private string FixSummary(string summary)
		{
			summary = summary.Trim();
			summary = summary.Replace("            ", "");
			summary = summary.Replace("1)", "  #");
			summary = summary.Replace("2)", "  #");
			summary = summary.Replace("3)", "  #");
			summary = summary.Replace("4)", "  #");
			summary = summary.Replace("5)", "  #");
			summary = summary.Replace("6)", "  #");
			summary = summary.Replace("7)", "  #");
			summary = summary.Replace("8)", "  #");
			summary = summary.Replace("9)", "  #");

			return summary;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cname"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
		private string FixMethodName(string cname, string method, Hashtable args)
		{
			string name = method;
			string arguments = "";

			if (name.EndsWith(")"))
			{
				List<string> pnames = new List<string>();
				foreach (DictionaryEntry o in args)
					pnames.Add(o.Key as string);
				string paras = "";

				name = method.Substring(0, method.IndexOf('('));
				paras = method.Substring(method.IndexOf('(') + 1);
				paras = paras.Substring(0, paras.Length - 1);

				string[] para = paras.Split(',');
				int i=0;
				foreach (string ptype in para)
				{
					arguments += ptype.Substring(ptype.LastIndexOf('.') + 1) + " " + pnames[i++] + ", ";
				}

				arguments = arguments.Substring(0, arguments.Length - 2);
			}

			return name.Replace("Finalize", "~" + cname).Replace("#cctor", cname).Replace("#ctor", cname) + "(" + arguments + ")";
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mname"></param>
        /// <returns></returns>
		private string StripMethodName(string mname)
		{
			if (mname.Contains('(')) return mname.Substring(0, mname.IndexOf('('));
			else return mname;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
		private string ParseSeeAlso(string str)
		{
			if (!str.Contains(':')) return str;

			string type = str.Substring(0, str.IndexOf(':'));
			str = str.Substring(str.IndexOf(':')+1);

			if (str.Contains('(')) str = str.Substring(0, str.IndexOf('('));

			int i = str.LastIndexOf('.');
			if (i < 0) return str;

			string parent = str.Substring(0, i);
			string name = str.Substring(i + 1);

			if (parent.StartsWith("Stoffi.Utilities."))
				parent = parent.Remove(0, "Stoffi.Utilities.".Length);

			else if (parent.StartsWith("Stoffi."))
				parent = parent.Remove(0, "Stoffi.".Length);

			if (type == "T")
			{
				foreach (Class c in classes)
					if (c.Name == name) return "[" + c.Name + "Class " + name + "]";
				foreach (Enum e in enums)
					if (e.Name == name) return "[" + e.Name + "Enum " + name + "]";
				return "[" + parent + "." + name + " " + parent + "." + name + "]";
			}

			string lstr = name + " " + name;
			if (type == "M") lstr += "()";

			foreach (Class c in classes)
				if (c.Name == parent) return "[" + c.Name + "Class#" + lstr + "]";
			foreach (Enum e in enums)
				if (e.Name == parent) return "[" + e.Name + "Enum#" + lstr + "]";
			return "[" + parent + "." + name + " " + parent + "." + name + "]";
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
		private string ParseForLinks(string str)
		{
			if (str.Length <= 0) return str;

			string ret = "";
			foreach (string word in str.Split(new char[3] { ' ', '.', ','}))
			{
				string parsedWord = Linkalize(word);
				if (parsedWord == word && HasCamelCase(parsedWord)) parsedWord = "!" + parsedWord;
				ret += " " + parsedWord;
			}
			return ret.Substring(1);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
		private string Linkalize(string str)
		{
			foreach (Class c in classes)
			{
				if (c.Name == str) return "[" + c.Name + "Class " + str + "]";
				foreach (Field f in c.Fields)
					if (f.Name == str) return "[" + c.Name + "Class#" + f.Name + " " + str + "]";
				foreach (Property p in c.Properties)
					if (p.Name == str) return "[" + c.Name + "Class#" + p.Name + " " + str + "]";
				foreach (Method m in c.Methods)
					if (StripMethodName(m.Name) == str) return "[" + c.Name + "Class#" + StripMethodName(m.Name) + " " + str + "]";
				foreach (Event e in c.Events)
					if (e.Name == str) return "[" + c.Name + "Class#" + e.Name + " " + str + "]";
			}
			foreach (Enum e in enums)
			{
				if (e.Name == str) return "[" + e.Name + "Enum " + str + "]";
				foreach (Value v in e.Values)
					if (v.Name == str) return "[" + e.Name + "Enum#" + v.Name + " " + str + "]";
			}
			return str;
		}

        #endregion

        #region Event handlers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			Nullable<bool> result = dlg.ShowDialog();
			if (result == true)
			{
				Read(dlg.FileName);
			}
		}

        #endregion

        #endregion

        #region Classes

        /// <summary>
        /// 
        /// </summary>
		class Entity
        {
            #region Members

            public string Name;
			public string Summary;
			public List<string> SeeAlsos = new List<string>();
			public List<string> Remarks = new List<string>();

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
		class Class : Entity
        {
            #region Members

            public List<Property> Properties = new List<Property>();
			public Method Constructor = null;
			public Method Destructor = null;
			public List<Method> Methods = new List<Method>();
			public List<Delegate> Delegates = new List<Delegate>();
			public List<Event> Events = new List<Event>();
			public List<Field> Fields = new List<Field>();

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
		class Field : Entity
		{
		}

        /// <summary>
        /// 
        /// </summary>
		class Event : Entity
		{
		}

        /// <summary>
        /// 
        /// </summary>
		class Property : Entity
		{
		}

        /// <summary>
        /// 
        /// </summary>
		class Method : Entity
        {
            #region Members

            public string Return;
			public string TypeParam;
			public Hashtable Params = new Hashtable();

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
		class Enum : Entity
        {
            #region Members

            public List<Value> Values = new List<Value>();

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
		class Value : Entity
		{
        }

		/// <summary>
		/// 
		/// </summary>
		class Delegate : Entity
		{
			#region Members

			public string Return;
			public string TypeParam;
			public Hashtable Params = new Hashtable();

			#endregion
		}

        #endregion
    }
}
