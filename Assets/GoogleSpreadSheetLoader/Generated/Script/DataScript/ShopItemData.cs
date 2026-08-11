using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class ShopItemData : IData
{
    public int shopItemId => _shopItemId;
    [SerializeField] private int _shopItemId;

    public ShopCategory category => _category;
    [SerializeField] private ShopCategory _category;

    public int itemRefId => _itemRefId;
    [SerializeField] private int _itemRefId;

    public int price => _price;
    [SerializeField] private int _price;

    public float stockWeight => _stockWeight;
    [SerializeField] private float _stockWeight;

    public int unlockFloor => _unlockFloor;
    [SerializeField] private int _unlockFloor;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_shopItemId = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_category = ShopCategory.Parse<ShopCategory>(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_itemRefId = int.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_price = int.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_stockWeight = float.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_unlockFloor = int.Parse(data[5]);
		}
	}
}
