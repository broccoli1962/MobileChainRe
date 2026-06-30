using System;
using System.Collections.Generic;
using Backend.Object.Management;
using R3;
using UnityEngine;

namespace Backend.Object.UI
{
    public class CharacterPartyPanel : UIPanel<CharacterPartyPresenter>
    {
        [SerializeField] private List<CharacterPartySlot> _characterPartySlots;
        [SerializeField] private CommonButton _gameStartButton;
        
        private IDisposable _tapSubscription;
        private CompositeDisposable _slotDisposables = new();

        protected override void OnOpen()
        {
            base.OnOpen();

            _tapSubscription = BottomNavBar.OnTabSelected.Subscribe(_ => UIManager.Close(this));
            _gameStartButton.OnClick.AddListener(Presenter.OnGameStartButtonClicked);
            RefreshSlots();
        }

        protected override void OnClose()
        {
            _tapSubscription?.Dispose();
            _tapSubscription = null;
            _slotDisposables.Clear();

            _gameStartButton.OnClick.RemoveListener(Presenter.OnGameStartButtonClicked);

            base.OnClose();
        }

        public void RefreshSlots()
        {
            _slotDisposables.Clear();

            for (int i = 0; i < _characterPartySlots.Count; i++)
            {
                var slot = _characterPartySlots[i];
                var index = i;

                slot.OnClicked
                    .Subscribe(_ => Presenter.OnSlotClicked(index))
                    .AddTo(_slotDisposables);

                var data = Presenter.GetUnitAt(index);
                if (data != null)
                    slot.SetCharacter(data);
                else
                    slot.SetEmpty();
            }
        }
    }
}
