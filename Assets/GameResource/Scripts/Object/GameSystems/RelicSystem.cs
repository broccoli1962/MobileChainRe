using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// Classic 런 보유 유물 배율. 상점에 있고 전투/퍼즐 훅에 바로 걸 수 있는 4종만 적용한다.
    /// </summary>
    public static class RelicSystem
    {
        public const string AttackPct = "attackPct";
        public const string HeartWeightPct = "heartWeightPct";
        public const string CpThresholdDeltaKey = "cpThresholdDelta";
        public const string TapBonusKey = "tapBonus";

        public static float AttackMultiplier { get; private set; } = 1f;
        public static float HeartWeightBonus { get; private set; }
        public static int CpThresholdDelta { get; private set; }
        public static int TapBonus { get; private set; }

        public static bool IsSupported(string effectKey)
        {
            return effectKey is AttackPct or HeartWeightPct or CpThresholdDeltaKey or TapBonusKey;
        }

        public static void Initialize()
        {
            Rebuild();
        }

        public static void Dispose()
        {
            AttackMultiplier = 1f;
            HeartWeightBonus = 0f;
            CpThresholdDelta = 0;
            TapBonus = 0;
        }

        public static void Rebuild()
        {
            AttackMultiplier = 1f;
            HeartWeightBonus = 0f;
            CpThresholdDelta = 0;
            TapBonus = 0;

            if (ActiveSession.Current is ClassicGameSession session)
            {
                var owned = session.OwnedRelics;
                for (int i = 0; i < owned.Count; i++)
                {
                    var relic = TableManager.GetRelic(owned[i]);
                    if (relic == null) continue;
                    Apply(relic);
                }
            }

            SkillSystem.RefreshBoardMods();
        }

        private static void Apply(RelicData relic)
        {
            switch (relic.effectKey)
            {
                case AttackPct:
                    AttackMultiplier += relic.effectValue;
                    break;
                case HeartWeightPct:
                    HeartWeightBonus += relic.effectValue;
                    break;
                case CpThresholdDeltaKey:
                    CpThresholdDelta += Mathf.RoundToInt(relic.effectValue);
                    break;
                case TapBonusKey:
                    TapBonus += Mathf.Max(0, Mathf.RoundToInt(relic.effectValue));
                    break;
            }
        }
    }
}
