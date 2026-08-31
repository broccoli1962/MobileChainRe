using System.Collections.Generic;
using System.Threading;
using Backend.Object.CharacterObject;
using Backend.Object.MonsterObject;
using Backend.Util;
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
        private const float MinDamage = 1f;

        private const int RapidFireIntervalMs = 80; // 연속 타격 간 발사 간격(겹쳐 발사되는 빠른 연사)

        public static async UniTask ExecuteAsync(Monster monster, MonsterActionData action, CancellationToken token)
        {
            switch (action.actionType)
            {
                case MonsterActionType.attack:
                case MonsterActionType.skill:
                    await ApplyHitsAsync(monster, action, 1, token);
                    break;
                case MonsterActionType.multiAttack:
                    await ApplyHitsAsync(monster, action, action.actionCount, token);
                    break;
                case MonsterActionType.effect:
                    ApplyStatusEffect(action);
                    break;
            }
        }

        // 짧은 간격(RapidFireIntervalMs)으로 구체를 겹쳐 발사하는 빠른 연사. 데미지는 각 구체의 착탄
        // 순간에 적용되며, 모든 구체의 연출(비행 + 잔상)이 끝난 뒤에야 ExecuteAsync 가 반환되어 상위
        // 흐름(로테이션 등)과 겹치지 않는다.
        private static async UniTask ApplyHitsAsync(Monster monster, MonsterActionData action, int hitCount, CancellationToken token)
        {
            var tasks = new List<UniTask>(hitCount);

            for (int i = 0; i < hitCount; i++)
            {
                var target = GetRandomCharacterSlot();
                if (target != null)
                {
                    float damage = CalculateHitDamage(monster, action, target);

                    tasks.Add(MonsterAttackVfxSystem.PlayMonsterAttackAsync(
                        monster,
                        target,
                        onImpact: () => { if (damage > 0f) PartySystem.ApplyDamage(damage); },
                        token: token));
                }

                if (i < hitCount - 1)
                    await UniTask.Delay(RapidFireIntervalMs, cancellationToken: token);
            }

            await UniTask.WhenAll(tasks);
        }

        private static float CalculateHitDamage(Monster monster, MonsterActionData action, CharacterSlot target)
        {
            var unitData = target.UnitData;
            float perHit = monster.FinalDamage
                * StatusSystem.AttackMultiplier(monster)
                * ElementUtil.Multiplier(monster.MonsterType, unitData.unitType)
                * action.actionValue
                * StatusSystem.DamageTakenMultiplier(target);
            return Mathf.Max(perHit - unitData.unitDefense, MinDamage);
        }

        private static void ApplyStatusEffect(MonsterActionData action)
        {
            var target = GetRandomCharacterSlot();
            if (target == null) return;

            int duration = action.actionCount > 0 ? action.actionCount : 1;
            StatusSystem.Apply(target, action.effectType, action.actionValue, duration);
        }

        private static CharacterSlot GetRandomCharacterSlot()
        {
            int count = CharacterSystem.Count;
            if (count <= 0) return null;

            int slot = UnityEngine.Random.Range(1, count + 1);
            return CharacterSystem.GetCharacter(slot) as CharacterSlot;
        }
    }
}
