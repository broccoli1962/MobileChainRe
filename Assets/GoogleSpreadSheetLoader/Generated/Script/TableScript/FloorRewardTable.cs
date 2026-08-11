using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "FloorRewardTable", menuName = "Tables/FloorRewardTable")]
public class FloorRewardTable : ScriptableObject, ITable
{
    public List<FloorRewardData> dataList = new List<FloorRewardData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<FloorRewardData>();
		foreach (var item in data)
		{
			FloorRewardData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
