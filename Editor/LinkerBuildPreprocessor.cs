using System.Collections.Generic;
using System.Xml;
using SwiftFramework.Core.Editor;
using SwiftFramework.Core.SharedData;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Editor
{
    internal static class LinkerBuildPreprocessor
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
        }
 
        private static void BuildPlayerHandler(BuildPlayerOptions options)
        {
            GenerateLinker();
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }

        private static IEnumerable<AssemblyLinkerInfo> GetPreserveInfos()
        {
            foreach (PluginData plugin in PluginsManifest.Instance.GetPlugins())
            {
                PluginInfo info = AssetDatabase.LoadAssetAtPath<PluginInfo>(plugin.path);
                if (info == null || info.linkAssemblies == null || info.Installed == false)
                {
                    continue;
                }

                foreach (AssemblyLinkerInfo assemblyLinker in info.linkAssemblies)
                {
                    yield return assemblyLinker;
                }
            }

            foreach (LinkerPreserveDefinition preserveDefinition in Util.GetAssets<LinkerPreserveDefinition>())
            {
                yield return preserveDefinition.assemblyToPreserve;
            }
        }
        
        
        private static void GenerateLinker()
        {
            XmlDocument linker = new XmlDocument();
            XmlElement root = linker.CreateElement("linker");
            linker.AppendChild(root);
            foreach (AssemblyLinkerInfo assemblyInfo in GetPreserveInfos())
            {
                XmlElement assemblyElement = linker.CreateElement("assembly");
                AssemblyDefinition data = assemblyInfo.assembly.GetData();
                assemblyElement.SetAttribute("fullname", data.name);
                XmlElement type = linker.CreateElement("type");
                foreach (string rootNamespace in assemblyInfo.rootNamespaces)
                {
                    type.SetAttribute("fullname", rootNamespace + ".*");
                    type.SetAttribute("preserve", "all");
                }

                assemblyElement.AppendChild(type);
                root.AppendChild(assemblyElement);
            }
            linker.Save(Application.dataPath + "/link.xml");
        }
    }
}