using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using Backend.Util;
using Backend.Util.Interface;
using Backend.AddressableKey;
using Backend.Object.Management;
using TMPro;

namespace Backend.Object.CharacterObject
{
    public class CharacterSlot : CachedMonobehaviour, ICharacter
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

        private int _characterid;
        private Image _characterImage;

        private CancellationTokenSource _expandCts;

        public int Id => _characterid;

        public void Awake()
        {
            _characterImage = GetComponent<Image>();

            if (_expandRoot != null)
                SetWidth(_collapsedWidth);

            if (_expandedInfo != null)
            {
                _expandedInfo.alpha = 0f;
                _expandedInfo.blocksRaycasts = false;
            }
        }

        public void Initialize(UnitData unitData)
        {
            _characterid = unitData.unitId;
            _damageText.text = unitData.unitDamage.ToString("F0");
            _shieldText.text = unitData.unitDefense.ToString("F0");
            _resilienceText.text = unitData.unitResilience.ToString("F0");

            SetSlotColor(unitData.unitType);

            _characterImage.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.UI.Get(_characterid.ToString()));
        }

        public void OnSlotChanged(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot) return;

            // toSlot == 0 → 1번(맨 앞) 슬롯으로 진입 시 펼침
            SetExpanded(toSlot == 0);
        }

        private void SetSlotColor(UnitType type){
            _colorBorder.GetComponent<Image>().color = type switch
            {
                UnitType.fire => new Color(1f,   0.3f, 0.1f),
                UnitType.light => new Color(1f,   1f,   0.2f),
                UnitType.water => new Color(0.2f, 0.5f, 1f),
                UnitType.grass => new Color(0.2f, 0.8f, 0.2f),
            };
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

            _expandCts?.Cancel();
            _expandCts?.Dispose();
        }
    }
}
