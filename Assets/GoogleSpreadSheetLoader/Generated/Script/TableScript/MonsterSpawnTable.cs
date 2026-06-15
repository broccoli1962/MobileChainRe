using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterSpawnTable", menuName = "Tables/MonsterSpawnTable")]
public class MonsterSpawnTable : ScriptableObject, ITable
{
    public List<MonsterSpawnData> dataList = new List<MonsterSpawnData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<MonsterSpawnData>();
		foreach (var item in data)
		{
			MonsterSpawnData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
