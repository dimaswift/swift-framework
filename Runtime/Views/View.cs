using SwiftFramework.Core.Pooling;
using System;
using UnityEngine;

namespace SwiftFramework.Core.Views
{
    public class View : MonoBehaviour, IPooled, IView
    {
        [SerializeField] private int poolCapacity = 1;

        public int InitialPoolCapacity => poolCapacity;

        public bool Active
        {
            get
            {
                return isActive;
            }
            set
            {
                isActive = value;
                gameObject.SetActive(value);
            }
        }

        [NonSerialized] private bool isActive;

        private IPool pool;

        private Transform parent;

        public void Init(IPool pool)
        {
            this.pool = pool;
            parent = transform.parent;
            OnInit();
        }

        protected virtual void OnInit()
        {

        }

        public virtual void TakeFromPool()
        {
            Active = true;
        }

        public virtual void ReturnToPool()
        {
            transform.SetParent(parent);
            pool.Return(this);
            Active = false;
        }

        private void OnDisable()
        {
            isActive = false;
        }

        private void OnEnable()
        {
            isActive = true;
        }

        public virtual void Process(float deltaTime) { }

        public GameObject GetRoot()
        {
            return gameObject;
        }

        public void Dispose()
        {
            if (gameObject != null)
            {
                isActive = false;
                Destroy(gameObject);
            }
        }
    }
}
