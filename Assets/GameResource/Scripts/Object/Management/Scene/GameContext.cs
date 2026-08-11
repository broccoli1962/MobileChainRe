using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Backend.AddressableKey;
using Backend.Object.Controller;
using Backend.Object.GameSystems;

namespace Backend.Object.Management
{
    /// <summary>
    /// 게임(GameScene) 진입점. 퍼즐/배틀 등 게임 전용 시스템을 켜고, 이탈 시 정리한다.
    /// </summary>
    public sealed class GameContext : SceneContext
    {
        protected override async UniTask OnEnterAsync()
        {
            await UIManager.CloseAllUIAsync();

            var session = ActiveSession.Current;
            if (session == null)
            {
                Debug.LogError("[GameContext] ActiveSession.Current is null.");
                return;
            }

            var inGameTopHud = await UIManager.OpenAsync<InGameTopHud>();
            var inGameBottomHud = await UIManager.OpenAsync<InGameBottomHud>();

            session.BootstrapPartyHp();

            AudioManager.PreloadSounds();

            var puzzlePrefab = await ResourceManager.LoadComponentAsync<PuzzleController>(AddressableKeys.InGame.Get("PuzzleController"));
            Instantiate(puzzlePrefab);

            var playerPrefab = await ResourceManager.LoadComponentAsync<CharacterSlotController>(AddressableKeys.InGame.Get("CharacterSlotController"));
            var playerController = Instantiate(playerPrefab);
            playerController.SetPlayerContainer(inGameTopHud.PlayerContainer, inGameTopHud.PlayerAnchors);
            await playerController.SpawnPartyAsync(session.Party);

            var monsterPrefab = await ResourceManager.LoadComponentAsync<MonsterController>(AddressableKeys.InGame.Get("MonsterController"));
            var monsterController = Instantiate(monsterPrefab);
            monsterController.SetMonsterContainer(inGameTopHud.MonsterContainer);

            await session.InitMonstersAsync(monsterController);
            Debug.Log($"[GameContext] Mode={session.Mode}");

            var turnPrefab = await ResourceManager.LoadComponentAsync<TurnController>(AddressableKeys.InGame.Get("TurnController"));
            var turnController = Instantiate(turnPrefab);
            turnController.SetTurnContainer(inGameBottomHud.TurnContainer);
            turnController.Initialize();

            GameManager.StartGameplay();

            session.SpawnInitialFloor(monsterController);
            UIManager.HideLoading();
        }

        protected override void OnExit()
        {
            GameManager.EndGameplay();
        }
    }
}
