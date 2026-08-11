using Backend.Object.Management;
using Backend.Util.Enum;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.UI
{
    public class HomeView : UIView
    {
        [SerializeField] private CommonButton _classicRunButton;
        [SerializeField] private CommonButton _practiceButton;
        [SerializeField] private CommonButton _playButton; // legacy — Classic Run fallback

        protected override void OnShow()
        {
            base.OnShow();
            var classic = _classicRunButton != null ? _classicRunButton : _playButton;
            if (classic != null)
                classic.OnClick.AddListener(OnClassicRunClicked);
            if (_practiceButton != null)
                _practiceButton.OnClick.AddListener(OnPracticeClicked);
        }

        protected override void OnHide()
        {
            base.OnHide();
            var classic = _classicRunButton != null ? _classicRunButton : _playButton;
            if (classic != null)
                classic.OnClick.RemoveListener(OnClassicRunClicked);
            if (_practiceButton != null)
                _practiceButton.OnClick.RemoveListener(OnPracticeClicked);
        }

        private void OnClassicRunClicked()
        {
            ActiveSession.BeginClassic();
            OpenPartyPanelAsync().Forget();
        }

        private void OnPracticeClicked()
        {
            BottomNavBar.SelectTab(LobbyTabType.Quest);
        }

        private async UniTaskVoid OpenPartyPanelAsync()
        {
            await UIManager.OpenAsync<UnitPartyPanel>();
        }
    }
}
