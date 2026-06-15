using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillTable", menuName = "Tables/SkillTable")]
public class SkillTable : ScriptableObject, ITable
{
    public List<SkillData> dataList = new List<SkillData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<SkillData>();
		foreach (var item in data)
		{
			SkillData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
