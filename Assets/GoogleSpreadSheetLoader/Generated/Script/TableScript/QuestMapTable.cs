using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestMapTable", menuName = "Tables/QuestMapTable")]
public class QuestMapTable : ScriptableObject, ITable
{
    public List<QuestMapData> dataList = new List<QuestMapData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<QuestMapData>();
		foreach (var item in data)
		{
			QuestMapData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
