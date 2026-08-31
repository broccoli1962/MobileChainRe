using System.Collections.Generic;
using Backend;
using Backend.Object.CharacterObject;
using Backend.Object.Management;
using Backend.Object.MonsterObject;
using Backend.Util.Interface;
using R3;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// 유닛 Active 스킬. 쿨은 ICharacter 인스턴스 단위이며 층 전환 동안 유지된다.
    /// </summary>
    public static class SkillSystem
    {
        private const float MinDamage = 1f;
        private const float MultiHitScale = 0.4f;

        private struct TimedMod
        {
            public float Value;
            public int RemainingTurns;
        }

        private static readonly Dictionary<ICharacter, int> _remainingCooldown = new();
        private static readonly Subject<(ICharacter character, int remaining)> _onCooldownChanged = new();

        private static TimedMod _heartWeight;
        private static TimedMod _tapBonus;
        private static TimedMod _cpThresholdDelta;

        public static Observable<(ICharacter character, int remaining)> OnCooldownChanged => _onCooldownChanged;
        public static int TapBonus => Mathf.Max(0, Mathf.RoundToInt(_tapBonus.Value));

        public static void Initialize()
        {
            Dispose();
            PrimePartyStartingCooldowns();
        }

        public static void Dispose()
        {
            _remainingCooldown.Clear();
            _heartWeight = default;
            _tapBonus = default;
            _cpThresholdDelta = default;
            PuzzleSystem.ExtraHeartWeight = 0f;
            PuzzleSystem.CpThresholdDelta = 0;
        }

        public static int GetRemainingCooldown(ICharacter character)
        {
            if (character == null)
                return 0;
            return _remainingCooldown.TryGetValue(character, out int remaining) ? remaining : 0;
        }

        public static void EnsureStartingCooldown(ICharacter character)
        {
            if (character == null)
                return;
            if (_remainingCooldown.ContainsKey(character))
                return;

            SetRemainingCooldown(character, GetMaxCooldown(character));
        }

        public static UnitSkillData GetSkill(ICharacter character)
        {
            if (character is not CharacterSlot slot || slot.UnitData == null)
                return null;

            return TableManager.GetUnitSkill(slot.UnitData.unitSkillId, ResolveSkillLevel(character));
        }

        public static bool CanUse(ICharacter character, out string reason)
        {
            reason = null;

            if (character == null)
            {
                reason = "대상 없음";
                return false;
            }

            if (GameManager.CurrentState != GameState.Playing)
            {
                reason = "전투 중이 아님";
                return false;
            }

            if (GameManager.CurrentPhase != GamePhase.PlayerTurn)
            {
                reason = "플레이어 턴에만 사용";
                return false;
            }

            if (PuzzleSystem.IsProcessing)
            {
                reason = "보드 처리 중";
                return false;
            }

            if (StatusSystem.Has(character, EffectType.sleep))
            {
                reason = "수면 상태";
                return false;
            }

            int remaining = GetRemainingCooldown(character);
            if (remaining > 0)
            {
                reason = $"쿨타임 {remaining}턴";
                return false;
            }

            var skill = GetSkill(character);
            if (skill == null)
            {
                reason = "스킬 없음";
                return false;
            }

            var effects = TableManager.GetUnitSkillEffects(skill.skillId, skill.skillLevel);
            if (effects.Count == 0)
            {
                reason = "효과가 없습니다";
                return false;
            }

            return true;
        }

        public static bool TryUse(ICharacter character)
        {
            if (!CanUse(character, out _))
                return false;

            var skill = GetSkill(character);
            var effects = TableManager.GetUnitSkillEffects(skill.skillId, skill.skillLevel);

            for (int i = 0; i < effects.Count; i++)
                ExecuteEffect(character, effects[i]);

            MonsterSystem.CleanUpDefeated();

            SetRemainingCooldown(character, GetMaxCooldown(character));
            return true;
        }

        public static void TickCooldowns()
        {
            if (_remainingCooldown.Count > 0)
            {
                var keys = new List<ICharacter>(_remainingCooldown.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    var character = keys[i];
                    if (character is UnityEngine.Object unityObj && unityObj == null)
                    {
                        _remainingCooldown.Remove(character);
                        continue;
                    }

                    int remaining = _remainingCooldown[character];
                    if (remaining <= 0)
                        continue;

                    remaining = Mathf.Max(0, remaining - 1);
                    SetRemainingCooldown(character, remaining);
                }
            }

            TickTimedMod(ref _heartWeight);
            TickTimedMod(ref _tapBonus);
            TickTimedMod(ref _cpThresholdDelta);
            ApplyBoardMods();
        }

        private static void PrimePartyStartingCooldowns()
        {
            int count = CharacterSystem.Count;
            for (int i = 1; i <= count; i++)
                EnsureStartingCooldown(CharacterSystem.GetCharacter(i));
        }

        private static int GetMaxCooldown(ICharacter character)
        {
            var skill = GetSkill(character);
            return skill != null ? Mathf.Max(0, skill.skillCoolDown) : 0;
        }

        private static void SetRemainingCooldown(ICharacter character, int remaining)
        {
            _remainingCooldown[character] = remaining;
            _onCooldownChanged.OnNext((character, remaining));
        }

        private static void ExecuteEffect(ICharacter caster, UnitSkillEffectData effect)
        {
            switch (effect.effectType)
            {
                case SkillEffectType.damage:
                    DealToTarget(caster, effect.effectValue);
                    break;
                case SkillEffectType.damageAoe:
                    DealToAll(caster, effect.effectValue);
                    break;
                case SkillEffectType.damageMulti:
                    int hits = Mathf.Max(1, Mathf.RoundToInt(effect.effectValue));
                    for (int i = 0; i < hits; i++)
                        DealToTarget(caster, MultiHitScale);
                    break;
                case SkillEffectType.damageBoss:
                    DealToTarget(caster, IsBossFloor() ? effect.effectValue : 1f);
                    break;
                case SkillEffectType.heal:
                    if (caster is CharacterSlot healSlot && healSlot.UnitData != null)
                        PartySystem.Heal(healSlot.UnitData.unitResilience * effect.effectValue);
                    break;
                case SkillEffectType.convertColor:
                    if (caster is CharacterSlot convertSlot && convertSlot.UnitData != null)
                        PuzzleSystem.ConvertRandomPanels(
                            (PanelType)(int)convertSlot.UnitData.unitType,
                            Mathf.Max(0, Mathf.RoundToInt(effect.effectValue)));
                    break;
                case SkillEffectType.heartWeight:
                    _heartWeight = new TimedMod { Value = effect.effectValue, RemainingTurns = effect.effectDuration };
                    ApplyBoardMods();
                    break;
                case SkillEffectType.breakLine:
                    PuzzleSystem.DestroyHorizontalBand(Mathf.Max(0.5f, effect.effectValue));
                    break;
                case SkillEffectType.obstacleClear:
                    PuzzleSystem.DestroyObstacles();
                    break;
                case SkillEffectType.spawnBomb:
                    PuzzleSystem.SpawnBombs(Mathf.Max(1, Mathf.RoundToInt(effect.effectValue)));
                    break;
                case SkillEffectType.spawnCp:
                    if (caster is CharacterSlot cpSlot && cpSlot.UnitData != null)
                        PuzzleSystem.TrySpawnCrashPanel((PanelType)(int)cpSlot.UnitData.unitType);
                    break;
                case SkillEffectType.applyStatus:
                    ApplyCasterStatus(caster, effect);
                    break;
                case SkillEffectType.cleanse:
                    StatusSystem.CleanseHarmfulParty();
                    break;
                case SkillEffectType.tapBonus:
                    _tapBonus = new TimedMod { Value = effect.effectValue, RemainingTurns = effect.effectDuration };
                    break;
                case SkillEffectType.cpThresholdDelta:
                    _cpThresholdDelta = new TimedMod { Value = effect.effectValue, RemainingTurns = effect.effectDuration };
                    ApplyBoardMods();
                    break;
                case SkillEffectType.feverCharge:
                    break;
            }
        }

        private static void ApplyCasterStatus(ICharacter caster, UnitSkillEffectData effect)
        {
            if (effect.statusType == EffectType.none)
                return;

            if (effect.statusType == EffectType.defenseUp)
            {
                int count = CharacterSystem.Count;
                for (int i = 1; i <= count; i++)
                    StatusSystem.Apply(CharacterSystem.GetCharacter(i), effect.statusType, effect.effectValue, effect.effectDuration);
                return;
            }

            StatusSystem.Apply(caster, effect.statusType, effect.effectValue, effect.effectDuration);
        }

        private static void DealToTarget(ICharacter caster, float attackScale)
        {
            var monster = MonsterSystem.ResolveTarget();
            DealToMonster(caster, monster, attackScale);
        }

        private static void DealToAll(ICharacter caster, float attackScale)
        {
            var monsters = MonsterSystem.ActiveMonsters;
            for (int i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];
                if (monster == null || monster.IsDefeated)
                    continue;
                DealToMonster(caster, monster, attackScale);
            }
        }

        private static void DealToMonster(ICharacter caster, Monster monster, float attackScale)
        {
            if (monster == null || monster.IsDefeated)
                return;
            if (caster is not CharacterSlot slot || slot.UnitData == null)
                return;

            float raw = slot.UnitData.unitDamage
                * StatusSystem.AttackMultiplier(caster)
                * attackScale
                * StatusSystem.DamageTakenMultiplier(monster);
            monster.TakeDamage(Mathf.Max(raw, MinDamage));
            monster.PlayHitReaction(default);
        }

        private static bool IsBossFloor()
        {
            if (ActiveSession.Current is not ClassicGameSession classic)
                return false;

            var data = TableManager.GetRunFloor(classic.CurrentFloor);
            return data != null && data.floorType == FloorType.boss;
        }

        private static int ResolveSkillLevel(ICharacter character)
        {
            var party = ActiveSession.Current?.Party;
            if (party == null)
                return 1;

            for (int i = 0; i < party.Count; i++)
            {
                if (party[i].unitIds != character.Id)
                    continue;
                return party[i].unitSkillLevels > 0 ? party[i].unitSkillLevels : 1;
            }

            return 1;
        }

        private static void TickTimedMod(ref TimedMod mod)
        {
            if (mod.RemainingTurns <= 0)
            {
                mod = default;
                return;
            }

            mod.RemainingTurns--;
            if (mod.RemainingTurns <= 0)
                mod = default;
        }

        private static void ApplyBoardMods()
        {
            PuzzleSystem.ExtraHeartWeight = _heartWeight.RemainingTurns > 0 ? _heartWeight.Value : 0f;
            PuzzleSystem.CpThresholdDelta = _cpThresholdDelta.RemainingTurns > 0
                ? Mathf.RoundToInt(_cpThresholdDelta.Value)
                : 0;
        }
    }
}
