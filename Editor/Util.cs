using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Swift.Core;
using Swift.Core.Editor;
using Swift.Helpers;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Swift.EditorUtils
{
    public class Util : ScriptableSingleton<Util>
    {
        public static IEnumerable<Type> GetCustomModuleInterfaces()
        {
            CacheModuleTypes();
            
            foreach (Type type in cachedModuleInterfaces)
            {
                ModuleGroupAttribute groupAttribute = type.GetCustomAttribute<ModuleGroupAttribute>();
                if (groupAttribute != null && groupAttribute.GroupId == ModuleGroups.Core)
                {
                    continue;
                }

                yield return type;
            }
        }

        public static IEnumerable<Type> GetModuleInterfaces(string groupId = null)
        {
            CacheModuleTypes();
            
            foreach (Type type in cachedModuleInterfaces)
            {
                ModuleGroupAttribute groupAttribute = type.GetCustomAttribute<ModuleGroupAttribute>();
                if (groupId != null && (groupAttribute == null || groupAttribute.GroupId != groupId))
                {
                    continue;
                }

                yield return type;
            }
        }
        
        private static void CacheModuleTypes()
        {
            if (cachedModuleInterfaces.Count == 0)
            {
                foreach (Type type in GetAllTypes())
                {
                    if (type.IsInterface && typeof(IModule).IsAssignableFrom(type) && type != typeof(IModule))
                    {
                        cachedModuleInterfaces.Add(type);
                    }
                }
            }
        }
        
        public static event Action<Type, ModuleConfig> OnModuleConfigApplied = (type, moduleConfig) => { };

        public static event Action OnScriptsReloaded = () => { };
        public static event Action OnAssetsReimported = () => { };
        private static readonly List<Type> cachedModuleInterfaces = new List<Type>();
        private static readonly List<Type> cachedTypes = new List<Type>();
        private static readonly MD5CryptoServiceProvider hashSumProvider = new MD5CryptoServiceProvider();

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            
        }
        
        [SerializeField]
        internal List<DeferredInstantiation> deferredScriptableCreations = new List<DeferredInstantiation>();


        [Serializable]
        internal class DeferredInstantiation
        {
            public string path;
            public string type;
        }

        public static void DeleteEmptyFolders()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories("*.*", SearchOption.AllDirectories))
            {
                if (subDirectory.Exists)
                {
                    ScanDirectory(subDirectory);
                }
            }
        }

        private static void ScanDirectory(DirectoryInfo subDirectory)
        {
            var filesInSubDirectory = subDirectory.GetFiles("*.*", SearchOption.AllDirectories);

            if (filesInSubDirectory.Length == 0 || filesInSubDirectory.All(t => t.FullName.EndsWith(".meta")))
            {
                subDirectory.Delete(true);
                if (File.Exists(subDirectory.FullName + ".meta"))
                {
                    File.Delete(subDirectory.FullName + ".meta");
                }
            }
        }

        private static string ManifestPackagePath
        {
            get
            {
                DirectoryInfo dir = new DirectoryInfo(Application.dataPath);
                return $"{dir.Parent?.FullName}/Packages/manifest.json";
            }
        }
        
        public static bool HasPackageDependency(string dependencyName, string version)
        {
            string dep = $"\"{dependencyName}\": \"{version}\"";
            string manifestText = File.ReadAllText(ManifestPackagePath);
            return manifestText.Contains(dep);
        }

        public static Type FindImplementation(Type interfaceType)
        {
            foreach (Type type in GetAllTypes())
            {
                if (type != interfaceType && interfaceType.IsAssignableFrom(type))
                {
                    return type;
                }
            }

            return null;
        }
        
        public static bool AddDependencyToPackageManifest(string dependency, string version)
        {
            if (string.IsNullOrEmpty(dependency) || string.IsNullOrEmpty(version))
            {
                Debug.LogError($"Cannot add package {dependency}:{version}. Invalid name and/or version");
                return false;
            }

            if (HasPackageDependency(dependency, version))
            {
                return false;
            }

            var dep = $"\"{dependency}\": \"{version}\"";
            var manifestLines = new List<string>(File.ReadAllLines(ManifestPackagePath));
            var startIndex = manifestLines.FindIndex(l => l.Contains($"dependencies\": {{")) + 1;
            manifestLines.Insert(startIndex, $"   {dep},");
            File.WriteAllLines(ManifestPackagePath, manifestLines);
            return true;
        }

        internal static string RelativeFrameworkRootFolder
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(instance));
                return new FileInfo(path).Directory?.Parent?.Parent?.FullName;
            }
        }

        internal static string FrameworkRootFolder
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(instance));
                return PathUtils.ToRelativePath(new FileInfo(path).Directory?.Parent?.Parent?.FullName);
            }
        }

        public static void CreateAssetAfterScriptReload(string type, string path)
        {
            instance.deferredScriptableCreations.Add(new DeferredInstantiation()
            {
                path = path,
                type = type
            });
            EditorUtility.SetDirty(instance);
        }

        public static IEnumerable<string> CopyDirectory([NotNull] string source, [NotNull] string destination,
            string exceptFilesWithExtensions = null)
        {
            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var newDir = dirPath.Replace(source, destination);
                Directory.CreateDirectory(newDir);
            }


            foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                if (exceptFilesWithExtensions != null && Path.GetExtension(newPath) == exceptFilesWithExtensions)
                {
                    continue;
                }

                string dir = new FileInfo(newPath?.Replace(source, destination)).Directory?.FullName;

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string newFile = newPath?.Replace(source, destination);

                if (File.Exists(newFile) == false)
                {
                    File.Copy(newPath, newFile);
                }

                yield return newFile;
            }
        }


        internal static string EditorFolder => "Assets/Editor";

        public static void ApplyModuleConfig(Type moduleType, ModuleConfig moduleConfig)
        {
            OnModuleConfigApplied(moduleType, moduleConfig);
        }

        public static void PromptMoveAssetsFromFolder(string sourceFolder, string destFolder)
        {
            var sourceFolderPath = ToAbsolutePath("Assets/" + sourceFolder);

            if (Directory.Exists(sourceFolderPath))
            {
                if (new DirectoryInfo(sourceFolderPath).EnumerateFiles().CountFast() == 0)
                {
                    return;
                }

                if (EditorUtility.DisplayDialog("Warning",
                    $"{sourceFolder} folder detected. Do you want to move assets to {destFolder} folder?", "Yes", "No"))
                {
                    AssetDatabase.CreateFolder("Assets", destFolder);

                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                    foreach (var item in AssetDatabase.FindAssets("t: Object", new string[] {"Assets/" + sourceFolder}))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(item);
                        AssetDatabase.MoveAsset(path, path.Replace($"/{sourceFolder}/", $"/{destFolder}/"));
                    }

                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    Directory.Delete(sourceFolderPath, true);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }

        public static void OpenPrefab<T>()
        {
            int? id = FindPrefabPath(typeof(T))?.GetInstanceID();
            if (id.HasValue)
                AssetDatabase.OpenAsset(id.Value);
        }

        private static GameObject FindPrefabPath(Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject"))
            {
                GameObject go =
                    AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go == null)
                {
                    continue;
                }

                if (go.GetComponent(type))
                {
                    return go;
                }
            }

            return null;
        }


        public static string ToAbsolutePath(string relativePath)
        {
            return Application.dataPath + relativePath.Remove(0, 6);
        }
        
        public static IEnumerable<Object> GetAllAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets(""))
            {
                yield return AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        public static IEnumerable<T> GetAssets<T>(string name = "", params string[] folders)
            where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name} {name}", folders)
                .Select(x => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(x), typeof(T)) as T);
        }

        public static IEnumerable<Object> GetAssets(Type type, string name = "", params string[] folders)
        {
            return AssetDatabase.FindAssets($"t:{type.Name} {name}", folders)
                .Select(x => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(x), type));
        }

        private static Object GetAsset(Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{type.Name}", new string[] { ResourcesAssetHelper.RootFolder }))
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);
            }

            return null;
        }

        public static GameObject FindPrefabWithInterface(Type interfaceType)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new string[] { ResourcesAssetHelper.RootFolder }))
            {
                GameObject go =
                    AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)) as GameObject;
                if (go == null)
                {
                    continue;
                }

                if (go.GetComponent(interfaceType))
                {
                    return go;
                }
            }

            return null;
        }

        public static IEnumerable<T> FindModules<T>() where T : IModule
        {
            foreach (GameObject asset in GetAssets<GameObject>())
            {
                if (asset.GetComponent<T>() != null)
                {
                    yield return asset.GetComponent<T>();
                }
            }
        }

        public static T PromptCreateScriptable<T>() where T : ScriptableObject
        {
            return PromptCreateScriptable(typeof(T), typeof(T).Name) as T;
        }

        private static int GetConfigCount(Type type)
        {
            int result = 0;
            foreach (Object asset in GetAssets(type, "", ResourcesAssetHelper.RootFolder))
            {
                result++;
            }

            return result;
        }

        public static T PromptChooseScriptable<T>(string folder = "Assets") where T : ScriptableObject
        {
            return PromptChooseScriptable(typeof(T), folder) as T;
        }

        private static Object PromptCreateScriptable(Type type, string folder)
        {
            return PromptCreateScriptable(type, type.Name, folder);
        }

        private static Object PromptChooseScriptable(Type type, string folder = "Assets")
        {
            int count = GetConfigCount(type);
            switch (count)
            {
                case 0:
                    Debug.LogError($"Scriptable object of type {type.Name} not found");
                    return null;
                case 1:
                    return GetAsset(type);
            }

            string configPath = EditorUtility.OpenFilePanel("Choose config", folder, "asset");

            return string.IsNullOrEmpty(configPath) ? null : AssetDatabase.LoadAssetAtPath(PathUtils.ToRelativePath(configPath), type);
        }


        private static Object PromptCreateScriptable(Type type, string name, string folder)
        {
            string configPath = EditorUtility.SaveFilePanelInProject("Choose config path", name, "asset", "Save",
                Application.dataPath + "/Resources/" + folder);

            if (string.IsNullOrEmpty(configPath))
            {
                return null;
            }


            ScriptableObject config = CreateInstance(type);
            config.name = name;
            AssetDatabase.CreateAsset(config, configPath);

            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath(configPath, type);
        }

        public static ScriptableObject CreateScriptable(Type type, string name, string folder)
        {
            string configPath = Path.Combine(folder, name + ".asset");
            ScriptableObject config = CreateInstance(type);
            config.name = name;
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<ScriptableObject>(configPath);
        }

        public static void CreateModuleBehaviour(Type type, SerializedProperty link = null)
        {
            GameObject newBehaviour = new GameObject(type.Name);
            newBehaviour.AddComponent(type);
            string moduleFolder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Behaviours}";

            EnsureProjectFolderExists(moduleFolder);
            string prefabName = newBehaviour.name;
            string PrefabPath() => $"{moduleFolder}/{prefabName}.prefab";

            while (AssetDatabase.LoadAssetAtPath(PrefabPath(), type) != null)
            {
                prefabName += " (NEW)";
            }

            link?.SaveLink($"{Folders.Behaviours}/{prefabName}");

            PrefabUtility.SaveAsPrefabAsset(newBehaviour, PrefabPath(), out bool created);
            
            DestroyImmediate(newBehaviour);

            ReloadLinks();
        }

        public static void ReloadLinks()
        {
#if USE_ADDRESSABLES
            AddrHelper.Reload();
#else
            ResourcesAssetHelper.Reload();
#endif
        }
        
        
        public static ModuleConfig CreateModuleConfig(Type type, SerializedProperty link = null)
        {
            ScriptableObject config = CreateInstance(type);
            config.name = type.Name;
            string configsFolder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Configs}";

            EnsureProjectFolderExists(configsFolder);

            string configName = config.name;

            string ConfigPath() => $"{configsFolder}/{configName}.asset";

            while (AssetDatabase.LoadAssetAtPath(ConfigPath(), type) != null)
            {
                configName += " (NEW)";
            }

            AssetDatabase.CreateAsset(config, ConfigPath());
            
            link?.SaveLink($"{Folders.Configs}/{configName}");
            
            ReloadLinks();

            return AssetDatabase.LoadAssetAtPath(ConfigPath(), type) as ModuleConfig;
        }

        public static IEnumerable<ModuleManifest> GetModuleManifests()
        {
            foreach (ModuleManifest manifest in GetAssets<ModuleManifest>())
            {
                if (manifest.State == ModuleState.Enabled)
                {
                    yield return manifest;
                }
            }
        }
        
        public static void EnsureProjectFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        public static T CreateScriptable<T>(string name, string folder) where T : ScriptableObject
        {
            return CreateScriptable(typeof(T), name, folder) as T;
        }

        public static IEnumerable<BehaviourModule> GetAllModules()
        {
            foreach (GameObject asset in GetAssets<GameObject>())
            {
                if (asset.GetComponent<BehaviourModule>() != null)
                {
                    foreach (BehaviourModule subModule in asset.GetComponentsInChildren<BehaviourModule>())
                    {
                        yield return subModule;
                    }
                }
            }
        }

        public static T FindModule<T>() where T : IModule
        {
            foreach (GameObject asset in GetAssets<GameObject>())
            {
                if (asset.GetComponent<T>() != null)
                {
                    return asset.GetComponent<T>();
                }
            }

            return default;
        }

        public static T FindScriptableObject<T>() where T : ScriptableObject
        {
            foreach (ScriptableObject asset in GetAssets<ScriptableObject>())
            {
                if (asset is T scriptableObject)
                {
                    return scriptableObject;
                }
            }

            return default;
        }

        public static T FindScriptableObject<T>(Func<T, bool> filter) where T : ScriptableObject
        {
            foreach (ScriptableObject asset in GetAssets<ScriptableObject>())
            {
                if (asset is T scriptableObject && filter(scriptableObject))
                {
                    return scriptableObject;
                }
            }

            return default;
        }

        public static IEnumerable<Type> GetAllTypes()
        {
            if (cachedTypes.Count == 0)
            {
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (TypeInfo type in a.DefinedTypes)
                    {
                        cachedTypes.Add(type);
                    }
                }
            }

            return cachedTypes;
        }

        private static Dictionary<string, string> GetAssemblyDefinitionLocations()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (AssemblyDefinitionAsset a in GetAssets<AssemblyDefinitionAsset>())
            {
                (string, string) result = (null, null);
                try
                {
                    JObject json = JObject.Parse(a.text);
                    result = (json.Value<string>("name"), AssetDatabase.GetAssetPath(a));
                }
                catch
                {
                    // ignored
                }

                if (result.Item1 != null)
                {
                    if (dict.ContainsKey(result.Item1) == false)
                    {
                        dict.Add(result.Item1, result.Item2);
                    }
                }
            }

            return dict;
        }

        public static IEnumerable<(string assemblyLocation, Type type)> GetAllTypesWithAssemblyPath()
        {
            var assemblyLocations = GetAssemblyDefinitionLocations();

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assemblyLocations.TryGetValue(a.GetName().Name, out string path) == false)
                {
                    path = "Assets/Scripts/commond.amdsf";
                }

                foreach (TypeInfo type in a.DefinedTypes)
                {
                    if (type.IsVisible == false)
                    {
                        continue;
                    }

                    yield return (path, type);
                }
            }
        }

        public static IEnumerable<Type> GetAllTypes(Func<Type, bool> predicate)
        {
            foreach (Type type in GetAllTypes())
            {
                if (predicate(type))
                {
                    yield return type;
                }
            }
        }

        public static bool IsDerivedFrom(Type childType, Type baseType)
        {
            while (childType.BaseType != null)
            {
                if (childType == baseType)
                {
                    return true;
                }

                childType = childType.BaseType;
            }

            return false;
        }

        public static string GetScriptFolder(Type type)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(MonoScript).Name}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms.GetClass() == type)
                {
                    FileInfo fi = new FileInfo(path);
                    return fi.Directory?.ToString();
                }
            }

            return null;
        }

        public static Type FindChildClass(Type baseClass)
        {
            foreach (Type type in GetAllTypes())
            {
                if (baseClass.IsAssignableFrom(type))
                {
                    if (type.Namespace != baseClass.Namespace)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        public static ScriptableObject FindScriptableObject(Type type)
        {
            foreach (ScriptableObject asset in GetAssets<ScriptableObject>())
            {
                if (asset.GetType() == type)
                {
                    return asset;
                }
            }

            return default;
        }

        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal)).Replace("[", "")
                        .Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }

            return obj;
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            Type type = source.GetType();

            while (type != null)
            {
                FieldInfo f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                PropertyInfo p = type.GetProperty(name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }

            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            if (!(GetValue_Imp(source, name) is IEnumerable enumerable)) return null;
            IEnumerator enm = enumerable.GetEnumerator();

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }

            return enm.Current;
        }


        public static List<PropertyAttribute> GetFieldAttributes(FieldInfo field)
        {
            if (field == null)
                return null;

            object[] attrs = field.GetCustomAttributes(typeof(PropertyAttribute), true);
            return attrs.Length > 0 ? new List<PropertyAttribute>(attrs.Select(e => e as PropertyAttribute).OrderBy(e => -e.order)) : null;
        }

        [DidReloadScripts(1)]
        private static void DidReloadScripts()
        {
            OnScriptsReloaded();

            foreach (var item in instance.deferredScriptableCreations)
            {
                AssetDatabase.CreateAsset(CreateInstance(item.type), item.path);
            }

            instance.deferredScriptableCreations.Clear();
        }
    }

    public static class ScriptBuilder
    {
        public static CodeCompileUnit GenerateManifestClass(string classNamespace, Type gameClass)
        {
            CodeCompileUnit manifestFile = new CodeCompileUnit();

            CodeNamespace manifestNamespace = new CodeNamespace(classNamespace);

            manifestNamespace.Imports.Add(new CodeNamespaceImport("SwiftFramework.Core"));
            manifestNamespace.Imports.Add(new CodeNamespaceImport("UnityEngine"));

            const string className = "ModuleManifest";

            CodeTypeDeclaration manifestClass = new CodeTypeDeclaration(className);

            manifestClass.BaseTypes.Add(new CodeTypeReference("BaseModuleManifest"));

            foreach (PropertyInfo property in gameClass.GetProperties(
                BindingFlags.Instance | BindingFlags.Public))
            {
                if (typeof(IModule).IsAssignableFrom(property.PropertyType) &&
                    property.PropertyType.GetCustomAttribute<BuiltInModuleAttribute>() == null)
                {
                    string moduleName = property.Name;
                    CodeMemberField moduleFiled = new CodeMemberField()
                    {
                        Name = moduleName[0].ToString().ToLower() + moduleName.Substring(1, moduleName.Length - 1),
                        Type = new CodeTypeReference(typeof(ModuleLink)),
                    };
                    moduleFiled.CustomAttributes.Add(new CodeAttributeDeclaration("SerializeField"));

                    CodeAttributeDeclaration filterAttr = new CodeAttributeDeclaration(new CodeTypeReference("LinkFilter"));
                    
                    filterAttr.Arguments.Add(
                        new CodeAttributeArgument(new CodeTypeOfExpression(property.PropertyType)));

                    moduleFiled.CustomAttributes.Add(filterAttr);
                    
                    manifestClass.Members.Add(moduleFiled);
                }
            }

            CodeAttributeDeclaration createAssetAttr = new CodeAttributeDeclaration(new CodeTypeReference("CreateAssetMenu"));

            string projectName = string.IsNullOrEmpty(gameClass.Namespace) ? "Game" : gameClass.Namespace.Split('.')[0];

            createAssetAttr.Arguments.Add(new CodeAttributeArgument("menuName",
                new CodePrimitiveExpression($"{projectName}/{Folders.Configs}ModuleManifest")));

            createAssetAttr.Arguments.Add(new CodeAttributeArgument("fileName",
                new CodePrimitiveExpression("ModuleManifest")));

            manifestClass.CustomAttributes.Add(createAssetAttr);

            manifestNamespace.Types.Add(manifestClass);

            manifestFile.Namespaces.Add(manifestNamespace);

            return manifestFile;
        }

        public static CodeCompileUnit GenerateAppClass(string projectName, string nameSpace)
        {
            CodeCompileUnit unit = new CodeCompileUnit();

            CodeNamespace codeNamespace = new CodeNamespace(nameSpace);

            codeNamespace.Imports.Add(new CodeNamespaceImport("SwiftFramework.Core"));

            codeNamespace.Imports.Add(new CodeNamespaceImport("UnityEngine"));

            string className = $"{projectName}";

            CodeTypeDeclaration cls = new CodeTypeDeclaration(className);

            cls.BaseTypes.Add(new CodeTypeReference($"App<{className}>"));

            codeNamespace.Types.Add(cls);

            unit.Namespaces.Add(codeNamespace);

            return unit;
        }

        public static CodeCompileUnit GenerateAppBootClass(Type appClass)
        {
            CodeCompileUnit unit = new CodeCompileUnit();

            CodeNamespace codeNamespace = new CodeNamespace(appClass.Namespace);

            codeNamespace.Imports.Add(new CodeNamespaceImport("SwiftFramework.Core"));

            string className = $"AppBoot";

            CodeTypeDeclaration cls = new CodeTypeDeclaration(className);

            cls.BaseTypes.Add(new CodeTypeReference($"AppSceneBoot<{appClass.Name}>"));

            codeNamespace.Types.Add(cls);

            unit.Namespaces.Add(codeNamespace);

            return unit;
        }


        public static void SaveClassToDisc(CodeCompileUnit code, string path, bool force,
            Action<List<string>> postProcessor = null)
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions { BracingStyle = "C" };

            string tmpLinksScript = Path.GetTempFileName();

            StreamWriter sourceWriter = new StreamWriter(tmpLinksScript);
            provider.GenerateCodeFromCompileUnit(code, sourceWriter, options);
            sourceWriter.Close();

            bool updated = false;

            string dir = new FileInfo(path).Directory?.FullName;

            if (string.IsNullOrEmpty(dir) == false && Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }

            var file = new List<string>(File.ReadAllLines(tmpLinksScript));

            for (int i = file.Count - 1; i >= 0; i--)
            {
                if (file[i].StartsWith("//"))
                {
                    file.RemoveAt(i);
                }
            }

            file.RemoveAt(0);

            postProcessor?.Invoke(file);

            File.WriteAllLines(tmpLinksScript, file);

            if (force || File.Exists(path) == false || File.ReadAllText(tmpLinksScript) != File.ReadAllText(path))
            {
                File.WriteAllText(path, File.ReadAllText(tmpLinksScript));
                updated = true;
            }
            
            if (updated)
            {
                AssetDatabase.Refresh();
            }
        }

        public static string GetGameNamespace()
        {
            Type gameClass = Util.FindChildClass(typeof(App));
            if (gameClass != null)
            {
                return string.IsNullOrEmpty(gameClass.Namespace) ? "" : gameClass.Namespace.Split('.')[0];
            }

            return "";
        }
    }
}