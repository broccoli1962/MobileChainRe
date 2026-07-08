using System.Collections.Generic;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        private readonly Dictionary<int, UnitSkillData> _dicSkill = new();

        private void CreateSkillDict()
        {
            _dicSkill.Clear();
            foreach (var data in _tableLinker.UnitSkillTable.dataList)
            {
                _dicSkill.TryAdd(data.skillId, data);
            }
        }
    }
}
