using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Backend.Util;
using Backend.Util.Interface;
using Backend.Util.Enum;
using Backend.AddressableKey;
using Backend.Object.Management;

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

        private string _characterid;
        private Image _characterImage;
        private float _currentDamage;
        private float _currentShield;
        private CharacterType _currentType;

        private CancellationTokenSource _expandCts;

        public string Id => _characterid;
        public CharacterType Type => _currentType;

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

        public void Initialize(string id, CharacterType type)
        {
            _characterid = id;
            _currentType = type;
            _characterImage.sprite = ResourceManager.LoadResource<Sprite>(AddressableKeys.UI.Get(_characterid));
        }

        public void OnSlotChanged(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot) return;

            // toSlot == 0 → 1번(맨 앞) 슬롯으로 진입 시 펼침
            SetExpanded(toSlot == 0);
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
