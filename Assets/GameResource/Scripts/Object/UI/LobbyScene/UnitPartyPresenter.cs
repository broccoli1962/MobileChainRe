using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Backend.Object.UI
{
    public class UnitPartyPresenter : UIPresenter<UnitPartyPanel>
    {
        // 슬롯 인덱스 -> UserData.OwnedUnitIds 인덱스(보유 유닛 인스턴스). 비어있는 슬롯은 키가 없다.
        private readonly Dictionary<int, int> _slotToOwnedIndex = new();

        /// <summary> 슬롯 클릭 시 호출. 유닛 선택창을 열어 해당 슬롯에 배정할 유닛을 고르게 한다. </summary>
        public void OnSlotClicked(int slotIndex)
        {
            OpenUnitSelectPanelAsync(slotIndex).Forget();
        }

        private async UniTaskVoid OpenUnitSelectPanelAsync(int slotIndex)
        {
            var panel = await UIManager.OpenAsync<UnitPartySelectPanel>();
            panel?.SetData(slotIndex, this);
        }

        public void SetCharacter(int slotIndex, int ownedIndex)
        {
            _slotToOwnedIndex[slotIndex] = ownedIndex;
            View.RefreshSlots();
        }

        public void RemoveCharacter(int slotIndex)
        {
            _slotToOwnedIndex.Remove(slotIndex);
            View.RefreshSlots();
        }

        /// <summary> 해당 보유 유닛 인스턴스가 이미 다른 슬롯(exceptSlotIndex 제외)에 배정되어 있는지 여부. </summary>
        public bool IsOwnedUnitUsed(int ownedIndex, int exceptSlotIndex)
        {
            foreach (var pair in _slotToOwnedIndex)
            {
                if (pair.Key == exceptSlotIndex) continue;
                if (pair.Value == ownedIndex) return true;
            }
            return false;
        }

        public void OnGameStartButtonClicked()
        {
            if (_slotToOwnedIndex.Count < 2)
            {
                Debug.LogError("캐릭터가 2명 이상 필요합니다.");
                return;
            }
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            string address = AddressableKeys.InGame.Get("GameScene");
            await Addressables.LoadSceneAsync(address, LoadSceneMode.Single).ToUniTask();
        }

        /// <summary> 슬롯 인덱스에 배정된 UserUnitData 반환. 빈 슬롯이면 null. </summary>
        public UserUnitData? GetUnitAt(int slotIndex)
        {
            if (_slotToOwnedIndex.TryGetValue(slotIndex, out var ownedIndex)
                && ownedIndex >= 0 && ownedIndex < UserData.OwnedUnitIds.Count)
                return UserData.OwnedUnitIds[ownedIndex];
            return null;
        }
    }
}