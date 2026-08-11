using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MetaUpgradeData : IData
{
    public int upgradeId => _upgradeId;
    [SerializeField] private int _upgradeId;

    public string upgradeName => _upgradeName;
    [SerializeField] private string _upgradeName;

    public string effectKey => _effectKey;
    [SerializeField] private string _effectKey;

    public float effectValue => _effectValue;
    [SerializeField] private float _effectValue;

    public int costMetaCoin => _costMetaCoin;
    [SerializeField] private int _costMetaCoin;

    public int maxLevel => _maxLevel;
    [SerializeField] private int _maxLevel;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_upgradeId = int.Parse(data[0]);
		}
		_upgradeName = data.Count > 1 ? data[1] : string.Empty;
		_effectKey = data.Count > 2 ? data[2] : string.Empty;
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_effectValue = float.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_costMetaCoin = int.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_maxLevel = int.Parse(data[5]);
		}
	}
}
