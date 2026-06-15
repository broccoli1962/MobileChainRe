using System.Collections.Generic;

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
    }
}
