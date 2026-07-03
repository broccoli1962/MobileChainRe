using Backend.AddressableKey;
using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.UI
{
    public class UnitDetailPresenter : UIPresenter<UnitDetailPanel>
    {
        public void SetData(UserUnitData userUnitData, UnitData unitData)
        {
            var icon = ResourceManager.LoadResource<Sprite>(AddressableKeys.InGame.Get($"Unit_{unitData.unitId}"));
            View.ShowUnitInfo(icon, unitData, userUnitData);
        }
    }
}
