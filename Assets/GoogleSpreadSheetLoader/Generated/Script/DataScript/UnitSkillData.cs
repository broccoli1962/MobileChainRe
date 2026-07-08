using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class UnitSkillData : IData
{
    public int skillId => _skillId;
    [SerializeField] private int _skillId;

    public int skillLevel => _skillLevel;
    [SerializeField] private int _skillLevel;

    public string skillName => _skillName;
    [SerializeField] private string _skillName;

    public string skillDescript => _skillDescript;
    [SerializeField] private string _skillDescript;

    public int skillCoolDown => _skillCoolDown;
    [SerializeField] private int _skillCoolDown;

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
		_skillName = data.Count > 2 ? data[2] : string.Empty;
		_skillDescript = data.Count > 3 ? data[3] : string.Empty;
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_skillCoolDown = int.Parse(data[4]);
		}
	}
}
