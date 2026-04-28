using Backend.Object.Management;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 테스트용 팝업. 닫기 버튼은 UIManager.Close 로 반환한다.
    /// </summary>
    public class TestPopupUI : UIPopup
    {
        [SerializeField] private Button _closeButton;

        protected override void Awake()
        {
            base.Awake();
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnCloseClicked()
        {
            UIManager.Close(this);
        }
    }
}
