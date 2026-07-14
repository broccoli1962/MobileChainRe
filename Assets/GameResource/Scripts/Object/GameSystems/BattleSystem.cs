using R3;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.CharacterObject;
using Backend.Object.MonsterObject;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    public static class BattleSystem
    {
        private static readonly CompositeDisposable subscriptions = new CompositeDisposable();

        private const float DamagePerBrokenPanel = 10f;
        private const int HitIntervalMs = 150;

        private static int _totalBrokenCount;
        private static readonly Dictionary<PanelType, int> _brokenCountByType = new Dictionary<PanelType, int>();

        public static int TotalBrokenCount => _totalBrokenCount;

        public static void Initialize()
        {
            Dispose();

            PuzzleSystem.OnChainBroken
                .Subscribe(OnChainBroken)
                .AddTo(subscriptions);
        }

        public static void Dispose()
        {
            subscriptions.Clear();
            _totalBrokenCount = 0;
            _brokenCountByType.Clear();
        }

        private static void OnChainBroken(ChainBrokenInfo info)
        {
            _totalBrokenCount += info.TotalCount;

            foreach (var kvp in info.CountByType)
            {
                _brokenCountByType.TryGetValue(kvp.Key, out int cur);
                _brokenCountByType[kvp.Key] = cur + kvp.Value;
            }
        }

        private static int GetAttackBrokenCount(){
            int attackCount = _totalBrokenCount - GetBrokenCount(PanelType.heart);
            return Mathf.Max(0, attackCount);
        }

        public static async UniTask ExcutePlayerAttackAsync(CancellationToken token)
        {
            ApplyHeartHeal();

            var monster = MonsterSystem.ResolveTarget();
            int attackCount = GetAttackBrokenCount();

            _totalBrokenCount = 0;
            _brokenCountByType.Clear();

            if (monster == null || attackCount <= 0)
                return;


            bool layerBroken = false;

            for (int i = 0; i < attackCount; i++)
            {
                if (monster.IsDefeated)
                    break;

                // 한 번의 공격(턴)은 최대 1개 레이어만 파괴한다. 레이어 전환 이후의 데미지는 폐기(오버킬 방지)하되,
                // 남은 히트의 타격 연출은 그대로 재생해 공격이 중간에 끊긴 것처럼 보이지 않게 한다.
                if (!layerBroken)
                {
                    int phaseDelta = monster.TakeDamage(DamagePerBrokenPanel);
                    if (phaseDelta > 0)
                        layerBroken = true;
                }
                
                AttackVfxSystem.PlayPlayerAttack(monster);
                await monster.PlayHitReactionAsync(token);

                if (i < attackCount - 1 && !monster.IsDefeated)
                    await UniTask.Delay(HitIntervalMs, cancellationToken: token);
            }
        }

        public static int GetBrokenCount(PanelType type)
            => _brokenCountByType.TryGetValue(type, out int v) ? v : 0;

        private static void ApplyHeartHeal()
        {
            int heartCount = GetBrokenCount(PanelType.heart);
            if (heartCount <= 0 || CharacterSystem.Count <= 0)
                return;

            if (CharacterSystem.GetCharacter(1) is not CharacterSlot frontUnit)
                return;

            float healAmount = heartCount * frontUnit.UnitData.unitResilience * 0.5f;
            PartySystem.Heal(healAmount);
        }
    }
}
