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

        public bool HasNextFloor => _floors != null && _floorIndex + 1 < _floors.Count;

        public void SetMonsterContainer(RectTransform monsterContainer)
        {
            _monsterContainer = monsterContainer;
        }

        public async UniTask InitializeAsync(int questMapId)
        {
            _monsterPool = await ObjectPoolManager.GetOrCreatePoolAsync<Monster>(
                AddressableKeys.UI.Get("Monster"),
                _preloadCount,
                parent: _monsterContainer,
                defaultCapacity: _maxLoadCount,
                maxSize: 24,
                onGet: p => p.gameObject.SetActive(true),
                onRelease: p => p.gameObject.SetActive(false));

            MonsterSystem.OnMonsterRemoved
                .Subscribe(ReleaseMonster)
                .AddTo(_disposables);

            MonsterSystem.OnAllDefeated
                .Subscribe(_ => OnAllMonstersDefeated())
                .AddTo(_disposables);

            BuildFloors(questMapId);
        }

        private void OnAllMonstersDefeated()
        {
            if (HasNextFloor)
                SpawnNextFloor();
            else
                GameManager.StageClear();
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

        public bool SpawnNextFloor()
        {
            if (!HasNextFloor) return false;
            _floorIndex++;

            foreach (var spawn in _spawnsByFloor[_floors[_floorIndex]])
                CreateMonster(spawn.monsterId, spawn.behaviorSetId);

            if(MonsterSystem.CurrentTarget.CurrentValue == null){
                MonsterSystem.ResolveTarget();
            }

            return true;
        }

        public void CreateMonster(int monsterId, int behaviorSetId)
        {
            var monsterData = TableManager.GetMonsterData(monsterId);
            var behaviorData = TableManager.GetMonsterBehaviors(behaviorSetId);
            var actionGroups = TableManager.GetActionGroups(behaviorSetId);
            if (monsterData == null || behaviorData == null || actionGroups == null) return;

            var monster = _monsterPool.Get();
            monster.InitializeMonster(monsterData, behaviorData, actionGroups);
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
