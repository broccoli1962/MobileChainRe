using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MonsterBehaviorData : IData
{
    public int behaviorSetId => _behaviorSetId;
    [SerializeField] private int _behaviorSetId;

    public int phaseIndex => _phaseIndex;
    [SerializeField] private int _phaseIndex;

    public float phaseHealth => _phaseHealth;
    [SerializeField] private float _phaseHealth;

    public float phaseDamage => _phaseDamage;
    [SerializeField] private float _phaseDamage;

    public int actionGroupId => _actionGroupId;
    [SerializeField] private int _actionGroupId;

    public float specialThreshold => _specialThreshold;
    [SerializeField] private float _specialThreshold;

    public int specialActionGroupId => _specialActionGroupId;
    [SerializeField] private int _specialActionGroupId;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_behaviorSetId = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_phaseIndex = int.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_phaseHealth = float.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_phaseDamage = float.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_actionGroupId = int.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_specialThreshold = float.Parse(data[5]);
		}
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_specialActionGroupId = int.Parse(data[6]);
		}
	}
}
