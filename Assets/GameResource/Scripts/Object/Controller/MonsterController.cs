using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.MonsterObject;
using Backend.Util;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Pool;

namespace Backend.Object.Controller
{
    public class MonsterController : CachedMonobehaviour
    {
        private Pooling<Monster> _monsterPool;
        private int _preloadCount = 10;
        private int _maxLoadCount = 10;

        private readonly Dictionary<int, List<MonsterSpawnData>> _spawnsByFloor = new();
        private List<int> _floors;
        private int _floorIndex = -1;

        private RectTransform _monsterContainer;
        private readonly CompositeDisposable _disposables = new();

        /// <summary>Quest 전용: 아직 스폰하지 않은 층이 남아 있는지.</summary>
        public bool HasNextQuestFloor =>
            _floors != null && _floorIndex + 1 < _floors.Count;

        /// <summary>Quest 맵의 총 층 수.</summary>
        public int QuestFloorCount => _floors?.Count ?? 0;

        /// <summary>Quest 현재 진행 층(1-based). 스폰 전이면 0.</summary>
        public int CurrentQuestFloorDisplay => _floorIndex < 0 ? 0 : _floorIndex + 1;

        public void SetMonsterContainer(RectTransform monsterContainer)
        {
            _monsterContainer = monsterContainer;
        }

        /// <summary>Quest 맵 스폰 테이블을 준비하고 전멸 구독을 건다.</summary>
        public async UniTask PrepareQuestAsync(int questMapId)
        {
            await EnsurePoolAsync();
            SubscribeDefeat();
            BuildFloors(questMapId);
        }

        /// <summary>Classic Run 용 풀·전멸 구독만 준비한다.</summary>
        public async UniTask PrepareClassicAsync()
        {
            _floors = null;
            _floorIndex = -1;
            _spawnsByFloor.Clear();
            await EnsurePoolAsync();
            SubscribeDefeat();
        }

        private async UniTask EnsurePoolAsync()
        {
            if (_monsterPool != null) return;

            _monsterPool = await ObjectPoolManager.GetOrCreatePoolAsync<Monster>(
                AddressableKeys.UI.Get("Monster"),
                _preloadCount,
                parent: _monsterContainer,
                defaultCapacity: _maxLoadCount,
                maxSize: 24,
                onGet: p => p.gameObject.SetActive(true),
                onRelease: p => p.gameObject.SetActive(false));
        }

        private void SubscribeDefeat()
        {
            _disposables.Clear();

            MonsterSystem.OnMonsterRemoved
                .Subscribe(ReleaseMonster)
                .AddTo(_disposables);

            MonsterSystem.OnAllDefeated
                .Subscribe(_ => OnAllMonstersDefeated())
                .AddTo(_disposables);
        }

        private void OnAllMonstersDefeated()
        {
            ActiveSession.Current?.OnAllMonstersDefeated(this);
        }

        private void BuildFloors(int questMapId)
        {
            _spawnsByFloor.Clear();
            var spawns = TableManager.GetMonsterSpawns(questMapId);
            if (spawns == null) return;

            foreach (var spawn in spawns)
            {
                if (!_spawnsByFloor.TryGetValue(spawn.questFloor, out var list))
                {
                    _spawnsByFloor[spawn.questFloor] = list = new List<MonsterSpawnData>();
                }
                list.Add(spawn);
            }

            _floors = new List<int>(_spawnsByFloor.Keys);
            _floors.Sort();
            _floorIndex = -1;
        }

        /// <summary>Quest 다음 층을 스폰한다.</summary>
        public bool SpawnQuestNextFloor()
        {
            if (!HasNextQuestFloor) return false;
            _floorIndex++;

            foreach (var spawn in _spawnsByFloor[_floors[_floorIndex]])
                CreateMonster(spawn.monsterId, spawn.behaviorSetId);

            if (MonsterSystem.CurrentTarget.CurrentValue == null)
                MonsterSystem.ResolveTarget();

            return true;
        }

        public bool SpawnClassicFloor(int floor)
        {
            var floorData = TableManager.GetRunFloor(floor);
            if (floorData == null) return false;

            var group = TableManager.GetSpawnGroup(floorData.spawnGroupId);
            if (group == null || group.Count == 0) return false;

            foreach (var spawn in group)
                CreateMonster(spawn.monsterId, spawn.behaviorSetId, floorData.hpScale, floorData.atkScale);

            if (MonsterSystem.CurrentTarget.CurrentValue == null)
                MonsterSystem.ResolveTarget();

            return true;
        }

        public void CreateMonster(int monsterId, int behaviorSetId, float hpScale = 1f, float atkScale = 1f)
        {
            var monsterData = TableManager.GetMonsterData(monsterId);
            var behaviorData = TableManager.GetMonsterBehaviors(behaviorSetId);
            var actionGroups = TableManager.GetActionGroups(behaviorSetId);
            if (monsterData == null || behaviorData == null || actionGroups == null) return;

            var monster = _monsterPool.Get();
            monster.InitializeMonster(monsterData, behaviorData, actionGroups, hpScale, atkScale);
            MonsterSystem.MonsterRegister(monster);
        }

        private void ReleaseMonster(Monster monster) => _monsterPool?.Release(monster);

        private void OnDestroy()
        {
            if (GameStateUtil.IsQuitting) return;

            _disposables.Dispose();

            if (_monsterPool == null) return;

            var active = _monsterPool.GetAllActive();
            foreach (var monster in active)
                _monsterPool.Release(monster);
            ListPool<Monster>.Release(active);

            ObjectPoolManager.ReleasePool<Monster>();
            _monsterPool = null;
        }
    }
}
