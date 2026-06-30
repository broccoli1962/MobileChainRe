using Backend.Object.UI;
using UnityEngine;

namespace Backend.Object.UI
{
    public class InGameBottomHud : UIPanel<InGameBottomHudPresenter>
    {
        public override UILayer Layer => UILayer.HUD;
        public RectTransform TurnContainer;

        protected override void Awake()
        {
            base.Awake();
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
