using R3;
using System.Collections.Generic;
using Backend.Object.Controller;

namespace Backend.Object.GameSystems
{
    public static class BattleSystem
    {
        private static readonly CompositeDisposable subscriptions = new CompositeDisposable();

        private static int _totalBrokenCount;
        private static readonly Dictionary<PanelType, int> _brokenCountByType = new Dictionary<PanelType, int>();

        public static int TotalBrokenCount => _totalBrokenCount;

        public static void Initialize()
        {
            Dispose();

            PuzzleSystem.OnChainBroken
                .Subscribe(OnChainBroken)
                .AddTo(subscriptions);
        }

        public static void Dispose()
        {
            subscriptions.Clear();
            _totalBrokenCount = 0;
            _brokenCountByType.Clear();
        }

        private static void OnChainBroken(ChainBrokenInfo info)
        {
            _totalBrokenCount += info.TotalCount;

            foreach (var kvp in info.CountByType)
            {
                _brokenCountByType.TryGetValue(kvp.Key, out int cur);
                _brokenCountByType[kvp.Key] = cur + kvp.Value;
            }
        }

        private static void CompatibilityCheck(ChainBrokenInfo info){
            
        }

        public static void ExcutePlayerAttack(){
            // 1차 데미지 공식: 깨진 패널 1개당 100 데미지. 실제 콤보/속성 계산은 후속 작업.
            var monster = MonsterController.ActiveMonsters[0];
            if (monster != null && !monster.IsDefeated)
                monster.TakeDamage(_totalBrokenCount * 100f);

            _totalBrokenCount = 0;
            _brokenCountByType.Clear();
        }

        public static int GetBrokenCount(PanelType type)
            => _brokenCountByType.TryGetValue(type, out int v) ? v : 0;
    }
}
