using System.Collections.Generic;
using Backend.Object.Management;

namespace Backend.Object.UI
{
    /// <summary>
    /// QuestView 의 Presenter. TableManager 를 통한 정적 데이터 조회 책임.
    /// </summary>
    public class QuestPresenter : UIPresenter<QuestView>
    {
        public IReadOnlyCollection<QuestData> GetQuests() => TableManager.GetAllQuests();
    }
}
