using Cysharp.Threading.Tasks;
using Backend.AddressableKey;
using Backend.Object.Management;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.UI
{
    public class UnitView : UIView<UnitPresenter>
    {
        [SerializeField] private RectTransform _unitBoxRoot;
        private readonly List<UnitBox> _unitBoxes = new();
        
        protected override void OnShow()
        {
            base.OnShow();
            ShowUnitBoxesAsync().Forget();
        }

        protected override void OnHide()
        {
            base.OnHide();
            foreach (var box in _unitBoxes){
                ObjectPoolManager.Release(box);
            }
            _unitBoxes.Clear();
        }

        private async UniTaskVoid ShowUnitBoxesAsync(){
            var unitBoxPool = await ObjectPoolManager.GetOrCreatePoolAsync<UnitBox>(AddressableKeys.UI.Get<UnitBox>(), _unitBoxRoot, defaultCapacity: 10);

            foreach (var unitData in Presenter.GetUnitData()){
                var box = unitBoxPool.Get();
                box.SetData(unitData);
                _unitBoxes.Add(box);
            }
        }
    }
}
