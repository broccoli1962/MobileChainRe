using UnityEngine;
using Backend.Util;
using UnityEngine.UI;
using Backend.AddressableKey;
using Backend.Object.Management;
using TMPro;

namespace Backend.Object.UI
{
    public class UnitBox : CachedMonobehaviour
    {
        [SerializeField] private Image _unitImage;
        [SerializeField] private Image _unitTypeColor;
        [SerializeField] private Image _unitAnotherTypeColor;
        [SerializeField] private TextMeshProUGUI _unitLevel;

        public void SetData(UserUnitData userUnitData){
            var unitData = TableManager.GetUnitData(userUnitData.unitIds);

            _unitImage.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.InGame.Get($"Unit_{unitData.unitId}"));
            _unitTypeColor.color = GetTypeColor(unitData.unitType);
            _unitAnotherTypeColor.color = GetTypeColor(unitData.unitType);
            _unitLevel.text = userUnitData.unitLevels.ToString();
        }

        private Color GetTypeColor(UnitType type){
            return type switch
            {
                UnitType.fire => new Color(1f,   0.3f, 0.1f),
                UnitType.light => new Color(1f,   1f,   0.2f),
                UnitType.water => new Color(0.2f, 0.5f, 1f),
                UnitType.grass => new Color(0.2f, 0.8f, 0.2f),
            };
        }
    }
}
