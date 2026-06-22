using UnityEngine;
using UnityEngine.UI;
using LitMotion;

namespace Backend.Object.UI
{
    /// <summary>
    /// 한 줄 연속형 게이지. 플레이어 체력, 로딩 게이지 등에 사용한다.
    /// </summary>
    public class SingleGaugeBar : CommonGaugeBar
    {
        [Header("Fill")]
        [SerializeField] private RectTransform _fillRect;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _fillColor = new Color(0.55f, 0.85f, 0.35f, 1f);

        [Header("Animation")]
        [SerializeField] private float _animationDuration = 0.2f;

        private float _current;
        private float _max = 1f;
        private MotionHandle _motionHandle;

        protected override void Awake()
        {
            base.Awake();
            if (_fillImage != null)
                _fillImage.color = _fillColor;
            ApplyFill(GetNormalized());
        }

        public void SetValues(float current, float max, bool animate = false)
        {
            _current = current;
            _max = max;
            UpdateFill(animate);
        }

        public void SetNormalized(float normalized, bool animate = false)
        {
            _current = Mathf.Clamp01(normalized);
            _max = 1f;
            UpdateFill(animate);
        }

        private float GetNormalized()
            => _max > 0f ? Mathf.Clamp01(_current / _max) : 0f;

        private void UpdateFill(bool animate)
        {
            float target = GetNormalized();

            if (_motionHandle.IsActive())
                _motionHandle.Cancel();

            if (!animate || !gameObject.activeInHierarchy)
            {
                ApplyFill(target);
                return;
            }

            float from = _fillRect.localScale.x;
            _motionHandle = LMotion.Create(from, target, _animationDuration)
                .Bind(v => ApplyFill(v));
        }

        public void SetColor(Color color)
        {
            _fillImage.color = color;
        }

        private void ApplyFill(float normalized)
        {
            var scale = _fillRect.localScale;
            scale.x = normalized;
            _fillRect.localScale = scale;
        }
    }
}
