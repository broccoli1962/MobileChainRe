using UnityEngine;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using Backend.Object.GameSystems;

namespace Backend.Object.UI
{
    public class InGameTopHudPresenter : UIPresenter<InGameTopHud>
    {
        public void OnOptionButtonClicked()
        {
            UIManager.OpenAsync<OptionPopup>().Forget();
        }

        public void OnSkipButtonClicked(){
            CharacterSystem.AdvanceTurn();
        }
    }
}