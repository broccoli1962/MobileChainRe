using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TableData
{
    [CreateAssetMenu(fileName = "TableLinker", menuName = "Tables/TableLinker")]
    public class TableLinker : ScriptableObject
    {
		 public MonsterActionTable MonsterActionTable;
		 public QuestTable QuestTable;
		 public QuestMapTable QuestMapTable;
		 public MonsterTable MonsterTable;
		 public MonsterSpawnTable MonsterSpawnTable;
		 public MonsterBehaviorTable MonsterBehaviorTable;
		 public AbilityTable AbilityTable;
		 public UnitTable UnitTable;
		 public UnitSkillTable UnitSkillTable;

    }
}