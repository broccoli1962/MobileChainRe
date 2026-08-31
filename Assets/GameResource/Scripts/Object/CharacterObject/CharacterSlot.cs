using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using Backend.Util;
using Backend.Util.Interface;
using Backend.AddressableKey;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using Backend.Object.UI;
using R3;
using TMPro;

namespace Backend.Object.CharacterObject
{
    public class CharacterSlot : CachedMonobehaviour, ICharacter, IPointerClickHandler
    {
        [Header("Expand")]
        [SerializeField] private RectTransform _expandRoot;
        [SerializeField] private CanvasGroup _expandedInfo;
        [SerializeField] private float _collapsedWidth = 100f;
        [SerializeField] private float _expandedWidth = 260f;
        [SerializeField] private float _expandDuration = 0.2f;

        [Header("Expanded Info")]
        [SerializeField] private TextMeshProUGUI _damageText;
        [SerializeField] private TextMeshProUGUI _shieldText;
        [SerializeField] private TextMeshProUGUI _resilienceText;

        [Header("Color")]
        [SerializeField] private Image _colorBorder;
        [SerializeField] private Image _colorInsideBorder;

        [Header("Character")]
        [SerializeField] private Image _characterImage;

        [Header("Skill")]
        [SerializeField] private RectTransform _cooldownBox;
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [SerializeField] private RectTransform _cooldownCompleteBox;

        private const float CompleteBoxPulseDuration = 0.7f;
        private const float CompleteBoxPulseMinAlpha = 0f;

        private int _characterid;
        private UnitData _unitData;
        private Color _baseBorderColor = Color.white;
        private readonly CompositeDisposable _skillSubscriptions = new();
        private MotionHandle _fillHandle;
        private MotionHandle _completeBoxPulseHandle;
        private CanvasGroup _cooldownCompleteGroup;
        private bool _skillVisualReady;

        private CancellationTokenSource _expandCts;

        public int Id => _characterid;
        public UnitData UnitData => _unitData;

        public void Awake()
        {
            if (_expandRoot != null)
                SetWidth(_collapsedWidth);

            if (_expandedInfo != null)
            {
                _expandedInfo.alpha = 0f;
                _expandedInfo.blocksRaycasts = false;
            }

            ApplyCooldownFill(0f, animate: false);
        }

        public void Initialize(UnitData unitData)
        {
            // TODO: 플레이어 데이터 추가
            _unitData = unitData;
            _characterid = unitData.unitId;
            _damageText.text = unitData.unitDamage.ToString("F0");
            _shieldText.text = unitData.unitDefense.ToString("F0");
            _resilienceText.text = unitData.unitResilience.ToString("F0");

            SetSlotColor(unitData.unitType);

            _characterImage.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.InGame.Get($"Unit_{_characterid}"));

            _skillSubscriptions.Clear();
            SkillSystem.OnCooldownChanged
                .Subscribe(pair =>
                {
                    if (pair.character == this)
                        RefreshSkillVisual();
                })
                .AddTo(_skillSubscriptions);
            SkillSystem.EnsureStartingCooldown(this);
            RefreshSkillVisual();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OpenSkillPopupAsync().Forget();
        }

        public void OnSlotChanged(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot) return;

            // toSlot == 0 → 1번(맨 앞) 슬롯으로 진입 시 펼침
            SetExpanded(toSlot == 0);
        }

        private void SetSlotColor(UnitType type){
            _baseBorderColor = type switch
            {
                UnitType.fire => new Color(1f, 0.3f, 0.1f),
                UnitType.light => new Color(1f, 1f, 0.2f),
                UnitType.water => new Color(0.2f, 0.5f, 1f),
                UnitType.grass => new Color(0.2f, 0.8f, 0.2f),
                _ => throw new NotImplementedException(),
            };
            _colorBorder.color = _baseBorderColor;
            _colorInsideBorder.color = _baseBorderColor;
        }

        private async UniTaskVoid OpenSkillPopupAsync()
        {
            var popup = await UIManager.OpenAsync<SkillConfirmPopup>();
            if (popup == null) return;
            popup.Bind(this);
        }

        private void RefreshSkillVisual()
        {
            int remaining = SkillSystem.GetRemainingCooldown(this);
            if (_cooldownText != null)
                _cooldownText.text = remaining.ToString();

            var skill = SkillSystem.GetSkill(this);
            int max = skill != null ? skill.skillCoolDown : 0;
            float fill = max <= 0 || remaining <= 0
                ? 1f
                : 1f - Mathf.Clamp01(remaining / (float)max);

            RefreshCooldownVisual(remaining);
            ApplyCooldownFill(fill, animate: _skillVisualReady);
            _skillVisualReady = true;
        }

        private void RefreshCooldownVisual(int remaining)
        {
            _cooldownBox.gameObject.SetActive(remaining > 0);

            if (remaining <= 0)
                PlayCompleteBoxPulse();
            else
                StopCompleteBoxPulse();
        }

        private void PlayCompleteBoxPulse()
        {
            if (_cooldownCompleteBox == null)
                return;

            if (_completeBoxPulseHandle.IsActive() && _cooldownCompleteBox.gameObject.activeSelf)
                return;

            EnsureCompleteBoxGroup();
            _cooldownCompleteBox.gameObject.SetActive(true);
            _cooldownCompleteGroup.alpha = 1f;

            if (_completeBoxPulseHandle.IsActive())
                _completeBoxPulseHandle.Cancel();

            _completeBoxPulseHandle = LMotion.Create(1f, CompleteBoxPulseMinAlpha, CompleteBoxPulseDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .Bind(a => _cooldownCompleteGroup.alpha = a);
        }

        private void StopCompleteBoxPulse()
        {
            if (_completeBoxPulseHandle.IsActive())
                _completeBoxPulseHandle.Cancel();

            if (_cooldownCompleteGroup != null)
                _cooldownCompleteGroup.alpha = 1f;

            if (_cooldownCompleteBox != null)
                _cooldownCompleteBox.gameObject.SetActive(false);
        }

        private void EnsureCompleteBoxGroup()
        {
            if (_cooldownCompleteGroup != null || _cooldownCompleteBox == null)
                return;

            if (!_cooldownCompleteBox.TryGetComponent(out _cooldownCompleteGroup))
                _cooldownCompleteGroup = _cooldownCompleteBox.gameObject.AddComponent<CanvasGroup>();
        }

        private void ApplyCooldownFill(float fill, bool animate)
        {
            if (_colorBorder == null)
                return;

            _colorBorder.color = _baseBorderColor;

            var mask = _colorBorder.rectTransform.parent as RectTransform;
            if (mask == null)
                return;

            float fullHeight = _colorBorder.rectTransform.sizeDelta.y;
            if (fullHeight <= 0f && _expandRoot != null)
                fullHeight = _expandRoot.sizeDelta.y;

            float targetHeight = fullHeight * Mathf.Clamp01(fill);

            if (_fillHandle.IsActive())
                _fillHandle.Cancel();

            if (!animate || !gameObject.activeInHierarchy)
            {
                SetMaskHeight(mask, targetHeight);
                return;
            }

            _fillHandle = LMotion.Create(mask.sizeDelta.y, targetHeight, _expandDuration)
                .WithEase(Ease.OutQuad)
                .Bind(h => SetMaskHeight(mask, h));
        }

        private static void SetMaskHeight(RectTransform mask, float height)
        {
            var size = mask.sizeDelta;
            size.y = height;
            mask.sizeDelta = size;
        }

        private void SetExpanded(bool expanded)
        {
            _expandCts?.Cancel();
            _expandCts?.Dispose();
            _expandCts = new CancellationTokenSource();
            AnimateExpandAsync(expanded, _expandCts.Token).Forget();
        }

        private async UniTaskVoid AnimateExpandAsync(bool expanded, CancellationToken token)
        {
            float targetWidth = expanded ? _expandedWidth : _collapsedWidth;
            float targetAlpha = expanded ? 1f : 0f;

            if (_expandedInfo != null)
                _expandedInfo.blocksRaycasts = expanded;

            try
            {
                UniTask widthTask = UniTask.CompletedTask;
                if (_expandRoot != null)
                {
                    widthTask = LMotion.Create(_expandRoot.sizeDelta.x, targetWidth, _expandDuration)
                        .WithEase(Ease.OutQuad)
                        .Bind(w => SetWidth(w))
                        .ToUniTask(token);
                }

                UniTask fadeTask = UniTask.CompletedTask;
                if (_expandedInfo != null)
                {
                    fadeTask = LMotion.Create(_expandedInfo.alpha, targetAlpha, _expandDuration)
                        .Bind(a => _expandedInfo.alpha = a)
                        .ToUniTask(token);
                }

                await UniTask.WhenAll(widthTask, fadeTask);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void SetWidth(float width)
        {
            var size = _expandRoot.sizeDelta;
            size.x = width;
            _expandRoot.sizeDelta = size;
        }

        private void OnDestroy()
        {
            if (GameStateUtil.IsQuitting) return;

            _skillSubscriptions.Dispose();
            if (_fillHandle.IsActive())
                _fillHandle.Cancel();
            if (_completeBoxPulseHandle.IsActive())
                _completeBoxPulseHandle.Cancel();
            _expandCts?.Cancel();
            _expandCts?.Dispose();
        }
    }
}
