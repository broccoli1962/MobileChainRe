using System.Threading;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using R3;

namespace Backend.Object.GameSystems
{
    public static class TurnSystem
    {
        public const int DefaultActionCount = 3;

        private static int _actionPerTurn = DefaultActionCount;
        private static int _actionRemaining;
        private static bool _isEndingTurn;
        private static bool _floorCleared;

        private static readonly ReactiveProperty<int> _actionRemainPoint = new(0);
        public static ReadOnlyReactiveProperty<int> ActionRemainPoint => _actionRemainPoint;

        private static readonly CompositeDisposable _subscriptions = new();
        private static CancellationTokenSource _cts;

        public static void Initialize(int actionPerTurn = DefaultActionCount)
        {
            Dispose();
            _actionPerTurn = actionPerTurn;
            _cts = new CancellationTokenSource();

            PuzzleSystem.OnChainBroken
                .Subscribe(OnPlayerAction)
                .AddTo(_subscriptions);

            MonsterSystem.OnAllDefeated
                .Subscribe(_ => OnFloorCleared())
                .AddTo(_subscriptions);

            StartPlayerTurn();
        }

        public static void Dispose()
        {
            _subscriptions.Clear();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isEndingTurn = false;
            _floorCleared = false;
        }

        public static void StartPlayerTurn()
        {
            _isEndingTurn = false;
            _actionPerTurn = DefaultActionCount + SkillSystem.TapBonus + RelicSystem.TapBonus;
            _actionRemaining = _actionPerTurn;
            _actionRemainPoint.Value = _actionRemaining;
            GameManager.SetPhase(GamePhase.PlayerTurn);
        }

        /// <summary>
        /// 남은 액션을 건너뛰고 플레이어 턴을 종료한다.
        /// 부서진 패널이 있으면 공격/회복 정산 후 몬스터 턴으로 진행한다.
        /// </summary>
        public static void SkipPlayerTurn()
        {
            if (_cts == null) return;
            SkipPlayerTurnAsync(_cts.Token).Forget();
        }

        private static async UniTaskVoid SkipPlayerTurnAsync(CancellationToken token)
        {
            if (!CanSkipPlayerTurn()) return;

            try
            {
                if (PuzzleSystem.IsProcessing)
                    await UniTask.WaitUntil(() => !PuzzleSystem.IsProcessing, cancellationToken: token);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            if (!CanSkipPlayerTurn()) return;

            PuzzleSystem.CancelActiveInput();
            _actionRemaining = 0;
            _actionRemainPoint.Value = 0;
            EndPlayerTurnAsync(token).Forget();
        }

        private static bool CanSkipPlayerTurn()
        {
            if (_isEndingTurn) return false;
            if (GameManager.CurrentState != GameState.Playing) return false;
            return GameManager.CurrentPhase == GamePhase.PlayerTurn;
        }

        private static void OnPlayerAction(ChainBrokenInfo _)
        {
            if (GameManager.CurrentPhase != GamePhase.PlayerTurn)
                return;

            _actionRemaining--;
            _actionRemainPoint.Value = _actionRemaining;

            if (_actionRemaining <= 0)
                EndPlayerTurnAsync(_cts.Token).Forget();
        }

        private static void OnFloorCleared()
        {
            _floorCleared = true;

            // 스킬 막타로 층이 비면 남은 행동을 버리고 턴을 즉시 마무리한다.
            SkipPlayerTurn();
        }

        private static async UniTaskVoid EndPlayerTurnAsync(CancellationToken token)
        {
            if (_isEndingTurn) return;
            _isEndingTurn = true;

            PuzzleSystem.CancelActiveInput();
            GameManager.SetPhase(GamePhase.PlayerActionTurn);
            await BattleSystem.ExcutePlayerAttackAsync(token);
            MonsterSystem.CleanUpDefeated();

            if (!_floorCleared)
            {
                GameManager.SetPhase(GamePhase.MonsterTurn);
                foreach (var monster in MonsterSystem.ActiveMonsters)
                {
                    if (monster.IsDefeated) continue;

                    GameManager.SetPhase(GamePhase.MonsterActionTurn);
                    await monster.AdvanceTurnAsync(token);

                    if (PartySystem.IsAllDefeated)
                    {
                        GameManager.GameOver();
                        return;
                    }
                }
            }

            GameManager.SetPhase(GamePhase.NextTurn);
            await CharacterSystem.AdvanceTurnAsync();
            SkillSystem.TickCooldowns();
            StatusSystem.Tick();
            MonsterSystem.CleanUpDefeated();

            if (PartySystem.IsAllDefeated)
            {
                GameManager.GameOver();
                return;
            }

            if (_floorCleared)
            {
                await AdvanceFloorAsync(token);
                return;
            }

            StartPlayerTurn();
        }

        private static async UniTask AdvanceFloorAsync(CancellationToken token)
        {
            _floorCleared = false;
            GameManager.SetPhase(GamePhase.FloorTransition);

            var session = ActiveSession.Current;
            if (session != null)
            {
                try
                {
                    await session.AdvanceFloorAsync(token);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }

            if (GameManager.CurrentState != GameState.Playing)
                return;

            StartPlayerTurn();
        }
    }
}
