using Cysharp.Threading.Tasks;
using Backend.Object.UI;

namespace Backend.Object.Management
{
    /// <summary>
    /// 로비(MainScene) 진입점. 게임 전용 시스템(Input/Puzzle/Battle)은 켜지 않는다.
    /// </summary>
    public sealed class LobbyContext : SceneContext
    {
        protected override UniTask OnEnterAsync()
        {
            UIManager.CloseAllUI();
            UIManager.OpenAsync<LobbyPanel>().Forget();
            UIManager.OpenAsync<TopNavBar>().Forget();
            UIManager.OpenAsync<BottomNavBar>().Forget();
            return UniTask.CompletedTask;
        }
    }
}
