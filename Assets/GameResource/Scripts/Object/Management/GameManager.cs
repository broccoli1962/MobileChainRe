using Backend.Object.GameSystems;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Management
{
    public class GameManager : SingletonGameObject<GameManager>
    {
        private readonly ReactiveProperty<GameState> _state = new(GameState.Ready);

        public GameState CurrentState => _state.Value;
        public static Observable<GameState> OnStateChanged => Instance._state;

        protected override void OnAwake()
        {
            base.OnAwake();

            Application.targetFrameRate = 60;
        }

        private void Initialize_Internal()
        {
            InputSystem.Initialize();
            PuzzleSystem.Initialize();
            BattleSystem.Initialize();

            StartGame();
        }

        public static UniTask Initialize()
        {
            Instance.Initialize_Internal();
            return UniTask.CompletedTask;
        }

        private void StartGame()
        {
            _state.Value = GameState.Playing;
        }

        public void GameOver()
        {
            _state.Value = GameState.GameOver;
        }
    }
}
