using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    public static class SharedResources
    {
        public static string TimerVersion = "v." + DateTime.Now.ToShortDateString();
        public static string AssemblyName = "MetaCity";
        public static Bitmap AssemblyIcon = Resources.AssemblyIcon;
        public static string AssemblyDescription = "MetaCityGenerator GH plugin for urban designers";
        public static string AssemblyAuthor = "Luo, Lin, Deng, Yang";
        public static Guid AssemblyGuid = new Guid("C2B39A46-47F0-45BF-A933-727D5FE575E5");
        public static string AssemblyContacts = "MetaCity Lab";


        public static XElement GetXML(string moduleName, string componentId)
        {
            return (from c in (from c in XDocument.Parse(Resources.MetaData).Root.Descendants("module")
                               where (string)c.Attribute("name") == moduleName
                               select c).Elements("component")
                    where c.Element("id").Value == componentId
                    select c).First();
        }

        public static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            if (name.Contains(".resources")) { return null; }
            string name2 = name + ".dll";
            Assembly.GetAssembly(typeof(SharedResources)).GetManifestResourceNames();
            Assembly result;
            using (Stream manifestResourceStream = Assembly.GetAssembly(typeof(SharedResources)).GetManifestResourceStream(name2))
            {
                if (manifestResourceStream == null) { result = null; }
                else
                {
                    byte[] array = new byte[manifestResourceStream.Length];
                    manifestResourceStream.Read(array, 0, array.Length);
                    result = Assembly.Load(array);
                }
            }
            return result;
        }
    }
}
