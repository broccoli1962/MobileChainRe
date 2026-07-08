using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitSkillTable", menuName = "Tables/UnitSkillTable")]
public class UnitSkillTable : ScriptableObject, ITable
{
    public List<UnitSkillData> dataList = new List<UnitSkillData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<UnitSkillData>();
		foreach (var item in data)
		{
			UnitSkillData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
