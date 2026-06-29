using System.Collections.Generic;
using Backend.Object.CharacterObject;
using Backend.Object.GameSystems;
using Backend.Util;
using LitMotion;
using LitMotion.Extensions;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Backend.Object.Controller
{
    public class CharacterSlotController : CachedMonobehaviour
    {
        [SerializeField] private float _moveDuration = 0.25f;

        private CharacterSlot[] _characterSlots;
        private RectTransform _playerContainer;
        private IReadOnlyList<RectTransform> _slotAnchors;

        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<CharacterSlot, MotionHandle> _moveHandles = new();


        private void Awake()
        {
            CharacterSystem.OnRotated.Subscribe(OnRotated).AddTo(_disposables);
        }

        private void Start()
        {
            // [임시] 파티 선택 UI 도입 전까지 인스펙터에 할당된 슬롯으로 테스트 세팅
            if (_characterSlots != null && _characterSlots.Length > 0)
                SetupParty(_characterSlots);
        }

        private void Update()
        {
            // [임시] 턴 진행 테스트 트리거: 키패드 0
            if (Keyboard.current != null && Keyboard.current.numpad0Key.wasPressedThisFrame)
                CharacterSystem.AdvanceTurn();
        }

        public void SetupParty(IReadOnlyList<CharacterSlot> party)
        {
            CharacterSystem.Setup(party);

            for (int i = 0; i < party.Count; i++)
                party[i].CachedTransform.localPosition = _slotAnchors[i].localPosition;

            party[0].OnSlotChanged(1, 0);
        }

        public void SetPlayerContainer(RectTransform playerContainer, IReadOnlyList<RectTransform> slotAnchors)
        {
            _playerContainer = playerContainer;
            _slotAnchors = slotAnchors;
        }

        private void OnRotated(RotationResult result)
        {
            foreach (var move in result.Moves)
            {
                if (move.Character is not CharacterSlot slot) continue;

                if (_moveHandles.TryGetValue(slot, out var handle) && handle.IsActive())
                    handle.Cancel();

                var target = _slotAnchors[move.ToSlot - 1].localPosition;
                _moveHandles[slot] = LMotion.Create(slot.CachedTransform.localPosition, target, _moveDuration)
                    .WithEase(Ease.OutQuad)
                    .BindToLocalPosition(slot.CachedTransform);
            }
        }

        private void OnSlotLongPressed(int slot)
        {
            //캐릭터 상세보기 기능
        }

        private void OnSlotClicked(int slot)
        {
            //캐릭터 스킬 사용 연동
        }

        private void OnDestroy()
        {
            if (GameStateUtil.IsQuitting) return;
            _disposables.Dispose();
        }
    }
}
