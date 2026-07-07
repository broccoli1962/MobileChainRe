using UnityEngine;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using Backend.Object.GameSystems;

namespace Backend.Object.UI
{
    public class InGameTopHudPresenter : UIPresenter<InGameTopHud>
    {
        public void UpdatePlayerHpBar(float hp, float maxHp){
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