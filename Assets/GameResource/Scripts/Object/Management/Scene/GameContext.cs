using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Backend.AddressableKey;
using Backend.Object.Controller;

namespace Backend.Object.Management
{
    /// <summary>
    /// 게임(GameScene) 진입점. 퍼즐/배틀 등 게임 전용 시스템을 켜고, 이탈 시 정리한다.
    /// </summary>
    public sealed class GameContext : SceneContext
    {
        protected override async UniTask OnEnterAsync()
        {
            UIManager.CloseAllUI();

            var inGameTopHud = await UIManager.OpenAsync<InGameTopHud>();
            var inGameBottomHud = await UIManager.OpenAsync<InGameBottomHud>();


            AudioManager.PreloadSounds();

            //각종 컨트롤러 동적 생성 후 바인딩 기능
            var puzzlePrefab = await ResourceManager.LoadComponentAsync<PuzzleController>(AddressableKeys.InGame.Get("PuzzleController"));
            var puzzleController = Instantiate(puzzlePrefab);

            var playerPrefab = await ResourceManager.LoadComponentAsync<CharacterSlotController>(AddressableKeys.InGame.Get("CharacterSlotController"));
            var playerController = Instantiate(playerPrefab);
            playerController.SetPlayerContainer(inGameTopHud.PlayerContainer, inGameTopHud.PlayerAnchors);
            await playerController.SpawnPartyAsync(GameSessionData.PartyUnits);
            
            var monsterPrefab = await ResourceManager.LoadComponentAsync<MonsterController>(AddressableKeys.InGame.Get("MonsterController"));
            var monsterController = Instantiate(monsterPrefab);
            monsterController.SetMonsterContainer(inGameTopHud.MonsterContainer);
            await monsterController.InitializeAsync(GameSessionData.QuestMapId);

            var turnPrefab = await ResourceManager.LoadComponentAsync<TurnController>(AddressableKeys.InGame.Get("TurnController"));
            var turnController = Instantiate(turnPrefab);
            turnController.SetTurnContainer(inGameBottomHud.TurnContainer);
            turnController.Initialize();

            Debug.Log($"QuestMapIdLoad: {GameSessionData.QuestMapId}");

            GameManager.StartGameplay();

            monsterController.SpawnNextFloor();
        }

        protected override void OnExit()
        {
            GameManager.EndGameplay();
        }
    }
}
