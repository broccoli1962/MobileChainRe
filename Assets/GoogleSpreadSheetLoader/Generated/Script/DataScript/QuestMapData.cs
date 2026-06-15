using System;
using System.Collections.Generic;
using TableData;
using UnityEngine;

[Serializable]
public partial class QuestMapData : IData
{
    public int questMapId => _questMapId;
    [SerializeField] private int _questMapId;

    public QuestDifficulty questDifficulty => _questDifficulty;
    [SerializeField] private QuestDifficulty _questDifficulty;

	public void SetData(List<string> data)
	{
		if (data.Count > 0 && !string.IsNullOrEmpty(data[0]))
		{
			_questMapId = int.Parse(data[0]);
		}
		if (data.Count > 1 && !string.IsNullOrEmpty(data[1]))
		{
			_questDifficulty = QuestDifficulty.Parse<QuestDifficulty>(data[1]);
		}
	}
}
