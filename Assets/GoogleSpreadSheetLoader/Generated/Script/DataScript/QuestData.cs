using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class QuestData : IData
{
    public int questId => _questId;
    [SerializeField] private int _questId;

    public string questName => _questName;
    [SerializeField] private string _questName;

    public string questDescript => _questDescript;
    [SerializeField] private string _questDescript;

    public int questMapId => _questMapId;
    [SerializeField] private int _questMapId;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_questId = int.Parse(data[0]);
		}
		_questName = data.Count > 1 ? data[1] : string.Empty;
		_questDescript = data.Count > 2 ? data[2] : string.Empty;
		if (data.Count > 3 && !string.IsNullOrEmpty(data[3]))
		{
			_questMapId = int.Parse(data[3]);
		}
	}
}
