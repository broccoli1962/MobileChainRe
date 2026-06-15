using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class UnitData : IData
{
    public int unitId => _unitId;
    [SerializeField] private int _unitId;

    public string unitName => _unitName;
    [SerializeField] private string _unitName;

    public PanelType unitType => _unitType;
    [SerializeField] private PanelType _unitType;

    public UnitRarity unitRarity => _unitRarity;
    [SerializeField] private UnitRarity _unitRarity;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_unitId = int.Parse(data[0]);
		}
		_unitName = data.Count > 1 ? data[1] : string.Empty;
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_unitType = PanelType.Parse<PanelType>(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_unitRarity = UnitRarity.Parse<UnitRarity>(data[3]);
		}
	}
}
