using System.Collections.Generic;
using TableData;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemTable", menuName = "Tables/ShopItemTable")]
public class ShopItemTable : ScriptableObject, ITable
{
    public List<ShopItemData> dataList = new List<ShopItemData>();

	public void SetData(List<List<string>> data)
	{
		dataList = new List<ShopItemData>();
		foreach (var item in data)
		{
			ShopItemData newData = new();
			newData.SetData(item);
			dataList.Add(newData);
		}
	}
}
