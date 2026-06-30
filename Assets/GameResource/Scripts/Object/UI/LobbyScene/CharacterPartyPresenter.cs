using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Backend.Object.UI
{
    public class CharacterPartyPresenter : UIPresenter<CharacterPartyPanel>
    {
        private readonly List<int> _partyUnitIds = new();
        public IReadOnlyList<int> PartyUnitIds => _partyUnitIds;

        /// <summary> 슬롯 클릭 시 호출. 캐릭터 선택창 연동은 추후 구현. </summary>
        public void OnSlotClicked(int slotIndex)
        {
            //UIManager.Open<CharacterSelectionPanel>();
            // 선택 완료 콜백 → SetCharacter(slotIndex, unitId)
        }

        public void SetCharacter(int slotIndex, int unitId)
        {
            if (slotIndex < _partyUnitIds.Count)
                _partyUnitIds[slotIndex] = unitId;
            else
                _partyUnitIds.Add(unitId);

            View.RefreshSlots();
        }

        public void RemoveCharacter(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _partyUnitIds.Count) return;
            _partyUnitIds.RemoveAt(slotIndex);
            View.RefreshSlots();
        }

        public void OnGameStartButtonClicked()
        {
            if (_partyUnitIds.Count < 2)
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

        /// <summary> 슬롯 인덱스에 해당하는 UnitData 반환. 빈 슬롯이면 null. </summary>
        public UnitData GetUnitAt(int slotIndex)
            => slotIndex < _partyUnitIds.Count ? TableManager.GetUnitData(_partyUnitIds[slotIndex]) : null;
    }
}