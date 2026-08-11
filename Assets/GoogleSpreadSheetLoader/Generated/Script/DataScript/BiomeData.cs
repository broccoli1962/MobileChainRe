using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class BiomeData : IData
{
    public int biomeId => _biomeId;
    [SerializeField] private int _biomeId;

    public string biomeName => _biomeName;
    [SerializeField] private string _biomeName;

    public float weightFire => _weightFire;
    [SerializeField] private float _weightFire;

    public float weightLight => _weightLight;
    [SerializeField] private float _weightLight;

    public float weightWater => _weightWater;
    [SerializeField] private float _weightWater;

    public float weightGrass => _weightGrass;
    [SerializeField] private float _weightGrass;

    public float weightHeart => _weightHeart;
    [SerializeField] private float _weightHeart;

    public float weightObstacle => _weightObstacle;
    [SerializeField] private float _weightObstacle;

    public float hpMod => _hpMod;
    [SerializeField] private float _hpMod;

    public float atkMod => _atkMod;
    [SerializeField] private float _atkMod;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_biomeId = int.Parse(data[0]);
		}
		_biomeName = data.Count > 1 ? data[1] : string.Empty;
		if (data.Count > 2 && !string.IsNullOrEmpty(data[2]))
		{
			_weightFire = float.Parse(data[2]);
		}
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_weightLight = float.Parse(data[3]);
		}
		if (data.Count > 4 && !string.IsNullOrEmpty(data[4]))
		{
			_weightWater = float.Parse(data[4]);
		}
		if (data.Count > 5 && !string.IsNullOrEmpty(data[5]))
		{
			_weightGrass = float.Parse(data[5]);
		}
		if (data.Count > 6 && !string.IsNullOrEmpty(data[6]))
		{
			_weightHeart = float.Parse(data[6]);
		}
		if (data.Count > 7 && !string.IsNullOrEmpty(data[7]))
		{
			_weightObstacle = float.Parse(data[7]);
		}
		if (data.Count > 8 && !string.IsNullOrEmpty(data[8]))
		{
			_hpMod = float.Parse(data[8]);
		}
		if (data.Count > 9 && !string.IsNullOrEmpty(data[9]))
		{
			_atkMod = float.Parse(data[9]);
		}
	}
}
