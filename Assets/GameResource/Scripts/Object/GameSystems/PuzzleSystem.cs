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
        private const float ConnectDistanceMultiplier = 1.8f; // 연결 허용 거리 배수
        private const float ChainBreakDelay = 0.15f;

        private static readonly List<Panel> activePanels = new List<Panel>();
        public static int ActivePanelCount => activePanels.Count;

        private static readonly CompositeDisposable subscriptions = new CompositeDisposable();

        private static CancellationTokenSource cts;

        private static readonly Subject<ChainBrokenInfo> _onChainBroken = new Subject<ChainBrokenInfo>();
        public static Observable<ChainBrokenInfo> OnChainBroken => _onChainBroken;

        public static Action<Panel> OnPanelBroken;

        private static bool _isProcessing = false;

        public static void Initialize()
        {
            Dispose();

            cts = new CancellationTokenSource();

            InputSystem.OnPointerDown
                .ThrottleFirst(TimeSpan.FromSeconds(0.2f))
                .Subscribe(HandleTouchInput)
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

            activePanels.Clear();
        }

        public static void RegisterPanel(Panel newPanel)
        {
            if (!activePanels.Contains(newPanel))
            {
                activePanels.Add(newPanel);
            }
        }

        private static void HandleTouchInput(Vector2 pos)
        {
            if (_isProcessing) return; // 연출 진행 중 재입력 차단

            RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(pos));

            if (hit.transform != null && hit.transform.TryGetComponent(out Panel clickedPanel))
            {
                if (clickedPanel != null)
                {
                    var chainLayers = FindConnectedPanels(clickedPanel);

                    if (chainLayers.Count >= 1)
                    {
                        PanelChangeKinematic(); // 체인 패널 포함 전체 Kinematic (activePanels 기준, Remove 전)
                        BreakChainSequence(chainLayers).Forget();
                    }
                }
            }
        }

        /// <summary>
        /// 같은 타입이며 거리 조건으로 연결된 패널을 BFS 레이어 단위로 반환합니다.
        /// 레이어 0은 시작 패널, 다음 레이어는 이전 레이어 패널들과 인접한 미방문 패널입니다.
        /// </summary>
        private static List<List<Panel>> FindConnectedPanels(Panel startPanel)
        {
            var layers = new List<List<Panel>>();
            var visited = new HashSet<Panel>();
            var currentLayer = new List<Panel> { startPanel };
            visited.Add(startPanel);

            while (currentLayer.Count > 0)
            {
                layers.Add(currentLayer);
                var nextLayer = new List<Panel>();

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
                        }
                    }
                }

                currentLayer = nextLayer;
            }

            return layers;
        }

        private static bool IsNear(Panel p1, Panel p2)
        {
            float dist = Vector3.Distance(p1.CachedTransform.position, p2.CachedTransform.position);

            float threshold = ConnectDistanceMultiplier * ((p1.Radius + p2.Radius) / 2f);
            return dist < threshold;
        }

        private static async UniTaskVoid BreakChainSequence(List<List<Panel>> chainLayers)
        {
            _isProcessing = true;

            try
            {
                _onChainBroken.OnNext(new ChainBrokenInfo(chainLayers));

                // Phase 1: 레이어별 동시 반투명 연출 (레이어 사이에만 대기)
                for (int i = 0; i < chainLayers.Count; i++)
                {
                    foreach (var panel in chainLayers[i])
                    {
                        panel.BrokenPanel();
                        //AudioManager.PlaySfx(, 0.8f);
                    }

                    if (i < chainLayers.Count - 1)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(ChainBreakDelay), cancellationToken: cts.Token);
                    }
                }

                // Phase 2: 레이어 순서대로 일괄 제거 및 풀 반환
                foreach (var layer in chainLayers)
                {
                    foreach (var panel in layer)
                    {
                        activePanels.Remove(panel);
                        panel.CachedTransform.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
                        OnPanelBroken?.Invoke(panel);
                    }
                }

                PanelChangeDynamic();
            }
            finally
            {
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
