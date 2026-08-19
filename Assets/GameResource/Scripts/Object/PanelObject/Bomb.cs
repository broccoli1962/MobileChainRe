using Backend.Util;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Backend.Object.PanelObject
{
    /// <summary>
    /// SCP 파괴 등으로 필드에 떨어지는 폭탄. 콜라이더·강체로 패널과 물리 상호작용한다.
    /// </summary>
    public class Bomb : CachedMonobehaviour
    {
        private const float PulseScale = 1.25f;
        private const float PulseDuration = 0.3f;

        [SerializeField] private SpriteRenderer _visual;

        private MotionHandle _fuseHandle;
        private Vector3 _baseVisualScale = Vector3.one;
        private bool _baseVisualScaleCached;

        /// <summary>폭발 판정에 쓰는 현재 월드 좌표.</summary>
        public Vector3 ExplosionPosition => CachedTransform.position;

        private Transform FuseTarget => _visual != null ? _visual.transform : CachedTransform;

        private void Awake()
        {
            if (_visual == null)
                TryGetComponent(out _visual);
            CacheBaseVisualScale();
        }

        private void OnDisable()
        {
            StopFuse();
        }

        /// <summary>
        /// 풀에서 꺼낼 때 속도와 바디 타입을 초기화한다.
        /// </summary>
        public void ResetMotion()
        {
            if (!TryGetComponent(out Rigidbody2D rb)) return;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        /// <summary>
        /// 퓨즈 동안 비주얼만 스케일 펄스한다. 콜라이더 크기는 유지한다.
        /// </summary>
        public void PlayFuse()
        {
            StopFuse();
            CacheBaseVisualScale();
            FuseTarget.localScale = _baseVisualScale;

            _fuseHandle = LMotion.Create(_baseVisualScale, _baseVisualScale * PulseScale, PulseDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .BindToLocalScale(FuseTarget);
        }

        /// <summary>
        /// 퓨즈 연출을 중단하고 비주얼 스케일을 복구한다.
        /// </summary>
        public void StopFuse()
        {
            if (_fuseHandle.IsActive())
                _fuseHandle.Cancel();

            if (_baseVisualScaleCached)
                FuseTarget.localScale = _baseVisualScale;
        }

        private void CacheBaseVisualScale()
        {
            if (_baseVisualScaleCached) return;
            _baseVisualScale = FuseTarget.localScale;
            if (_baseVisualScale.sqrMagnitude <= 0f)
                _baseVisualScale = Vector3.one;
            _baseVisualScaleCached = true;
        }
    }
}
