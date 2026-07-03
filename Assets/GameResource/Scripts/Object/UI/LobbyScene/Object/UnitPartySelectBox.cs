using Backend.AddressableKey;
using Backend.Object.Management;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Backend.Util;

namespace Backend.Object.UI
{
    /// <summary> UnitPartySelectPanel 전용 유닛 박스. UnitBox 와 비주얼은 같지만 클릭 시 파티 슬롯 선택 용도로 사용된다. </summary>
    public class UnitPartySelectBox : CachedMonobehaviour
    {
        [SerializeField] private Image _unitImage;
        [SerializeField] private Image _unitTypeColor;
        [SerializeField] private Image _unitAnotherTypeColor;
        [SerializeField] private TextMeshProUGUI _unitLevel;
        [SerializeField] private CommonButton _unitButton;

        public UserUnitData UserUnitData { get; private set; }
        public Observable<Unit> OnClicked => _unitButton.OnClickAsObservable();

        public void SetData(UserUnitData userUnitData, bool selectable)
        {
            var unitData = TableManager.GetUnitData(userUnitData.unitIds);
            UserUnitData = userUnitData;

            _unitImage.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.InGame.Get($"Unit_{unitData.unitId}"));
            _unitTypeColor.color = GetTypeColor(unitData.unitType);
            _unitAnotherTypeColor.color = GetTypeColor(unitData.unitType);
            _unitLevel.text = userUnitData.unitLevel.ToString();

            // 이미 다른 슬롯에 배정된 유닛은 선택 불가 + 흐리게 표시
            _unitButton.interactable = selectable;
            SetDimmed(!selectable);
        }

        private void SetDimmed(bool dimmed)
        {
            float alpha = dimmed ? 0.35f : 1f;
            SetAlpha(_unitImage, alpha);
            SetAlpha(_unitTypeColor, alpha);
            SetAlpha(_unitAnotherTypeColor, alpha);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
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
