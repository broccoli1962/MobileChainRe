using System.Collections.Generic;

namespace Backend.Object.Management
{
    public struct UserUnitData
    {
        public int unitIds;
        public int unitLevels;
    }

    public static class UserData
    {
        public static int Level { get; private set; } = 1;
        public static int Energy { get; private set; } = 100;
        public static int MaxEnergy { get; private set; } = 100;

        public static IReadOnlyList<UserUnitData> OwnedUnitIds => _ownedUnitIds;
        public static IReadOnlyList<int> ClearedStageIds => _clearedStageIds;

        // 임시 초기 보유 유닛 (unitId 기준)
        private static readonly List<UserUnitData> _ownedUnitIds = new() {
            new UserUnitData { unitIds = 0, unitLevels = 2 },
            new UserUnitData { unitIds = 0, unitLevels = 1 },
            new UserUnitData { unitIds = 0, unitLevels = 1 },
            new UserUnitData { unitIds = 0, unitLevels = 1 },
            new UserUnitData { unitIds = 0, unitLevels = 1 },
        };

        private static readonly List<int> _clearedStageIds = new();

        public static void SetLevel(int level)
        {
            Level = level;
        }

        public static bool ConsumeEnergy(int amount)
        {
            if (Energy < amount) return false;
            Energy -= amount;
            return true;
        }

        public static void RestoreEnergy(int amount)
        {
            Energy = System.Math.Min(Energy + amount, MaxEnergy);
        }

        public static void ClearStage(int questMapId)
        {
            if (!_clearedStageIds.Contains(questMapId))
                _clearedStageIds.Add(questMapId);
        }

        public static bool IsStageClear(int questMapId)
        {
            return _clearedStageIds.Contains(questMapId);
        }

        public static void AddUnit(int unitId)
        {
            _ownedUnitIds.Add(new UserUnitData { unitIds = unitId, unitLevels = 1 });
        }
    }
}
