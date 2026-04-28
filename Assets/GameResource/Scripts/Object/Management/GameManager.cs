using Backend.Object.GameSystems;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using Backend.AddressableKey;
using Backend.Object.PanelObject;

namespace Backend.Object.Management
{
    public class GameManager : SingletonGameObject<GameManager>
    {
        [Header("CurrentState(Debugging)")]
        public GameState CurrentState { get; private set; } = GameState.Ready;

        [Header("SpawnSetting")]
        [SerializeField] private Panel panelPrefab;
        [SerializeField] private int maxPanelCount = 30;
        [SerializeField] private float spawnInterval = 0.05f;

        private CancellationTokenSource spawnCts;

        protected override void OnAwake()
        {
            base.OnAwake();
            panelPrefab = ResourceManager.LoadResource<Panel>(AddressableKeys.InGame.Get<Panel>());

            Application.targetFrameRate = 60;
            StartGame();
        }

        public void StartGame()
        {
            CurrentState = GameState.Playing;

            CancelSpawnTask();

            spawnCts = new CancellationTokenSource();

            PanelDropRoutine(spawnCts.Token).Forget();
        }

        private async UniTaskVoid PanelDropRoutine(CancellationToken token)
        {
            while (CurrentState == GameState.Playing && !token.IsCancellationRequested)
            {
                if(PuzzleController.ActivePanelCount < maxPanelCount)
                {
                    CreatePanel();
                }

                await UniTask.Delay(TimeSpan.FromSeconds(spawnInterval), cancellationToken: token);
            }
        }

        private void CreatePanel()
        {
            PuzzleController.RegisterPanel(panelPrefab);
        }

        public void GameOver()
        {
            CurrentState = GameState.GameOver;

            CancelSpawnTask();
        }

        private void CancelSpawnTask()
        {
            if(spawnCts != null)
            {
                spawnCts.Cancel();
                spawnCts.Dispose();
                spawnCts = null;
            }
        }

        private void OnDestroy()
        {
            CancelSpawnTask();
        }
    }
}
