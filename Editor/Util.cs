using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Reflection;
using SwiftFramework.Core;
using SwiftFramework.Core.Editor;
using System.CodeDom.Compiler;
using System.CodeDom;
using System;
using System.Security.Cryptography;
using UnityEditor.Callbacks;
using Newtonsoft.Json.Linq;

namespace SwiftFramework.EditorUtils
{
    public static class EditorUtilExtentions
    {
        public static void MoveToCenter(this EditorWindow window)
        {
            var position = window.position;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            window.position = position;
            window.Show();
        }

        public static string FromRelativeResourcesPathToAbsoluteProjectPath(this string path)
        {
            return string.IsNullOrEmpty(path) ? "Assets/Resources" : $"Assets/Resources/{path}";
        }

        public static string ToRelativeResourcesPath(this string path)
        {
            if (path.Contains("/Resources/") == false)
            {
                return "";
            }
            var file = new FileInfo(path);
            string result = file.Directory.Name;

            if (result == "Resources")
            {
                return "";
            }

            DirectoryInfo current = file.Directory.Parent;
            while (current != null && current.Name != "Resources")
            {
                result = $"{current.Name}/{result}";
                current = current.Parent;
            }
            return result;
        }

        public static T Value<T>(this LinkTo<T> link) where T : UnityEngine.Object
        {
#if USE_ADDRESSABLES
            return AddrHelper.GetAsset<T>(link);
#else
            return link.Value;
#endif
        }
    }


    public class Util : ScriptableSingleton<Util>
    {
        public const string AddressableVersion = "1.8.3";

        public static event Action<Type, ModuleConfig> OnModuleConfigApplied = (type, moduleConfig) => { };

        public static event Action OnScriptsReloaded = () => { };

        private static readonly List<Type> cachedTypes = new List<Type>();
        private static readonly MD5CryptoServiceProvider hashSumProvider = new MD5CryptoServiceProvider();


        [SerializeField] internal List<DeferredInstantiation> deferredScriptableCreations = new List<DeferredInstantiation>();


        [Serializable]
        internal class DeferredInstantiation
        {
            public string path;
            public string type;
        }

        public static void DeleteEmptyFolders()
        {
            var directoryInfo = new DirectoryInfo(Application.dataPath);
            foreach (var subDirectory in directoryInfo.GetDirectories("*.*", SearchOption.AllDirectories))
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

            if (filesInSubDirectory.Length == 0 ||
                !filesInSubDirectory.Any(t => t.FullName.EndsWith(".meta") == false))
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
                var dir = new DirectoryInfo(Application.dataPath);
                return $"{dir.Parent.FullName}/Packages/manifest.json";
            }
        }

        public static bool HasPackageDependency(string dependencyName)
        {
            var manifestText = File.ReadAllText(ManifestPackagePath);
            return manifestText.Contains(dependencyName);
        }

        public static void AddDependencyToPackageManifest(string dependency, string version)
        {
            if (string.IsNullOrEmpty(dependency) || string.IsNullOrEmpty(version))
            {
                Debug.LogError($"Cannot add package {dependency}:{version}. Invalid name and/or version");
                return;
            }

            var dep = $"\"{dependency}\": \"{version}\"";
            if (HasPackageDependency(dep))
            {
                return;
            }
            var manifestLines = new List<string>(File.ReadAllLines(ManifestPackagePath));
            var startIndex = manifestLines.FindIndex(l => l.Contains($"dependencies\": {{")) + 1;
            manifestLines.Insert(startIndex, $"   {dep},");
            File.WriteAllLines(ManifestPackagePath, manifestLines);
        }

        internal static string RelativeFrameworkRootFolder
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(instance));
                return new FileInfo(path).Directory.Parent.Parent.FullName;
            }
        }

        internal static string FrameworkRootFolder
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(instance));
                return ToRelativePath(new FileInfo(path).Directory.Parent.Parent.FullName);
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

        public static IEnumerable<string> CopyDirectory(string source, string destination, string exceptFilesWithExtentions = null)
        {
            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var newDir = dirPath.Replace(source, destination);
                Directory.CreateDirectory(newDir);
            }


            foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                if (exceptFilesWithExtentions != null && Path.GetExtension(newPath) == exceptFilesWithExtentions)
                {
                    continue;
                }

                var dir = new FileInfo(newPath.Replace(source, destination)).Directory.FullName;

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var newFile = newPath.Replace(source, destination);

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
                if (EditorUtility.DisplayDialog("Warning", $"{sourceFolder} folder detected. Do you want to move assets to {destFolder} folder?", "Yes", "No"))
                {
                    AssetDatabase.CreateFolder("Assets", destFolder);

                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                    foreach (var item in AssetDatabase.FindAssets("t: Object", new string[] { "Assets/" + sourceFolder }))
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
            var id = FindPrefabPath(typeof(T))?.GetInstanceID();
            if (id.HasValue)
                AssetDatabase.OpenAsset(id.Value);
        }

        public static GameObject FindPrefabPath(Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{ "GameObject" }"))
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)) as GameObject;
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

        public static string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return "";
            }
            if (Path.GetFullPath(absolutePath).StartsWith(Path.GetFullPath(Application.dataPath)))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            return absolutePath;
        }

        public const string PackageRelativeRootFolder = "Packages/com.dimaswift.swiftframework/";

        public static string ToAbsolutePath(string relativePath)
        {
            return Application.dataPath + relativePath.Remove(0, 6);
        }

        public static byte[] CalculateAssetHashSum(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return new byte[0];
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(assetPath))
            {
                return new byte[0];
            }

            List<byte> result = new List<byte>();

            foreach (string depAssetPath in AssetDatabase.GetDependencies(assetPath, true))
            {
                result.AddRange(CalculateHashSum(ToAbsolutePath(depAssetPath)));
                result.AddRange(CalculateHashSum(ToAbsolutePath(AssetDatabase.GetTextMetaFilePathFromAssetPath(depAssetPath))));
            }

            return result.ToArray();
        }


        public static IEnumerable<UnityEngine.Object> GetAllAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets(""))
            {
                yield return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        public static IEnumerable<T> GetAssets<T>(string name = "", params string[] folders) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets(string.Format("t:{0} {1}", typeof(T).Name, name), folders)
                .Select(x => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(x), typeof(T)) as T);
        }

        public static IEnumerable<UnityEngine.Object> GetAssets(Type type, string name = "", params string[] folders)
        {
            return AssetDatabase.FindAssets(string.Format("t:{0} {1}", type.Name, name), folders)
                .Select(x => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(x), type));
        }

        public static T GetAsset<T>() where T : UnityEngine.Object
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{ typeof(T).Name }"))
            {
                return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
            return null;
        }

        public static UnityEngine.Object GetAsset(Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{ type.Name }"))
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);
            }
            return null;
        }

        public static GameObject FindPrefabWithInterface(Type interfaceType)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{ "GameObject" }"))
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)) as GameObject;
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
            foreach (var asset in GetAssets<GameObject>())
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

        public static int GetConfigCount(Type type)
        {
            int result = 0;
            foreach (var asset in GetAssets(type))
            {
                result++;
            }
            return result;
        }

        public static T PromptChooseScriptable<T>(string folder = "Assets") where T : ScriptableObject
        {
            return PromptChooseScriptable(typeof(T), folder) as T;
        }

        public static UnityEngine.Object PromptCreateScriptable(Type type, string folder)
        {
            return PromptCreateScriptable(type, type.Name, folder);
        }

        public static UnityEngine.Object PromptChooseScriptable(Type type, string folder = "Assets")
        {
            int count = GetConfigCount(type);
            if (count == 0)
            {
                Debug.LogError($"Scriptable object of type {type.Name} not found");
                return null;
            }
            if (count == 1)
            {
                return GetAsset(type);
            }

            string configPath = EditorUtility.OpenFilePanel("Choose config", folder, "asset");

            if (string.IsNullOrEmpty(configPath))
            {
                return null;
            }


            return AssetDatabase.LoadAssetAtPath(ToRelativePath(configPath), type);
        }


        public static UnityEngine.Object PromptCreateScriptable(Type type, string name, string folder)
        {
            string configPath = EditorUtility.SaveFilePanelInProject("Choose config path", name, "asset", "Save", Application.dataPath + "/Resources/" + folder);

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

        public static byte[] CalculateHashSum(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                return new byte[0];
            }
            byte[] fileBytes = File.ReadAllBytes(filePath);
            return hashSumProvider.ComputeHash(fileBytes);
        }

        public static bool HashesAreEqual(byte[] h1, byte[] h2)
        {
            if (h1 == null || h2 == null || h1.Length != h2.Length) { return false; }

            for (int i = 0, maxi = h1.Length - 1; i <= maxi; i++)
            {
                if (h1[i] != h2[i]) { return false; }
            }

            return true;
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

        public static GameObject CreateModuleBehaviour(Type type, SerializedProperty link = null)
        {
            GameObject newBehaviour = new GameObject(type.Name);
            newBehaviour.AddComponent(type);
            string moduleFolder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Modules}";

            Util.EnsureProjectFolderExists(moduleFolder);
            string prefabName = newBehaviour.name;
            Func<string> prefabPath = () => $"{moduleFolder}/{prefabName}.prefab";

            while (AssetDatabase.LoadAssetAtPath(prefabPath(), type) != null)
            {
                prefabName += " (NEW)";
            }

            if (link != null)
            {
                link.SaveLink($"{ResourcesAssetHelper.RootFolder}/{Folders.Modules}/{prefabName}");
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(newBehaviour, prefabPath(), out bool created);

            DestroyImmediate(newBehaviour);

            return prefab;
        }

        public static ModuleConfig CreateModuleConfig(Type type, SerializedProperty link = null)
        {
            ScriptableObject config = CreateInstance(type);
            config.name = type.Name;
            string configsFolder = $"{ResourcesAssetHelper.RootFolder}/{Folders.Configs}";

            EnsureProjectFolderExists(configsFolder);

            string configName = config.name;

            Func<string> configPath = () => $"{configsFolder}/{configName}.asset";

            while (AssetDatabase.LoadAssetAtPath(configPath(), type) != null)
            {
                configName += " (NEW)";
            }
            AssetDatabase.CreateAsset(config, configPath());
            if (link != null)
            {
                link.SaveLink($"{Folders.Configs}/{configName}");
            }
            return AssetDatabase.LoadAssetAtPath(configPath(), type) as ModuleConfig;
        }

        public static void EnsureProjectFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        public static T CreateScriptable<T>(string name, string folder) where T : ScriptableObject
        {
            return CreateScriptable(typeof(T), name, folder) as T;
        }

        public static IEnumerable<Core.BehaviourModule> GetAllModules()
        {
            foreach (var asset in GetAssets<GameObject>())
            {
                if (asset.GetComponent<Core.BehaviourModule>() != null)
                {
                    foreach (var subModule in asset.GetComponentsInChildren<Core.BehaviourModule>())
                    {
                        yield return subModule;
                    }
                }
            }
        }

        public static T FindModule<T>() where T : IModule
        {
            foreach (var asset in GetAssets<GameObject>())
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
            foreach (var asset in GetAssets<ScriptableObject>())
            {
                if (asset is T)
                {
                    return asset as T;
                }
            }
            return default;
        }

        public static T FindScriptableObject<T>(Func<T, bool> filter) where T : ScriptableObject
        {
            foreach (var asset in GetAssets<ScriptableObject>())
            {
                if (asset is T && filter(asset as T))
                {
                    return asset as T;
                }
            }
            return default;
        }

        public static IEnumerable<Type> GetAllTypes()
        {
            if (cachedTypes.Count == 0)
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in a.DefinedTypes)
                    {
                        cachedTypes.Add(type);
                    }
                }
            }
            return cachedTypes;
        }

        public static Dictionary<string, string> GetAssembyDefinitionLocations()
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
            var assemblyLocations = GetAssembyDefinitionLocations();

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assemblyLocations.TryGetValue(a.GetName().Name, out string path) == false)
                {
                    path = "Assets/Scripts/commond.amdsf";
                }

                foreach (var type in a.DefinedTypes)
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
            foreach (var type in GetAllTypes())
            {
                if (predicate(type))
                {
                    yield return type;
                }
            }
        }

        public static bool IsDerrivedFrom(Type childType, Type baseType)
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
                    return fi.Directory.ToString();
                }
            }
            return null;
        }

        public static Type FindChildClass(Type baseClass)
        {
            foreach (var type in GetAllTypes())
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
            foreach (var asset in GetAssets<ScriptableObject>())
            {
                if (asset.GetType() == type)
                {
                    return asset;
                }
            }
            return default;
        }

        public static T ToLink<T>(UnityEngine.Object asset) where T : Link, new()
        {
            var path = new List<string>(AssetDatabase.GetAssetPath(asset).Split('/'));

            while (true)
            {
                path.RemoveAt(0);
                if (path[0] == "Resources")
                {
                    path.RemoveAt(0);
                    break;
                }
            }

            path[path.Count - 1] = Path.GetFileNameWithoutExtension(path[path.Count - 1]);

            return Link.Create<T>(string.Join("/", path.ToArray()));
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
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
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
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();

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
            if (attrs != null && attrs.Length > 0)
                return new List<PropertyAttribute>(attrs.Select(e => e as PropertyAttribute).OrderBy(e => -e.order));

            return null;
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

            string className = "ModuleManifest";

            CodeTypeDeclaration manifestClass = new CodeTypeDeclaration(className);

            manifestClass.BaseTypes.Add(new CodeTypeReference("BaseModuleManifest"));

            foreach (var property in gameClass.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                if (typeof(IModule).IsAssignableFrom(property.PropertyType) && property.PropertyType.GetCustomAttribute<BuiltInModuleAttribute>() == null)
                {
                    string moduleName = property.Name;
                    var moduleFiled = new CodeMemberField()
                    {
                        Name = moduleName[0].ToString().ToLower() + moduleName.Substring(1, moduleName.Length - 1),
                        Type = new CodeTypeReference(typeof(ModuleLink)),

                    };
                    moduleFiled.CustomAttributes.Add(new CodeAttributeDeclaration("SerializeField"));

                    var filterAttr = new CodeAttributeDeclaration(new CodeTypeReference("LinkFilter"));


                    filterAttr.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(property.PropertyType)));

                    moduleFiled.CustomAttributes.Add(filterAttr);


                    manifestClass.Members.Add(moduleFiled);
                }
            }

            var createAssetAttr = new CodeAttributeDeclaration(new CodeTypeReference("CreateAssetMenu"));

            string projectName = string.IsNullOrEmpty(gameClass.Namespace) ? "Game" : gameClass.Namespace.Split('.')[0];

            createAssetAttr.Arguments.Add(new CodeAttributeArgument("menuName", new CodePrimitiveExpression($"{projectName}/{Folders.Configs}ModuleManifest")));

            createAssetAttr.Arguments.Add(new CodeAttributeArgument("fileName", new CodePrimitiveExpression("ModuleManifest")));

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


        public static void SaveClassToDisc(CodeCompileUnit code, string path, bool force, Action<List<string>> postProcessor = null)
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";

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
