using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MonsterActionData : IData
{
    public int actionGroupId => _actionGroupId;
    [SerializeField] private int _actionGroupId;

    public int order => _order;
    [SerializeField] private int _order;

    public int turnDelay => _turnDelay;
    [SerializeField] private int _turnDelay;

    public MonsterActionType actionType => _actionType;
    [SerializeField] private MonsterActionType _actionType;

    public EffectType effectType => _effectType;
    [SerializeField] private EffectType _effectType;

    public float actionValue => _actionValue;
    [SerializeField] private float _actionValue;

    public int actionCount => _actionCount;
    [SerializeField] private int _actionCount;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_actionGroupId = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_order = int.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_turnDelay = int.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_actionType = MonsterActionType.Parse<MonsterActionType>(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_effectType = EffectType.Parse<EffectType>(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_actionValue = float.Parse(data[5]);
		}
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_actionCount = int.Parse(data[6]);
		}
	}
}
