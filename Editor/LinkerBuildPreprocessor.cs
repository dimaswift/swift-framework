using System.Collections.Generic;
using System.Xml;
using SwiftFramework.Core.Editor;
using SwiftFramework.Core.SharedData;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Editor
{
    public static class LinkerBuildPreprocessor
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildHook.OnBeforeBuild += GenerateLinker;
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
            BuildHook.OnBeforeBuild -= GenerateLinker;
            XmlDocument linker = new XmlDocument();
            XmlElement root = linker.CreateElement("linker");
            linker.AppendChild(root);
            foreach (AssemblyLinkerInfo assemblyInfo in GetPreserveInfos())
            {
                XmlElement assemblyElement = linker.CreateElement("assembly");
                AssemblyDefinition data = assemblyInfo.assembly.GetData();
                assemblyElement.SetAttribute("fullname", data.name);
                assemblyElement.SetAttribute("preserve", "all");
                root.AppendChild(assemblyElement);
            }
            linker.Save(Application.dataPath + "/link.xml");
        }
    }
}