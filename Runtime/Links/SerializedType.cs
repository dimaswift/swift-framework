using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [Serializable]
    public struct SerializedType : ISerializationCallbackReceiver
    {
        [SerializeField] private string type;

        private Type CachedType { get; set; }

        public bool IsDefined => Type != null;
        
        public Type Type
        {
            get
            {
                if (string.IsNullOrEmpty(type))
                {
                    return null;
                }
                if (CachedType != null)
                {
                    return CachedType;
                }
                CachedType = Type.GetType(type);
                return CachedType;
            }
            set
            {
                if (value == null)
                {
                    type = null;
                }
                else
                {
                    type = value.AssemblyQualifiedName;
                }
                CachedType = value;
            }
        }

        public override int GetHashCode()
        {
            return type == null ? 0 : type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ((SerializedType) obj).type == type;
        }

        public static bool operator ==(SerializedType a, SerializedType b)
        {
            return a.type == b.type;
        }
        
        public static bool operator !=(SerializedType a, SerializedType b)
        {
            return a.type != b.type;
        }

        public SerializedType(Type type)
        {
            CachedType = type;
            this.type = type != null ? type.AssemblyQualifiedName : null;
        }

        public SerializedType(string type)
        {
            CachedType = Type.GetType(type);
            this.type = type;
        }
        
        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            CachedType = string.IsNullOrEmpty(type) ? null : Type.GetType(type);
        }
    }
}