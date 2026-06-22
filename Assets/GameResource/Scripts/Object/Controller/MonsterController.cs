using Backend.Util;
using Backend.Object.MonsterObject;
using Backend.Object.Management;
using Backend.AddressableKey;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Backend.Object.Management.Pool;
using System.Collections.Generic;

namespace Backend.Object.Controller
{
    public class MonsterController : CachedMonobehaviour
    {
        //미리 몬스터들 로드하기
        private Pooling<Monster> _monsterPool;
        private int _preloadCount = 10;
        private int _maxLoadCount = 10;

        public static List<Monster> ActiveMonsters { get; private set; }

        private void Start(){
            InitializeAndSubscribeAsync().Forget();
        }
  
        private async UniTaskVoid InitializeAndSubscribeAsync(){
            _monsterPool = await ObjectPoolManager.GetOrCreatePoolAsync<Monster>(
                AddressableKeys.UI.Get("Monster"),
                _preloadCount,
                parent: CachedTransform,
                defaultCapacity: _maxLoadCount,
                maxSize: 24,
                onGet: p => p.gameObject.SetActive(true),
                onRelease: p => p.gameObject.SetActive(false));

            CreateMonster();
        }

        private void CreateMonster(){
            Monster monster = _monsterPool.Get();

            //테스트
            int monsterId = 101;
            int behaviorSetId = 5001;

            MonsterData monsterData = TableManager.GetMonsterData(monsterId);
            IReadOnlyList<MonsterBehaviorData> behaviorData = TableManager.GetMonsterBehaviors(behaviorSetId);

            monster.InitializeMonster(monsterData, behaviorData);
            ActiveMonsters.Add(monster);
        }

        private void OnDestroy(){
            if (ActiveMonsters != null)
                ActiveMonsters.Clear();
            _monsterPool?.Dispose();
        }
    }
}
