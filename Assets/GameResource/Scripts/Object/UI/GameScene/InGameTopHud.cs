using UnityEngine;
using Backend.Object.UI;
using System.Collections.Generic;

namespace Backend.Object.UI
{
    public class InGameTopHud : UIPanel<InGameTopHudPresenter>
    {
        public override UILayer Layer => UILayer.HUD;
        public RectTransform MonsterContainer;
        public RectTransform PlayerContainer;

        [SerializeField] private RectTransform[] _playerAnchors;
        public IReadOnlyList<RectTransform> PlayerAnchors => _playerAnchors;

        [SerializeField] private CommonButton _optionButton;
        [SerializeField] private CommonButton _skipButton;

        protected override void OnOpen()
        {
            base.OnOpen();
            _optionButton.OnClick.AddListener(Presenter.OnOptionButtonClicked);
            _skipButton.OnClick.AddListener(Presenter.OnSkipButtonClicked);
        }

        protected override void OnClose(){
            base.OnClose();
            _optionButton.OnClick.RemoveListener(Presenter.OnOptionButtonClicked);
            _skipButton.OnClick.RemoveListener(Presenter.OnSkipButtonClicked);
        }
    }
}
