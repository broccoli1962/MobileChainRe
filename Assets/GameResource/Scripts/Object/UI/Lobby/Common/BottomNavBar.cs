using UnityEngine;
using Backend.Util.Enum;
using R3;

namespace Backend.Object.UI
{
    public class BottomNavBar : MonoBehaviour
    {
        [SerializeField] private CommonButton _homeButton;
        [SerializeField] private CommonButton _shopButton;
        [SerializeField] private CommonButton _unitButton;
        [SerializeField] private CommonButton _gachaButton;
        [SerializeField] private CommonButton _missionButton;
        [SerializeField] private CommonButton _friendButton;

        private Subject<LobbyTabType> _onTabSelected = new();
        public Observable<LobbyTabType> OnTabSelected => _onTabSelected;

        private void Awake()
        {
            Observable.Merge(
                _homeButton.OnClickAsObservable().Select(_ => LobbyTabType.Home),
                _shopButton.OnClickAsObservable().Select(_ => LobbyTabType.Shop),
                _unitButton.OnClickAsObservable().Select(_ => LobbyTabType.Unit),
                _gachaButton.OnClickAsObservable().Select(_ => LobbyTabType.Gacha),
                _missionButton.OnClickAsObservable().Select(_ => LobbyTabType.Mission),
                _friendButton.OnClickAsObservable().Select(_ => LobbyTabType.Friend)
            )
            .Subscribe(_onTabSelected.OnNext)
            .AddTo(this);
        }
    }
}
