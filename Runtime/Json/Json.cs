using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SwiftFramework.Core
{
    public static class Json
    {
        private class CustomContractResolver : DefaultContractResolver
        {
            protected override JsonObjectContract CreateObjectContract(Type objectType)
            {
                if (typeof(ScriptableObject).IsAssignableFrom(objectType))
                {
                    var contract = base.CreateObjectContract(objectType);

                    ScriptableObject Create()
                    {
                        var instance = ScriptableObject.CreateInstance(objectType);
                        instance.hideFlags = HideFlags.DontSave;
                        return instance;
                    }

                    contract.DefaultCreator = () => Create();

                    return contract;
                }

                return base.CreateObjectContract(objectType);
                
            }


            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                List<JsonProperty> jsonProps = new List<JsonProperty>();

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field.IsPublic == false && field.GetCustomAttribute<SerializeField>() == null)
                    {
                        continue;
                    }
                    jsonProps.Add(CreateProperty(field, memberSerialization));
                }

                jsonProps.ForEach(p => { p.Writable = true; p.Readable = true; });

                return jsonProps;
            }
        }

        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
            ContractResolver = new CustomContractResolver(),
            Error = HandleDeserializationError,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string Compress(string json)
        {
            var bytes = Encoding.Unicode.GetBytes(json);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }
                return Convert.ToBase64String(mso.ToArray());
            }
        }

        public static string Decompress(string json)
        {
            var bytes = Convert.FromBase64String(json);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return Encoding.Unicode.GetString(mso.ToArray());
            }
        }


        private static void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            Debug.LogWarning($"Json deserialization error: {errorArgs.ErrorContext.Error.Message}");
            errorArgs.ErrorContext.Handled = true;
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }

        public static void Pupulate(object obj, string json)
        {
            JsonConvert.PopulateObject(json, obj, serializerSettings);
        }

        public static string Serialize<T>(T data, Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(data, formatting, serializerSettings);
        }

    }

}
