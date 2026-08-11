using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class SpawnGroupData : IData
{
    public int spawnGroupId => _spawnGroupId;
    [SerializeField] private int _spawnGroupId;

    public int spawnSlot => _spawnSlot;
    [SerializeField] private int _spawnSlot;

    public int monsterId => _monsterId;
    [SerializeField] private int _monsterId;

    public int behaviorSetId => _behaviorSetId;
    [SerializeField] private int _behaviorSetId;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_spawnGroupId = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_spawnSlot = int.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_monsterId = int.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_behaviorSetId = int.Parse(data[3]);
		}
	}
}
