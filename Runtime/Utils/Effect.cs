using System.Collections;
using System.Collections.Generic;
using Swift.Core;
using Swift.Core.Pooling;
using UnityEngine;

namespace Swift.Utils
{
    public class Effect : MonoBehaviour
    {
        [SerializeField] private int capacity = 5;
        [SerializeField] private ParticleSystem fx = null;

        private BehaviourPool<ParticleSystem> pool;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (pool == null)
            {
                pool = new BehaviourPool<ParticleSystem>(CreateInstance);
                pool.WarmUp(capacity);
            }
        }

        public void Play(Vector3 point)
        {
            Init();
            var effect = pool.Take();
            effect.transform.position = point;
            effect.Play();
            App.Core.Timer.WaitForUnscaled(effect.main.duration).Done(() => pool.Return(effect));
        }



        private ParticleSystem CreateInstance()
        {
            var instance = Instantiate(fx);
            instance.transform.SetParent(transform);
            return instance;
        }
    }
}
