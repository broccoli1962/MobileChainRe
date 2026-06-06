using Backend.AddressableKey;
using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Backend
{
    public class TestBackPanel : UIPanel
    {
        [UnityEngine.SerializeField] private CommonButton _testGameButton;

        protected override void Awake()
        {
            base.Awake();
            _testGameButton.OnClick.AddListener(OnClickTestGameButton);
        }

        private void OnDestroy()
        {
            _testGameButton.OnClick.RemoveListener(OnClickTestGameButton);
        }

        private void OnClickTestGameButton()
        {
            LoadMainSceneAsync().Forget();
        }

        private async UniTaskVoid LoadMainSceneAsync()
        {
            string address = AddressableKeys.InGame.Get("Assets_GameResource_Scenes_LobbyScene_unity");
            await Addressables.LoadSceneAsync(address, LoadSceneMode.Single).ToUniTask();
        }
    }
}
