using System.Collections.Generic;
using Backend.Object.MonsterObject;
using Backend.Util.Interface;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// EffectType 상태이상을 파티 슬롯과 몬스터에 같은 정의로 부여·틱·해제한다.
    /// </summary>
    public static class StatusSystem
    {
        private struct StatusInstance
        {
            public float Value;
            public int RemainingTurns;
        }

        private static readonly Dictionary<ICharacter, Dictionary<EffectType, StatusInstance>> _party = new();
        private static readonly Dictionary<Monster, Dictionary<EffectType, StatusInstance>> _monsters = new();

        public static void Initialize()
        {
            Dispose();
        }

        public static void Dispose()
        {
            _party.Clear();
            _monsters.Clear();
        }

        public static void Apply(ICharacter target, EffectType type, float value, int durationTurns)
        {
            if (target == null || type == EffectType.none || durationTurns <= 0)
                return;

            ApplyToMap(_party, target, type, value, durationTurns);
        }

        public static void Apply(Monster target, EffectType type, float value, int durationTurns)
        {
            if (target == null || type == EffectType.none || durationTurns <= 0)
                return;

            ApplyToMap(_monsters, target, type, value, durationTurns);
        }

        public static void CleanseHarmful(ICharacter target)
        {
            if (target == null || !_party.TryGetValue(target, out var map))
                return;

            RemoveHarmful(map);
            if (map.Count == 0)
                _party.Remove(target);
        }

        public static void CleanseHarmfulParty()
        {
            int count = CharacterSystem.Count;
            for (int i = 1; i <= count; i++)
                CleanseHarmful(CharacterSystem.GetCharacter(i));
        }

        public static void Tick()
        {
            TickParty();
            TickMonsters();
        }

        public static bool Has(ICharacter target, EffectType type)
            => target != null && _party.TryGetValue(target, out var map) && map.ContainsKey(type);

        public static bool Has(Monster target, EffectType type)
            => target != null && _monsters.TryGetValue(target, out var map) && map.ContainsKey(type);

        public static float AttackMultiplier(ICharacter target) => AttackMultiplier(GetMap(_party, target));

        public static float AttackMultiplier(Monster target) => AttackMultiplier(GetMap(_monsters, target));

        public static float DamageTakenMultiplier(ICharacter target) => DamageTakenMultiplier(GetMap(_party, target));

        public static float DamageTakenMultiplier(Monster target) => DamageTakenMultiplier(GetMap(_monsters, target));

        private static void ApplyToMap<T>(
            Dictionary<T, Dictionary<EffectType, StatusInstance>> root,
            T target,
            EffectType type,
            float value,
            int durationTurns)
        {
            if (!root.TryGetValue(target, out var map))
            {
                map = new Dictionary<EffectType, StatusInstance>();
                root[target] = map;
            }

            map[type] = new StatusInstance
            {
                Value = value,
                RemainingTurns = durationTurns,
            };
        }

        private static Dictionary<EffectType, StatusInstance> GetMap<T>(
            Dictionary<T, Dictionary<EffectType, StatusInstance>> root,
            T target)
        {
            if (target == null)
                return null;
            return root.TryGetValue(target, out var map) ? map : null;
        }

        private static float AttackMultiplier(Dictionary<EffectType, StatusInstance> map)
        {
            float mul = 1f;
            if (map != null && map.TryGetValue(EffectType.strengthUp, out var up))
                mul *= 1f + up.Value;
            if (map != null && map.TryGetValue(EffectType.strengthDown, out var down))
                mul *= Mathf.Max(0f, 1f - down.Value);
            return mul;
        }

        private static float DamageTakenMultiplier(Dictionary<EffectType, StatusInstance> map)
        {
            float mul = 1f;
            if (map != null && map.TryGetValue(EffectType.defenseUp, out var up))
                mul *= Mathf.Max(0f, 1f - up.Value);
            if (map != null && map.TryGetValue(EffectType.defenseDown, out var down))
                mul *= 1f + down.Value;
            return mul;
        }

        private static void TickParty()
        {
            var expiredOwners = new List<ICharacter>();
            foreach (var kvp in _party)
            {
                if (kvp.Key is UnityEngine.Object unityObj && unityObj == null)
                {
                    expiredOwners.Add(kvp.Key);
                    continue;
                }

                ApplyPoisonParty(kvp.Value);
                TickMap(kvp.Value);
                if (kvp.Value.Count == 0)
                    expiredOwners.Add(kvp.Key);
            }

            for (int i = 0; i < expiredOwners.Count; i++)
                _party.Remove(expiredOwners[i]);
        }

        private static void TickMonsters()
        {
            var expiredOwners = new List<Monster>();
            foreach (var kvp in _monsters)
            {
                if (kvp.Key == null)
                {
                    expiredOwners.Add(kvp.Key);
                    continue;
                }

                if (kvp.Value.TryGetValue(EffectType.poison, out var poison) && !kvp.Key.IsDefeated)
                    kvp.Key.TakeDamage(poison.Value);

                TickMap(kvp.Value);
                if (kvp.Value.Count == 0)
                    expiredOwners.Add(kvp.Key);
            }

            for (int i = 0; i < expiredOwners.Count; i++)
                _monsters.Remove(expiredOwners[i]);
        }

        private static void ApplyPoisonParty(Dictionary<EffectType, StatusInstance> map)
        {
            if (!map.TryGetValue(EffectType.poison, out var poison))
                return;

            PartySystem.ApplyDamage(poison.Value);
        }

        private static void TickMap(Dictionary<EffectType, StatusInstance> map)
        {
            var expired = new List<EffectType>();
            var keys = new List<EffectType>(map.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var type = keys[i];
                var inst = map[type];
                inst.RemainingTurns--;
                if (inst.RemainingTurns <= 0)
                    expired.Add(type);
                else
                    map[type] = inst;
            }

            for (int i = 0; i < expired.Count; i++)
                map.Remove(expired[i]);
        }

        private static void RemoveHarmful(Dictionary<EffectType, StatusInstance> map)
        {
            map.Remove(EffectType.poison);
            map.Remove(EffectType.sleep);
            map.Remove(EffectType.strengthDown);
            map.Remove(EffectType.defenseDown);
        }
    }
}
