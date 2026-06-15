using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Backend.Object.UI
{
    public class QuestSelectPopup : UIPopup
    {
        [SerializeField] private CommonButton _blocker;
        [SerializeField] private CommonButton _easyButton;
        [SerializeField] private CommonButton _normalButton;
        [SerializeField] private CommonButton _hardButton;

        private QuestData _questData;

        protected override void OnOpen()
        {
            base.OnOpen();
            _blocker.OnClick.AddListener(OnBlockerClicked);
            _easyButton.OnClick.AddListener(OnEasyClicked);
            _normalButton.OnClick.AddListener(OnNormalClicked);
            _hardButton.OnClick.AddListener(OnHardClicked);
        }

        protected override void OnClose()
        {
            base.OnClose();
            _blocker.OnClick.RemoveListener(OnBlockerClicked);
            _easyButton.OnClick.RemoveListener(OnEasyClicked);
            _normalButton.OnClick.RemoveListener(OnNormalClicked);
            _hardButton.OnClick.RemoveListener(OnHardClicked);
        }

        public void SetQuest(QuestData questData)
        {
            _questData = questData;
            RefreshDifficultyButtons();
        }

        private void RefreshDifficultyButtons()
        {
            var maps = TableManager.GetQuestMapFloors(_questData.questMapId);
            var difficulties = new HashSet<QuestDifficulty>();
            if (maps != null)
                foreach (var m in maps)
                    difficulties.Add(m.questDifficulty);

            _easyButton.gameObject.SetActive(difficulties.Contains(QuestDifficulty.easy));
            _normalButton.gameObject.SetActive(difficulties.Contains(QuestDifficulty.normal));
            _hardButton.gameObject.SetActive(difficulties.Contains(QuestDifficulty.hard));
        }

        private void OnBlockerClicked() => UIManager.CloseDynamic(this);

        private void OnEasyClicked() => OnDifficultySelected(QuestDifficulty.easy);
        private void OnNormalClicked() => OnDifficultySelected(QuestDifficulty.normal);
        private void OnHardClicked() => OnDifficultySelected(QuestDifficulty.hard);

        private void OnDifficultySelected(QuestDifficulty difficulty)
        {
            GameSessionData.SetQuestMap(_questData.questMapId, difficulty);
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            UIManager.CloseAllUI();
            string address = AddressableKeys.InGame.Get("Assets_GameResource_Scenes_MainScene_unity");
            await Addressables.LoadSceneAsync(address, LoadSceneMode.Single).ToUniTask();
        }
    }
}
