using System;
using System.Collections.Generic;
using Backend.Util.Enum;
using R3;
using UnityEngine;

namespace Backend.Object.UI
{
    public class LobbyPanel : UIPanel
    {
        [SerializeField] private BottomNavBar _bottomNavBar;
        [SerializeField] private HomeView _homeView;
        [SerializeField] private QuestView _questView;
        [SerializeField] private UnitView _unitView;
        [SerializeField] private ShopView _shopView;
        [SerializeField] private GachaView _gachaView;
        [SerializeField] private FriendView _friendView;

        private UIView _currentView;
        private Dictionary<LobbyTabType, UIView> _views;
        private IDisposable _tabSubscription;

        protected override void Awake()
        {
            base.Awake();
            _views = new Dictionary<LobbyTabType, UIView>
            {
                { LobbyTabType.Home, _homeView },
                { LobbyTabType.Shop, _shopView },
                { LobbyTabType.Unit, _unitView },
                { LobbyTabType.Quest, _questView },
                { LobbyTabType.Gacha, _gachaView },
                { LobbyTabType.Friend, _friendView },
            };

            _tabSubscription = _bottomNavBar.OnTabSelected.Subscribe(SwitchView);
        }

        protected override void OnOpen()
        {
            SwitchView(LobbyTabType.Home);
        }

        private void SwitchView(LobbyTabType tab)
        {
            if (!_views.TryGetValue(tab, out var next) || next == _currentView) return;

            _currentView?.Hide();
            _currentView = next;
            _currentView.Show();
        }

        private void OnDestroy()
        {
            _tabSubscription?.Dispose();
        }
    }
}
