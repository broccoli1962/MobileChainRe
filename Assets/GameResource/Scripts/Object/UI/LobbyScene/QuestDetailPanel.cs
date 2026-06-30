using System.Collections.Generic;
using Backend.Object.Management;
using TMPro;
using UnityEngine;
using R3;
using System;

namespace Backend.Object.UI
{
    /// <summary>
    /// 퀘스트 박스 클릭 시 열리는 상세 화면. UIManager 백 스택으로 뒤로가기 처리되며,
    /// Navigation 레이어가 NavBar 를 보호하므로 Panel 레이어에 떠도 NavBar 는 가려지지 않는다.
    /// </summary>
    public class QuestDetailPanel : UIPanel<QuestDetailPresenter>
    {
        [SerializeField] private TextMeshProUGUI _questName;
        [SerializeField] private TextMeshProUGUI _questDescription;
        [SerializeField] private CommonButton _backButton;

        [Header("Difficulty Buttons")]
        [SerializeField] private Transform _difficultyButtonContainer;
        [SerializeField] private CommonButton _difficultyButtonPrefab;

        private readonly List<CommonButton> _difficultyButtons = new();
        private IDisposable _tapSubscription;

        protected override bool DefaultHandleBackButton => true;

        public void SetData(QuestData questData) => Presenter.SetData(questData);

        protected override void OnOpen()
        {
            base.OnOpen();
            _backButton.OnClick.AddListener(OnBackClicked);

            _tapSubscription = BottomNavBar.OnTabSelected.Subscribe(_ => UIManager.Close(this));
        }

        protected override void OnClose()
        {
            _tapSubscription?.Dispose();
            _tapSubscription = null;

            base.OnClose();
            _backButton.OnClick.RemoveListener(OnBackClicked);
            ClearDifficultyButtons();
        }

        public void ShowQuestInfo(string name, string desc)
        {
            if (_questName != null) _questName.text = name;
            if (_questDescription != null) _questDescription.text = desc;
        }

        public void BuildDifficultyButtons(List<QuestDifficulty> difficulties)
        {
            ClearDifficultyButtons();

            if (_difficultyButtonContainer == null || _difficultyButtonPrefab == null)
                return;

            foreach (var difficulty in difficulties)
            {
                var btn = Instantiate(_difficultyButtonPrefab, _difficultyButtonContainer);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = difficulty.ToString();

                var captured = difficulty;
                btn.OnClick.AddListener(() => Presenter.OnDifficultySelected(captured));
                _difficultyButtons.Add(btn);
            }
        }

        private void ClearDifficultyButtons()
        {
            foreach (var btn in _difficultyButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _difficultyButtons.Clear();
        }

        private void OnBackClicked() => UIManager.Close(this);
    }
}
