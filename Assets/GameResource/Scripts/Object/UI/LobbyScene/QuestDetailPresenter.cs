using System.Collections.Generic;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace Backend.Object.UI
{
    /// <summary>
    /// QuestDetailPanel 의 Presenter. 퀘스트 데이터 소유, 난이도 선택 및 씬 전환 처리.
    /// </summary>
    public class QuestDetailPresenter : UIPresenter<QuestDetailPanel>
    {
        private QuestData _questData;

        public void SetData(QuestData questData)
        {
            _questData = questData;
            View.ShowQuestInfo(questData.questName, questData.questDescript);
            View.BuildDifficultyButtons(GetAvailableDifficulties(questData.questMapId));
        }

        public void OnDifficultySelected(QuestDifficulty difficulty)
        {
            GameSessionData.SetQuestMap(_questData.questMapId, difficulty);
            OpenCharacterPartyPanelAsync().Forget();
        }

        private async UniTaskVoid OpenCharacterPartyPanelAsync()
        {
            await UIManager.OpenAsync<UnitPartyPanel>();
        }

        public List<QuestMapData> GetMapFloors(int questMapId)
            => TableManager.GetQuestMapFloors(questMapId);

        /// <summary>
        /// questMapId 에 존재하는 중복 없는 난이도 목록을 enum 선언 순서(easy→normal→hard)대로 반환한다.
        /// </summary>
        public List<QuestDifficulty> GetAvailableDifficulties(int questMapId)
        {
            var floors = TableManager.GetQuestMapFloors(questMapId);
            var seen = new HashSet<QuestDifficulty>();
            var result = new List<QuestDifficulty>();
            if (floors == null) return result;

            foreach (var floor in floors)
            {
                if (seen.Add(floor.questDifficulty))
                    result.Add(floor.questDifficulty);
            }

            result.Sort((a, b) => ((int)a).CompareTo((int)b));
            return result;
        }
    }
}
