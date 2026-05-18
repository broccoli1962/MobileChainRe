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

        private static readonly List<Panel> activePanels = new List<Panel>();
        public static int ActivePanelCount => activePanels.Count;

        private static readonly CompositeDisposable subscriptions = new CompositeDisposable();

        private static CancellationTokenSource cts;

        private static readonly Subject<ChainBrokenInfo> _onChainBroken = new Subject<ChainBrokenInfo>();
        public static Observable<ChainBrokenInfo> OnChainBroken => _onChainBroken;

        public static Action<Panel> OnPanelBroken;

        private static bool _isProcessing = false;
        public static bool IsProcessing => _isProcessing;

        private static ChainLine _chainLine;

        // 캐시: 같은 누름 동안 hold-preview와 release-break가 동일 결과를 쓰도록 보장
        private static Panel _cachedRootPanel;
        private static List<List<Panel>> _cachedLayers;
        private static List<List<(Panel from, Panel to)>> _cachedEdgesByLayer;

        public static void Initialize()
        {
            Dispose();

            cts = new CancellationTokenSource();

            InputSystem.OnPointerPressed
                .Subscribe(HandlePointerPressed)
                .AddTo(subscriptions);

            InputSystem.OnPointerHoldBegan
                .Subscribe(HandlePointerHoldBegan)
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

            _onChainBroken.OnCompleted();

            ClearCache();
            _chainLine = null;
            activePanels.Clear();
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

            ClearCache();

            RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(pos));
            if (hit.transform == null) return;
            if (!hit.transform.TryGetComponent(out Panel clickedPanel)) return;

            var (layers, edgesByLayer) = FindConnectedPanels(clickedPanel);
            if (layers.Count < 1) return;

            _cachedRootPanel = clickedPanel;
            _cachedLayers = layers;
            _cachedEdgesByLayer = edgesByLayer;
        }

        private static void HandlePointerHoldBegan(Vector2 _)
        {
            if (_isProcessing) return;
            if (_cachedLayers == null || _chainLine == null) return;

            _chainLine.ShowPreview(_cachedEdgesByLayer);
        }

        private static void HandlePointerReleased((Vector2 pos, bool wasHold) e)
        {
            if (_isProcessing)
            {
                ClearCache();
                return;
            }

            if (_cachedLayers == null)
            {
                _chainLine?.Hide();
                return;
            }

            if (e.wasHold)
            {
                _chainLine?.Hide();
                ClearCache();
                return;
            }

            var layers = _cachedLayers;
            var edges = _cachedEdgesByLayer;
            ClearCache();

            PanelChangeKinematic();
            BreakChainSequence(layers, edges).Forget();
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
                        panel.CachedTransform.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                        OnPanelBroken?.Invoke(panel);
                    }
                }

                PanelChangeDynamic();
            }
            finally
            {
                _chainLine?.Hide();
                _isProcessing = false;
            }
        }

        private static void PanelChangeKinematic()
        {
            foreach (var panel in activePanels)
            {
                panel.CachedTransform.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            }
        }

        private static void PanelChangeDynamic()
        {
            foreach (var panel in activePanels)
            {
                panel.CachedTransform.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }
}
