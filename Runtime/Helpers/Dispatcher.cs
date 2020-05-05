using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [ExecuteInEditMode]
	public class Dispatcher : MonoBehaviour
    {
        public static bool IsOnMainThread
        {
            get
            {
                if (instance == null)
                {
                    return false;
                }

                return mainThread == Thread.CurrentThread;
            }
        }

        private static Thread mainThread;

        private static Dispatcher instance;
        private static volatile bool queued = false;
        private static List<Action> backlog = new List<Action>(8);
        private static List<Action> actions = new List<Action>(8);

        public static void RunAsync(Action action) 
		{
			ThreadPool.QueueUserWorkItem(o => action());
		}
 
		public static void RunAsync(Action<object> action, object state) 
		{
			ThreadPool.QueueUserWorkItem(o => action(o), state);
		}
 
		public static void RunOnMainThread(Action action)
		{
            lock (backlog) 
			{
				backlog.Add(action);
				queued = true;
			}
		}
 
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			if(instance == null)
			{
               
                instance = new GameObject(nameof(Dispatcher)).AddComponent<Dispatcher>();
                DontDestroyOnLoad(instance.gameObject);
            }
        }

        static Dispatcher()
        {
#if UNITY_EDITOR

            UnityEditor.SceneView.duringSceneGui += s => Process();

#endif
        }

        private static void Process()
        {
            if (mainThread == null)
            {
                mainThread = Thread.CurrentThread;
            }
            if (queued)
            {
                lock (backlog)
                {
                    var tmp = actions;
                    actions = backlog;
                    backlog = tmp;
                    queued = false;
                }

                foreach (var action in actions)
                    action();

                actions.Clear();
            }
        }

        private void Update()
		{
            Process();

        }
	}
}
