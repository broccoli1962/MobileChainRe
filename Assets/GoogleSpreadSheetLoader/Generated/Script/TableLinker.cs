using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TableData
{
    [CreateAssetMenu(fileName = "TableLinker", menuName = "Tables/TableLinker")]
    public class TableLinker : ScriptableObject
    {
		 public QuestTable QuestTable;
		 public QuestMapTable QuestMapTable;
		 public MonsterTable MonsterTable;
		 public MonsterSpawnTable MonsterSpawnTable;
		 public MonsterBehaviorTable MonsterBehaviorTable;
		 public MonsterActionTable MonsterActionTable;
		 public SkillTable SkillTable;
		 public UnitTable UnitTable;

    }
}