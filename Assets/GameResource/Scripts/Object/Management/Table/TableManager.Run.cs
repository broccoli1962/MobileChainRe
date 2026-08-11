using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        private readonly Dictionary<int, RunFloorData> _dicRunFloor = new();
        private readonly Dictionary<int, List<SpawnGroupData>> _dicSpawnGroup = new();

        private void CreateRunDict()
        {
            _dicRunFloor.Clear();
            if (_tableLinker.RunFloorTable?.dataList != null)
            {
                foreach (var data in _tableLinker.RunFloorTable.dataList)
                    _dicRunFloor.TryAdd(data.floor, data);
            }

            _dicSpawnGroup.Clear();
            if (_tableLinker.SpawnGroupTable?.dataList != null)
            {
                foreach (var data in _tableLinker.SpawnGroupTable.dataList)
                {
                    if (!_dicSpawnGroup.TryGetValue(data.spawnGroupId, out var list))
                    {
                        list = new List<SpawnGroupData>();
                        _dicSpawnGroup[data.spawnGroupId] = list;
                    }
                    list.Add(data);
                }

                foreach (var list in _dicSpawnGroup.Values)
                    list.Sort((a, b) => a.spawnSlot.CompareTo(b.spawnSlot));
            }
        }

        public static RunFloorData GetRunFloor(int floor)
        {
            if (Instance._dicRunFloor.TryGetValue(floor, out var data))
                return data;

            Debug.LogWarning($"[TableManager] RunFloorData not found: {floor}");
            return null;
        }

        public static IReadOnlyList<SpawnGroupData> GetSpawnGroup(int spawnGroupId)
        {
            if (Instance._dicSpawnGroup.TryGetValue(spawnGroupId, out var list))
                return list;

            Debug.LogWarning($"[TableManager] SpawnGroup not found: {spawnGroupId}");
            return null;
        }
    }
}
