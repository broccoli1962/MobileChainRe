using Backend.Util;
using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Backend.Object.UI
{
    public class CharacterPartySlot : CachedMonobehaviour
    {
        [SerializeField] private CommonButton _button;
        [SerializeField] private Image _characterImage;
        [SerializeField] private TextMeshProUGUI _characterName;
        [SerializeField] private TextMeshProUGUI _characterDescription;
        [SerializeField] private Image _characterTypeColor;

        public Observable<Unit> OnClicked => _button.OnClickAsObservable();

        public void SetCharacter(UnitData data)
        {
            _characterName.text = data.unitName;
            _characterDescription.text = $"{data.unitType}  {data.unitRarity}";
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
