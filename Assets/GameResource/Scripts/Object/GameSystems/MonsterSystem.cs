using System.Collections.Generic;
using Backend.Object.Management;
using Backend.Object.MonsterObject;
using R3;

namespace Backend.Object.GameSystems
{
    public static class MonsterSystem
    {
        private static readonly List<Monster> _activeMonsters = new();
        private static Monster _currentTarget;

        public static IReadOnlyList<Monster> ActiveMonsters => _activeMonsters;
        public static bool IsAllDefeated => _activeMonsters.Count == 0;

        private static readonly Subject<Monster> _onMonsterRemoved = new();
        public static Observable<Monster> OnMonsterRemoved => _onMonsterRemoved;

        private static readonly Subject<Unit> _onAllDefeated = new();
        /// <summary>등록된 몬스터가 존재하던 상태에서 전원 처치되어 0명이 된 시점에 발행.</summary>
        public static Observable<Unit> OnAllDefeated => _onAllDefeated;

        private static readonly ReactiveProperty<Monster> _currentTargetRp = new();
        public static ReadOnlyReactiveProperty<Monster> CurrentTarget => _currentTargetRp;

        public static void MonsterRegister(Monster monster)
        {
            if (!_activeMonsters.Contains(monster))
            {
                _activeMonsters.Add(monster);
            }
        }

        public static bool TrySelectTarget(Monster monster){
            if(monster == null || monster.IsDefeated) return false;
            if(!_activeMonsters.Contains(monster)) return false;
        
            if(GameManager.CurrentPhase != GamePhase.PlayerTurn) return false;

            SetTarget(monster);
            return true;
        }

        public static void SetTarget(Monster target)
        {
            _currentTarget = target;
            _currentTargetRp.Value = target;
        }

        public static Monster ResolveTarget()
        {
            if (_currentTarget != null && !_currentTarget.IsDefeated)
                return _currentTarget;

            foreach (var monster in _activeMonsters)
            {
                if (!monster.IsDefeated)
                {
                    SetTarget(monster);
                    return monster;
                }
            }

            SetTarget(null);
            return null;
        }

        public static void CleanUpDefeated(){
            bool anyRemoved = false;

            for(int i = _activeMonsters.Count - 1; i >= 0; i--){
                var monster = _activeMonsters[i];
                if(!monster.IsDefeated) continue;

                _activeMonsters.RemoveAt(i);
                if(_currentTarget == monster) SetTarget(null);
                _onMonsterRemoved.OnNext(monster);
                anyRemoved = true;
            }

            if(anyRemoved && _activeMonsters.Count == 0)
                _onAllDefeated.OnNext(Unit.Default);
        }

        public static void Dispose(){
            _activeMonsters.Clear();
            _currentTarget = null;
            _currentTargetRp.Value = null;
        }
    }
}
