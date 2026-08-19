using System.Collections.Generic;

namespace Backend.Object.Management
{
    public struct UserUnitData
    {
        public int unitIds;
        public int unitLevel;
        public int unitSkillLevels;

        //강화 횟수
        public int unitAppendHpPoint;
        public int unitAppendAtkPoint;
        public int unitAppendResiliencePoint;
        public int maxUnitAppendPoint;

        //실 적용 스텟
        public float unitAppendHpValue;
        public float unitAppendAtkValue;
        public float unitAppendResilienceValue;
    }

    public static class UserData
    {
        public static int Level { get; private set; } = 1;
        public static int Energy { get; private set; } = 100;
        public static int MaxEnergy { get; private set; } = 100;
        public static int BestFloorReached { get; private set; }

        public static IReadOnlyList<UserUnitData> OwnedUnitIds => _ownedUnitIds;
        public static IReadOnlyList<int> ClearedStageIds => _clearedStageIds;

        public static void SetBestFloorReached(int floor)
        {
            if (floor > BestFloorReached)
                BestFloorReached = floor;
        }

        // 임시 초기 보유 유닛 (unitId 기준)
        private static readonly List<UserUnitData> _ownedUnitIds = new() {
            new UserUnitData { unitIds = 0, unitLevel = 1 },
            new UserUnitData { unitIds = 1, unitLevel = 1 },
            new UserUnitData { unitIds = 2, unitLevel = 1 },
            new UserUnitData { unitIds = 3, unitLevel = 1 },
            new UserUnitData { unitIds = 4, unitLevel = 1 },
            new UserUnitData { unitIds = 5, unitLevel = 1 },
            new UserUnitData { unitIds = 6, unitLevel = 1 },
            new UserUnitData { unitIds = 7, unitLevel = 1 },
            new UserUnitData { unitIds = 8, unitLevel = 1 },
            new UserUnitData { unitIds = 9, unitLevel = 1 },
            new UserUnitData { unitIds = 10, unitLevel = 1 },
            new UserUnitData { unitIds = 11, unitLevel = 1 },
            new UserUnitData { unitIds = 12, unitLevel = 1 },
            new UserUnitData { unitIds = 13, unitLevel = 1 },
            new UserUnitData { unitIds = 14, unitLevel = 1 },
            new UserUnitData { unitIds = 15, unitLevel = 1 },
            new UserUnitData { unitIds = 16, unitLevel = 1 },
            new UserUnitData { unitIds = 17, unitLevel = 1 },
            new UserUnitData { unitIds = 18, unitLevel = 1 },
            new UserUnitData { unitIds = 19, unitLevel = 1 },
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
            _ownedUnitIds.Add(new UserUnitData { unitIds = unitId, unitLevel = 1 });
        }
    }
}
