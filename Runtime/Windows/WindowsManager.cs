using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Reflection;
using SwiftFramework.Core;

namespace SwiftFramework.Core.Windows
{
    public class WindowsManager : BehaviourModule, IWindowsManager
    {
        [SerializeField] private AppearAnimationHandler topBarAnimation = null;
        [SerializeField] [CheckInterface(typeof(ITopBar))] private GameObject topBarObject = null;
        [SerializeField] private CanvasGroup transitionPanel = null;
        [SerializeField] private float transitionDuration = .25f;
        [SerializeField] private float animationDuration = .5f;

        private readonly Stack<Window> windowStack = new Stack<Window>();

        private readonly Dictionary<Type, Window> singletonWindowInstances = new Dictionary<Type, Window>();
        private readonly Dictionary<WindowLink, Window> windowInstances = new Dictionary<WindowLink, Window>();

        private readonly List<Window> windowBuffer = new List<Window>();

        private readonly List<Window> windowShowBuffer = new List<Window>();

        private readonly List<Window> animationShowWindowBuffer = new List<Window>();

        private readonly List<Window> animationHideWindowBuffer = new List<Window>();

        private Coroutine showRoutine;

        private Coroutine hideRoutine;

        private ITopBar topBar = null;

        private readonly Queue<Action> actionQueue = new Queue<Action>();

        private Vector3 topBarStartPos;

        private Transform topBarParent;

        private readonly List<RootCanvas> canvases = new List<RootCanvas>();

        public event Action<IWindow> OnWindowShown = w => { };
        public event Action<IWindow> OnWindowWillBeShown = w => { };
        public event Action<IWindow> OnWindowHidden = w => { };
        public event Action<IWindow> OnWindowWillBeHidden = w => { };
        public event Action<IWindow> OnWindowJustEnabled = w => { };

        public bool IsInTransition { get; private set; }
        
        private readonly LinkedList<Promise> currentChainedWindowQueue = new LinkedList<Promise>();

        public bool IsShowingAnimation()
        {
            return showRoutine != null || hideRoutine != null;
        }

        public IWindow GetBottomWindow()
        {
            int i = 0;
            foreach (var window in windowStack)
            {
                if (i == windowStack.Count - 1)
                {
                    return window;
                }
                i++;
            }
            return default;
        }

        public IWindow GetTopWindow()
        {
            return windowStack.Count > 0 ? windowStack.Peek() : default;
        }

        protected override IPromise GetInitPromise()
        {
            GetComponentsInChildren(canvases);
            foreach (RootCanvas rootCanvas in canvases)
            {
                rootCanvas.ApplySafeArea();
            }

            if (topBarObject != null)
            {
                topBar = topBarObject.GetComponent<ITopBar>();
                if (topBar != null)
                {
                    topBar.Init();
                    topBarStartPos = topBar.RectTransform.localPosition;
                    topBarParent = topBar.RectTransform.parent;
                    topBar.IsShown = false;
                }
            }
            
            Promise promise = Promise.Create();

            foreach (Window winPrefab in AssetCache.GetPrefabs<Window>())
            {
                if (winPrefab.GetType().GetCustomAttribute<WarmUpInstanceAttribute>() != null)
                {
                    InstantiateWindow(winPrefab);
                }
            }

            promise.Resolve();


            return promise;
        }

        private void InstantiateWindow(Window windowPrefab, WindowLink windowLink = null)
        {
            Type type = windowPrefab.GetType();
            
            if (windowLink != null && windowInstances.ContainsKey(windowLink))
            {
                Debug.LogError($"Ignoring window {windowPrefab.name} of type {type.Name}. Already created window with link: {windowLink.GetPath()}!");
                return;
            }
            else if (singletonWindowInstances.ContainsKey(type))
            {
                Debug.LogError($"Ignoring window {windowPrefab.name} of type {type.Name}. There can only be one singleton window type at a time!");
                return;
            }

            Window window = Instantiate(windowPrefab);
            window.name = windowPrefab.name;

            try
            {
                window.Init(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Window <b>{window.name}</b> not initialized! Exception was thrown. Ignoring...");
                window.gameObject.SetActive(false);
                return;
            }

            RootCanvas rootCanvas = FindCanvas(window.CanvasType);

            window.transform.SetParent(rootCanvas.RectTransform);
            window.transform.localScale = Vector3.one;
            window.RectTransform.anchoredPosition = windowPrefab.RectTransform.anchoredPosition;
            window.RectTransform.sizeDelta = windowPrefab.RectTransform.sizeDelta;
            if (window.IsFullScreen)
            {
                window.RectTransform.anchoredPosition = new Vector2(0, 0);
                window.RectTransform.sizeDelta = new Vector2(0, 0);
                window.RectTransform.anchorMax = new Vector2(1, 1);
                window.RectTransform.anchorMin = new Vector2(0, 0);
            }
            window.SetHidden();

            if (windowLink != null)
            {
                windowInstances.Add(windowLink, window);
            }
            else
            {
                singletonWindowInstances.Add(window.GetType(), window);
            }

            Core.App.WaitForState(AppState.ModulesInitialized, () => window.WarmUp());
            
        }

        public IEnumerable<IWindow> GetWindowStack()
        {
            foreach (var window in windowStack)
            {
                yield return window;
            }
        }

        public void HideTopFullScreenWindow()
        {
            actionQueue.Enqueue(() =>
            {
                windowBuffer.Clear();

                while (windowStack.Count > 0 && windowStack.Peek().IsFullScreen == false)
                {
                    windowBuffer.Add(windowStack.Pop());
                }

                if (windowStack.Count == 0)
                {
                    Hide(windowBuffer);
                    return;
                }

                Window topFullScreenWin = windowStack.Pop();
                windowBuffer.Add(topFullScreenWin);

                if (windowStack.Count == 0)
                {
                    Hide(windowBuffer);
                    return;
                }

                Hide(windowBuffer);

                windowBuffer.Clear();

                while (windowStack.Count > 0 && windowStack.Peek().IsFullScreen == false)
                {
                    windowBuffer.Add(windowStack.Pop());
                }

                if (windowStack.Count > 0)
                {
                    var win = windowStack.Pop();
                    windowBuffer.Add(win);
                }

                windowBuffer.Reverse();

                foreach (var win in windowBuffer)
                {
                    windowStack.Push(win);
                }

                Show(windowBuffer);
            });

            ProcessActionQueue();

        }

        public IPromise<IWindow> Show(WindowLink windowLink)
        {
            Promise<IWindow> promise = Promise<IWindow>.Create();

            GetWindow<IWindow>(windowLink).Then(win =>
            {
                Show(win as Window);
                promise.Resolve(win);
            })
            .Catch(e => promise.Reject(e));

            return promise;
        }

        public IPromise<T> Show<T>(WindowLink windowLink = null) where T : IWindow
        {
            Promise<T> promise = Promise<T>.Create();

            GetWindow<T>(windowLink).Then(win =>
            {
                Show(win as Window);
                promise.Resolve(win);
            })
            .Catch(e => promise.Reject(e));

            return promise;
        }

        public IPromise<T> ShowChained<T>(WindowLink windowLink = null) where T : IWindow
        {
            return ShowChaninedWindow<T>(windowLink, w => { });
        }

        public IPromise<T> ShowChained<T, A>(A args, WindowLink windowLink = null) where T : IWindowWithArgs<A>
        {
            return ShowChaninedWindow<T>(windowLink, w => w.SetArgs(args));
        }

        private IPromise<T> ShowChaninedWindow<T>(WindowLink windowLink, Action<T> onShow) where T : IWindow
        {
            Promise<T> promise = Promise<T>.Create();

            if (currentChainedWindowQueue.Count > 0)
            {
                IPromise lastHidePromise = currentChainedWindowQueue.Last.Value;

                Promise nextHidePromise = Promise.Create();

                lastHidePromise.Done(() =>
                {
                    DoShow().Channel(nextHidePromise);
                });

                currentChainedWindowQueue.AddLast(nextHidePromise);
            }
            else
            {
                currentChainedWindowQueue.AddLast(DoShow());
            }

            Promise DoShow()
            {
                Promise hidePromise = Promise.Create();

                GetWindow<T>(windowLink).Then(win =>
                {
                    onShow(win);
                    Show(win as Window);
                    promise.Resolve(win);

                    win.HidePromise.Done(() =>
                    {
                        hidePromise.Resolve();
                        currentChainedWindowQueue.Remove(hidePromise);
                    });
                })
                .Catch(e => promise.Reject(e));
                return hidePromise;
            }

            return promise;
        }

        public IPromise<R> Show<T, R>(WindowLink windowLink = null) where T : IWindowWithResult<R>
        {
            Promise<R> promise = Promise<R>.Create();

            GetWindow<T>(windowLink).Then(win =>
            {
                win.CreateNewResultPromise();
                Show(win as Window);
                win.Result.Channel(promise);
            })
            .Catch(e => promise.Reject(e));

            return promise;
        }

        public IPromise<T> Show<T, A>(A args, WindowLink windowLink = null) where T : IWindowWithArgs<A>
        {
            Promise<T> promise = Promise<T>.Create();

            GetWindow<T>(windowLink).Then(win =>
            {
                win.SetArgs(args);
                Show(win as Window);
                promise.Resolve(win);
            })
            .Catch(e => promise.Reject(e));

            return promise;
        }

        public IPromise<R> Show<T, A, R>(A args, WindowLink windowLink = null) where T : IWindowWithArgsAndResult<A, R>
        {
            Promise<R> promise = Promise<R>.Create();

            GetWindow<T>(windowLink).Then(win =>
            {
                win.CreateNewResultPromise();
                win.SetArgs(args);
                Show(win as Window);
                win.Result.Channel(promise);
            })
            .Catch(e => promise.Reject(e));

            return promise;
        }

        public void Show(Window window)
        {
            if (window == null)
            {
                Debug.LogError("Cannot show window! Argument is null");
                return;
            }
            if (window.IsFullScreen)
            {
                actionQueue.Enqueue(() =>
                {
                    ShowFullScreen(window);
                });
            }
            else
            {
                actionQueue.Enqueue(() =>
                {
                    ShowSingle(window);
                });
            }
            ProcessActionQueue();
        }

        public void HideAll()
        {
            actionQueue.Enqueue(() =>
            {
                windowBuffer.Clear();

                foreach (var win in singletonWindowInstances)
                {
                    if (win.Value.IsShown)
                    {
                        windowBuffer.Add(win.Value);
                    }
                }
                windowStack.Clear();

                Hide(windowBuffer);
            });

            ProcessActionQueue();
        }

        public IPromise SetStack(IEnumerable<IWindow> windows)
        {
            Promise promise = Promise.Create();

            actionQueue.Enqueue(() =>
            {
                windowStack.Clear();
                foreach (var win in windows)
                {
                    windowStack.Push(win as Window);
                }
                ShowCurrentStack();
                promise.Resolve();
            });

            ProcessActionQueue();
            return promise;
        }

        public IPromise SetStack<W1>() where W1 : IWindow
        {
            Promise promise = Promise.Create();

            actionQueue.Enqueue(() =>
            {
                GetWindow<W1>().Done(w =>
                {
                    windowStack.Clear();
                    windowStack.Push(w as Window);
                    ShowCurrentStack();
                    promise.Resolve();
                });
            });

            ProcessActionQueue();

            return promise;
        }

        public IPromise SetStack<W1, W2>()
            where W1 : IWindow
            where W2 : IWindow
        {
            Promise promise = Promise.Create();

            actionQueue.Enqueue(() =>
            {
                W1 w1 = default;
                W2 w2 = default;
                var p1 = GetWindow<W1>().Then(w =>
                {
                    w1 = w;
                });
                var p2 = GetWindow<W2>().Then(w =>
                {
                    w2 = w;
                });
                Promise.All(p1, p2).Then(() =>
                {
                    windowStack.Clear();

                    windowStack.Push(w1 as Window);
                    windowStack.Push(w2 as Window);

                    ShowCurrentStack();

                    promise.Resolve();
                })
                .Catch(e => promise.Reject(e));
            });

            ProcessActionQueue();

            return promise;
        }

        public IPromise SetStack<W1, W2, W3>()
            where W1 : IWindow
            where W2 : IWindow
            where W3 : IWindow
        {
            Promise promise = Promise.Create();

            actionQueue.Enqueue(() =>
            {
                W1 w1 = default;
                W2 w2 = default;
                W3 w3 = default;
                var p1 = GetWindow<W1>().Then(w =>
                {
                    w1 = w;
                });
                var p2 = GetWindow<W2>().Then(w =>
                {
                    w2 = w;
                });
                var p3 = GetWindow<W3>().Then(w =>
                {
                    w3 = w;
                });
                Promise.All(p1, p2, p3).Then(() =>
                {
                    windowStack.Clear();

                    windowStack.Push(w1 as Window);
                    windowStack.Push(w2 as Window);
                    windowStack.Push(w3 as Window);

                    ShowCurrentStack();

                    promise.Resolve();
                })
                .Catch(e => promise.Reject(e));
            });

            ProcessActionQueue();

            return promise;
        }

        public IPromise SetStack<W1, W2, W3, W4>()
            where W1 : IWindow
            where W2 : IWindow
            where W3 : IWindow
            where W4 : IWindow
        {
            Promise promise = Promise.Create();

            actionQueue.Enqueue(() =>
            {
                W1 w1 = default;
                W2 w2 = default;
                W3 w3 = default;
                W4 w4 = default;
                var p1 = GetWindow<W1>().Then(w =>
                {
                    w1 = w;
                });
                var p2 = GetWindow<W2>().Then(w =>
                {
                    w2 = w;
                });
                var p3 = GetWindow<W3>().Then(w =>
                {
                    w3 = w;
                });
                var p4 = GetWindow<W4>().Then(w =>
                {
                    w4 = w;
                });
                Promise.All(p1, p2, p3, p4).Then(() =>
                {
                    windowStack.Clear();

                    windowStack.Push(w1 as Window);
                    windowStack.Push(w2 as Window);
                    windowStack.Push(w3 as Window);
                    windowStack.Push(w4 as Window);

                    ShowCurrentStack();

                    promise.Resolve();
                })
                .Catch(e => promise.Reject(e));
            });

            ProcessActionQueue();

            return promise;
        }

        public void Hide<T>(WindowLink windowLink = null) where T : IWindow
        {
            GetWindow<T>(windowLink).Done(win => Hide(win as Window));
        }

        public void Hide(Window window)
        {
            if (window.IsFullScreen)
            {
                bool isOnTop = false;
                foreach (Window w in windowStack)
                {
                    if (w.IsFullScreen)
                    {
                        isOnTop = w == window;
                        break;
                    }
                }
                if (window.IsShown && isOnTop)
                {
                    HideTopFullScreenWindow();
                }
            }
            else
            {
                HidePopUp(window);
            }
        }

        private RootCanvas FindCanvas(CanvasType type)
        {
            foreach (RootCanvas canvas in canvases)
            {
                if (canvas.Type == type)
                {
                    return canvas;
                }
            }

            RootCanvas rootCanvas = new GameObject(type.ToString() + "s", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).AddComponent<RootCanvas>();
            rootCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.Type = type;
            canvases.Add(rootCanvas);
            Debug.LogWarning($"Canvas root of type {rootCanvas.Type} not found. Creating default one");
            rootCanvas.transform.SetParent(transform);
            rootCanvas.transform.localPosition = Vector3.zero;
            return rootCanvas;
        }

        private void StartShowAnimation(IList<Window> windows)
        {
            if (windows.Count == 0)
            {
                return;
            }
            showRoutine = StartCoroutine(AnimationRoutine(true, windows));
        }

        private void StartHideAnimation(IList<Window> windows)
        {
            if (windows.Count == 0)
            {
                return;
            }
            hideRoutine = StartCoroutine(AnimationRoutine(false, windows));
        }

        private void Show(IList<Window> windows)
        {
            StartShowAnimation(windows);
        }

        private void Hide(IList<Window> windows)
        {
            StartHideAnimation(windows);
        }

        private void HideSingle(Window window)
        {
            actionQueue.Enqueue(() =>
            {
                windowBuffer.Clear();
                windowBuffer.Add(window);
                Hide(windowBuffer);
            });
        }

        IEnumerator AnimationRoutine(bool show, IList<Window> windows)
        {
            if (windows.Count == 0)
            {
                yield break;
            }

            if (show)
            {
                animationShowWindowBuffer.Clear();
                animationShowWindowBuffer.AddRange(windows);
                windows = animationShowWindowBuffer;
            }
            else
            {
                animationHideWindowBuffer.Clear();
                animationHideWindowBuffer.AddRange(windows);
                windows = animationHideWindowBuffer;
            }

            bool topBarProcessed = false;
            bool topBarShown = false;
            bool topBarHidden = false;

            foreach (Window win in windows)
            {
                if (show)
                {
                    OnWindowWillBeShown(win);
                    win.SetShown();
                    win.OnStartShowing();
                    if (win.IsFullScreen && topBarProcessed == false && topBar != null)
                    {
                        if (win.ShowTopBar && topBar.IsShown == false)
                        {
                            topBarProcessed = true;
                            topBarShown = true;
                            topBar.IsShown = true;
                        }
                        else if (win.ShowTopBar == false && topBar.IsShown)
                        {
                            topBarProcessed = true;
                            topBarHidden = true;
                        }
                    }
                    OnWindowJustEnabled(win);
                }
                else
                {
                    OnWindowWillBeHidden(win);
                    win.OnStartHiding();
                }
            }

            float time = 0;
            float duration = animationDuration;

            while (time < duration)
            {
                if (topBarAnimation.HasValue)
                {
                    if (topBarShown)
                    {
                        topBarAnimation.Value.ProcessShowing(time / duration);
                    }
                    if (topBarHidden)
                    {
                        topBarAnimation.Value.ProcessHiding(time / duration);
                    }
                }

                foreach (Window win in windows)
                {
                    if (win.Animation != null)
                    {
                        if (show)
                        {
                            win.Animation.ProcessShowing(time / duration);
                        }
                        else
                        {
                            win.Animation.ProcessHiding(time / duration);
                        }
                    }
                }

                time += Time.unscaledDeltaTime;

                if (time > duration)
                {
                    break;
                }

                yield return null;

            }

            foreach (Window win in windows)
            {
                if (win.Animation != null)
                {
                    if (show)
                    {
                        win.Animation.ProcessShowing(1);
                    }
                    else
                    {
                        win.Animation.ProcessHiding(1);
                    }
                }
            }

            topBarProcessed = false;

            foreach (var win in windows)
            {
                if (show)
                {
                    win.RectTransform.SetAsLastSibling();
                    win.OnShown();
                    OnWindowShown(win);
                    if (topBarShown)
                    {
                        topBarShown = false;
                        ResetTopBarPos();
                    }

                    if (topBarHidden)
                    {
                        topBarHidden = false;
                        SetTopBarShown(false);
                        ResetTopBarPos();
                    }
                }
                else
                {
                    win.SetHidden();
                    win.OnHidden();
                    OnWindowHidden(win);
                }
            }

            if (show)
            {
                showRoutine = null;
            }
            else
            {
                hideRoutine = null;
            }
        }

        private void ResetTopBarPos()
        {
            if (topBar != null)
            {
                topBar.RectTransform.SetParent(topBarParent);
                topBar.RectTransform.localScale = Vector3.one;
                topBar.RectTransform.localPosition = topBarStartPos;
            }
        }

        private void ProcessActionQueue()
        {
            if (IsShowingAnimation() == false && actionQueue.Count > 0)
            {
                actionQueue.Dequeue().Invoke();
            }
        }

        private void ShowSingle(Window window)
        {
            if (window.IsShown)
            {
                return;
            }

            windowBuffer.Clear();
            windowBuffer.Add(window);
            windowStack.Push(window);
            Show(windowBuffer);
        }

        public void OnBackClick(Window window)
        {
            if (IsShowingAnimation())
            {
                return;
            }

            if (window.IsFullScreen)
            {
                HideTopFullScreenWindow();
            }
            else
            {
                HidePopUp(window);
                ProcessActionQueue();
            }
        }

        private void ShowFullScreen(Window window)
        {
            if (window.IsShown)
            {
                return;
            }

            windowBuffer.Clear();

            while (windowStack.Count > 0 && windowStack.Peek().IsFullScreen == false)
            {
                windowBuffer.Add(windowStack.Pop());
            }

            if (windowStack.Count > 0)
            {
                windowBuffer.Add(windowStack.Peek());
            }

            Hide(windowBuffer);

            ShowSingle(window);
        }

        public IPromise<T> GetWindow<T>(WindowLink windowLink = null) where T : IWindow
        {
            Promise<T> promise = Promise<T>.Create();

            IWindow win;

            if (windowLink == null)
            {
                Type type = typeof(T);

                if (singletonWindowInstances.ContainsKey(type) == false)
                {
                    AssetCache.LoadSingletonPrefab<T>().Then(prefab =>
                    {
                        InstantiateWindow(prefab as Window);
                        GetWindow<T>().Channel(promise);
                    })
                    .Catch(e =>
                    {
                        Debug.LogError($"<b>Cannot load singleton Window of type {typeof(T).Name}. Add [AddrSingleton] attribute to it in order to load it using its type.</b>");
                        promise.Reject(e);
                    });

                    return promise;
                }

                win = singletonWindowInstances[type];
            }
            else
            {
                if (windowInstances.ContainsKey(windowLink) == false)
                {
                    windowLink.Load().Then(prefab =>
                    {
                        InstantiateWindow(prefab as Window, windowLink);
                        GetWindow<T>(windowLink).Channel(promise);
                    })
                    .Catch(e => promise.Reject(e));

                    return promise;
                }
                win = windowInstances[windowLink];
            }

            if (win is T == false)
            {
                promise.Reject(new InvalidCastException($"Trying to cast window of type {win.GetType().Name} to type {typeof(T).Name}"));
            }
            else
            {
                promise.Resolve((T)win);
            }

            return promise;
        }

        private void HidePopUp(Window window)
        {
            actionQueue.Enqueue(() =>
            {
                if (window.IsShown == false)
                {
                    return;
                }
                windowBuffer.Clear();

                windowBuffer.AddRange(windowStack);

                windowShowBuffer.Clear();
                windowShowBuffer.Add(window);

                Hide(windowShowBuffer);

                windowBuffer.Remove(window);

                windowStack.Clear();

                windowBuffer.Reverse();

                foreach (var win in windowBuffer)
                {
                    windowStack.Push(win);
                }
            });
        }

        private void ShowCurrentStack()
        {
            if (windowStack.Count == 0)
            {
                return;
            }
            windowShowBuffer.Clear();
            windowBuffer.Clear();
            windowShowBuffer.AddRange(windowStack);

            for (int i = 0; i < windowShowBuffer.Count; i++)
            {
                var win = windowShowBuffer[i];
                windowBuffer.Add(win);
                if (win.IsFullScreen)
                {
                    break;
                }
            }

            windowBuffer.Reverse();

            Show(windowBuffer);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (windowStack.Count > 0)
                {
                    if (windowStack.Peek().IsFullScreen)
                    {
                        if (windowStack.Peek().CanBeClosed)
                        {
                            windowStack.Peek().HandleCloseButtonClick();
                        }
                    }
                    else
                    {
                        if (windowStack.Peek().CanBeClosed)
                        {
                            windowStack.Pop().HandleCloseButtonClick();
                        }
                    }
                }
            }
            ProcessActionQueue();
        }

        public void SetTopBarShown(bool shown)
        {
            if (topBar != null)
            {
                topBar.IsShown = shown;
            }
        }

        public IRootCanvas GetRootCanvas(CanvasType type)
        {
            return FindCanvas(type);
        }

        public IPromise SetStack(params WindowLink[] windows)
        {
            Promise promise = Promise.Create();

            List<IPromise> promises = new List<IPromise>();
            List<IWindow> windowInstances = new List<IWindow>();
            actionQueue.Enqueue(() =>
            {
                foreach (var w in windows)
                {
                    promises.Add(GetWindow<IWindow>(w).Then(_w => windowInstances.Add(_w)));
                }
                Promise.All(promises).Then(() =>
                {
                    windowStack.Clear();
                    foreach (var win in windowInstances)
                    {
                        windowStack.Push(win as Window);
                    }
                    ShowCurrentStack();
                })
                .Catch(e => promise.Reject(e));

            });

            ProcessActionQueue();

            return promise;
        }

        public void Hide(WindowLink link)
        {
            GetWindow<IWindow>(link).Done(win => Hide(win as Window));
        }

        public IPromise MakeTransition(IPromise promiseToWait, Action action)
        {
            Promise promise = Promise.Create();

            if (IsInTransition)
            {
                promise.Reject(new InvalidOperationException("Already in transition!"));
                return promise;
            }

            StartCoroutine(TransitionRoutine(promiseToWait, action, promise));

            return promise;
        }

        private IEnumerator TransitionRoutine(IPromise promiseToWait, Action action, Promise promise)
        {
            IsInTransition = true;

            transitionPanel.gameObject.SetActive(true);
            float a = 0f;

            while (a < 1)
            {
                transitionPanel.alpha = a;
                a += Time.unscaledDeltaTime / transitionDuration;
                yield return null;
            }

            transitionPanel.alpha = 1;
            action();

            yield return new WaitForSecondsRealtime(transitionDuration);

            while (promiseToWait.CurrentState == PromiseState.Pending)
            {
                yield return null;
            }

            while (a > 0)
            {
                transitionPanel.alpha = a;
                a -= Time.unscaledDeltaTime / transitionDuration;
                yield return null;
            }
            transitionPanel.gameObject.SetActive(false);
            IsInTransition = false;
            
            promise.Resolve();
        }

        public ITopBar GetTopBar()
        {
            return topBar;
        }


    }
}
