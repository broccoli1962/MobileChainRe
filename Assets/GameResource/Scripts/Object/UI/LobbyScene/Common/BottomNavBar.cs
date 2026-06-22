using UnityEngine;
using Backend.Util.Enum;
using R3;

namespace Backend.Object.UI
{
    /// <summary>
    /// 로비 하단 탭 네비게이션. Navigation 레이어에 상주하며,
    /// 탭 선택은 static Observable 로 발행해 누가 구독하든 진입 순서에 무관하게 동작한다.
    /// </summary>
    public class BottomNavBar : UIPanel
    {
        public override UILayer Layer => UILayer.Navigation;

        [SerializeField] private CommonButton _homeButton;
        [SerializeField] private CommonButton _shopButton;
        [SerializeField] private CommonButton _unitButton;
        [SerializeField] private CommonButton _gachaButton;
        [SerializeField] private CommonButton _questButton;
        [SerializeField] private CommonButton _friendButton;

        private static readonly Subject<LobbyTabType> _onTabSelected = new();
        public static Observable<LobbyTabType> OnTabSelected => _onTabSelected;

        protected override void Awake()
        {
            base.Awake();

            Observable.Merge(
                _homeButton.OnClickAsObservable().Select(_ => LobbyTabType.Home),
                _shopButton.OnClickAsObservable().Select(_ => LobbyTabType.Shop),
                _unitButton.OnClickAsObservable().Select(_ => LobbyTabType.Unit),
                _gachaButton.OnClickAsObservable().Select(_ => LobbyTabType.Gacha),
                _questButton.OnClickAsObservable().Select(_ => LobbyTabType.Quest),
                _friendButton.OnClickAsObservable().Select(_ => LobbyTabType.Friend)
            )
            .Subscribe(_onTabSelected.OnNext)
            .AddTo(this);
        }
    }
}
