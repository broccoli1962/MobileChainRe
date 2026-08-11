using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace Backend.Object.UI
{
    public class ResultPanelPresenter : UIPresenter<ResultPanel>
    {
        /// <summary>
        /// 최종 정산 정보를 View에 반영한다. (층 중간 정산 없음)
        /// </summary>
        public void Refresh()
        {
            switch (ActiveSession.Current)
            {
                case ClassicGameSession classic:
                    View.TitleText.text = "Classic Clear";
                    View.FloorText.text = $"Floor {classic.CurrentFloor} / {ClassicGameSession.MaxFloor}";
                    View.GoldText.text = $"Gold {classic.Gold}";
                    break;
                case QuestGameSession quest:
                    View.TitleText.text = "Quest Clear";
                    View.FloorText.text = $"Floor {quest.CurrentFloor} / {quest.MaxFloor}";
                    View.GoldText.text = $"Gold {quest.Gold}";
                    break;
                default:
                    View.TitleText.text = "Stage Clear";
                    View.FloorText.text = string.Empty;
                    View.GoldText.text = string.Empty;
                    break;
            }
        }

        /// <summary>
        /// 확인 후 로비로 복귀한다.
        /// </summary>
        public void OnConfirmClicked()
        {
            ActiveSession.AbortToLobbyAsync().Forget();
        }
    }
}
