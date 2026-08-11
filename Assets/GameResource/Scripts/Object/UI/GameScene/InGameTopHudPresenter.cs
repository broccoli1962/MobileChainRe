using System;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using Backend.Object.GameSystems;
using R3;

namespace Backend.Object.UI
{
    public class InGameTopHudPresenter : UIPresenter<InGameTopHud>
    {
        private IDisposable _hpSubscription;
        private IDisposable _progressSubscription;

        public override void OnOpen()
        {
            base.OnOpen();
            _hpSubscription = PartySystem.OnHpChanged.Subscribe(hp => UpdatePlayerHpBar(hp.cur, hp.max));

            var session = ActiveSession.Current;
            if (session != null && session.TryGetProgressHud(out var hud))
            {
                if (View.ClassicHudRoot != null)
                    View.ClassicHudRoot.SetActive(true);

                UpdateProgressHud(hud.Floor, hud.MaxFloor, hud.Gold);
                var maxFloor = hud.MaxFloor;
                _progressSubscription = hud.OnChanged.Subscribe(v =>
                    UpdateProgressHud(v.floor, maxFloor, v.gold));
            }
            else
            {
                if (View.ClassicHudRoot != null)
                    View.ClassicHudRoot.SetActive(false);
                if (View.FloorText != null) View.FloorText.gameObject.SetActive(false);
                if (View.GoldText != null) View.GoldText.gameObject.SetActive(false);
            }
        }

        public override void OnClose()
        {
            _hpSubscription?.Dispose();
            _hpSubscription = null;
            _progressSubscription?.Dispose();
            _progressSubscription = null;
            base.OnClose();
        }

        private void UpdatePlayerHpBar(float hp, float maxHp)
        {
            View.PlayerHpBar.SetValues(hp, maxHp);
            View.PlayerCurrentHpText.text = hp.ToString();
            View.PlayerMaxHpText.text = maxHp.ToString();
        }

        private void UpdateProgressHud(int floor, int maxFloor, int gold)
        {
            if (View.FloorText != null)
            {
                View.FloorText.gameObject.SetActive(true);
                View.FloorText.text = $"Floor {floor} / {maxFloor}";
            }
            if (View.GoldText != null)
            {
                View.GoldText.gameObject.SetActive(true);
                View.GoldText.text = $"Gold {gold}";
            }
        }

        public void OnOptionButtonClicked()
        {
            UIManager.OpenAsync<OptionPopup>().Forget();
        }

        public void OnSkipButtonClicked()
        {
            CharacterSystem.AdvanceTurn();
        }
    }
}
