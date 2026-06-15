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

        /// <summary>
        /// 앱 전역에서 항상 필요한 코어 초기화. Boot 에서 1회 호출된다.
        /// 게임 전용 시스템(Input/Puzzle/Battle)은 여기서 켜지 않는다.
        /// </summary>
        private async UniTask InitializeCore_Internal()
        {
            await AudioManager.InitMixer();
            TableManager.Init();
        }

        public static UniTask InitializeCore() => Instance.InitializeCore_Internal();

        /// <summary>
        /// 게임 씬 진입 시 게임 전용 시스템을 켜고 플레이 상태로 전환한다.
        /// GameContext 에서 호출된다.
        /// </summary>
        private void StartGameplay_Internal()
        {
            InputSystem.Initialize();
            PuzzleSystem.Initialize();
            BattleSystem.Initialize();

            _state.Value = GameState.PlayerPlaying;
        }

        public static void StartGameplay()
        {
            Instance.StartGameplay_Internal();
        }

        /// <summary>
        /// 게임 씬 이탈 시 게임 전용 시스템을 정리하고 대기 상태로 되돌린다.
        /// GameContext 에서 호출된다.
        /// </summary>
        private void EndGameplay_Internal()
        {
            BattleSystem.Dispose();
            PuzzleSystem.Dispose();
            CharacterSystem.Dispose();
            InputSystem.Dispose();

            _state.Value = GameState.Ready;
        }

        public static void EndGameplay()
        {
            Instance.EndGameplay_Internal();
        }

        public void GameOver()
        {
            _state.Value = GameState.GameOver;
        }
    }
}
