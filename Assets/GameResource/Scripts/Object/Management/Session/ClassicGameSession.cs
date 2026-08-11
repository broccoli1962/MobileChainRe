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
    /// 100층 Classic Run 세션 인스턴스. 파티·층·골드·시드·HP·클리어 오케스트레이션을 소유한다.
    /// </summary>
    public sealed class ClassicGameSession : IGameSession
    {
        public const int MaxFloor = 100;

        public SessionMode Mode => SessionMode.Classic;
        public ClassicRunState State { get; private set; } = ClassicRunState.None;
        public int CurrentFloor { get; private set; }
        public float CurrentHp { get; private set; }
        public float MaxHp { get; private set; }
        public int Gold { get; private set; }
        public int Seed { get; private set; }
        public IReadOnlyList<UserUnitData> Party { get; private set; } = new List<UserUnitData>();

        private readonly Subject<(int floor, int gold)> _onProgressChanged = new();
        private CompositeDisposable _subscriptions;
        private bool _resultShown;

        public void BindParty(IReadOnlyList<UserUnitData> party)
        {
            Party = new List<UserUnitData>(party);
            CurrentFloor = 1;
            Gold = 0;
            MaxHp = UnitHpCalculator.CalcMaxHp(Party);
            CurrentHp = MaxHp;
            Seed = System.Environment.TickCount;
            State = ClassicRunState.Active;
            Random.InitState(Seed);
            Debug.Log($"[ClassicGameSession] BindParty floor=1 seed={Seed} maxHp={MaxHp}");
            NotifyProgress();
        }

        public void BootstrapPartyHp()
        {
            PartySystem.Setup(CurrentHp, MaxHp);
        }

        public async UniTask InitMonstersAsync(MonsterController controller)
        {
            await controller.PrepareClassicAsync();
        }

        public void SpawnInitialFloor(MonsterController controller)
        {
            controller.SpawnClassicFloor(CurrentFloor);
        }

        public void OnAllMonstersDefeated(MonsterController controller)
        {
            if (State != ClassicRunState.Active) return;

            SyncHp(PartySystem.CurrentHp);

            var clearedFloor = CurrentFloor;
            var floorData = TableManager.GetRunFloor(clearedFloor);
            var gold = floorData?.goldReward ?? 0;

            AdvanceFloor(gold);

            if (State == ClassicRunState.Cleared)
            {
                GameManager.StageClear();
                return;
            }

            Debug.Log($"[ClassicGameSession] Floor {clearedFloor} clear → {CurrentFloor} gold={Gold} hp={CurrentHp}");
            controller.SpawnClassicFloor(CurrentFloor);
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
            State = ClassicRunState.None;
            CurrentFloor = 0;
            CurrentHp = 0f;
            MaxHp = 0f;
            Gold = 0;
            Seed = 0;
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
                    SyncHp(PartySystem.CurrentHp);
                    State = ClassicRunState.Defeated;
                    NotifyProgress();
                    SettleMeta();
                    ShowFailAsync().Forget();
                    break;
                case GameState.Clear:
                    if (State != ClassicRunState.Cleared)
                        State = ClassicRunState.Cleared;
                    NotifyProgress();
                    SettleMeta();
                    ShowResultAsync().Forget();
                    break;
            }
        }

        private void SyncHp(float currentHp)
        {
            CurrentHp = Mathf.Clamp(currentHp, 0f, MaxHp);
        }

        private void AdvanceFloor(int goldReward)
        {
            Gold += goldReward;
            if (CurrentFloor >= MaxFloor)
            {
                State = ClassicRunState.Cleared;
                NotifyProgress();
                return;
            }

            CurrentFloor++;
            NotifyProgress();
        }

        private void SettleMeta()
        {
            if (CurrentFloor > UserData.BestFloorReached)
                UserData.SetBestFloorReached(CurrentFloor);
        }

        private async UniTaskVoid ShowResultAsync()
        {
            if (_resultShown) return;
            _resultShown = true;

            var panel = await UIManager.OpenAsync<ResultPanel>();
            if (panel == null)
            {
                Debug.LogWarning("[ClassicGameSession] ResultPanel open failed — return to lobby.");
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
                Debug.LogWarning("[ClassicGameSession] FailPanel open failed — return to lobby.");
                await ActiveSession.AbortToLobbyAsync();
            }
        }

        private void NotifyProgress()
        {
            _onProgressChanged.OnNext((CurrentFloor, Gold));
        }
    }
}
