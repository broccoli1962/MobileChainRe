using Backend.Util.Enum;
using R3;
using System.Collections.Generic;

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

            // TODO: 실제 전투 연산 (데미지, 콤보, 버프 등) 이 자리에 추가
        }

        public static int GetBrokenCount(PanelType type)
            => _brokenCountByType.TryGetValue(type, out int v) ? v : 0;
    }
}
