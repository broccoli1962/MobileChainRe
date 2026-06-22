using Backend.AddressableKey;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Backend.Object.UI
{
    public class OptionPopup : UIPopup
    {
        [SerializeField] private CommonButton _blocker;
        [SerializeField] private CommonButton _toLobbyButton;

        protected override void OnOpen()
        {
            base.OnOpen();
            _blocker.OnClick.AddListener(OnBlockerClicked);
            _toLobbyButton.OnClick.AddListener(OnToLobbyButtonClicked);
        }

        protected override void OnClose()
        {
            base.OnClose();
            _blocker.OnClick.RemoveListener(OnBlockerClicked);
            _toLobbyButton.OnClick.RemoveListener(OnToLobbyButtonClicked);
        }

        private void OnBlockerClicked()
        {
            UIManager.Close(this);
        }

        private void OnToLobbyButtonClicked()
        {
            LoadLobbySceneAsync().Forget();
        }

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            UIManager.CloseAllUI();
            string address = AddressableKeys.InGame.Get("LobbyScene");
            await Addressables.LoadSceneAsync(address, LoadSceneMode.Single).ToUniTask();
        }
    }
}
