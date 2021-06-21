using UnityEngine;

namespace Swift.Core
{
    [System.Serializable]
    public class StateSwitch<T>
    {
        [SerializeField] private State[] states = { };

        [System.Serializable]
        public class State : State<T>
        {

        }

        [System.Serializable]
        public class State<T1>
        {
            public T1 state;
            public GameObject[] activeObjects;
        }

        public void SetUp(T current)
        {
            foreach (var s in states)
            {
                foreach (var o in s.activeObjects)
                {
                    o.gameObject.SetActive(false);
                }
            }

            foreach (var s in states)
            {
                if (current.Equals(s.state))
                {
                    foreach (var o in s.activeObjects)
                    {
                        o.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}
