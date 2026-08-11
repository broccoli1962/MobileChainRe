using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeTable", menuName = "Tables/BiomeTable")]
public class BiomeTable : ScriptableObject, ITable
{
    public List<BiomeData> dataList = new List<BiomeData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<BiomeData>();
		foreach (var item in data)
		{
			BiomeData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
