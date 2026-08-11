using R3;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// 파티 공유 HP 풀. 슬롯별 HP가 아닌 단일 풀로 파티 전체의 체력을 관리한다.
    /// MaxHp/CurrentHp 수치는 IGameSession.BootstrapPartyHp 가 주입한다.
    /// </summary>
    public static class PartySystem
    {
        public static float CurrentHp { get; private set; }
        public static float MaxHp { get; private set; }
        public static bool IsAllDefeated => CurrentHp <= 0f;

        private static readonly Subject<(float cur, float max)> _onHpChanged = new();
        public static Observable<(float cur, float max)> OnHpChanged => _onHpChanged;

        /// <summary>세션이 계산한 HP를 전투 풀에 주입한다.</summary>
        public static void Setup(float currentHp, float maxHp)
        {
            MaxHp = Mathf.Max(0f, maxHp);
            CurrentHp = Mathf.Clamp(currentHp, 0f, MaxHp);
            _onHpChanged.OnNext((CurrentHp, MaxHp));
        }

        public static void ApplyDamage(float damage)
        {
            if (IsAllDefeated) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - damage);
            _onHpChanged.OnNext((CurrentHp, MaxHp));
        }

        public static void Heal(float amount)
        {
            if (IsAllDefeated) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            _onHpChanged.OnNext((CurrentHp, MaxHp));
        }

        public static void Dispose()
        {
            CurrentHp = 0f;
            MaxHp = 0f;
        }
    }
}
