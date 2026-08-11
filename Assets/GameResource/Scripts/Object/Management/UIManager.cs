using System;
using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.UI;
using Backend.Util.Input;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Backend.Object.Management
{
    /// <summary>
    /// UI 의 생성/오픈/닫기/뒤로가기 스택을 통합 관리한다.
    /// - Open / Close: 풀이 이미 만들어진 UI 의 동기 오픈/닫기
    /// - OpenAsync (동적 오픈): Addressable 로 풀을 만들고 첫 인스턴스 반환
    /// - PreloadAsync: Addressable 로 풀만 미리 생성 (오픈하지 않음)
    /// - ShowLoadingAsync / HideLoading: 씬 전환용 LoadingPanel
    /// - CloseDynamic (동적 닫기): 닫음과 동시에 해당 UI 의 풀 자체를 해제
    /// - PopBack: 모바일 뒤로가기 / PC ESC. PuzzleControl.UI.Cancel 액션으로 직접 구독.
    /// </summary>
    public class UIManager : SingletonGameObject<UIManager>
    {
        private readonly struct UILifecycle
        {
            public readonly Action Release;
            public readonly Action ReleasePool;

            public UILifecycle(Action release, Action releasePool)
            {
                Release = release;
                ReleasePool = releasePool;
            }
        }

        [Header("Refs")]
        [SerializeField] private UIRegistry _registry;

        private UniTaskCompletionSource _registryReady;

        private readonly Dictionary<Type, UIBase> _active = new();
        private readonly Dictionary<UIBase, UILifecycle> _lifecycles = new();
        private readonly Stack<UIBase> _backStack = new();
        private readonly Subject<Unit> _onBackEmpty = new();

        private GameObject _blockerRoot;
        private PuzzleControl _puzzleControl;
        private Action<InputAction.CallbackContext> _onCancelPerformed;

        /// <summary> 백 스택이 비어있을 때 뒤로가기 입력이 들어오면 발행되는 이벤트. </summary>
        public static Observable<Unit> OnBackEmpty => Instance._onBackEmpty;

        protected override void OnAwake()
        {
            base.OnAwake();

            _registryReady = new UniTaskCompletionSource();

            _puzzleControl = new PuzzleControl();
            _onCancelPerformed = _ => PopBack_Internal();
            _puzzleControl.UI.Cancel.performed += _onCancelPerformed;
            _puzzleControl.UI.Enable();

            if (_registry != null)
            {
                _registryReady.TrySetResult();
                PreloadAsync_Internal<LoadingPanel>(preloadCount: 1).Forget();
            }
            else
            {
                InitRegistryAsync().Forget();
            }
        }

        private async UniTaskVoid InitRegistryAsync()
        {
            var prefab = await ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.UI.Get("UIRoot"));
            if (prefab == null)
            {
                Debug.LogError("[UIManager] UIRoot 프리팹 로드 실패. _registry 없이 동작합니다.");
                _registryReady.TrySetResult();
                return;
            }

            var go = Instantiate(prefab);
            DontDestroyOnLoad(go);
            _registry = go.GetComponent<UIRegistry>();

            if (_registry == null)
                Debug.LogError("[UIManager] UIRoot 프리팹에 UIRegistry 컴포넌트가 없습니다.");

            _registryReady.TrySetResult();

            // 씬 전환 직전에 Addressable 로드가 걸리지 않도록 LoadingPanel 풀을 미리 데운다.
            await PreloadAsync_Internal<LoadingPanel>(preloadCount: 1);
        }

        private void OnDestroy()
        {
            if (_puzzleControl != null)
            {
                _puzzleControl.UI.Cancel.performed -= _onCancelPerformed;
                _puzzleControl.UI.Disable();
                _puzzleControl.Dispose();
                _puzzleControl = null;
            }
            _onBackEmpty?.Dispose();
        }

        #region Open Internal

        /// <summary>
        /// 풀이 이미 만들어져 있는 UI 를 동기로 오픈한다.
        /// </summary>
        private T Open_Internal<T>() where T : UIBase
        {
            if (_active.TryGetValue(typeof(T), out var existing) && existing != null)
                return (T)existing;

            if (!ObjectPoolManager.HasPool<T>())
            {
                Debug.LogError($"[UIManager] Pool for {typeof(T).Name} not created. Use OpenAsync first.");
                return null;
            }

            var ui = ObjectPoolManager.Get<T>();
            if (ui == null)
            {
                Debug.LogError($"[UIManager] Failed to get {typeof(T).Name} from pool.");
                return null;
            }

            RegisterLifecycle(ui, ui);
            Activate(ui);
            return ui;
        }

        /// <summary>
        /// Addressable 에서 비동기 로드하여 풀을 생성한 뒤 첫 인스턴스를 오픈한다.
        /// addressableKey 가 null 이면 AddressableKeys.UI.Get&lt;T&gt;() 를 사용.
        /// </summary>
        private async UniTask<T> OpenAsync_Internal<T>(string addressableKey) where T : UIBase
        {
            await PreloadAsync_Internal<T>(addressableKey);
            if (!ObjectPoolManager.HasPool<T>())
                return null;

            return Open_Internal<T>();
        }

        /// <summary>
        /// Addressable 로 UI 풀만 미리 생성한다. 인스턴스는 오픈하지 않는다.
        /// </summary>
        private async UniTask PreloadAsync_Internal<T>(string addressableKey = null, int preloadCount = 1) where T : UIBase
        {
            await _registryReady.Task;

            if (ObjectPoolManager.HasPool<T>())
                return;

            var key = addressableKey ?? AddressableKeys.UI.Get<T>();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"[UIManager] Addressable key for {typeof(T).Name} is empty.");
                return;
            }

            var pool = await ObjectPoolManager.GetOrCreatePoolAsync<T>(
                addressableKey: key,
                preloadCount: preloadCount,
                parent: null,
                onGet: instance => Reparent(instance),
                onRelease: null);

            if (pool == null)
                Debug.LogError($"[UIManager] Failed to preload pool for {typeof(T).Name} (key={key}).");
        }

        private async UniTask ShowLoadingAsync_Internal(string message)
        {
            await PreloadAsync_Internal<LoadingPanel>(preloadCount: 1);

            if (_active.TryGetValue(typeof(LoadingPanel), out var existing) && existing != null)
            {
                if (existing is LoadingPanel openPanel)
                    openPanel.SetMessage(message);
                return;
            }

            var panel = Open_Internal<LoadingPanel>();
            panel?.SetMessage(message);
        }

        private void HideLoading_Internal()
        {
            if (!_active.TryGetValue(typeof(LoadingPanel), out var ui) || ui == null)
                return;

            Close_Internal((LoadingPanel)ui);
        }

        #endregion

        #region Close Internal

        private void Close_Internal<T>(T ui) where T : UIBase
        {
            if (ui == null) return;
            RunCloseAsync(ui, releasePool: false).Forget();
        }

        private void CloseDynamic_Internal<T>(T ui) where T : UIBase
        {
            if (ui == null) return;
            RunCloseAsync(ui, releasePool: true).Forget();
        }

        /// <summary>
        /// 닫기 시작 시점에 _active / lifecycle 을 즉시 떼어,
        /// CloseAsync await 중에 Open 이 끼어들어 새 인스턴스 추적이 지워지는 레이스를 막는다.
        /// </summary>
        private async UniTask RunCloseAsync(UIBase target, bool releasePool)
        {
            if (target == null) return;

            if (_active.TryGetValue(target.GetType(), out var current) && ReferenceEquals(current, target))
                _active.Remove(target.GetType());
            RemoveFromBackStack(target);

            var hasLifecycle = _lifecycles.TryGetValue(target, out var lifecycle);
            if (hasLifecycle)
                _lifecycles.Remove(target);

            await target.CloseAsync();

            if (hasLifecycle)
            {
                lifecycle.Release?.Invoke();
                if (releasePool)
                    lifecycle.ReleasePool?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[UIManager] Lifecycle missing for {target.GetType().Name}. Falling back to deactivate.");
                if (target != null)
                    target.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Back Stack Internal

        /// <summary>
        /// 뒤로가기 처리. InputActionHandler 콜백 또는 외부에서 직접 호출 가능.
        /// </summary>
        private void PopBack_Internal()
        {
            while (_backStack.Count > 0)
            {
                var top = _backStack.Peek();
                if (top == null)
                {
                    _backStack.Pop();
                    continue;
                }

                if (!top.OnBackPressed())
                {
                    return;
                }

                _backStack.Pop();
                RunCloseAsync(top, releasePool: false).Forget();
                return;
            }

            _onBackEmpty.OnNext(Unit.Default);
        }

        private void RemoveFromBackStack(UIBase ui)
        {
            if (ui == null || _backStack.Count == 0 || !ui.HandleBackButton) return;

            if (ReferenceEquals(_backStack.Peek(), ui))
            {
                _backStack.Pop();
                return;
            }

            var temp = ListPool<UIBase>.Get();
            try
            {
                while (_backStack.Count > 0)
                {
                    var item = _backStack.Pop();
                    if (ReferenceEquals(item, ui))
                    {
                        break;
                    }
                    temp.Add(item);
                }

                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    _backStack.Push(temp[i]);
                }
            }
            finally
            {
                ListPool<UIBase>.Release(temp);
            }
        }

        #endregion

        #region Helpers

        private void Activate(UIBase ui)
        {
            Reparent(ui);
            _active[ui.GetType()] = ui;

            if (ui.HandleBackButton)
            {
                _backStack.Push(ui);
            }

            ui.HandleOpen();
        }

        private void Reparent(UIBase ui)
        {
            if (ui == null || _registry == null) return;

            var root = _registry.GetRoot(ui.Layer);
            if (root == null)
            {
                Debug.LogWarning($"[UIManager] No root mapped for layer '{ui.Layer}'. {ui.GetType().Name} will stay at scene root.");
                return;
            }

            ui.transform.SetParent(root, false);
            ui.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Open&lt;T&gt; 시점에 컴파일 타임 타입 T 를 캡처한 Release/ReleasePool 델리게이트를 등록.
        /// 백 스택을 통해 닫힐 때처럼 런타임 타입만 알아도 정확한 풀 메서드를 호출할 수 있게 해준다.
        /// </summary>
        private void RegisterLifecycle<T>(UIBase key, T target) where T : UIBase
        {
            _lifecycles[key] = new UILifecycle(
                release: () => ObjectPoolManager.Release(target),
                releasePool: () => ObjectPoolManager.ReleasePool<T>());
        }

        #endregion

        #region Block / Close All Internal

        private UniTask BlockUI_Internal()
        {
            if (_blockerRoot != null)
            {
                _blockerRoot.SetActive(true);
                return UniTask.CompletedTask;
            }

            _blockerRoot = new GameObject("[UIManager] InputBlocker");
            DontDestroyOnLoad(_blockerRoot);

            var canvas = _blockerRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            _blockerRoot.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("Image");
            imageGo.transform.SetParent(_blockerRoot.transform, false);
            var img = imageGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return UniTask.CompletedTask;
        }

        private void UnblockUI_Internal()
        {
            if (_blockerRoot != null) _blockerRoot.SetActive(false);
        }

        private void CloseAllUI_Internal()
        {
            CloseAllUIAsync_Internal().Forget();
        }

        private async UniTask CloseAllUIAsync_Internal()
        {
            var snapshot = ListPool<UIBase>.Get();
            try
            {
                snapshot.AddRange(_active.Values);
                var closeTasks = new List<UniTask>(snapshot.Count);
                for (var i = 0; i < snapshot.Count; i++)
                {
                    var ui = snapshot[i];
                    // 씬 전환 중 LoadingPanel 은 CloseAllUI 대상에서 제외한다.
                    if (ui is LoadingPanel)
                        continue;

                    closeTasks.Add(RunCloseAsync(ui, releasePool: false));
                }

                if (closeTasks.Count > 0)
                    await UniTask.WhenAll(closeTasks);
            }
            finally
            {
                ListPool<UIBase>.Release(snapshot);
            }

            _backStack.Clear();
            if (_active.TryGetValue(typeof(LoadingPanel), out var loading)
                && loading != null
                && loading.HandleBackButton)
            {
                _backStack.Push(loading);
            }

            UnblockUI_Internal();
        }

        #endregion

        #region Static Public Methods

        /// <summary>
        /// 풀이 이미 만들어져 있는 UI 를 동기로 오픈한다.
        /// </summary>
        public static T Open<T>() where T : UIBase
            => Instance.Open_Internal<T>();

        /// <summary>
        /// Addressable 에서 비동기 로드하여 풀을 생성한 뒤 첫 인스턴스를 오픈한다.
        /// addressableKey 가 null 이면 AddressableKeys.UI.Get&lt;T&gt;() 를 사용.
        /// </summary>
        public static UniTask<T> OpenAsync<T>(string addressableKey = null) where T : UIBase
            => Instance.OpenAsync_Internal<T>(addressableKey);

        /// <summary>
        /// Addressable 로 UI 풀만 미리 생성한다. 인스턴스는 오픈하지 않는다.
        /// </summary>
        public static UniTask PreloadAsync<T>(string addressableKey = null, int preloadCount = 1) where T : UIBase
            => Instance.PreloadAsync_Internal<T>(addressableKey, preloadCount);

        /// <summary>
        /// 프리로드된 LoadingPanel 을 오픈한다. 씬 전환 직전에 호출한다.
        /// </summary>
        public static UniTask ShowLoadingAsync(string message = "Loading...")
            => Instance.ShowLoadingAsync_Internal(message);

        /// <summary>
        /// LoadingPanel 을 닫는다. 씬 진입 UI 준비가 끝난 뒤 호출한다.
        /// </summary>
        public static void HideLoading()
            => Instance.HideLoading_Internal();

        /// <summary>
        /// UI 를 닫고 풀로 반환한다 (풀은 유지).
        /// </summary>
        public static void Close<T>(T ui) where T : UIBase
            => Instance.Close_Internal(ui);

        /// <summary>
        /// UI 를 닫고 해당 타입의 풀까지 해제한다 (Addressable 핸들도 반환).
        /// </summary>
        public static void CloseDynamic<T>(T ui) where T : UIBase
            => Instance.CloseDynamic_Internal(ui);

        /// <summary>
        /// 뒤로가기 처리. InputActionHandler 콜백 또는 외부에서 직접 호출 가능.
        /// </summary>
        public static void PopBack()
            => Instance.PopBack_Internal();

        /// <summary>
        /// 입력을 받지 않는 풀스크린 블로커를 활성화한다. 씬 전환 중 입력 차단 용도.
        /// </summary>
        public static UniTask BlockUI()
            => Instance.BlockUI_Internal();

        /// <summary>
        /// BlockUI 로 활성화된 블로커를 비활성화한다.
        /// </summary>
        public static void UnblockUI()
            => Instance.UnblockUI_Internal();

        /// <summary>
        /// 현재 활성화된 모든 UI 를 닫고 백 스택과 블로커를 정리한다.
        /// 완료를 기다리지 않는다. 씬 진입 직전 Open 과 함께 쓰지 말 것 — <see cref="CloseAllUIAsync"/> 사용.
        /// </summary>
        public static void CloseAllUI()
            => Instance.CloseAllUI_Internal();

        /// <summary>
        /// 현재 활성화된 모든 UI 가 풀에 반환될 때까지 대기한다.
        /// 씬 Context OnEnter 에서 Open 하기 전에 호출한다.
        /// </summary>
        public static UniTask CloseAllUIAsync()
            => Instance.CloseAllUIAsync_Internal();

        #endregion
    }
}
