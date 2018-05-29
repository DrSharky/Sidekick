using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using UnityEditor;

public static class LinkXMLFactory
{
    // Incomplete list of default components and types that shouldn't be stripped for debugging
    public static readonly Type[] DEFAULT_TYPES =
    {
        typeof(GameObject),
    };

    public static void GenerateForAllAssemblies()
    {
        //foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        //{
        //    //Debug.Log(assembly.FullName);
        //}
    }

    public static List<Type> GetUnityComponentTypes()
    {
        List<Type> componentTypes = new List<Type>();
        Type baseType = typeof(Component);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Restrict to official Unity runtime assemblies
            if(assembly.FullName.StartsWith("UnityEngine"))
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if(type.IsSubclassOf(baseType))
                    {
                        componentTypes.Add(type);
                    }
                }
            }
        }

        return componentTypes;
    }

    public static void Generate(List<Type> types)
    {
        Dictionary<Assembly, List<Type>> assemblyTypes = new Dictionary<Assembly, List<Type>>();
        foreach (var type in types)
        {
            Assembly assembly = type.Assembly;
            if (!assemblyTypes.ContainsKey(assembly))
            {
                assemblyTypes.Add(assembly, new List<Type>());
            }

            assemblyTypes[assembly].Add(type);
        }

        // Generate a link.xml
        var settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.Indent = true;

        using (XmlWriter writer = XmlWriter.Create("Assets/link.xml", settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("linker");

            foreach (var mapping in assemblyTypes)
            {
                writer.WriteStartElement("assembly");
                writer.WriteAttributeString("fullname", new AssemblyName(mapping.Key.FullName).Name);
                // Assemblies won't always be present, make sure they're optional
                writer.WriteAttributeString("ignoreIfMissing", "1");

                foreach (var item in mapping.Value)
                {
                    writer.WriteStartElement("type");
                    writer.WriteAttributeString("fullname", item.FullName);
                    writer.WriteAttributeString("preserve", "all");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        AssetDatabase.Refresh();
    }
}