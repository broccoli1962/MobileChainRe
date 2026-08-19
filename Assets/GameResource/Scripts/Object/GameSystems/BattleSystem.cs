using R3;
using System;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.CharacterObject;
using Backend.Object.MonsterObject;
using Backend.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    public static class BattleSystem
    {
        private static readonly CompositeDisposable subscriptions = new CompositeDisposable();

        private const float DamageVariance = 0.1f; // 히트당 ±10% 균등 난수
        private const float ColorBonusMultiplier = 1.3f; // 선두 속성과 동일 색 패널
        private const float MinDamage = 1f;
        private const int RapidFireIntervalMs = 55; // 연속 히트 간 발사 간격(기관단총 느낌)
        private const int ImpactTailMs = 200; // 마지막 발의 비행/피격 연출이 끝날 시간을 확보

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

        public static async UniTask ExcutePlayerAttackAsync(CancellationToken token)
        {
            ApplyHeartHeal();

            var monster = MonsterSystem.ResolveTarget();
            var attackHits = SnapshotAttackHits();

            _totalBrokenCount = 0;
            _brokenCountByType.Clear();

            if (CharacterSystem.Count < 1
                || CharacterSystem.GetCharacter(1) is not CharacterSlot front
                || front.UnitData == null)
                return;

            if (monster == null || attackHits.Count <= 0)
                return;

            var layerBrokenMonsters = new HashSet<Monster>();

            for (int i = 0; i < attackHits.Count; i++)
            {
                // 현재 타깃이 처치됐으면 살아있는 다음 몬스터로 이어서 공격한다.
                if (monster.IsDefeated)
                {
                    monster = MonsterSystem.ResolveTarget();
                    if (monster == null)
                        break;
                }

                // 몬스터마다 턴당 최대 1개 레이어만 파괴한다. 해당 몬스터의 레이어 전환 이후
                // 데미지는 폐기(오버킬 방지)하되, 남은 히트의 타격 연출은 그대로 재생한다.
                // A의 레이어가 파괴된 뒤 B로 넘어가면 B에게도 데미지가 적용되며, B도 1레이어 파괴 후엔
                // 더 이상 데미지가 들어가지 않는다.
                if (!layerBrokenMonsters.Contains(monster))
                {
                    int phaseDelta = monster.TakeDamage(CalculateHitDamage(front, monster, attackHits[i]));
                    if (phaseDelta > 0)
                        layerBrokenMonsters.Add(monster);
                }

                // 발사는 착탄/피격을 기다리지 않고 계속 이어간다(fire-and-forget). 착탄 시점에
                // 몬스터 피격 연출이 개별적으로 트리거되어, 연속 타격이 끊기지 않고 겹쳐 보인다.
                PlayHitEffectAsync(monster, token).Forget();

                if (i < attackHits.Count - 1)
                    await UniTask.Delay(RapidFireIntervalMs, cancellationToken: token);
            }

            // 마지막 발의 비행/피격 연출이 끝날 시간을 확보한 뒤 턴을 종료한다.
            await UniTask.Delay(ImpactTailMs, cancellationToken: token);
        }

        private static async UniTaskVoid PlayHitEffectAsync(Monster monster, CancellationToken token)
        {
            try
            {
                await AttackVfxSystem.PlayPlayerAttackAsync(monster, token);
                monster.PlayHitReaction(token);
            }
            catch (OperationCanceledException) { }
        }

        private static float CalculateHitDamage(CharacterSlot front, Monster monster, PanelType brokenType)
        {
            float colorBonus = IsColorBonus(front.UnitData.unitType, brokenType)
                ? ColorBonusMultiplier
                : 1f;
            float baseline = front.UnitData.unitDamage
                * colorBonus
                * ElementUtil.Multiplier(front.UnitData.unitType, monster.MonsterType);
            float rolled = baseline * UnityEngine.Random.Range(1f - DamageVariance, 1f + DamageVariance);
            return Mathf.Max(Mathf.Round(rolled), MinDamage);
        }

        private static bool IsColorBonus(UnitType frontType, PanelType brokenType)
        {
            if (brokenType is PanelType.heart or PanelType.obstacle)
                return false;
            return (PanelType)(int)frontType == brokenType;
        }

        private static List<PanelType> SnapshotAttackHits()
        {
            var hits = new List<PanelType>();
            foreach (var kvp in _brokenCountByType)
            {
                if (kvp.Key == PanelType.heart)
                    continue;
                for (int i = 0; i < kvp.Value; i++)
                    hits.Add(kvp.Key);
            }
            return hits;
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
