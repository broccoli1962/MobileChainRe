using Backend.Object.Management;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Backend.Object.UI
{
    public class UnitDetailPanel : UIPanel<UnitDetailPresenter>
    {
        [SerializeField] private Image _unitIcon;
        [SerializeField] private TextMeshProUGUI _unitName;
        [SerializeField] private TextMeshProUGUI _unitType;
        [SerializeField] private TextMeshProUGUI _unitLevel;
        [SerializeField] private TextMeshProUGUI _unitHealth;
        [SerializeField] private TextMeshProUGUI _unitDamage;
        [SerializeField] private TextMeshProUGUI _unitResilience;

        public void SetData(UserUnitData userUnitData, UnitData unitData)
        {
            Presenter.SetData(userUnitData, unitData);
        }

        public void ShowUnitInfo(Sprite icon, UnitData unitData, UserUnitData userUnitData)
        {
            _unitIcon.sprite = icon;
            _unitName.text = unitData.unitName;
            _unitType.text = unitData.unitType.ToString();
            _unitLevel.text = userUnitData.unitLevel.ToString();
            _unitHealth.text = unitData.unithealth.ToString();
            _unitDamage.text = unitData.unitDamage.ToString();
            _unitResilience.text = unitData.unitResilience.ToString();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }
}
