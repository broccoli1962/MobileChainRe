using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitTable", menuName = "Tables/UnitTable")]
public class UnitTable : ScriptableObject, ITable
{
    public List<UnitData> dataList = new List<UnitData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<UnitData>();
		foreach (var item in data)
		{
			UnitData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
