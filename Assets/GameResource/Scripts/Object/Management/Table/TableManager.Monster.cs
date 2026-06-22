using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {   
        private readonly Dictionary<int, MonsterData> _dicMonster = new();
        private readonly Dictionary<int, List<MonsterSpawnData>> _dicMonsterSpawn = new();
        private readonly Dictionary<int, List<MonsterActionData>> _dicMonsterAction = new();
        private readonly Dictionary<int, List<MonsterBehaviorData>> _dicMonsterBehavior = new();

        private void CreateMonsterDict()
        {
            //몬스터 데이터
            _dicMonster.Clear();
            foreach (var data in _tableLinker.MonsterTable.dataList)
            {
                _dicMonster.TryAdd(data.monsterId, data);
            }

            //몬스터 스폰 데이터
            _dicMonsterSpawn.Clear();
            foreach (var data in _tableLinker.MonsterSpawnTable.dataList)
            {
                if (!_dicMonsterSpawn.TryGetValue(data.questMapId, out var list))
                {
                    list = new List<MonsterSpawnData>();
                    _dicMonsterSpawn[data.questMapId] = list;
                }
                list.Add(data);
            }

            //몬스터 액션 데이터
            _dicMonsterAction.Clear();
            foreach (var data in _tableLinker.MonsterActionTable.dataList)
            {
                if (!_dicMonsterAction.TryGetValue(data.actionGroupId, out var list))
                {
                    list = new List<MonsterActionData>();
                    _dicMonsterAction[data.actionGroupId] = list;
                }
                list.Add(data);
            }

            //몬스터 동작 데이터
            _dicMonsterBehavior.Clear();
            foreach (var data in _tableLinker.MonsterBehaviorTable.dataList)
            {
                if (!_dicMonsterBehavior.TryGetValue(data.behaviorSetId, out var list))
                {
                    list = new List<MonsterBehaviorData>();
                    _dicMonsterBehavior[data.behaviorSetId] = list;
                }
                list.Add(data);
            }

            foreach (var list in _dicMonsterBehavior.Values)
            {
                list.Sort((a, b) => a.phaseIndex.CompareTo(b.phaseIndex));
            }
        }

        public static MonsterData GetMonsterData(int monsterId){
            if (Instance._dicMonster.TryGetValue(monsterId, out var data))
                return data;

            Debug.LogWarning($"[TableManager] MonsterData not found: {monsterId}");
            return null;
        }

        //몬스터 동작 데이터 호출 (phaseIndex 오름차순)
        public static IReadOnlyList<MonsterBehaviorData> GetMonsterBehaviors(int behaviorSetId)
        {
            if (Instance._dicMonsterBehavior.TryGetValue(behaviorSetId, out var list))
                return list;

            Debug.LogWarning($"[TableManager] MonsterBehavior not found: {behaviorSetId}");
            return null;
        }
    }
}
