using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class MonsterSpawnData : IData
{
    public int questMapId => _questMapId;
    [SerializeField] private int _questMapId;

    public int questFloor => _questFloor;
    [SerializeField] private int _questFloor;

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
			_questMapId = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_questFloor = int.Parse(data[1]);
		}
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_spawnSlot = int.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_monsterId = int.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_behaviorSetId = int.Parse(data[4]);
		}
	}
}
