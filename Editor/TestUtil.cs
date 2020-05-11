using SwiftFramework.Core;
using SwiftFramework.Core.Editor;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SwiftFramework.EditorUtils
{
    internal class TestUtil : ScriptableSingleton<TestUtil>
    {
        [SerializeField] private List<TestConfig> pendingTests = new List<TestConfig>();

        [Serializable]
        private class TestConfig
        {
            public string manifestPath;
            public string manifestType;
        }

        public static void CreateTest()
        {
            var candidates = Util.GetAllTypes(t => typeof(IModule).IsAssignableFrom(t) && t.IsInterface == false);
            TypeSelectorWindow.Open(candidates, "Choose Modules").Done(moduleToTest => 
            {
                var modules = new List<Type>() { moduleToTest.GetInterfaces()[1] };

                var deps = moduleToTest.GetCustomAttribute<DependsOnModulesAttribute>();

                if (deps != null)
                {
                    modules.AddRange(deps.dependencies);
                }



                var folder = EditorUtility.SaveFolderPanel("Choose test folder", Util.RelativeFrameworkRootFolder + "/Tests", "NewTest");

                Directory.CreateDirectory(folder + "/Resources");
                Directory.CreateDirectory(folder + "/Resources/Configs");

                string name = new DirectoryInfo(folder).Name;
                string testNamespace = "SwiftFramework.Tests." + name;
                string prefix = $"{name}__";

                var manifest = GenerateCustomManifestClass(prefix, testNamespace, modules.ToArray());

                ScriptBuilder.SaveClassToDisc(manifest, folder + "/" + prefix + "ModuleManifest.cs", true);

                var manifestClassName = manifest.Namespaces[0].Types[0].Name;

                var manifestPath = Util.ToRelativePath(folder + "/Resources/Configs/" + manifestClassName + ".asset");

                Util.CreateAssetAfterScriptReload(manifestClassName, manifestPath);

                instance.pendingTests.Add(new TestConfig()
                {
                    manifestPath = manifestPath,
                    manifestType = manifestClassName
                });

                var game = GenerateGameClass(prefix, testNamespace, modules.ToArray());

                ScriptBuilder.SaveClassToDisc(game, folder + "/" + prefix + "Game.cs", true);

                var test = GenerateTestClass(prefix, testNamespace);

                ScriptBuilder.SaveClassToDisc(test, folder + "/" + prefix + "Test.cs", true);

                EditorUtility.SetDirty(instance);
            });

            
        }

        [DidReloadScripts(1000)]
        private static void OnScriptsReload()
        {
            if (instance.pendingTests.Count == 0)
            {
                return;
            }
            AssetDatabase.Refresh();

            foreach (var t in instance.pendingTests)
            {
                BaseModuleManifest manifest = AssetDatabase.LoadAssetAtPath<BaseModuleManifest>(t.manifestPath);

                var serializedObject = new SerializedObject(manifest);

                foreach (var item in manifest.GetAllModuleLinks())
                {
                   
                    Type implementationType = null;
               
                    
                    SerializedProperty prop = serializedObject.FindProperty(item.field.Name);

                    if (prop != null)
                    {
                        Type interfaceType = item.field.GetCustomAttribute<LinkFilterAttribute>().interfaceType;
                           
                        implementationType = RuntimeModuleFactory.FindFirstModuleImplementation(interfaceType);

                        if (implementationType != null)
                        {
                            prop.FindPropertyRelative("implementationType").stringValue = implementationType.AssemblyQualifiedName;
                            serializedObject.ApplyModifiedProperties();
                        }
                    }

                    if(implementationType != null)
                    {
                        ConfigurableAttribute attr = implementationType.GetCustomAttribute<ConfigurableAttribute>();
                        if (attr != null)
                        {
                            ScriptableObject config = Util.FindScriptableObject(attr.configType);

                            if (config == null)
                            {
                                config = CreateInstance(attr.configType);
                                AssetDatabase.CreateAsset(config, Util.ToRelativePath(new DirectoryInfo(t.manifestPath).Parent.FullName) + "/" + attr.configType.Name + ".asset");
                            }
                            prop.FindPropertyRelative("configLink").FindPropertyRelative("Path").stringValue = "Tests/" + config.name;
                            serializedObject.ApplyModifiedProperties();
                        }
                    }

                    EditorUtility.SetDirty(manifest);
                } 
            }

            AssetDatabase.Refresh();

            AssetDatabase.SaveAssets();

            ModuleLinkDrawer.NotifyAboutModuleImplementationChange();

            instance.pendingTests.Clear();
        }

        public static CodeCompileUnit GenerateTestClass(string prefix, string classNamespace)
        {
            CodeCompileUnit file = new CodeCompileUnit();

            CodeNamespace namespaces = new CodeNamespace(classNamespace);

            namespaces.Imports.Add(new CodeNamespaceImport("SwiftFramework.Core"));

            namespaces.Imports.Add(new CodeNamespaceImport("UnityEngine"));

            namespaces.Imports.Add(new CodeNamespaceImport("NUnit.Framework"));

            string className = prefix + "Test";

            CodeTypeDeclaration gameClass = new CodeTypeDeclaration(className);

            CodeMemberMethod testMethod = new CodeMemberMethod();

            testMethod.Name = "Test";

            testMethod.CustomAttributes.Add(new CodeAttributeDeclaration("Test"));

            testMethod.Statements.Add(new CodeVariableDeclarationStatement(prefix + "ModuleManifest", "manifest", new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Resources"), $"Load<{prefix + "ModuleManifest"}>", new CodePrimitiveExpression("Tests/" + prefix + "ModuleManifest"))));

            testMethod.Statements.Add( new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Assert"), $"NotNull", new CodeVariableReferenceExpression("manifest")));

            CodeExpression[] createArgs =
            {
                new CodeObjectCreateExpression("DummyBoot"),
                new CodeObjectCreateExpression("DummyLogger"),
                new CodeVariableReferenceExpression("manifest"),
                new CodePrimitiveExpression(false)
            };

            testMethod.Statements.Add(new CodeVariableDeclarationStatement("IPromise", "create", new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(prefix + "Game"), $"Create", createArgs)));

            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("create"), $"Done", new CodeVariableReferenceExpression("OnAppInitialized")));

            testMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("create"), $"Catch", new CodeSnippetExpression("e => Assert.Fail(e.Message)")));

            testMethod.Attributes = MemberAttributes.Public;

            var appInitMethod = new CodeMemberMethod();

            appInitMethod.Statements.Add(new CodeSnippetExpression("Assert.IsTrue(true)"));

            appInitMethod.Name = "OnAppInitialized";

            gameClass.Members.Add(appInitMethod);

            gameClass.Members.Add(testMethod);

            namespaces.Types.Add(gameClass);

            file.Namespaces.Add(namespaces);

            return file;
        }

        public static CodeCompileUnit GenerateGameClass(string prefix, string classNamespace, params Type[] modules)
        {
            CodeCompileUnit file = new CodeCompileUnit();

            CodeNamespace namespaces = new CodeNamespace(classNamespace);

            namespaces.Imports.Add(new CodeNamespaceImport("SwiftFramework.Core"));

            namespaces.Imports.Add(new CodeNamespaceImport("UnityEngine"));

            string className = prefix + "Game";

            CodeTypeDeclaration gameClass = new CodeTypeDeclaration(className);

            gameClass.BaseTypes.Add(new CodeTypeReference($"App<{className}>"));

            foreach (var property in modules)
            {
                string moduleName = property.Name;
                var moduleField = new CodeMemberField()
                {
                    Name = moduleName[0].ToString().ToLower() + moduleName.Substring(1, moduleName.Length - 1),
                    Type = new CodeTypeReference(property),

                };

                var moduleProp = new CodeMemberProperty()
                {
                    Name = moduleName[0].ToString() + moduleName.Substring(1, moduleName.Length - 1),
                    Type = new CodeTypeReference(property),
                    
                };

                moduleProp.Attributes = MemberAttributes.Public;

                var param = new CodeArgumentReferenceExpression("ref " + moduleField.Name);
                

                moduleProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "GetCachedModule", param)));

                gameClass.Members.Add(moduleField);
                gameClass.Members.Add(moduleProp);
            }

            namespaces.Types.Add(gameClass);

            file.Namespaces.Add(namespaces);

            return file;
        }

        public static CodeCompileUnit GenerateCustomManifestClass(string prefix, string classNamespace, params Type[] modules)
        {
            CodeCompileUnit manifestFile = new CodeCompileUnit();

            CodeNamespace manifestNamespace = new CodeNamespace(classNamespace);

            manifestNamespace.Imports.Add(new CodeNamespaceImport("SwiftFramework.Core"));
            manifestNamespace.Imports.Add(new CodeNamespaceImport("UnityEngine"));

            string className = prefix + "ModuleManifest";

            CodeTypeDeclaration manifestClass = new CodeTypeDeclaration(className);

            manifestClass.BaseTypes.Add(new CodeTypeReference("BaseModuleManifest"));

            foreach (var property in modules)
            {
                string moduleName = property.Name;
                var moduleFiled = new CodeMemberField()
                {
                    Name = moduleName[0].ToString().ToLower() + moduleName.Substring(1, moduleName.Length - 1),
                    Type = new CodeTypeReference(typeof(ModuleLink)),

                };
                moduleFiled.CustomAttributes.Add(new CodeAttributeDeclaration("SerializeField"));

                var filterAttr = new CodeAttributeDeclaration(new CodeTypeReference("LinkFilter"));


                filterAttr.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(property)));

                moduleFiled.CustomAttributes.Add(filterAttr);


                manifestClass.Members.Add(moduleFiled);
                
            }

            foreach (var property in typeof(App).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (typeof(IModule).IsAssignableFrom(property.PropertyType))
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

            string projectName = classNamespace;

            createAssetAttr.Arguments.Add(new CodeAttributeArgument("menuName", new CodePrimitiveExpression($"{projectName.Replace('.','/')}/ModuleManifest")));

            createAssetAttr.Arguments.Add(new CodeAttributeArgument("fileName", new CodePrimitiveExpression(prefix + "ModuleManifest")));

            manifestClass.CustomAttributes.Add(createAssetAttr);

            manifestNamespace.Types.Add(manifestClass);

            manifestFile.Namespaces.Add(manifestNamespace);

            return manifestFile;
        }
    }
}
