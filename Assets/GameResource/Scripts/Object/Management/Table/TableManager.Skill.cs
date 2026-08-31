using System;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        private readonly Dictionary<(int skillId, int skillLevel), UnitSkillData> _dicSkill = new();
        private readonly Dictionary<(int skillId, int skillLevel), List<UnitSkillEffectData>> _dicSkillEffects = new();
        private static readonly IReadOnlyList<UnitSkillEffectData> EmptyEffects = Array.Empty<UnitSkillEffectData>();

        private void CreateSkillDict()
        {
            _dicSkill.Clear();
            _dicSkillEffects.Clear();

            if (_tableLinker.UnitSkillTable?.dataList != null)
            {
                foreach (var data in _tableLinker.UnitSkillTable.dataList)
                    _dicSkill.TryAdd((data.skillId, data.skillLevel), data);
            }

            if (_tableLinker.UnitSkillEffectTable?.dataList != null)
            {
                foreach (var data in _tableLinker.UnitSkillEffectTable.dataList)
                {
                    var key = (data.skillId, data.skillLevel);
                    if (!_dicSkillEffects.TryGetValue(key, out var list))
                    {
                        list = new List<UnitSkillEffectData>();
                        _dicSkillEffects[key] = list;
                    }
                    list.Add(data);
                }

                foreach (var list in _dicSkillEffects.Values)
                    list.Sort((a, b) => a.effectIndex.CompareTo(b.effectIndex));
            }
        }

        public static UnitSkillData GetUnitSkill(int skillId, int level)
            => Instance.FindSkill(skillId, level);

        public static IReadOnlyList<UnitSkillEffectData> GetUnitSkillEffects(int skillId, int level)
        {
            var skill = Instance.FindSkill(skillId, level);
            if (skill == null)
                return EmptyEffects;

            if (Instance._dicSkillEffects.TryGetValue((skill.skillId, skill.skillLevel), out var list))
                return list;

            return EmptyEffects;
        }

        private UnitSkillData FindSkill(int skillId, int level)
        {
            int resolvedLevel = level > 0 ? level : 1;
            if (_dicSkill.TryGetValue((skillId, resolvedLevel), out var exact))
                return exact;

            UnitSkillData best = null;
            int bestLevel = -1;
            foreach (var kvp in _dicSkill)
            {
                if (kvp.Key.skillId != skillId)
                    continue;
                if (kvp.Key.skillLevel <= resolvedLevel && kvp.Key.skillLevel > bestLevel)
                {
                    best = kvp.Value;
                    bestLevel = kvp.Key.skillLevel;
                }
            }

            if (best != null)
                return best;

            if (_dicSkill.TryGetValue((skillId, 1), out var levelOne))
                return levelOne;

            Debug.LogWarning($"[TableManager] UnitSkillData not found: {skillId}/{resolvedLevel}");
            return null;
        }
    }
}
