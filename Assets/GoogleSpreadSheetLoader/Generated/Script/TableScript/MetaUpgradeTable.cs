using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "MetaUpgradeTable", menuName = "Tables/MetaUpgradeTable")]
public class MetaUpgradeTable : ScriptableObject, ITable
{
    public List<MetaUpgradeData> dataList = new List<MetaUpgradeData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<MetaUpgradeData>();
		foreach (var item in data)
		{
			MetaUpgradeData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
