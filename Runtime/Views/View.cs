using System;
using Swift.Core.Pooling;
using UnityEngine;

namespace Swift.Core.Views
{
    public class View : MonoBehaviour, IPooled, IView
    {
        [SerializeField] private int poolCapacity = 1;

        public int InitialPoolCapacity => poolCapacity;

        public bool Active
        {
            get => isActive;
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

        protected virtual void OnDisable()
        {
            isActive = false;
        }

        protected virtual void OnEnable()
        {
            isActive = true;
        }

        public virtual void Process(float deltaTime) { }

        public GameObject GetRoot()
        {
            return gameObject;
        }

        protected virtual void OnBeforeDestroy() {}
        
        public void Dispose()
        {
            if (gameObject != null)
            {
                isActive = false;
                pool.Dispose(this);
                OnBeforeDestroy();
                Destroy(gameObject);
            }
        }
    }
}
