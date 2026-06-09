using UnityEngine;
using Backend.Object.UI;

namespace Backend.Object.UI
{
    public class InGameTopHud : UIPanel<InGameTopHudPresenter>
    {
        public override UILayer Layer => UILayer.HUD;

        [SerializeField] private CommonButton _optionButton;

        protected override void OnOpen()
        {
            base.OnOpen();
            _optionButton.OnClick.AddListener(Presenter.OnOptionButtonClicked);
        }

        protected override void OnClose(){
            base.OnClose();
            _optionButton.OnClick.RemoveListener(Presenter.OnOptionButtonClicked);
        }
    }
}
