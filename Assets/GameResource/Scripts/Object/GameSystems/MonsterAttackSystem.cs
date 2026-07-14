using System.Threading;
using Backend.Object.CharacterObject;
using Backend.Object.MonsterObject;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// 몬스터 → 파티 공유 HP 데미지 실행기. BattleSystem(플레이어 → 몬스터)과 대칭 구조.
    /// attack/skill: 랜덤 파티원 1명 1회 타격. multiAttack: 매 타격마다 랜덤 파티원을 actionCount회 타격.
    /// 각 타격은 몬스터-피격자 속성 상성과 피격자 방어력으로 개별 계산 후 합산해 공유 풀에서 차감한다.
    /// </summary>
    public static class MonsterAttackSystem
    {
        private const float ElementAdvantageMultiplier = 1.5f;
        private const float ElementDisadvantageMultiplier = 0.75f;
        private const float MinDamage = 1f;

        public static UniTask ExecuteAsync(Monster monster, MonsterActionData action, CancellationToken token)
        {
            switch (action.actionType)
            {
                case MonsterActionType.attack:
                case MonsterActionType.skill:
                    ApplyHits(monster, action, 1);
                    break;
                case MonsterActionType.multiAttack:
                    ApplyHits(monster, action, action.actionCount);
                    break;
            }

            return UniTask.CompletedTask;
        }

        private static void ApplyHits(Monster monster, MonsterActionData action, int hitCount)
        {
            float total = 0f;

            for (int i = 0; i < hitCount; i++)
            {
                var target = GetRandomCharacterSlot();
                if (target == null) continue;

                total += CalculateHitDamage(monster, action, target);
            }

            if (total > 0f)
                PartySystem.ApplyDamage(total);
        }

        private static float CalculateHitDamage(Monster monster, MonsterActionData action, CharacterSlot target)
        {
            var unitData = target.UnitData;
            float perHit = monster.FinalDamage * ElementMultiplier(monster.MonsterType, unitData.unitType) * action.actionValue;
            return Mathf.Max(perHit - unitData.unitDefense, MinDamage);
        }

        private static CharacterSlot GetRandomCharacterSlot()
        {
            int count = CharacterSystem.Count;
            if (count <= 0) return null;

            int slot = Random.Range(1, count + 1);
            return CharacterSystem.GetCharacter(slot) as CharacterSlot;
        }

        private static float ElementMultiplier(PanelType attackerType, UnitType defenderType)
        {
            int atk = CycleIndex(attackerType);
            int def = CycleIndex(defenderType);
            if (atk < 0 || def < 0) return 1f;

            if (def == (atk + 1) % 4) return ElementAdvantageMultiplier;
            if (atk == (def + 1) % 4) return ElementDisadvantageMultiplier;
            return 1f;
        }

        // 속성 순환: fire → grass → light → water → fire (화살표 방향이 유리)
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
