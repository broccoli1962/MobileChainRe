using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace Backend.Object.UI
{
    public class FailPanelPresenter : UIPresenter<FailPanel>
    {
        /// <summary>
        /// 실패 문구를 View에 반영한다.
        /// </summary>
        public void Refresh()
        {
            if (View.TitleText == null) return;

            switch (ActiveSession.Current)
            {
                case ClassicGameSession classic:
                    View.TitleText.text = $"Failed — Floor {classic.CurrentFloor}";
                    break;
                case QuestGameSession quest:
                    View.TitleText.text = $"Failed — Floor {quest.CurrentFloor}";
                    break;
                default:
                    View.TitleText.text = "Failed";
                    break;
            }
        }

        /// <summary>
        /// 로비로 복귀한다.
        /// </summary>
        public void OnToLobbyClicked()
        {
            ActiveSession.AbortToLobbyAsync().Forget();
        }

        /// <summary>
        /// 동일 파티로 GameScene을 다시 시작한다.
        /// </summary>
        public void OnRetryClicked()
        {
            ActiveSession.RetryAsync().Forget();
        }
    }
}
