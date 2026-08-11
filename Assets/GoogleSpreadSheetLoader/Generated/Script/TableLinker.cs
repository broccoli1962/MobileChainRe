using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TableData
{
    [CreateAssetMenu(fileName = "TableLinker", menuName = "Tables/TableLinker")]
    public class TableLinker : ScriptableObject
    {
		 public MonsterActionTable MonsterActionTable;
		 public MonsterBehaviorTable MonsterBehaviorTable;
		 public SpawnGroupTable SpawnGroupTable;
		 public MonsterTable MonsterTable;
		 public RunFloorTable RunFloorTable;
		 public FloorRewardTable FloorRewardTable;
		 public QuestMapTable QuestMapTable;
		 public MonsterSpawnTable MonsterSpawnTable;
		 public UnitTable UnitTable;
		 public RelicTable RelicTable;
		 public BiomeTable BiomeTable;
		 public MetaUpgradeTable MetaUpgradeTable;
		 public AbilityTable AbilityTable;
		 public UnitSkillTable UnitSkillTable;
		 public QuestTable QuestTable;
		 public ShopItemTable ShopItemTable;

    }
}