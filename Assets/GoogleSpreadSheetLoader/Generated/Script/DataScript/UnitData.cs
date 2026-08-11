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

    public float unitDamage => _unitDamage;
    [SerializeField] private float _unitDamage;

    public float unitDefense => _unitDefense;
    [SerializeField] private float _unitDefense;

    public float unithealth => _unithealth;
    [SerializeField] private float _unithealth;

    public float unitResilience => _unitResilience;
    [SerializeField] private float _unitResilience;

    public UnitType unitType => _unitType;
    [SerializeField] private UnitType _unitType;

    public UnitRarity unitRarity => _unitRarity;
    [SerializeField] private UnitRarity _unitRarity;

    public int unitCost => _unitCost;
    [SerializeField] private int _unitCost;

    public int unitSkillId => _unitSkillId;
    [SerializeField] private int _unitSkillId;

    public int abilityId => _abilityId;
    [SerializeField] private int _abilityId;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_unitId = int.Parse(data[0]);
		}
		_unitName = data.Count > 1 ? data[1] : string.Empty;
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_unitDamage = float.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_unitDefense = float.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_unithealth = float.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_unitResilience = float.Parse(data[5]);
		}
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_unitType = UnitType.Parse<UnitType>(data[6]);
		}
		if (data.Count > 7 && !string.IsNullOrEmpty(data[7]))
		{
			_unitRarity = UnitRarity.Parse<UnitRarity>(data[7]);
		}
		if (data.Count > 8 && !string.IsNullOrEmpty(data[8]))
		{
			_unitCost = int.Parse(data[8]);
		}
		if (data.Count > 9 && !string.IsNullOrEmpty(data[9]))
		{
			_unitSkillId = int.Parse(data[9]);
		}
		if (data.Count > 10 && !string.IsNullOrEmpty(data[10]))
		{
			_abilityId = int.Parse(data[10]);
		}
	}
}
