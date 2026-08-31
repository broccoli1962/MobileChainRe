using Backend.Object.CharacterObject;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    public class SkillConfirmPopup : UIPopup
    {
        [SerializeField] private CommonButton _blocker;
        [SerializeField] private CommonButton _useButton;
        [SerializeField] private CommonButton _cancelButton;
        [SerializeField] private TextMeshProUGUI _unitNameText;
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private TextMeshProUGUI _skillDescriptText;
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [SerializeField] private TextMeshProUGUI _reasonText;

        private CharacterSlot _slot;

        public void Bind(CharacterSlot slot)
        {
            _slot = slot;
            Refresh();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            _blocker.OnClick.AddListener(OnCancelClicked);
            _useButton.OnClick.AddListener(OnUseClicked);
            _cancelButton.OnClick.AddListener(OnCancelClicked);
            Refresh();
        }

        protected override void OnClose()
        {
            _blocker.OnClick.RemoveListener(OnCancelClicked);
            _useButton.OnClick.RemoveListener(OnUseClicked);
            _cancelButton.OnClick.RemoveListener(OnCancelClicked);
            _slot = null;
            base.OnClose();
        }

        private void Refresh()
        {
            if (_slot == null || _slot.UnitData == null)
            {
                SetText(_unitNameText, string.Empty);
                SetText(_skillNameText, string.Empty);
                SetText(_skillDescriptText, string.Empty);
                SetText(_cooldownText, string.Empty);
                SetText(_reasonText, "대상 없음");
                _useButton.interactable = false;
                return;
            }

            var skill = SkillSystem.GetSkill(_slot);
            SetText(_unitNameText, _slot.UnitData.unitName);
            SetText(_skillNameText, skill != null ? skill.skillName : "스킬 없음");
            SetText(_skillDescriptText, skill != null ? skill.skillDescript : string.Empty);

            int remaining = SkillSystem.GetRemainingCooldown(_slot);
            int max = skill != null ? skill.skillCoolDown : 0;
            SetText(_cooldownText, $"쿨타임 {remaining}/{max}");

            bool canUse = SkillSystem.CanUse(_slot, out string reason);
            _useButton.interactable = canUse;
            SetText(_reasonText, canUse ? "이 스킬을 사용할까요?" : reason);
        }

        private void OnUseClicked()
        {
            if (_slot == null) return;
            if (!SkillSystem.TryUse(_slot))
            {
                Refresh();
                return;
            }

            UIManager.Close(this);
        }

        private void OnCancelClicked()
        {
            UIManager.Close(this);
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? string.Empty;
        }
    }
}
