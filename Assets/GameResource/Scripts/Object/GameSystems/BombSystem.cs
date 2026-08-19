using System;
using System.Collections.Generic;
using System.Threading;
using Backend.AddressableKey;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.PanelObject;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// SCP 파괴 등으로 떨어진 폭탄을 FIFO로 퓨즈·폭발시킨다.
    /// </summary>
    public static class BombSystem
    {
        private const float FuseSeconds = 2f;
        private const int PoolDefaultCapacity = 4;
        private const int PoolMaxSize = 16;
        private const string AddressableKeyName = "Bomb";

        private static readonly Queue<Bomb> _queue = new();

        private static bool _initialized;
        private static bool _running;
        private static int _pendingSpawns;
        private static CancellationTokenSource _cts;

        /// <summary>폭탄 큐를 처리 중이면 true.</summary>
        public static bool IsDetonating => _running || _pendingSpawns > 0;

        /// <summary>게임플레이 시작 시 1회 호출.</summary>
        public static void Initialize()
        {
            Dispose();
            _initialized = true;
            _cts = new CancellationTokenSource();
        }

        /// <summary>게임플레이 종료 시 큐·풀을 정리한다.</summary>
        public static void Dispose()
        {
            _initialized = false;
            _queue.Clear();
            _pendingSpawns = 0;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            ObjectPoolManager.ReleasePool<Bomb>();
            _running = false;
        }

        /// <summary>
        /// 월드 좌표에 물리 폭탄을 생성하고 폭발 큐에 넣는다.
        /// </summary>
        public static void Enqueue(Vector3 position)
        {
            if (!_initialized) return;
            EnqueueAsync(position).Forget();
        }

        /// <summary>
        /// 큐가 비고 처리가 끝날 때까지 대기한다.
        /// </summary>
        public static async UniTask WaitUntilIdle(CancellationToken token)
        {
            await UniTask.WaitUntil(
                () => _pendingSpawns <= 0 && !_running && _queue.Count == 0,
                cancellationToken: token);
        }

        private static async UniTaskVoid EnqueueAsync(Vector3 position)
        {
            _pendingSpawns++;
            try
            {
                Bomb bomb = await SpawnBombAsync(position);
                if (!_initialized || bomb == null) return;

                bomb.ResetMotion();
                _queue.Enqueue(bomb);
                TryStartProcessing();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _pendingSpawns--;
            }
        }

        private static void TryStartProcessing()
        {
            if (!_initialized || _running || _queue.Count == 0) return;
            ProcessQueueAsync().Forget();
        }

        private static async UniTaskVoid ProcessQueueAsync()
        {
            _running = true;
            try
            {
                while (_initialized && _queue.Count > 0)
                {
                    Bomb bomb = _queue.Dequeue();
                    await DetonateOneAsync(bomb);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _running = false;
            }
        }

        private static async UniTask DetonateOneAsync(Bomb bomb)
        {
            if (bomb == null) return;

            try
            {
                bomb.PlayFuse();
                await UniTask.Delay(TimeSpan.FromSeconds(FuseSeconds), cancellationToken: _cts.Token);

                Vector3 explosionPos = bomb.ExplosionPosition;
                PuzzleSystem.ApplyBombExplosion(explosionPos);
            }
            finally
            {
                if (bomb != null)
                {
                    bomb.StopFuse();
                    ObjectPoolManager.Release(bomb);
                }
            }
        }

        private static async UniTask<Bomb> SpawnBombAsync(Vector3 position)
        {
            string address = AddressableKeys.InGame.Get(AddressableKeyName);
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"[BombSystem] Addressable key '{AddressableKeyName}' is missing.");
                return null;
            }

            Pooling<Bomb> pool = await ObjectPoolManager.GetOrCreatePoolAsync<Bomb>(
                address,
                parent: null,
                defaultCapacity: PoolDefaultCapacity,
                maxSize: PoolMaxSize,
                onGet: b => b.gameObject.SetActive(true),
                onRelease: b => b.gameObject.SetActive(false),
                token: _cts.Token);

            if (!_initialized || pool == null) return null;

            Bomb bomb = pool.Get();
            if (bomb == null) return null;

            bomb.CachedTransform.position = position;
            bomb.ResetMotion();
            return bomb;
        }
    }
}
