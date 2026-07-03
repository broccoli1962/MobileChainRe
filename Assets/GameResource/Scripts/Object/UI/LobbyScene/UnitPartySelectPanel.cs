using System;
using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.UI
{
    public class UnitPartySelectPanel : UIPanel<UnitPartySelectPresenter>
    {
        [SerializeField] private RectTransform _unitBoxRoot;
        [SerializeField] private CommonButton _removeUnitButton;

        private readonly List<UnitPartySelectBox> _unitBoxes = new();
        private readonly CompositeDisposable _boxDisposables = new();
        private IDisposable _tapSubscription;

        public void SetData(int slotIndex, UnitPartyPresenter partyPresenter)
        {
            // 반드시 Presenter 데이터가 세팅된 뒤에 박스를 생성해야 선택 불가 상태가 즉시 반영된다.
            Presenter.SetData(slotIndex, partyPresenter);
            ShowUnitBoxesAsync().Forget();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            _tapSubscription = BottomNavBar.OnTabSelected.Subscribe(_ => UIManager.Close(this));
            _removeUnitButton.OnClick.AddListener(Presenter.OnRemoveUnitButtonClicked);
        }

        protected override void OnClose()
        {
            _tapSubscription?.Dispose();
            _tapSubscription = null;
            
            _boxDisposables.Clear();
            foreach (var box in _unitBoxes){
                ObjectPoolManager.Release(box);
            }
            _unitBoxes.Clear();

            base.OnClose();
        }

        private async UniTaskVoid ShowUnitBoxesAsync(){
            var unitBoxPool = await ObjectPoolManager.GetOrCreatePoolAsync<UnitPartySelectBox>(AddressableKeys.UI.Get<UnitPartySelectBox>(), _unitBoxRoot, defaultCapacity: 10);

            var units = Presenter.GetUnitData();
            for (int i = 0; i < units.Count; i++){
                var ownedIndex = i;
                var box = unitBoxPool.Get();
                // 풀에서 어떤 인스턴스를 반환받든(순서 무관) 화면상 위치를 ownedIndex 순서로 고정한다.
                box.transform.SetSiblingIndex(ownedIndex);
                box.SetData(units[ownedIndex], Presenter.IsOwnedUnitSelectable(ownedIndex));
                box.OnClicked.Subscribe(_ => Presenter.OnUnitSelected(ownedIndex)).AddTo(_boxDisposables);
                _unitBoxes.Add(box);
            }
        }
    }
}
