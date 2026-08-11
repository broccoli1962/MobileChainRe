using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MonsterData : IData
{
    public int monsterId => _monsterId;
    [SerializeField] private int _monsterId;

    public string monsterName => _monsterName;
    [SerializeField] private string _monsterName;

    public PanelType monsterType => _monsterType;
    [SerializeField] private PanelType _monsterType;

    public float monsterDamage => _monsterDamage;
    [SerializeField] private float _monsterDamage;

    public float monsterHealth => _monsterHealth;
    [SerializeField] private float _monsterHealth;

    public int defaultbehaviorId => _defaultbehaviorId;
    [SerializeField] private int _defaultbehaviorId;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_monsterId = int.Parse(data[0]);
		}
		_monsterName = data.Count > 1 ? data[1] : string.Empty;
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_monsterType = PanelType.Parse<PanelType>(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_monsterDamage = float.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_monsterHealth = float.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_defaultbehaviorId = int.Parse(data[5]);
		}
	}
}
