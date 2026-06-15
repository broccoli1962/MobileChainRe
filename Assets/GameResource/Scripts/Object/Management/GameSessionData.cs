namespace Backend.Object.Management
{
    public static class GameSessionData
    {
        public static int QuestMapId { get; private set; }
        public static QuestDifficulty SelectedDifficulty { get; private set; }

        public static void SetQuestMap(int questMapId, QuestDifficulty difficulty)
        {
            QuestMapId = questMapId;
            SelectedDifficulty = difficulty;
        }
    }
}
