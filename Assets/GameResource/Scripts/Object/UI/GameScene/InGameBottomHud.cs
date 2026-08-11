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
            // TurnContainer 자식(TapIcon)은 씬 컨트롤러가 붙인 DDOL UI 잔여물일 수 있어 닫을 때 정리한다.
            if (TurnContainer != null)
            {
                for (int i = TurnContainer.childCount - 1; i >= 0; i--)
                {
                    var child = TurnContainer.GetChild(i);
                    if (child != null)
                        Destroy(child.gameObject);
                }
            }
            base.OnClose();
        }
    }
}
