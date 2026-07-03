using Backend.Util;
using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Backend.Object.Management;
using Backend.AddressableKey;

namespace Backend.Object.UI
{
    public class UnitPartySlot : CachedMonobehaviour
    {
        [SerializeField] private CommonButton _button;
        [SerializeField] private Image _characterImage;
        [SerializeField] private TextMeshProUGUI _characterName;
        [SerializeField] private TextMeshProUGUI _characterDescription;
        [SerializeField] private TextMeshProUGUI _characterLevel;
        [SerializeField] private Image _characterTypeColor;


        public Observable<Unit> OnClicked => _button.OnClickAsObservable();

        public void SetCharacter(UserUnitData data)
        {
            var unitData = TableManager.GetUnitData(data.unitIds);

            _characterImage.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.InGame.Get($"Unit_{unitData.unitId}"));
            _characterTypeColor.color = ColorUtil.GetUnitTypeColor(unitData.unitType);
            _characterName.text = unitData.unitName;
            _characterDescription.text = unitData.unitRarity.ToString();
            _characterLevel.text = data.unitLevel.ToString();
        }

        public void SetEmpty()
        {
            _characterName.text = string.Empty;
            _characterDescription.text = string.Empty;
            _characterImage.sprite = null;
            _characterTypeColor.color = Color.gray;
        }
    }
}
