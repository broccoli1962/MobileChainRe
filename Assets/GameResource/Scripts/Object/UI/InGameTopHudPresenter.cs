using UnityEngine;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace Backend.Object.UI
{
    public class InGameTopHudPresenter : UIPresenter<InGameTopHud>
    {
        public void OnOptionButtonClicked()
        {
            UIManager.OpenAsync<OptionPopup>().Forget();
        }
    }
}