using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "RunFloorTable", menuName = "Tables/RunFloorTable")]
public class RunFloorTable : ScriptableObject, ITable
{
    public List<RunFloorData> dataList = new List<RunFloorData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<RunFloorData>();
		foreach (var item in data)
		{
			RunFloorData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
