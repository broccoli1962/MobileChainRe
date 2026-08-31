using System.Collections.Generic;
using System.Threading;
using Backend.Object.Controller;
using Cysharp.Threading.Tasks;
using R3;

namespace Backend.Object.Management
{
    /// <summary>층·골드 등 진행 HUD용 스냅샷. Classic/Quest 모두 TryGetProgressHud=true.</summary>
    public readonly struct SessionProgressHud
    {
        public int Floor { get; }
        public int MaxFloor { get; }
        public int Gold { get; }
        public Observable<(int floor, int gold)> OnChanged { get; }

        public SessionProgressHud(int floor, int maxFloor, int gold, Observable<(int floor, int gold)> onChanged)
        {
            Floor = floor;
            MaxFloor = maxFloor;
            Gold = gold;
            OnChanged = onChanged;
        }
    }

    /// <summary>
    /// 활성 런(Classic/Quest)의 생명주기 계약. 데이터는 구현체 인스턴스가 소유한다.
    /// HP: 전투 중 권위는 PartySystem, 중도 이어하기용 스냅샷은 CurrentHp/MaxHp.
    /// BootstrapPartyHp = 세션→풀, CaptureHp = 풀→세션.
    /// </summary>
    public interface IGameSession
    {
        SessionMode Mode { get; }
        IReadOnlyList<UserUnitData> Party { get; }

        /// <summary>이어하기용 현재 HP 스냅샷.</summary>
        float CurrentHp { get; }

        /// <summary>이어하기용 최대 HP 스냅샷.</summary>
        float MaxHp { get; }

        /// <summary>파티 확정. Classic 은 런 시작, Quest 는 파티만 저장.</summary>
        void BindParty(IReadOnlyList<UserUnitData> party);

        /// <summary>세션 HP 스냅샷을 PartySystem 에 주입.</summary>
        void BootstrapPartyHp();

        /// <summary>PartySystem 현재 HP를 세션 스냅샷으로 복사. Dispose 전에 호출.</summary>
        void CaptureHp();

        UniTask InitMonstersAsync(MonsterController controller);

        /// <summary>GameScene 첫 층/스테이지 스폰. 컨트롤러는 InitMonstersAsync에서 캐시한다.</summary>
        void SpawnInitialFloor();

        /// <summary>층 클리어 정산·다음 층 스폰. 턴 루프가 완료될 때까지 대기한다.</summary>
        UniTask AdvanceFloorAsync(CancellationToken token);

        /// <summary>GameManager StartGameplay 직후. 터미널 구독 등.</summary>
        void OnGameplayStarted();

        /// <summary>GameManager EndGameplay. 구독 해제.</summary>
        void OnGameplayEnded();

        /// <summary>세션 내부 상태 초기화. ActiveSession.Clear 가 호출.</summary>
        void End();

        bool TryGetProgressHud(out SessionProgressHud hud);
    }
}
