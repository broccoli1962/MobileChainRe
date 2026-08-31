using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitSkillEffectTable", menuName = "Tables/UnitSkillEffectTable")]
public class UnitSkillEffectTable : ScriptableObject, ITable
{
    public List<UnitSkillEffectData> dataList = new List<UnitSkillEffectData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<UnitSkillEffectData>();
		foreach (var item in data)
		{
			UnitSkillEffectData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
