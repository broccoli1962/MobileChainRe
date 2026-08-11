using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "RelicTable", menuName = "Tables/RelicTable")]
public class RelicTable : ScriptableObject, ITable
{
    public List<RelicData> dataList = new List<RelicData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<RelicData>();
		foreach (var item in data)
		{
			RelicData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
