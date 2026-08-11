using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class RunFloorData : IData
{
    public int floor => _floor;
    [SerializeField] private int _floor;

    public FloorType floorType => _floorType;
    [SerializeField] private FloorType _floorType;

    public int biomeId => _biomeId;
    [SerializeField] private int _biomeId;

    public int spawnGroupId => _spawnGroupId;
    [SerializeField] private int _spawnGroupId;

    public int rewardTableId => _rewardTableId;
    [SerializeField] private int _rewardTableId;

    public int goldReward => _goldReward;
    [SerializeField] private int _goldReward;

    public float hpScale => _hpScale;
    [SerializeField] private float _hpScale;

    public float atkScale => _atkScale;
    [SerializeField] private float _atkScale;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_floor = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_floorType = FloorType.Parse<FloorType>(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_biomeId = int.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_spawnGroupId = int.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_rewardTableId = int.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_goldReward = int.Parse(data[5]);
		}
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_hpScale = float.Parse(data[6]);
		}
		if (data.Count > 7 && !string.IsNullOrEmpty(data[7]))
		{
			_atkScale = float.Parse(data[7]);
		}
	}
}
