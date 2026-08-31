using System;
using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.CharacterObject;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using Backend.Util;
using Cysharp.Threading.Tasks;
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

        private CharacterSlot _characterSlotPrefab;
        private RectTransform _playerContainer;
        private IReadOnlyList<RectTransform> _slotAnchors;

        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<CharacterSlot, MotionHandle> _moveHandles = new();


        private void Awake()
        {
            CharacterSystem.OnRotated.Subscribe(OnRotated).AddTo(_disposables);
        }

        private void Update()
        {
            // [임시] 턴 진행 테스트 트리거: 키패드 0
            if (Keyboard.current != null && Keyboard.current.numpad0Key.wasPressedThisFrame)
                CharacterSystem.AdvanceTurn();
        }

        public async UniTask SpawnPartyAsync(IReadOnlyList<UserUnitData> partyUnits){
            _characterSlotPrefab ??= await ResourceManager.LoadComponentAsync<CharacterSlot>(AddressableKeys.InGame.Get("CharacterSlot"));

            var slots = new List<CharacterSlot>(partyUnits.Count);
            foreach (var userUnit in partyUnits){
                var slot = Instantiate(_characterSlotPrefab, _playerContainer);
                slot.Initialize(TableManager.GetUnitData(userUnit.unitIds));
                slots.Add(slot);
            }

            SetupParty(slots);
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

        private void OnRotated(RotationResult result) => OnRotatedAsync(result).Forget();

        private async UniTaskVoid OnRotatedAsync(RotationResult result)
        {
            bool anyMove = false;

            foreach (var move in result.Moves)
            {
                if (move.Character is not CharacterSlot slot) continue;

                anyMove = true;

                if (_moveHandles.TryGetValue(slot, out var handle) && handle.IsActive())
                    handle.Cancel();

                var target = _slotAnchors[move.ToSlot - 1].localPosition;
                _moveHandles[slot] = LMotion.Create(slot.CachedTransform.localPosition, target, _moveDuration)
                    .WithEase(Ease.OutQuad)
                    .BindToLocalPosition(slot.CachedTransform);
            }

            try
            {
                if (anyMove)
                    await UniTask.Delay(TimeSpan.FromSeconds(_moveDuration), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            finally
            {
                // 이동 애니메이션이 끝난(또는 파괴로 취소된) 뒤 로테이션 대기를 해제한다.
                CharacterSystem.CompleteRotationVisual();
            }
        }

        private void OnSlotLongPressed(int slot)
        {
            //캐릭터 상세보기 기능
        }

        private void OnDestroy()
        {
            if (GameStateUtil.IsQuitting) return;
            _disposables.Dispose();
        }
    }
}
