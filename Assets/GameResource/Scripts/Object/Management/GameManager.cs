using Backend.Object.Controller;
using Backend.Object.GameSystems;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.Management
{
    public class GameManager : SingletonGameObject<GameManager>
    {
        [Header("CurrentState(Debugging)")]
        public GameState CurrentState { get; private set; } = GameState.Ready;

        [SerializeField] private PuzzleController puzzleController;

        protected override void OnAwake()
        {
            base.OnAwake();

            Application.targetFrameRate = 60;
        }

        private async UniTask Initialize_Internal()
        {
            InputSystem.Initialize();
            PuzzleSystem.Initialize();
            BattleSystem.Initialize();

            await puzzleController.Initialize();

            StartGame();
        }

        public static async UniTask Initialize() => await Instance.Initialize_Internal();

        private void StartGame()
        {
            CurrentState = GameState.Playing;

            puzzleController.StartSpawning();
        }

        public void GameOver()
        {
            CurrentState = GameState.GameOver;

            puzzleController.StopSpawning();
        }
    }
}
