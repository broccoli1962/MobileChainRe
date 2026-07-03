using System.Collections.Generic;
using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.UI
{
    public class UnitPartySelectPresenter : UIPresenter<UnitPartySelectPanel>
    {
        private int _slotIndex;
        private UnitPartyPresenter _partyPresenter;

        public void SetData(int slotIndex, UnitPartyPresenter partyPresenter)
        {
            _slotIndex = slotIndex;
            _partyPresenter = partyPresenter;
        }

        public IReadOnlyList<UserUnitData> GetUnitData() => UserData.OwnedUnitIds;

        /// <summary> 해당 보유 유닛을 현재 슬롯에 선택할 수 있는지(다른 슬롯에서 이미 사용 중이면 불가). </summary>
        public bool IsOwnedUnitSelectable(int ownedIndex)
            => _partyPresenter == null || !_partyPresenter.IsOwnedUnitUsed(ownedIndex, _slotIndex);

        /// <summary> 유닛 박스 클릭 시 호출. 선택한 유닛을 파티 슬롯에 배정하고 창을 닫는다. </summary>
        public void OnUnitSelected(int ownedIndex)
        {
            if (!IsOwnedUnitSelectable(ownedIndex)) return;

            _partyPresenter?.SetCharacter(_slotIndex, ownedIndex);
            UIManager.Close(View);
        }

        public void OnRemoveUnitButtonClicked()
        {
            _partyPresenter?.RemoveCharacter(_slotIndex);
            UIManager.Close(View);
        }
    }
}
