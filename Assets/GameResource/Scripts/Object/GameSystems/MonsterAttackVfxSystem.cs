using System;
using System.Threading;
using Backend.AddressableKey;
using Backend.Object.CharacterObject;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.MonsterObject;
using Backend.Util;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// 몬스터 공격 시 빛나는 구체가 몬스터(원점)에서 피격 캐릭터 슬롯으로 날아가는 연출 레이어.
    /// AttackVfxSystem(플레이어 → 몬스터)의 대칭 구조이며, MonsterAttackSystem의 데미지 로직과는 분리된
    /// 순수 연출이다. 반환되는 UniTask는 착탄(비행 완료) 시점에 완료되어 호출자가 데미지 적용 타이밍을
    /// 맞출 수 있게 하며, 착탄 이후의 잔상/소멸 처리는 fire-and-forget 으로 이어진다.
    /// 단일 프리팹(MonsterAttackFx)을 몬스터 속성 색으로 런타임 틴트해 재사용한다.
    /// </summary>
    public static class MonsterAttackVfxSystem
    {
        private const string KeyName = "MonsterAttackFx";
        private const float FlyDuration = 0.4f;          // 원점 → 캐릭터 비행 시간(느리게 보이도록)
        private const float BurstLingerSeconds = 0.25f;  // 도착 후 잔상/소멸 대기
        private const int PoolDefaultCapacity = 6;
        private const int PoolMaxSize = 16;

        private static bool _initialized;
        private static RectTransform _vfxRoot;

        /// <summary>게임플레이 시작 시 1회 호출.</summary>
        public static void Initialize()
        {
            _initialized = true;
        }

        /// <summary>게임플레이 종료 시 풀/오버레이 루트를 정리한다.</summary>
        public static void Dispose()
        {
            _initialized = false;

            ObjectPoolManager.ReleasePool(KeyName);

            if (_vfxRoot != null)
                UnityEngine.Object.Destroy(_vfxRoot.gameObject);
            _vfxRoot = null;
        }

        /// <summary>
        /// 몬스터 공격 연출을 발사한다. 반환된 UniTask는 구체의 비행과 착탄 후 잔상/소멸까지 전체 연출이
        /// 끝난 시점에 완료된다. 데미지는 착탄 순간에 적용되도록 <paramref name="onImpact"/>로 전달하면,
        /// 연출 종료를 await 하면서도 데미지 타이밍은 착탄에 맞출 수 있다.
        /// </summary>
        public static UniTask PlayMonsterAttackAsync(Monster attacker, CharacterSlot target, Action onImpact = null, CancellationToken token = default)
        {
            if (!_initialized || attacker == null || target == null)
            {
                onImpact?.Invoke();
                return UniTask.CompletedTask;
            }

            Color tint = ColorUtil.GetPanelTypeColor(attacker.MonsterType);
            return FlyOrbAsync(attacker.CachedTransform.position, target, tint, onImpact, token);
        }

        private static async UniTask FlyOrbAsync(Vector3 originPos, CharacterSlot target, Color tint, Action onImpact, CancellationToken token)
        {
            var vfxRoot = GetOrCreateVfxRoot(target);
            if (vfxRoot == null) { onImpact?.Invoke(); return; }

            Vector3 targetPos = target.CachedTransform.position;

            var pool = await ObjectPoolManager.GetOrCreatePoolAsync<UIParticle>(
                KeyName,
                AddressableKeys.InGame.Get(KeyName),
                parent: vfxRoot,
                defaultCapacity: PoolDefaultCapacity,
                maxSize: PoolMaxSize,
                onGet: p => p.gameObject.SetActive(true),
                onRelease: p => p.gameObject.SetActive(false));

            // 로드 대기 중 Dispose 된 경우 중단.
            if (!_initialized || pool == null) { onImpact?.Invoke(); return; }

            var emitter = pool.Get();
            if (emitter == null) { onImpact?.Invoke(); return; }

            emitter.transform.SetParent(vfxRoot, false);
            emitter.transform.position = originPos;
            emitter.transform.SetAsLastSibling();
            ApplyTint(emitter, tint);
            emitter.Play();

            try
            {
                await LMotion.Create(originPos, targetPos, FlyDuration)
                    .WithEase(Ease.InQuad)
                    .Bind(p => { if (emitter != null) emitter.transform.position = p; })
                    .ToUniTask(token);

                // 착탄 순간에 데미지를 적용하고, 잔상/소멸까지 기다린 뒤 반환한다(연출 전체를 await).
                onImpact?.Invoke();

                await UniTask.Delay(TimeSpan.FromSeconds(BurstLingerSeconds), cancellationToken: token);
            }
            finally
            {
                if (emitter != null)
                    ObjectPoolManager.Release(KeyName, emitter);
            }
        }

        // 중립(흰색) 프리팹의 파티클 시작 색을 몬스터 속성 색으로 덮어써 재사용한다.
        private static void ApplyTint(UIParticle emitter, Color tint)
        {
            var systems = emitter.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(tint);
            }
        }

        private static RectTransform GetOrCreateVfxRoot(CharacterSlot target)
        {
            if (_vfxRoot != null) return _vfxRoot;

            var canvas = target.CachedTransform.GetComponentInParent<Canvas>();
            if (canvas == null) return null;

            var go = new GameObject("MonsterAttackVfxRoot", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();

            _vfxRoot = rt;
            return _vfxRoot;
        }
    }
}
