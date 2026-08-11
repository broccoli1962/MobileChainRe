using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class RelicData : IData
{
    public int relicId => _relicId;
    [SerializeField] private int _relicId;

    public string relicName => _relicName;
    [SerializeField] private string _relicName;

    public RelicRarity relicRarity => _relicRarity;
    [SerializeField] private RelicRarity _relicRarity;

    public string effectKey => _effectKey;
    [SerializeField] private string _effectKey;

    public float effectValue => _effectValue;
    [SerializeField] private float _effectValue;

    public string descript => _descript;
    [SerializeField] private string _descript;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_relicId = int.Parse(data[0]);
		}
		_relicName = data.Count > 1 ? data[1] : string.Empty;
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_relicRarity = RelicRarity.Parse<RelicRarity>(data[2]);
		}
		_effectKey = data.Count > 3 ? data[3] : string.Empty;
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_effectValue = float.Parse(data[4]);
		}
		_descript = data.Count > 5 ? data[5] : string.Empty;
	}
}
