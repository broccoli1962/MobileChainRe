using Backend.AddressableKey;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Backend.Object.UI
{
    public class HomeView : UIView
    {
        [SerializeField] private CommonButton _playButton;

        protected override void OnShow()
        {
            base.OnShow();
            _playButton.OnClick.AddListener(OnPlayButtonClicked);
        }

        protected override void OnHide()
        {
            base.OnHide();
            _playButton.OnClick.RemoveListener(OnPlayButtonClicked);
        }

        private void OnPlayButtonClicked()
        {
           LoadMainSceneAsync().Forget();
        }
        
        private async UniTaskVoid LoadMainSceneAsync()
        {
            //
            string address = AddressableKeys.InGame.Get("GameScene");
            await Addressables.LoadSceneAsync(address, LoadSceneMode.Single).ToUniTask();
        }
    }
}
