using System;
using UnityEngine;

namespace GoogleSpreadSheetLoader
{
    [Serializable]
    public class SheetData : ScriptableObject
    {
        public enum eTableStyle
        {
            None,
            EnumType,
            Localization,
        }
        
        [SerializeField] public string title;
        [SerializeField] public eTableStyle tableStyle;
        [SerializeField] public string data;
    }
}