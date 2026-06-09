using System;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.PanelObject;
using Backend.Util.Enum;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    public readonly struct ChainBrokenInfo
    {
        public readonly int TotalCount;
        public readonly IReadOnlyDictionary<PanelType, int> CountByType;

        /// <summary>루트에서 퍼져 나가는 레이어별 리스트 (각 레이어 내 패널은 동일 거리).</summary>
        public ChainBrokenInfo(List<List<Panel>> chainLayers)
        {
            var dict = new Dictionary<PanelType, int>();
            int total = 0;
            foreach (var layer in chainLayers)
            {
                foreach (var p in layer)
                {
                    total++;
                    dict.TryGetValue(p.panelType, out int cur);
                    dict[p.panelType] = cur + 1;
                }
            }

            TotalCount = total;
            CountByType = dict;
        }
    }

    public static class PuzzleSystem
    {
        private const float ConnectDistanceMultiplier = 2.5f; // 연결 허용 거리 배수
        private const float ChainBreakDelay = 0.15f;
        private const float PreviewDelay = 0.1f; // 누름 시점부터 미리보기 표시까지 대기 시간

        //활성 패널 개수
        private static readonly List<Panel> activePanels = new List<Panel>();
        public static int ActivePanelCount => activePanels.Count;

        private struct FrozenPanelPhysics
        {
            public Vector2 Velocity;
            public float AngularVelocity;
        }

        // 체인 파괴 중 정지시킨 패널의 잠금 직전 속도 캐시. 복원 시 사용.
        private static readonly Dictionary<Panel, FrozenPanelPhysics> _frozenPhysicsCache = new();

        private static readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private static CancellationTokenSource cts;

        private static readonly Subject<ChainBrokenInfo> _onChainBroken = new Subject<ChainBrokenInfo>();
        public static Observable<ChainBrokenInfo> OnChainBroken => _onChainBroken;

        public static Action<Panel> OnPanelBroken;

        private static bool _isProcessing = false;
        public static bool IsProcessing => _isProcessing;

        private static ChainLine _chainLine;

        // 캐시: 현재 hover 패널 기준 미리보기/파괴 데이터. hover 변경 시 갱신.
        private static Panel _cachedRootPanel;
        private static List<List<Panel>> _cachedLayers;
        private static List<List<(Panel from, Panel to)>> _cachedEdgesByLayer;

        // Press가 패널 위에서 시작했는가. true일 때만 드래그/릴리즈 처리 활성.
        private static bool _isPressActive;
        // 마지막으로 hover한 패널. null이면 빈 공간 위.
        private static Panel _currentHoverPanel;

        // 미리보기 표시 지연 타이머. 짧은 탭에서는 미리보기를 띄우지 않기 위함.
        private static CancellationTokenSource _previewCts;
        // 미리보기 표시 단계 진입 여부. true가 되면 hover에 따라 갱신/숨김.
        private static bool _previewVisible;

        public static void Initialize()
        {
            cts = new CancellationTokenSource();

            InputSystem.OnPointerPressed
                .Subscribe(HandlePointerPressed)
                .AddTo(subscriptions);

            InputSystem.OnPointerMoved
                .Subscribe(HandlePointerMoved)
                .AddTo(subscriptions);

            InputSystem.OnPointerReleased
                .Subscribe(HandlePointerReleased)
                .AddTo(subscriptions);
        }

        public static void Dispose()
        {
            subscriptions.Clear();

            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            OnPanelBroken = null;

            ClearCache();
            _chainLine = null;
            activePanels.Clear();
            _frozenPhysicsCache.Clear();
            _isPressActive = false;
            _currentHoverPanel = null;
            CancelPreviewTimer();
            _previewVisible = false;
        }

        public static void SetChainLine(ChainLine chainLine)
        {
            _chainLine = chainLine;
        }

        public static void RegisterPanel(Panel newPanel)
        {
            if (!activePanels.Contains(newPanel))
            {
                activePanels.Add(newPanel);
            }
        }

        private static void HandlePointerPressed(Vector2 pos)
        {
            if (_isProcessing) return;

            _isPressActive = false;
            _currentHoverPanel = null;
            _previewVisible = false;
            CancelPreviewTimer();
            ClearCache();

            RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(pos));
            if (hit.transform == null) return;
            if (!hit.transform.TryGetComponent(out Panel pressedPanel)) return;

            _isPressActive = true;
            _currentHoverPanel = pressedPanel;
            PanelChangeKinematic();
            UpdateCache(pressedPanel);
            StartPreviewTimer();
        }

        private static void HandlePointerMoved(Vector2 pos)
        {
            if (_isProcessing) return;
            if (!_isPressActive) return;

            RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(pos));
            Panel hoverPanel = null;
            if (hit.transform != null) hit.transform.TryGetComponent(out hoverPanel);

            if (hoverPanel == _currentHoverPanel) return;
            _currentHoverPanel = hoverPanel;

            if (hoverPanel == null)
            {
                ClearCache();
            }
            else
            {
                UpdateCache(hoverPanel);
            }

            RefreshPreview();
        }

        private static void HandlePointerReleased(Vector2 pos)
        {
            if (_isProcessing) return;
            if (!_isPressActive) return;

            _isPressActive = false;
            CancelPreviewTimer();
            _previewVisible = false;

            RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(pos));
            Panel releasedPanel = null;
            if (hit.transform != null) hit.transform.TryGetComponent(out releasedPanel);

            if (releasedPanel == null)
            {
                _chainLine?.Hide();
                ClearCache();
                _currentHoverPanel = null;
                PanelChangeDynamic();
                return;
            }

            List<List<Panel>> layers;
            List<List<(Panel from, Panel to)>> edges;
            if (releasedPanel == _cachedRootPanel && _cachedLayers != null)
            {
                layers = _cachedLayers;
                edges = _cachedEdgesByLayer;
            }
            else
            {
                (layers, edges) = FindConnectedPanels(releasedPanel);
            }
            ClearCache();
            _currentHoverPanel = null;

            // PanelChangeKinematic은 Press에서 이미 호출됨. BreakChainSequence finally에서 PanelChangeDynamic 호출.
            BreakChainSequence(layers, edges).Forget();
        }

        private static void UpdateCache(Panel rootPanel)
        {
            var (layers, edges) = FindConnectedPanels(rootPanel);
            _cachedRootPanel = rootPanel;
            _cachedLayers = layers;
            _cachedEdgesByLayer = edges;
        }

        private static void RefreshPreview()
        {
            if (!_previewVisible) return;
            if (_cachedEdgesByLayer != null)
                _chainLine?.ShowPreview(_cachedEdgesByLayer);
            else
                _chainLine?.Hide();
        }

        private static void StartPreviewTimer()
        {
            CancelPreviewTimer();
            _previewCts = new CancellationTokenSource();
            PreviewTimerAsync(_previewCts.Token).Forget();
        }

        private static async UniTaskVoid PreviewTimerAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(PreviewDelay), cancellationToken: token);
                _previewVisible = true;
                RefreshPreview();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static void CancelPreviewTimer()
        {
            if (_previewCts == null) return;
            _previewCts.Cancel();
            _previewCts.Dispose();
            _previewCts = null;
        }

        private static void ClearCache()
        {
            _cachedRootPanel = null;
            _cachedLayers = null;
            _cachedEdgesByLayer = null;
        }

        /// <summary>
        /// 같은 타입이며 거리 조건으로 연결된 패널을 BFS 레이어 단위로 반환합니다.
        /// 레이어 0은 시작 패널, 다음 레이어는 이전 레이어 패널들과 인접한 미방문 패널입니다.
        /// edgesByLayer[i]는 레이어 i-1 → 레이어 i 로 들어가는 엣지 목록입니다 (레이어 0은 빈 리스트).
        /// </summary>
        private static (List<List<Panel>> layers, List<List<(Panel from, Panel to)>> edgesByLayer)
            FindConnectedPanels(Panel startPanel)
        {
            var layers = new List<List<Panel>>();
            var edgesByLayer = new List<List<(Panel from, Panel to)>>();

            var visited = new HashSet<Panel>();
            var currentLayer = new List<Panel> { startPanel };
            visited.Add(startPanel);

            layers.Add(currentLayer);
            edgesByLayer.Add(new List<(Panel, Panel)>());

            while (currentLayer.Count > 0)
            {
                var nextLayer = new List<Panel>();
                var nextEdges = new List<(Panel from, Panel to)>();

                foreach (var current in currentLayer)
                {
                    foreach (var neighbor in activePanels)
                    {
                        if (visited.Contains(neighbor)) continue;
                        if (neighbor.panelType != startPanel.panelType) continue;

                        if (IsNear(current, neighbor))
                        {
                            visited.Add(neighbor);
                            nextLayer.Add(neighbor);
                            nextEdges.Add((current, neighbor));
                        }
                    }
                }

                if (nextLayer.Count == 0) break;

                layers.Add(nextLayer);
                edgesByLayer.Add(nextEdges);
                currentLayer = nextLayer;
            }

            return (layers, edgesByLayer);
        }

        private static bool IsNear(Panel p1, Panel p2)
        {
            float dist = Vector3.Distance(p1.SpriteBoundsCenter, p2.SpriteBoundsCenter);

            float threshold = ConnectDistanceMultiplier * ((p1.Radius + p2.Radius) / 2f);
            return dist < threshold;
        }

        private static async UniTaskVoid BreakChainSequence(
            List<List<Panel>> chainLayers,
            List<List<(Panel from, Panel to)>> edgesByLayer)
        {
            _isProcessing = true;
            _chainLine?.Hide();

            try
            {
                _onChainBroken.OnNext(new ChainBrokenInfo(chainLayers));

                // 레이어별 순차 처리: 라인 표시 → 페이드 → 딜레이 → 제거
                for (int i = 0; i < chainLayers.Count; i++)
                {
                    if (i > 0 && _chainLine != null)
                        _chainLine.ShowLayer(edgesByLayer[i]);

                    foreach (var panel in chainLayers[i])
                    {
                        panel.PopSound();
                        if (!panel.IsProtected)
                            panel.BrokenPanel();
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(ChainBreakDelay), cancellationToken: cts.Token);

                    foreach (var panel in chainLayers[i])
                    {
                        if (panel.IsProtected)
                        {
                            panel.SetProtected(false);
                            continue;
                        }
                        activePanels.Remove(panel);
                        _frozenPhysicsCache.Remove(panel);
                        // 풀 반환 전 Dynamic으로 복구. 재사용 시 Kinematic 잔존 방지.
                        panel.CachedTransform.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                        OnPanelBroken?.Invoke(panel);
                    }
                }
            }
            finally
            {
                PanelChangeDynamic();
                _chainLine?.Hide();
                _isProcessing = false;
            }
        }

        private static void PanelChangeKinematic()
        {
            foreach (var panel in activePanels)
            {
                var rb = panel.CachedTransform.GetComponent<Rigidbody2D>();
                _frozenPhysicsCache[panel] = new FrozenPanelPhysics
                {
                    Velocity = rb.linearVelocity,
                    AngularVelocity = rb.angularVelocity,
                };
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            // Physics2D.autoSyncTransforms = false 환경에서 잠금 직후 위치 동기화 보장
            Physics2D.SyncTransforms();
        }

        private static void PanelChangeDynamic()
        {
            foreach (var panel in activePanels)
            {
                var rb = panel.CachedTransform.GetComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                if (_frozenPhysicsCache.TryGetValue(panel, out var cached))
                {
                    rb.linearVelocity = cached.Velocity;
                    rb.angularVelocity = cached.AngularVelocity;
                }
            }

            _frozenPhysicsCache.Clear();
        }
    }
}
