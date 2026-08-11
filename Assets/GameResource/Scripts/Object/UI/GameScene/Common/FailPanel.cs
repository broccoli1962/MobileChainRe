using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 게임 실패 패널. 흰색 오버레이 페이드 인 후 로비/재시도 버튼을 노출한다.
    /// </summary>
    public class FailPanel : UIPanel<FailPanelPresenter>
    {
        [SerializeField] private CanvasGroup _whiteOverlay;
        [SerializeField] private CanvasGroup _contentGroup;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private CommonButton _toLobbyButton;
        [SerializeField] private CommonButton _retryButton;
        [SerializeField] private float _fadeDuration = 0.8f;

        private CancellationTokenSource _fadeCts;

        public override UILayer Layer => UILayer.Popup;

        protected override bool DefaultHandleBackButton => true;

        public TextMeshProUGUI TitleText => _titleText;

        protected override void OnOpen()
        {
            base.OnOpen();
            _toLobbyButton.OnClick.AddListener(Presenter.OnToLobbyClicked);
            _retryButton.OnClick.AddListener(Presenter.OnRetryClicked);
            Presenter.Refresh();
            PlayFailSequenceAsync().Forget();
        }

        protected override void OnClose()
        {
            CancelFade();
            _toLobbyButton.OnClick.RemoveListener(Presenter.OnToLobbyClicked);
            _retryButton.OnClick.RemoveListener(Presenter.OnRetryClicked);
            base.OnClose();
        }

        public override bool OnBackPressed() => false;

        private async UniTaskVoid PlayFailSequenceAsync()
        {
            CancelFade();
            _fadeCts = new CancellationTokenSource();
            var token = _fadeCts.Token;

            if (_whiteOverlay != null)
            {
                _whiteOverlay.alpha = 0f;
                _whiteOverlay.blocksRaycasts = true;
            }

            if (_contentGroup != null)
            {
                _contentGroup.alpha = 0f;
                _contentGroup.interactable = false;
                _contentGroup.blocksRaycasts = false;
            }

            SetButtonsInteractable(false);

            try
            {
                if (_whiteOverlay != null)
                {
                    await LMotion.Create(0f, 1f, _fadeDuration)
                        .BindToAlpha(_whiteOverlay)
                        .ToUniTask(cancellationToken: token);
                }

                if (_contentGroup != null)
                {
                    await LMotion.Create(0f, 1f, 0.25f)
                        .BindToAlpha(_contentGroup)
                        .ToUniTask(cancellationToken: token);
                    _contentGroup.interactable = true;
                    _contentGroup.blocksRaycasts = true;
                }

                SetButtonsInteractable(true);
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        private void SetButtonsInteractable(bool value)
        {
            if (_toLobbyButton != null)
                _toLobbyButton.interactable = value;
            if (_retryButton != null)
                _retryButton.interactable = value;
        }

        private void CancelFade()
        {
            if (_fadeCts == null) return;
            _fadeCts.Cancel();
            _fadeCts.Dispose();
            _fadeCts = null;
        }

        private void OnDestroy()
        {
            CancelFade();
        }
    }
}
