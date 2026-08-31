using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TableData
{
    [CreateAssetMenu(fileName = "TableLinker", menuName = "Tables/TableLinker")]
    public class TableLinker : ScriptableObject
    {
		 public MonsterTable MonsterTable;
		 public MonsterBehaviorTable MonsterBehaviorTable;
		 public QuestTable QuestTable;
		 public QuestMapTable QuestMapTable;
		 public MonsterSpawnTable MonsterSpawnTable;
		 public MonsterActionTable MonsterActionTable;
		 public AbilityTable AbilityTable;
		 public UnitTable UnitTable;
		 public UnitSkillTable UnitSkillTable;
		 public UnitSkillEffectTable UnitSkillEffectTable;
		 public BiomeTable BiomeTable;
		 public SpawnGroupTable SpawnGroupTable;
		 public RunFloorTable RunFloorTable;
		 public RelicTable RelicTable;
		 public ShopItemTable ShopItemTable;
		 public FloorRewardTable FloorRewardTable;
		 public MetaUpgradeTable MetaUpgradeTable;

    }
}