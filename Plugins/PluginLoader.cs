using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Stoffi.plugin
{
    /// <summary>
    /// Loads external plugin DLL files.
    /// </summary>
    public class PluginLoader
    {
        /// <summary>
        /// Loads a plugin given a file name to a DLL.
        /// Sample taken from: http://www.michael-clarke-blog.com/2010/08/c-dynamic-class-loading/
        /// </summary>
        /// <param name="fileName">Name of a plugin DLL</param>
        /// <returns>An instance of the plugin in the file if it is of correct type, otherwise returns null</returns>
        public static Plugin getInstance(String fileName)
        {

            /* Load in the assembly. */
            Assembly pluginAssembly = Assembly.LoadFile(fileName);

            /* Get the types of classes that are in this assembly. */
            Type[] types = pluginAssembly.GetTypes();

            /* Loop through the types in the assembly until we find
             * a class that implements a Plugin.
             */
            foreach (Type type in types)
            {
                if (type.GetInterface("Plugin") != null)
                {
                    /* Create a new instance of the 'Plugin'. */
                    return (Plugin)Activator.CreateInstance(type);
                }
            }

            return null;

        }
    }
}
