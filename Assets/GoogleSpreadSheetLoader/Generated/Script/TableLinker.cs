using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TableData
{
    [CreateAssetMenu(fileName = "TableLinker", menuName = "Tables/TableLinker")]
    public partial class TableLinker : ScriptableObject
    {
		 public CharacterTable CharacterTable;

    }
}