using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnGroupTable", menuName = "Tables/SpawnGroupTable")]
public class SpawnGroupTable : ScriptableObject, ITable
{
    public List<SpawnGroupData> dataList = new List<SpawnGroupData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<SpawnGroupData>();
		foreach (var item in data)
		{
			SpawnGroupData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
