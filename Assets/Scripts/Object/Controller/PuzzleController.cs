using Backend.Object.Management;
using Backend.Object.PanelObject;
using Backend.Util.Interface;
using Backend.Util.Management;
using R3;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Controller
{
    public class PuzzleController : SingletonGameObject<PuzzleController>
    {
        [Header("Settings")]
        [SerializeField] private float connectDistanceMultiplier = 1.3f; // 연결 허용 거리 배수
        [SerializeField] private float chainBreakDelay = 0.15f;

        private static readonly List<Panel> activePanels = new List<Panel>();
        public static int ActivePanelCount => activePanels.Count;

        protected override void OnAwake()
        {
            base.OnAwake();

            InputManager.OnPointerDown
                .ThrottleFirst(TimeSpan.FromSeconds(0.2f))
                .Subscribe(screenPosition =>
                {
                    HandleTouchInput(screenPosition);
                }).AddTo(this);
        }

        private void RegisterPanel_Internal(Panel newPanel)
        {
            if (!activePanels.Contains(newPanel))
            {
                activePanels.Add(newPanel);
            }
        }

        public static void RegisterPanel(Panel newPanel)
        {
            Instance.RegisterPanel_Internal(newPanel);
        }

        private void HandleTouchInput(Vector2 pos)
        {
            RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(pos));

            if (hit.transform != null && hit.transform.TryGetComponent(out Panel clickedPanel))
            {
                if (clickedPanel != null)
                {
                    var chain = FindConnectedPanels(clickedPanel);

                    if (chain.Count >= 3)
                    {
                        StartCoroutine(BreakChainSequence(chain));
                    }
                }
            }
        }

        private List<Panel> FindConnectedPanels(Panel startPanel)
        {
            List<Panel> connectedChain = new List<Panel>();
            Queue<Panel> searchQueue = new Queue<Panel>();
            HashSet<Panel> visited = new HashSet<Panel>();

            searchQueue.Enqueue(startPanel);
            visited.Add(startPanel);

            while (searchQueue.Count > 0)
            {
                Panel current = searchQueue.Dequeue();
                connectedChain.Add(current);

                // 현재 필드의 모든 패널 중 같은 타입이면서 인접한 패널 찾기
                foreach (var neighbor in activePanels)
                {
                    if (visited.Contains(neighbor)) continue;
                    if (neighbor.panelType != startPanel.panelType) continue;

                    if (IsNear(current, neighbor))
                    {
                        visited.Add(neighbor);
                        searchQueue.Enqueue(neighbor);
                    }
                }
            }

            return connectedChain;
        }

        private bool IsNear(Panel p1, Panel p2)
        {
            float dist = Vector3.Distance(p1.Position, p2.Position);

            float threshold = connectDistanceMultiplier * ((p1.Radius + p2.Radius) / 2f);
            return dist < threshold;
        }

        private IEnumerator BreakChainSequence(List<Panel> chain)
        {
            // 💡 터뜨릴 때 '중심에서 먼 순서' 혹은 '입력된 순서'대로 터뜨리기 위한 정렬 가능
            // 여기서는 탐색된 순서대로 진행합니다.

            foreach (var panel in chain)
            {
                // 1. 데이터 리스트에서 제거
                activePanels.Remove(panel);

                // 2. View/Effect 연출 실행
                panel.BrokenPanel(); // 반투명 연출
                AudioManager.instance.PlayOneShot(SoundClip.popSound, 0.8f);

                // 3. 실제 오브젝트 파괴
                panel.BreakPanel();

                // 4. 다음 연쇄까지 대기 (시각적 리듬감)
                yield return new WaitForSeconds(chainBreakDelay);
            }
        }
    }
}
