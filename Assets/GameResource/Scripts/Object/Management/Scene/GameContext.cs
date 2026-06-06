using Cysharp.Threading.Tasks;

namespace Backend.Object.Management
{
    /// <summary>
    /// 게임(GameScene) 진입점. 퍼즐/배틀 등 게임 전용 시스템을 켜고, 이탈 시 정리한다.
    /// </summary>
    public sealed class GameContext : SceneContext
    {
        protected override UniTask OnEnterAsync()
        {
            UIManager.CloseAllUI();

            UIManager.OpenAsync<TestBackPanel>().Forget();
            GameManager.StartGameplay();
            return UniTask.CompletedTask;
        }

        protected override void OnExit()
        {
            GameManager.EndGameplay();
        }
    }
}
