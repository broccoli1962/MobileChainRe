using System.Collections.Generic;
using Backend.Util;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 보스용 다중 레이어 체력바.
    /// - InstantFill: 데미지 즉시 반영
    /// - DelayedFill: 지연 감소 (철권 스타일 ghost bar)
    /// - UnderlayFill: 다음 레이어 색상을 미리 노출
    /// 현재 레이어가 소진되면 다음 레이어로 자동 전환되며, 레이어 수 텍스트는 표시하지 않는다.
    /// </summary>
    public class MonsterHealthBar : CommonGaugeBar
    {
        [Header("Fills")]
        [SerializeField] private RectTransform _instantFillRect;
        [SerializeField] private Image _instantFillImage;
        [SerializeField] private RectTransform _delayedFillRect;
        [SerializeField] private Image _delayedFillImage;
        [SerializeField] private Image _underlayFillImage;

        [Header("Animation")]
        [SerializeField] private float _delayedDuration = 0.6f;
        [SerializeField] private float _delayedStartDelay = 0.2f;

        private KeyframeColorGradient _gradient;
        private IReadOnlyList<float> _layerMaxHp;
        private int _currentLayerIndex;
        private float _currentHp;
        private float _delayedHp;
        private MotionHandle _delayedMotion;

        public int CurrentLayerIndex => _currentLayerIndex;
        public bool IsDefeated => _layerMaxHp != null && _currentLayerIndex >= _layerMaxHp.Count;

        public void Initialize(IReadOnlyList<float> layerMaxHp, KeyframeColorGradient gradient)
        {
            _layerMaxHp = layerMaxHp;
            _gradient = gradient;
            SetLayer(0, layerMaxHp != null && layerMaxHp.Count > 0 ? layerMaxHp[0] : 0f, instantDelayedSnap: true);
        }

        public void SetLayer(int layerIndex, float currentHp, bool instantDelayedSnap = false)
        {
            CancelDelayedMotion();

            _currentLayerIndex = layerIndex;

            if (IsDefeated)
            {
                _currentHp = 0f;
                _delayedHp = 0f;
                ApplyFills(0f, 0f);
                return;
            }

            _currentHp = Mathf.Max(0f, currentHp);
            _delayedHp = instantDelayedSnap ? _currentHp : _delayedHp;

            ApplyLayerColors();
            ApplyFills(GetNormalized(_currentHp), GetNormalized(_delayedHp));
        }

        /// <summary>
        /// 데미지를 적용하고 필요 시 다음 레이어로 전환한다.
        /// 반환값은 전환된 레이어의 새 인덱스 차이(>=1 이면 phase 전환 발생).
        /// </summary>
        public int ApplyDamage(float damage)
        {
            if (IsDefeated || damage <= 0f)
                return 0;

            int startLayer = _currentLayerIndex;

            while (damage > 0f && !IsDefeated)
            {
                float consume = Mathf.Min(_currentHp, damage);
                _currentHp -= consume;
                damage -= consume;

                if (_currentHp > 0f)
                    break;

                _currentLayerIndex++;
                if (IsDefeated)
                {
                    _currentHp = 0f;
                    _delayedHp = 0f;
                    break;
                }

                _currentHp = _layerMaxHp[_currentLayerIndex];
                _delayedHp = _currentHp;
                ApplyLayerColors();
            }

            UpdateFills();
            return _currentLayerIndex - startLayer;
        }

        private void UpdateFills()
        {
            CancelDelayedMotion();

            float instantTarget = GetNormalized(_currentHp);
            ApplyInstantFill(instantTarget);

            if (!gameObject.activeInHierarchy)
            {
                _delayedHp = _currentHp;
                ApplyDelayedFill(instantTarget);
                return;
            }

            float delayedStart = GetNormalized(_delayedHp);
            ApplyDelayedFill(delayedStart);

            _delayedMotion = LMotion.Create(delayedStart, instantTarget, _delayedDuration)
                .WithDelay(_delayedStartDelay)
                .WithEase(Ease.OutCubic)
                .Bind(v =>
                {
                    ApplyDelayedFill(v);
                    _delayedHp = GetHpFromNormalized(v);
                });
        }

        private void ApplyLayerColors()
        {
            if (_gradient == null || _layerMaxHp == null || _layerMaxHp.Count == 0)
                return;

            int count = _layerMaxHp.Count;
            Color currentColor = _gradient.Evaluate(_currentLayerIndex, count);

            if (_instantFillImage != null)
                _instantFillImage.color = currentColor;
            if (_delayedFillImage != null)
                _delayedFillImage.color = currentColor;

            if (_underlayFillImage != null)
            {
                bool hasNext = _currentLayerIndex + 1 < count;
                _underlayFillImage.enabled = hasNext;
                if (hasNext)
                    _underlayFillImage.color = _gradient.Evaluate(_currentLayerIndex + 1, count);
            }
        }

        private float GetNormalized(float hp)
        {
            if (_layerMaxHp == null || _currentLayerIndex >= _layerMaxHp.Count)
                return 0f;
            float max = _layerMaxHp[_currentLayerIndex];
            return max > 0f ? Mathf.Clamp01(hp / max) : 0f;
        }

        private float GetHpFromNormalized(float normalized)
        {
            if (_layerMaxHp == null || _currentLayerIndex >= _layerMaxHp.Count)
                return 0f;
            return Mathf.Clamp01(normalized) * _layerMaxHp[_currentLayerIndex];
        }

        private void ApplyFills(float instantNormalized, float delayedNormalized)
        {
            ApplyInstantFill(instantNormalized);
            ApplyDelayedFill(delayedNormalized);
        }

        private void ApplyInstantFill(float normalized)
        {
            if (_instantFillRect == null) return;
            var scale = _instantFillRect.localScale;
            scale.x = normalized;
            _instantFillRect.localScale = scale;
        }

        private void ApplyDelayedFill(float normalized)
        {
            if (_delayedFillRect == null) return;
            var scale = _delayedFillRect.localScale;
            scale.x = normalized;
            _delayedFillRect.localScale = scale;
        }

        private void CancelDelayedMotion()
        {
            if (_delayedMotion.IsActive())
                _delayedMotion.Cancel();
        }

        private void OnDisable()
        {
            CancelDelayedMotion();
        }
    }
}
