using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class CharacterData : IData
{
    public int id => _id;
    [SerializeField] private int _id;

    public string name => _name;
    [SerializeField] private string _name;

	public void SetData(List<string> data)
	{
		_id = int.Parse(data[0]);
		_name = data[1].ToString();
	}
}
