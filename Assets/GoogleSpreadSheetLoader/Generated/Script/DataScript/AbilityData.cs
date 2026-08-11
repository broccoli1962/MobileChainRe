using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class AbilityData : IData
{
    public int abilityId => _abilityId;
    [SerializeField] private int _abilityId;

    public string abilityName => _abilityName;
    [SerializeField] private string _abilityName;

    public string abilityDescript => _abilityDescript;
    [SerializeField] private string _abilityDescript;

    public string effectKey => _effectKey;
    [SerializeField] private string _effectKey;

    public float effectValue => _effectValue;
    [SerializeField] private float _effectValue;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_abilityId = int.Parse(data[0]);
		}
		_abilityName = data.Count > 1 ? data[1] : string.Empty;
		_abilityDescript = data.Count > 2 ? data[2] : string.Empty;
		_effectKey = data.Count > 3 ? data[3] : string.Empty;
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_effectValue = float.Parse(data[4]);
		}
	}
}
