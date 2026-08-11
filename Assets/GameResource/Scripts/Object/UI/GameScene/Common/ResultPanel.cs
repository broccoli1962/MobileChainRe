using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 런/퀘스트 최종 정산 패널(Classic/Quest 공용). 층 중간에는 열지 않는다.
    /// </summary>
    public class ResultPanel : UIPanel<ResultPanelPresenter>
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _floorText;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private CommonButton _confirmButton;

        public override UILayer Layer => UILayer.Popup;

        protected override bool DefaultHandleBackButton => true;

        public TextMeshProUGUI TitleText => _titleText;
        public TextMeshProUGUI FloorText => _floorText;
        public TextMeshProUGUI GoldText => _goldText;

        protected override void OnOpen()
        {
            base.OnOpen();
            _confirmButton.OnClick.AddListener(Presenter.OnConfirmClicked);
            Presenter.Refresh();
        }

        protected override void OnClose()
        {
            _confirmButton.OnClick.RemoveListener(Presenter.OnConfirmClicked);
            base.OnClose();
        }

        public override bool OnBackPressed() => false;
    }
}
