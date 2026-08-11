using System.Collections.Generic;

namespace Backend.Object.Management
{
    /// <summary>
    /// 유닛/파티 MaxHp 계산. Classic·Quest 세션이 공유한다.
    /// </summary>
    public static class UnitHpCalculator
    {
        /// <summary>유닛 1명의 HP 기여분.</summary>
        public static float CalcUnitMaxHp(UserUnitData unit)
        {
            var data = TableManager.GetUnitData(unit.unitIds);
            if (data == null) return 0f;
            return data.unithealth + unit.unitAppendHpValue;
        }

        /// <summary>파티 MaxHp 합.</summary>
        public static float CalcMaxHp(IReadOnlyList<UserUnitData> party)
        {
            float total = 0f;
            if (party == null) return 0f;
            foreach (var unit in party)
                total += CalcUnitMaxHp(unit);
            return total;
        }
    }
}
