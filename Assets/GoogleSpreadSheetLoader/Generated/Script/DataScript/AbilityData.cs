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

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_abilityId = int.Parse(data[0]);
		}
		_abilityName = data.Count > 1 ? data[1] : string.Empty;
		_abilityDescript = data.Count > 2 ? data[2] : string.Empty;
	}
}
