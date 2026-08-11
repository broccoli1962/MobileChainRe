using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Backend.Object.UI
{
    public class InGameTopHud : UIPanel<InGameTopHudPresenter>
    {
        public override UILayer Layer => UILayer.HUD;
        public RectTransform MonsterContainer;
        public RectTransform PlayerContainer;

        [Header("HpGauge")]
        [SerializeField] private SingleGaugeBar _playerHpBar;
        public SingleGaugeBar PlayerHpBar => _playerHpBar;
        [SerializeField] private TextMeshProUGUI _playerCurrentHpText;
        [SerializeField] private TextMeshProUGUI _playerMaxHpText;
        public TextMeshProUGUI PlayerCurrentHpText => _playerCurrentHpText;
        public TextMeshProUGUI PlayerMaxHpText => _playerMaxHpText;

        [Header("Classic Run")]
        [SerializeField] private TextMeshProUGUI _floorText;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private GameObject _classicHudRoot;
        public TextMeshProUGUI FloorText => _floorText;
        public TextMeshProUGUI GoldText => _goldText;
        public GameObject ClassicHudRoot => _classicHudRoot;


        [Header("PlayerAnchors")]
        [SerializeField] private RectTransform[] _playerAnchors;
        public IReadOnlyList<RectTransform> PlayerAnchors => _playerAnchors;

        [Header("Buttons")]
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
