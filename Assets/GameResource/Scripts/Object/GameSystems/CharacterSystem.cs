using System;
using System.Collections.Generic;
using Backend.Util.Interface;
using Cysharp.Threading.Tasks;
using R3;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// 캐릭터 슬롯(0~Count-1)을 관리하고 턴마다 한 칸씩 로테이션한다.
    /// 슬롯 인덱스는 0-based. 외부에는 1-based(1~Count)로 노출.
    /// 활성 슬롯 수(Count)는 Setup 으로 결정되며 1~MaxSlotCount 범위를 가진다.
    /// </summary>
    public static class CharacterSystem
    {
        /// <summary>슬롯 최대 수(파티 최대 인원).</summary>
        public const int MaxSlotCount = 4;

        private static readonly ICharacter[] _slots = new ICharacter[MaxSlotCount];

        private static int _count;

        private static readonly Subject<RotationResult> _onRotated = new();

        // 로테이션 연출(뷰 애니메이션) 완료를 대기하기 위한 신호. AdvanceTurnAsync 호출 시 생성되고,
        // 뷰(CharacterSlotController)가 애니메이션을 마친 뒤 CompleteRotationVisual 로 완료시킨다.
        private static UniTaskCompletionSource _rotationVisualUcs;

        /// <summary>현재 활성 슬롯 수(파티 인원). Setup 전에는 0.</summary>
        public static int Count => _count;


        /// <summary>
        /// 로테이션 완료 시 발행. 각 캐릭터의 이전/현재 슬롯 정보를 담은 목록을 전달한다.
        /// </summary>
        public static Observable<RotationResult> OnRotated => _onRotated;

        /// <summary>
        /// 파티(1~MaxSlotCount명)를 슬롯 1번부터 순서대로 채워 활성 슬롯 수를 설정한다.
        /// 데이터만 갱신하며 뷰 초기화/연출은 호출 측에서 수행한다.
        /// </summary>
        public static void Setup(IReadOnlyList<ICharacter> characters)
        {
            if (characters == null || characters.Count < 1 || characters.Count > MaxSlotCount)
                throw new ArgumentException($"캐릭터 수는 1~{MaxSlotCount} 이어야 합니다.", nameof(characters));

            Array.Clear(_slots, 0, MaxSlotCount);
            _count = characters.Count;

            for (int i = 0; i < _count; i++)
                _slots[i] = characters[i];
        }

        /// <summary>
        /// 슬롯(1-based)에 캐릭터를 등록한다.
        /// </summary>
        public static void Register(int slot, ICharacter character)
        {
            ValidateSlot(slot);
            _slots[slot - 1] = character;
        }

        /// <summary>
        /// 슬롯(1-based)의 캐릭터를 해제한다.
        /// </summary>
        public static void Unregister(int slot)
        {
            ValidateSlot(slot);
            _slots[slot - 1] = null;
        }

        /// <summary>
        /// 슬롯(1-based)의 캐릭터를 반환한다.
        /// </summary>
        public static ICharacter GetCharacter(int slot)
        {
            ValidateSlot(slot);
            return _slots[slot - 1];
        }

        /// <summary>
        /// 한 턴 진행: 1번 슬롯 캐릭터를 맨 뒤로 보내고 나머지를 한 칸씩 앞으로 당긴다.
        /// 예) A, B, C, D → B, C, D, A
        /// 활성 슬롯이 1개 이하면 회전 없이 빈 결과를 발행한다.
        /// 로테이션 연출(뷰 애니메이션)이 끝나면 완료되는 UniTask 를 반환한다.
        /// </summary>
        public static UniTask AdvanceTurnAsync()
        {
            _rotationVisualUcs = new UniTaskCompletionSource();
            AdvanceTurnInternal();
            return _rotationVisualUcs.Task;
        }

        /// <summary>
        /// 로테이션 데이터만 갱신하고 연출 완료를 기다리지 않는다(테스트/보조 트리거용).
        /// </summary>
        public static void AdvanceTurn()
        {
            _rotationVisualUcs = null;
            AdvanceTurnInternal();
        }

        /// <summary>뷰가 로테이션 애니메이션을 마쳤을 때 호출해 대기 중인 AdvanceTurnAsync 를 완료시킨다.</summary>
        public static void CompleteRotationVisual()
        {
            _rotationVisualUcs?.TrySetResult();
        }

        private static void AdvanceTurnInternal()
        {
            if (_count <= 1)
            {
                _onRotated.OnNext(new RotationResult(Array.Empty<SlotMove>()));
                _rotationVisualUcs?.TrySetResult();
                return;
            }

            var first = _slots[0];

            var moves = new List<SlotMove>(_count);

            for (int i = 0; i < _count - 1; i++)
            {
                var character = _slots[i + 1];
                _slots[i] = character;
                if (character != null)
                {
                    character.OnSlotChanged(i + 1, i);
                    moves.Add(new SlotMove(character, i + 2, i + 1));
                }
            }

            _slots[_count - 1] = first;
            if (first != null)
            {
                first.OnSlotChanged(0, _count - 1);
                moves.Add(new SlotMove(first, 1, _count));
            }

            _onRotated.OnNext(new RotationResult(moves));
        }

        public static void Dispose()
        {
            Array.Clear(_slots, 0, MaxSlotCount);
            _count = 0;
        }

        private static void ValidateSlot(int slot)
        {
            if (slot < 1 || slot > _count)
                throw new ArgumentOutOfRangeException(nameof(slot), $"슬롯은 1~{_count} 범위여야 합니다.");
        }
    }

    public readonly struct SlotMove
    {
        public readonly ICharacter Character;

        /// <summary>이동 전 슬롯 (1-based)</summary>
        public readonly int FromSlot;

        /// <summary>이동 후 슬롯 (1-based)</summary>
        public readonly int ToSlot;

        public SlotMove(ICharacter character, int fromSlot, int toSlot)
        {
            Character = character;
            FromSlot = fromSlot;
            ToSlot = toSlot;
        }
    }

    public readonly struct RotationResult
    {
        public readonly IReadOnlyList<SlotMove> Moves;

        public RotationResult(IReadOnlyList<SlotMove> moves)
        {
            Moves = moves;
        }
    }
}
