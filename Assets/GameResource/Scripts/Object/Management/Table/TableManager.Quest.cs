using System.Collections.Generic;
using TableData;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        private readonly Dictionary<int, QuestData> _dicQuest = new();
        private readonly Dictionary<int, List<QuestMapData>> _dicQuestMap = new();

        private void CreateQuestDict()
        {
            _dicQuestMap.Clear();
            foreach (var data in _tableLinker.QuestMapTable.dataList)
            {
                if (!_dicQuestMap.TryGetValue(data.questMapId, out var list))
                {
                    list = new List<QuestMapData>();
                    _dicQuestMap[data.questMapId] = list;
                }
                list.Add(data);
            }

            _dicQuest.Clear();
            foreach (var data in _tableLinker.QuestTable.dataList)
            {
                _dicQuest.TryAdd(data.questId, data);
            }
        }

        //모든 퀘스트 호출
        public static IReadOnlyCollection<QuestData> GetAllQuests() => Instance._dicQuest.Values;

        //퀘스트 호출
        public static QuestData GetQuest(int questId)
        {
            if (Instance._dicQuest.TryGetValue(questId, out var data))
                return data;

            Debug.LogWarning($"[TableManager] Quest not found: {questId}");
            return null;
        }

        //퀘스트 맵 데이터 호출
        public static List<QuestMapData> GetQuestMapFloors(int questMapId)
        {
            if (Instance._dicQuestMap.TryGetValue(questMapId, out var floors))
                return floors;

            Debug.LogWarning($"[TableManager] QuestMap not found: {questMapId}");
            return null;
        }
    }
}
