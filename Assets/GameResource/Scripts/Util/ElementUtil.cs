namespace Backend.Util
{
    /// <summary>
    /// 속성 상성 배율. 순환: fire → grass → light → water → fire (화살표 방향이 유리).
    /// UnitType/PanelType enum 정수값과 전투 순환이 다르므로 int 캐스트로 계산하지 않는다.
    /// </summary>
    public static class ElementUtil
    {
        private const float AdvantageMultiplier = 1.5f;
        private const float DisadvantageMultiplier = 0.5f;

        public static float Multiplier(PanelType attackerType, UnitType defenderType)
            => Multiplier(CycleIndex(attackerType), CycleIndex(defenderType));

        public static float Multiplier(UnitType attackerType, PanelType defenderType)
            => Multiplier(CycleIndex(attackerType), CycleIndex(defenderType));

        private static float Multiplier(int atk, int def)
        {
            if (atk < 0 || def < 0) return 1f;
            if (def == (atk + 1) % 4) return AdvantageMultiplier;
            if (atk == (def + 1) % 4) return DisadvantageMultiplier;
            return 1f;
        }

        private static int CycleIndex(PanelType type) => type switch
        {
            PanelType.fire => 0,
            PanelType.grass => 1,
            PanelType.light => 2,
            PanelType.water => 3,
            _ => -1,
        };

        private static int CycleIndex(UnitType type) => type switch
        {
            UnitType.fire => 0,
            UnitType.grass => 1,
            UnitType.light => 2,
            UnitType.water => 3,
            _ => throw new System.NotImplementedException(),
        };
    }
}
