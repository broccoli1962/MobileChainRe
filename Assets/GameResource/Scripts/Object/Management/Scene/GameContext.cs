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

            AudioManager.PreloadSounds();

            //각종 컨트롤러 동적 생성 후 바인딩 기능
            var puzzlePrefab = await ResourceManager.LoadComponentAsync<PuzzleController>(AddressableKeys.InGame.Get("PuzzleController"));
            var puzzleController = Instantiate(puzzlePrefab);

            var playerPrefab = await ResourceManager.LoadComponentAsync<CharacterSlotController>(AddressableKeys.UI.Get("CharacterSlotController"));
            var playerController = Instantiate(playerPrefab);
            playerController.SetPlayerContainer(inGameTopHud.PlayerContainer, inGameTopHud.PlayerAnchors);
            
            var monsterPrefab = await ResourceManager.LoadComponentAsync<MonsterController>(AddressableKeys.UI.Get("MonsterController"));
            var monsterController = Instantiate(monsterPrefab);
            monsterController.SetMonsterContainer(inGameTopHud.MonsterContainer);
            await monsterController.InitializeAsync(GameSessionData.QuestMapId);

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
