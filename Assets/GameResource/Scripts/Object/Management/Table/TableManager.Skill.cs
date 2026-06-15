using System.Collections.Generic;
using TableData;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        private readonly Dictionary<int, SkillData> _dicSkill = new();

        private void CreateSkillDict()
        {
            _dicSkill.Clear();
            foreach (var data in _tableLinker.SkillTable.dataList)
            {
                _dicSkill.TryAdd(data.skillId, data);
            }
        }
    }
}
