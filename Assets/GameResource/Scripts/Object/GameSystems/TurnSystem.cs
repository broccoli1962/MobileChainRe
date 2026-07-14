using R3;
using System.Threading;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace Backend.Object.GameSystems
{
    public static class TurnSystem
    {
        public const int DefaultActionCount = 3;

        private static int _actionPerTurn = DefaultActionCount;
        private static int _actionRemaining;

        private static readonly ReactiveProperty<int> _actionRemainPoint = new(0);
        public static ReadOnlyReactiveProperty<int> ActionRemainPoint => _actionRemainPoint;
        
        private static readonly CompositeDisposable _subscriptions = new();
        private static CancellationTokenSource _cts;

        public static void Initialize(int actionPerTurn = DefaultActionCount){
            Dispose();
            _actionPerTurn = actionPerTurn;
            _cts = new CancellationTokenSource();

            PuzzleSystem.OnChainBroken
                .Subscribe(OnPlayerAction)
                .AddTo(_subscriptions);

            StartPlayerTurn();
        }

        public static void Dispose(){
            _subscriptions.Clear();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public static void StartPlayerTurn(){
            _actionRemaining = _actionPerTurn;
            _actionRemainPoint.Value = _actionRemaining;
            GameManager.SetPhase(GamePhase.PlayerTurn);
        }

        private static void OnPlayerAction(ChainBrokenInfo _){
            if(GameManager.CurrentPhase != GamePhase.PlayerTurn)
                return;

            _actionRemaining--;
            _actionRemainPoint.Value = _actionRemaining;

            if(_actionRemaining <= 0)
                EndPlayerTurnAsync(_cts.Token).Forget();
        }

        private static async UniTaskVoid EndPlayerTurnAsync(CancellationToken token){
            GameManager.SetPhase(GamePhase.PlayerActionTurn);
            await BattleSystem.ExcutePlayerAttackAsync(token);

            MonsterSystem.CleanUpDefeated();

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

            GameManager.SetPhase(GamePhase.NextTurn);
            CharacterSystem.AdvanceTurn();

            StartPlayerTurn();
        }
    }
}
