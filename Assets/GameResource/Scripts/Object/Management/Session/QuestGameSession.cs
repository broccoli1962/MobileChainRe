using System.Collections.Generic;
using Backend.Object.Controller;
using Backend.Object.GameSystems;
using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Management
{
    /// <summary>
    /// Practice 퀘스트 세션. 맵 스폰 테이블 기준 다층 진행·골드 누적·Result/Fail UI를 소유한다.
    /// </summary>
    public sealed class QuestGameSession : IGameSession
    {
        /// <summary>층 클리어 시 런타임 누적용 임시 골드. 최종 ResultPanel 정산과는 별개.</summary>
        private const int PlaceholderGoldPerFloor = 10;

        public SessionMode Mode => SessionMode.Quest;
        public int QuestMapId { get; }
        public QuestDifficulty SelectedDifficulty { get; }
        public int CurrentFloor { get; private set; }
        public int MaxFloor { get; private set; }
        public int Gold { get; private set; }
        public IReadOnlyList<UserUnitData> Party { get; private set; } = new List<UserUnitData>();

        private readonly Subject<(int floor, int gold)> _onProgressChanged = new();
        private CompositeDisposable _subscriptions;
        private bool _resultShown;

        public QuestGameSession(int questMapId, QuestDifficulty difficulty)
        {
            QuestMapId = questMapId;
            SelectedDifficulty = difficulty;
        }

        public void BindParty(IReadOnlyList<UserUnitData> party)
        {
            Party = new List<UserUnitData>(party);
            CurrentFloor = 0;
            MaxFloor = 0;
            Gold = 0;
            NotifyProgress();
        }

        public void BootstrapPartyHp()
        {
            var max = UnitHpCalculator.CalcMaxHp(Party);
            PartySystem.Setup(max, max);
        }

        public async UniTask InitMonstersAsync(MonsterController controller)
        {
            await controller.PrepareQuestAsync(QuestMapId);
            MaxFloor = controller.QuestFloorCount;
            NotifyProgress();
        }

        public void SpawnInitialFloor(MonsterController controller)
        {
            if (MaxFloor <= 0)
            {
                Debug.LogWarning($"[QuestGameSession] No floors for questMapId={QuestMapId} — StageClear.");
                GameManager.StageClear();
                return;
            }

            controller.SpawnQuestNextFloor();
            CurrentFloor = controller.CurrentQuestFloorDisplay;
            NotifyProgress();
        }

        public void OnAllMonstersDefeated(MonsterController controller)
        {
            Gold += PlaceholderGoldPerFloor;
            NotifyProgress();

            if (controller.HasNextQuestFloor)
            {
                controller.SpawnQuestNextFloor();
                CurrentFloor = controller.CurrentQuestFloorDisplay;
                Debug.Log($"[QuestGameSession] Floor clear → {CurrentFloor}/{MaxFloor} gold={Gold}");
                NotifyProgress();
                return;
            }

            Debug.Log($"[QuestGameSession] Quest clear floor={CurrentFloor}/{MaxFloor} gold={Gold}");
            GameManager.StageClear();
        }

        public void OnGameplayStarted()
        {
            OnGameplayEnded();
            _subscriptions = new CompositeDisposable();
            _resultShown = false;

            GameManager.OnStateChanged
                .Subscribe(OnGameStateChanged)
                .AddTo(_subscriptions);
        }

        public void OnGameplayEnded()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
            _resultShown = false;
        }

        public void End()
        {
            OnGameplayEnded();
            CurrentFloor = 0;
            MaxFloor = 0;
            Gold = 0;
            Party = new List<UserUnitData>();
            NotifyProgress();
        }

        public bool TryGetProgressHud(out SessionProgressHud hud)
        {
            hud = new SessionProgressHud(CurrentFloor, MaxFloor, Gold, _onProgressChanged);
            return true;
        }

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.GameOver:
                    ShowFailAsync().Forget();
                    break;
                case GameState.Clear:
                    SettleMeta();
                    ShowResultAsync().Forget();
                    break;
            }
        }

        private void SettleMeta()
        {
            UserData.ClearStage(QuestMapId);
        }

        private async UniTaskVoid ShowResultAsync()
        {
            if (_resultShown) return;
            _resultShown = true;

            var panel = await UIManager.OpenAsync<ResultPanel>();
            if (panel == null)
            {
                Debug.LogWarning("[QuestGameSession] ResultPanel open failed — return to lobby.");
                await ActiveSession.AbortToLobbyAsync();
            }
        }

        private async UniTaskVoid ShowFailAsync()
        {
            if (_resultShown) return;
            _resultShown = true;

            var panel = await UIManager.OpenAsync<FailPanel>();
            if (panel == null)
            {
                Debug.LogWarning("[QuestGameSession] FailPanel open failed — return to lobby.");
                await ActiveSession.AbortToLobbyAsync();
            }
        }

        private void NotifyProgress()
        {
            _onProgressChanged.OnNext((CurrentFloor, Gold));
        }
    }
}
