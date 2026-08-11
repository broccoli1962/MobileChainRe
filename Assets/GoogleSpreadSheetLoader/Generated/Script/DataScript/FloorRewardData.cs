using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class FloorRewardData : IData
{
    public int rewardTableId => _rewardTableId;
    [SerializeField] private int _rewardTableId;

    public int choiceIndex => _choiceIndex;
    [SerializeField] private int _choiceIndex;

    public string rewardType => _rewardType;
    [SerializeField] private string _rewardType;

    public int rewardRefId => _rewardRefId;
    [SerializeField] private int _rewardRefId;

    public int amount => _amount;
    [SerializeField] private int _amount;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_rewardTableId = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_choiceIndex = int.Parse(data[1]);
		}
		_rewardType = data.Count > 2 ? data[2] : string.Empty;
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_rewardRefId = int.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_amount = int.Parse(data[4]);
		}
	}
}
