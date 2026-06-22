using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        private readonly Dictionary<int, UnitData> _dicUnit = new();

        private void CreateUnitDict()
        {
            _dicUnit.Clear();
            foreach (var data in _tableLinker.UnitTable.dataList)
            {
                _dicUnit.TryAdd(data.unitId, data);
            }
        }

        public static UnitData GetUnitData(int unitId){
            if (Instance._dicUnit.TryGetValue(unitId, out var data))
                return data;

            Debug.LogWarning($"[TableManager] UnitData not found: {unitId}");
            return null;
        }
    }
}
