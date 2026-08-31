using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class UnitSkillEffectData : IData
{
    public int skillId => _skillId;
    [SerializeField] private int _skillId;

    public int skillLevel => _skillLevel;
    [SerializeField] private int _skillLevel;

    public int effectIndex => _effectIndex;
    [SerializeField] private int _effectIndex;

    public SkillEffectType effectType => _effectType;
    [SerializeField] private SkillEffectType _effectType;

    public float effectValue => _effectValue;
    [SerializeField] private float _effectValue;

    public int effectDuration => _effectDuration;
    [SerializeField] private int _effectDuration;

    public EffectType statusType => _statusType;
    [SerializeField] private EffectType _statusType;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_skillId = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_skillLevel = int.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_effectIndex = int.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_effectType = SkillEffectType.Parse<SkillEffectType>(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_effectValue = float.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_effectDuration = int.Parse(data[5]);
		}
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_statusType = EffectType.Parse<EffectType>(data[6]);
		}
	}
}
