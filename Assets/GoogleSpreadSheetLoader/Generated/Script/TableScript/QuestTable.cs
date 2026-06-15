using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestTable", menuName = "Tables/QuestTable")]
public class QuestTable : ScriptableObject, ITable
{
    public List<QuestData> dataList = new List<QuestData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<QuestData>();
		foreach (var item in data)
		{
			QuestData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
