using System;
using System.Collections.Generic;
using System.Threading;
using Backend.AddressableKey;
using Backend.Object.CharacterObject;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.MonsterObject;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// 플레이어 공격 시 속성별 입자가 파티(원점)에서 타깃 몬스터로 날아가는 연출 레이어.
    /// BattleSystem 의 데미지 로직과는 분리된 순수 연출이다. 반환되는 UniTask는 착탄(비행 완료)
    /// 시점에 완료되어 BattleSystem이 피격 연출 타이밍을 맞출 수 있게 하며, 착탄 이후의 잔상/소멸
    /// 처리는 fire-and-forget 으로 계속된다.
    /// 레이아웃 그룹의 영향을 받지 않도록 캔버스 직속의 전용 오버레이 루트에서 재생한다.
    /// </summary>
    public static class AttackVfxSystem
    {
        private const float FlyDuration = 0.05f;      // 원점 → 몬스터 비행 시간
        private const float BurstLingerSeconds = 0.25f; // 도착 후 잔상/소멸 대기
        // 연속 타격 시 여러 발이 동시에 비행/잔상 상태로 존재할 수 있으므로 넉넉히 확보한다.
        private const int PoolDefaultCapacity = 6;
        private const int PoolMaxSize = 16;

        private static readonly Dictionary<UnitType, string> _keyNameByUnitType = new()
        {
            { UnitType.fire,  "AttackFx_Fire" },
            { UnitType.grass, "AttackFx_Grass" },
            { UnitType.light, "AttackFx_Light" },
            { UnitType.water, "AttackFx_Water" },
        };

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

            foreach (var keyName in _keyNameByUnitType.Values)
                ObjectPoolManager.ReleasePool(keyName);

            if (_vfxRoot != null)
                UnityEngine.Object.Destroy(_vfxRoot.gameObject);
            _vfxRoot = null;
        }

        /// <summary>
        /// 플레이어 공격 연출을 발사한다. 반환된 UniTask는 투사체가 몬스터에 착탄하는 시점에
        /// 완료되므로, 호출자는 이를 await 한 뒤 피격 연출을 재생해 타이밍을 맞출 수 있다.
        /// 착탄 이후의 잔상/소멸 처리는 별도로 fire-and-forget 진행된다.
        /// 선두(1번 슬롯) 캐릭터의 속성 1종만 발사한다.
        /// </summary>
        public static UniTask PlayPlayerAttackAsync(Monster target, CancellationToken token = default)
        {
            if (!_initialized || target == null) return UniTask.CompletedTask;

            // 원점 및 속성은 모두 선두(1번 슬롯) 캐릭터 기준.
            if (CharacterSystem.Count < 1 || CharacterSystem.GetCharacter(1) is not CharacterSlot frontSlot)
                return UniTask.CompletedTask;

            if (!_keyNameByUnitType.TryGetValue(frontSlot.UnitData.unitType, out var keyName))
                return UniTask.CompletedTask;

            return FlyElementAsync(keyName, frontSlot.CachedTransform.position, target, token);
        }

        private static async UniTask FlyElementAsync(string keyName, Vector3 originPos, Monster target, CancellationToken token)
        {
            var vfxRoot = GetOrCreateVfxRoot(target);
            if (vfxRoot == null) return;

            Vector3 targetPos = target.CachedTransform.position;

            var pool = await ObjectPoolManager.GetOrCreatePoolAsync<UIParticle>(
                keyName,
                AddressableKeys.InGame.Get(keyName),
                parent: vfxRoot,
                defaultCapacity: PoolDefaultCapacity,
                maxSize: PoolMaxSize,
                onGet: p => p.gameObject.SetActive(true),
                onRelease: p => p.gameObject.SetActive(false));

            // 로드 대기 중 Dispose 된 경우 중단.
            if (!_initialized || pool == null) return;

            var emitter = pool.Get();
            if (emitter == null) return;

            emitter.transform.SetParent(vfxRoot, false);
            emitter.transform.position = originPos;
            emitter.transform.SetAsLastSibling();
            emitter.Play();

            bool handedOff = false;
            try
            {
                await LMotion.Create(originPos, targetPos, FlyDuration)
                    .WithEase(Ease.InQuad)
                    .Bind(p => { if (emitter != null) emitter.transform.position = p; })
                    .ToUniTask(token);

                // 착탄. 잔상/소멸 처리는 여기서 넘겨받아 fire-and-forget으로 이어간다.
                handedOff = true;
                LingerAndReleaseAsync(keyName, emitter, token).Forget();
            }
            finally
            {
                if (!handedOff && emitter != null)
                    ObjectPoolManager.Release(keyName, emitter);
            }
        }

        private static async UniTaskVoid LingerAndReleaseAsync(string keyName, UIParticle emitter, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(BurstLingerSeconds), cancellationToken: token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (emitter != null)
                    ObjectPoolManager.Release(keyName, emitter);
            }
        }

        private static RectTransform GetOrCreateVfxRoot(Monster target)
        {
            if (_vfxRoot != null) return _vfxRoot;

            var canvas = target.CachedTransform.GetComponentInParent<Canvas>();
            if (canvas == null) return null;

            var go = new GameObject("AttackVfxRoot", typeof(RectTransform));
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
