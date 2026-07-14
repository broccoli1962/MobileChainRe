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

        public override void OnOpen()
        {
            base.OnOpen();
            _hpSubscription = PartySystem.OnHpChanged.Subscribe(hp => UpdatePlayerHpBar(hp.cur, hp.max));
        }

        public override void OnClose()
        {
            _hpSubscription?.Dispose();
            _hpSubscription = null;
            base.OnClose();
        }

        private void UpdatePlayerHpBar(float hp, float maxHp){
            View.PlayerHpBar.SetValues(hp, maxHp);
            View.PlayerCurrentHpText.text = hp.ToString();
            View.PlayerMaxHpText.text = maxHp.ToString();
        }
        public void OnOptionButtonClicked()
        {
            UIManager.OpenAsync<OptionPopup>().Forget();
        }

        public void OnSkipButtonClicked(){
            CharacterSystem.AdvanceTurn();
        }
    }
}