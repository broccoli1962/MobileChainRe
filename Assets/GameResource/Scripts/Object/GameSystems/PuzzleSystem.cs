using System;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.CharacterObject;
using Backend.Object.Management;
using Backend.Object.PanelObject;
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
        private const float BombExplosionRadius = 1.5f;
        private const int BaseCpThreshold = 6;
        private const int ScpThreshold = 12;

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
        private static readonly List<Vector3> _deferredBombPositions = new();
        private static readonly List<List<Panel>> _bombBrokenLayers = new();
        private static bool _deferBombEnqueue;

        private static readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private static CancellationTokenSource cts;

        private static readonly Subject<ChainBrokenInfo> _onChainBroken = new Subject<ChainBrokenInfo>();
        public static Observable<ChainBrokenInfo> OnChainBroken => _onChainBroken;

        public static Action<Panel> OnPanelBroken;
        public static Action<Vector3, PanelType, CrashRank> OnCrashPanelRequested;

        private static bool _isProcessing = false;
        public static bool IsProcessing => _isProcessing;

        public static float ExtraHeartWeight { get; set; }
        public static int CpThresholdDelta { get; set; }
        private static int EffectiveCpThreshold => Mathf.Max(1, BaseCpThreshold + CpThresholdDelta);

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
            OnCrashPanelRequested = null;

            ClearCache();
            _chainLine = null;
            activePanels.Clear();
            _frozenPhysicsCache.Clear();
            _deferredBombPositions.Clear();
            _bombBrokenLayers.Clear();
            _deferBombEnqueue = false;
            _isPressActive = false;
            _currentHoverPanel = null;
            CancelPreviewTimer();
            _previewVisible = false;
            ExtraHeartWeight = 0f;
            CpThresholdDelta = 0;
        }

        public static void SetChainLine(ChainLine chainLine)
        {
            _chainLine = chainLine;
        }

        /// <summary>
        /// 진행 중인 입력(누름/미리보기)만 취소한다. 체인 파괴 연출 중이면 건드리지 않는다.
        /// </summary>
        public static void CancelActiveInput()
        {
            if (_isProcessing) return;

            _isPressActive = false;
            _currentHoverPanel = null;
            _previewVisible = false;
            CancelPreviewTimer();
            ClearCache();
            _chainLine?.Hide();
            PanelChangeDynamic();
        }

        public static void RegisterPanel(Panel newPanel)
        {
            if (!activePanels.Contains(newPanel))
            {
                activePanels.Add(newPanel);
            }
        }

        private static bool CanAcceptPointerInput()
        {
            if (_isProcessing) return false;
            if (GameManager.CurrentState != GameState.Playing) return false;
            return GameManager.CurrentPhase == GamePhase.PlayerTurn;
        }

        private static void HandlePointerPressed(Vector2 pos)
        {
            if (!CanAcceptPointerInput()) return;

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
            if (!CanAcceptPointerInput())
            {
                CancelActiveInput();
                return;
            }

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
            if (!CanAcceptPointerInput())
            {
                CancelActiveInput();
                return;
            }

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
            _deferBombEnqueue = true;
            _deferredBombPositions.Clear();
            _bombBrokenLayers.Clear();

            try
            {
                int validBroken = 0;
                var hostPos = Vector3.zero;
                var hostChainType = PanelType.fire;
                var hasHost = false;

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

                        if (IsValidCrashCount(panel))
                        {
                            validBroken++;
                            hostPos = panel.CachedTransform.position;
                            hostChainType = panel.panelType;
                            hasHost = true;
                        }

                        RemoveBrokenPanel(panel);
                    }
                }

                _chainLine?.Hide();

                // 체인 직후 CP/SCP 보상을 먼저 스폰하고, 폭탄은 물리로 떨어진 뒤 순서대로 터진다.
                PanelChangeDynamic();
                if (hasHost)
                    TryRequestCrashPanel(validBroken, hostPos, hostChainType);

                _deferBombEnqueue = false;
                FlushDeferredBombs();
                await BombSystem.WaitUntilIdle(cts.Token);

                foreach (var layer in _bombBrokenLayers)
                    chainLayers.Add(layer);

                // 패널 파괴 연출이 끝난 뒤에 발행해야 액션 소진/공격 판정이 연출과 겹치지 않는다.
                _onChainBroken.OnNext(new ChainBrokenInfo(chainLayers));
            }
            finally
            {
                _deferBombEnqueue = false;
                _deferredBombPositions.Clear();
                _bombBrokenLayers.Clear();
                PanelChangeDynamic();
                _chainLine?.Hide();
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 폭탄 위치 반경 안의 패널을 파괴한다. 보호 패널은 방어만 제거한다.
        /// </summary>
        public static void ApplyBombExplosion(Vector3 position)
        {
            var hits = new List<(Panel panel, float dist)>();
            foreach (var panel in activePanels)
            {
                float dist = Vector3.Distance(panel.SpriteBoundsCenter, position);
                if (dist < BombExplosionRadius + panel.Radius)
                    hits.Add((panel, dist));
            }

            hits.Sort((a, b) => a.dist.CompareTo(b.dist));

            var destroyed = new List<Panel>();
            foreach (var (panel, _) in hits)
            {
                panel.PopSound();
                if (panel.IsProtected)
                {
                    panel.SetProtected(false);
                    continue;
                }

                panel.BrokenPanel();
                destroyed.Add(panel);
            }

            foreach (var panel in destroyed)
                RemoveBrokenPanel(panel);

            if (destroyed.Count > 0)
                _bombBrokenLayers.Add(destroyed);
        }

        private static void RemoveBrokenPanel(Panel panel)
        {
            TryEnqueueBomb(panel);

            activePanels.Remove(panel);
            _frozenPhysicsCache.Remove(panel);
            if (panel.CachedTransform.TryGetComponent(out Rigidbody2D rb))
                rb.bodyType = RigidbodyType2D.Dynamic;
            OnPanelBroken?.Invoke(panel);
        }

        private static void TryEnqueueBomb(Panel panel)
        {
            if (panel.CrashRank != CrashRank.SCP) return;

            Vector3 position = panel.CachedTransform.position;
            if (_deferBombEnqueue)
                _deferredBombPositions.Add(position);
            else
                BombSystem.Enqueue(position);
        }

        private static void FlushDeferredBombs()
        {
            for (int i = 0; i < _deferredBombPositions.Count; i++)
                BombSystem.Enqueue(_deferredBombPositions[i]);
            _deferredBombPositions.Clear();
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

        private static bool IsValidCrashCount(Panel panel)
        {
            return panel.panelType != PanelType.obstacle;
        }

        private static void TryRequestCrashPanel(int validBroken, Vector3 hostPos, PanelType hostChainType)
        {
            if (validBroken < EffectiveCpThreshold) return;

            var rank = validBroken >= ScpThreshold ? CrashRank.SCP : CrashRank.CP;
            var type = ResolveCrashPanelType(hostChainType);
            OnCrashPanelRequested?.Invoke(hostPos, type, rank);
        }

        private static PanelType ResolveCrashPanelType(PanelType fallback)
        {
            if (CharacterSystem.Count >= 1
                && CharacterSystem.GetCharacter(1) is CharacterSlot front
                && front.UnitData != null)
            {
                return (PanelType)(int)front.UnitData.unitType;
            }

            return fallback;
        }

        public static void ConvertRandomPanels(PanelType type, int count)
        {
            if (count <= 0) return;

            var candidates = new List<Panel>();
            foreach (var panel in activePanels)
            {
                if (panel == null) continue;
                if (panel.panelType == type) continue;
                if (panel.panelType == PanelType.obstacle) continue;
                if (panel.CrashRank != CrashRank.None) continue;
                candidates.Add(panel);
            }

            int convertCount = Mathf.Min(count, candidates.Count);
            for (int i = 0; i < convertCount; i++)
            {
                int swap = UnityEngine.Random.Range(i, candidates.Count);
                (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
                candidates[i].SetColor(type);
            }
        }

        public static void DestroyObstacles()
        {
            var hits = new List<Panel>();
            foreach (var panel in activePanels)
            {
                if (panel != null && panel.panelType == PanelType.obstacle)
                    hits.Add(panel);
            }

            DestroyPanelsImmediate(hits);
        }

        public static void DestroyHorizontalBand(float bandHeight)
        {
            if (activePanels.Count == 0) return;

            float half = Mathf.Max(0.1f, bandHeight);
            float centerY = 0f;
            if (Camera.main != null)
                centerY = Camera.main.transform.position.y;

            var hits = new List<Panel>();
            foreach (var panel in activePanels)
            {
                if (panel == null) continue;
                if (Mathf.Abs(panel.SpriteBoundsCenter.y - centerY) <= half)
                    hits.Add(panel);
            }

            DestroyPanelsImmediate(hits);
        }

        public static void TrySpawnCrashPanel(PanelType type)
        {
            Vector3 pos = Vector3.zero;
            if (activePanels.Count > 0)
            {
                var host = activePanels[UnityEngine.Random.Range(0, activePanels.Count)];
                pos = host.CachedTransform.position;
            }
            else if (Camera.main != null)
            {
                pos = Camera.main.transform.position;
            }

            OnCrashPanelRequested?.Invoke(pos, type, CrashRank.CP);
        }

        public static void SpawnBombs(int count)
        {
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = Vector3.zero;
                if (activePanels.Count > 0)
                    pos = activePanels[UnityEngine.Random.Range(0, activePanels.Count)].CachedTransform.position;
                else if (Camera.main != null)
                    pos = Camera.main.transform.position;

                BombSystem.Enqueue(pos);
            }
        }

        private static void DestroyPanelsImmediate(List<Panel> panels)
        {
            foreach (var panel in panels)
            {
                if (panel == null) continue;

                panel.PopSound();
                if (panel.IsProtected)
                {
                    panel.SetProtected(false);
                    continue;
                }

                panel.BrokenPanel();
                RemoveBrokenPanel(panel);
            }
        }
    }
}
