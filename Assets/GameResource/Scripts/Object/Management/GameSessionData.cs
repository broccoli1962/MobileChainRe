using System.Collections.Generic;

namespace Backend.Object.Management
{
    public static class GameSessionData
    {
        public static int QuestMapId { get; private set; }
        public static QuestDifficulty SelectedDifficulty { get; private set; }
        public static IReadOnlyList<UserUnitData> PartyUnits { get; private set; } = new List<UserUnitData>();

        public static void SetQuestMap(int questMapId, QuestDifficulty difficulty)
        {
            QuestMapId = questMapId;
            SelectedDifficulty = difficulty;
        }

        public static void SetParty(IReadOnlyList<UserUnitData> party){
            PartyUnits = new List<UserUnitData>(party);
        }
    }
}
