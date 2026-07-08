using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityTable", menuName = "Tables/AbilityTable")]
public class AbilityTable : ScriptableObject, ITable
{
    public List<AbilityData> dataList = new List<AbilityData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<AbilityData>();
		foreach (var item in data)
		{
			AbilityData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
