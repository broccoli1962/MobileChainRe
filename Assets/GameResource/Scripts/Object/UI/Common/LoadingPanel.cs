using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 씬 전환용 풀스크린 로딩 패널. Popup 레이어에 올라가며 뒤로가기로 닫히지 않는다.
    /// </summary>
    public class LoadingPanel : UIPanel
    {
        [SerializeField] private TextMeshProUGUI _messageText;

        public override UILayer Layer => UILayer.Popup;

        /// <summary>백 스택에 올려 로딩 중 ESC/뒤로가기를 삼킨다.</summary>
        protected override bool DefaultHandleBackButton => true;

        public override bool OnBackPressed() => false;

        public void SetMessage(string message)
        {
            if (_messageText != null)
                _messageText.text = message ?? string.Empty;
        }
    }
}
