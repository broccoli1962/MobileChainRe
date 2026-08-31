using System.Collections.Generic;
using Backend.Util;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 보스용 다중 레이어 체력바.
    /// - InstantFill: 데미지를 짧은 트윈으로 반영 (히트 1회당 1스텝)
    /// - UnderlayFill: 다음 레이어 색상을 미리 노출
    /// 현재 레이어가 소진되면 다음 레이어로 자동 전환되며, 레이어 수 텍스트는 표시하지 않는다.
    /// </summary>
    public class MonsterHealthBar : CommonGaugeBar
    {
        [Header("Fills")]
        [SerializeField] private RectTransform _instantFillRect;
        [SerializeField] private Image _instantFillImage;
        [SerializeField] private Image _underlayFillImage;

        [Header("Animation")]
        [SerializeField] private float _fillTweenDuration = 0.12f;

        private KeyframeColorGradient _gradient;
        private IReadOnlyList<float> _layerMaxHp;
        private int _currentLayerIndex;
        private float _currentHp;
        private float _displayedNormalized;
        private MotionHandle _fillMotion;

        public int CurrentLayerIndex => _currentLayerIndex;
        public float CurrentHp => _currentHp;
        public float CurrentLayerMaxHp => (_layerMaxHp != null && _currentLayerIndex < _layerMaxHp.Count) ? _layerMaxHp[_currentLayerIndex] : 0f;
        public bool IsDefeated => _layerMaxHp != null && _currentLayerIndex >= _layerMaxHp.Count;

        public void Initialize(IReadOnlyList<float> layerMaxHp, KeyframeColorGradient gradient)
        {
            _layerMaxHp = layerMaxHp;
            _gradient = gradient;
            SetLayer(0, layerMaxHp != null && layerMaxHp.Count > 0 ? layerMaxHp[0] : 0f);
        }

        public void SetLayer(int layerIndex, float currentHp)
        {
            CancelFillMotion();

            _currentLayerIndex = layerIndex;

            if (IsDefeated)
            {
                _currentHp = 0f;
                SnapFill(0f);
                return;
            }

            _currentHp = Mathf.Max(0f, currentHp);

            ApplyLayerColors();
            SnapFill(GetNormalized(_currentHp));
        }

        /// <summary>
        /// 데미지를 적용하고 필요 시 다음 레이어로 전환한다.
        /// 한 번의 공격은 최대 1개 레이어만 파괴하며, 레이어를 파괴하고 남은 잉여 데미지는 폐기한다(오버킬 방지).
        /// 반환값은 전환된 레이어의 새 인덱스 차이(>=1 이면 phase 전환 발생).
        /// </summary>
        public int ApplyDamage(float damage)
        {
            if (IsDefeated || damage <= 0f)
                return 0;

            int startLayer = _currentLayerIndex;

            float consume = Mathf.Min(_currentHp, damage);
            _currentHp -= consume;

            if (_currentHp <= 0f)
            {
                _currentLayerIndex++;
                if (IsDefeated)
                {
                    _currentHp = 0f;
                }
                else
                {
                    _currentHp = _layerMaxHp[_currentLayerIndex];
                    ApplyLayerColors();
                }
            }

            AnimateFillTo(GetNormalized(_currentHp));
            return _currentLayerIndex - startLayer;
        }

        /// <summary>현재 레이어 안에서만 회복한다. 레이어를 넘어선 회복/복구는 하지 않는다.</summary>
        public void Heal(float amount)
        {
            if (IsDefeated || amount <= 0f)
                return;

            _currentHp = Mathf.Min(_layerMaxHp[_currentLayerIndex], _currentHp + amount);
            AnimateFillTo(GetNormalized(_currentHp));
        }

        private void ApplyLayerColors()
        {
            if (_gradient == null || _layerMaxHp == null || _layerMaxHp.Count == 0)
                return;

            int count = _layerMaxHp.Count;
            Color currentColor = _gradient.Evaluate(_currentLayerIndex, count);

            if (_instantFillImage != null)
                _instantFillImage.color = currentColor;

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

        private void SnapFill(float normalized)
        {
            _displayedNormalized = normalized;
            SetFillScale(normalized);
        }

        private void AnimateFillTo(float normalized)
        {
            CancelFillMotion();

            float from = _displayedNormalized;
            _fillMotion = LMotion.Create(from, normalized, _fillTweenDuration)
                .WithEase(Ease.OutCubic)
                .Bind(v =>
                {
                    _displayedNormalized = v;
                    SetFillScale(v);
                });
        }

        private void SetFillScale(float normalized)
        {
            if (_instantFillRect == null) return;
            var scale = _instantFillRect.localScale;
            scale.x = normalized;
            _instantFillRect.localScale = scale;
        }

        private void CancelFillMotion()
        {
            if (_fillMotion.IsActive())
                _fillMotion.Cancel();
        }

        private void OnDisable()
        {
            CancelFillMotion();
        }
    }
}
