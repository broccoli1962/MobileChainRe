using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterBehaviorTable", menuName = "Tables/MonsterBehaviorTable")]
public class MonsterBehaviorTable : ScriptableObject, ITable
{
    public List<MonsterBehaviorData> dataList = new List<MonsterBehaviorData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<MonsterBehaviorData>();
		foreach (var item in data)
		{
			MonsterBehaviorData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
