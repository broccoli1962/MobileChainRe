using Backend.AddressableKey;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.PanelObject;
using Backend.Util;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Backend.Object.Controller
{
    public class PuzzleController : CachedMonobehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int maxPanelCount = 30;
        [SerializeField] private float spawnInterval = 0.05f;
        [SerializeField] private int preloadCount = 30;

        private Pooling<Panel> panelPool;
        private CancellationTokenSource spawnCts;

        public async UniTask Initialize()
        {
            panelPool = await ObjectPoolManager.GetOrCreatePoolAsync<Panel>(
                AddressableKeys.InGame.Get("Panel"),
                preloadCount,
                parent: CachedTransform,
                defaultCapacity: maxPanelCount,
                maxSize: 100,
                onGet: p => p.gameObject.SetActive(true),
                onRelease: p => p.gameObject.SetActive(false));

            PuzzleSystem.OnPanelBroken = ReleasePanel;
        }

        public void StartSpawning()
        {
            StopSpawning();
            spawnCts = new CancellationTokenSource();
            PanelDropRoutine(spawnCts.Token).Forget();
        }

        public void StopSpawning()
        {
            if (spawnCts != null)
            {
                spawnCts.Cancel();
                spawnCts.Dispose();
                spawnCts = null;
            }
        }

        public void ReleasePanel(Panel panel) => panelPool?.Release(panel);

        private async UniTaskVoid PanelDropRoutine(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (PuzzleSystem.ActivePanelCount < maxPanelCount)
                {
                    SpawnPanel();
                }

                await UniTask.Delay(TimeSpan.FromSeconds(spawnInterval), cancellationToken: token);
            }
        }

        private void SpawnPanel()
        {
            var panel = panelPool?.Get();
            if (panel == null) return;

            panel.transform.position = CachedTransform.position;
            PuzzleSystem.RegisterPanel(panel);
        }

        private void OnDestroy()
        {
            StopSpawning();

            if (PuzzleSystem.OnPanelBroken == ReleasePanel)
                PuzzleSystem.OnPanelBroken = null;
        }
    }
}
