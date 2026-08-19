using System;
using System.Collections.Generic;
using System.Threading;
using Backend.AddressableKey;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using Backend.Object.UI;
using Backend.Util;
using Cysharp.Threading.Tasks;
using LitMotion;
using R3;
using R3.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.MonsterObject
{
    public class Monster : CachedMonobehaviour
    {
        [Header("Monster Info References")]
        [SerializeField] private Image _monsterSprite;
        [SerializeField] private Image _targetIcon;
        [SerializeField] private TextMeshProUGUI _monsterActionCountText;
        [SerializeField] private Image _monsterTypeIcon;
        [SerializeField] private MonsterHealthBar _monsterHealthBar;
        [SerializeField] private KeyframeColorGradient _phaseColorGradient = new();

        //IconTimer
        private const float _targetIconDuration = 3f;
        private CancellationTokenSource _targetIconTimerCts;

        //HitReaction (좌우 미세 진동)
        private const float HitShakeDuration = 0.05f;
        private const float HitShakeAmplitude = 10f;
        private const float HitShakeCycles = 3f;
        private CancellationTokenSource _hitReactionCts;
        private Vector2 _hitReactionBasePos;
        private bool _hitReactionActive;

        private IReadOnlyList<MonsterBehaviorData> _behaviorData;
        private IReadOnlyList<MonsterActionData> _actionData;
        private Dictionary<int, IReadOnlyList<MonsterActionData>> _actionGroups;
        private float _baseDamage;
        private float _finalDamage;
        private PanelType _monsterType;
        private int _currentMonsterPhaseIndex = 0;
        private int _actionIndex = 0;
        private int _currentCountDown = 0;

        public bool IsDefeated => _monsterHealthBar != null && _monsterHealthBar.IsDefeated;
        public float FinalDamage => _finalDamage;
        public PanelType MonsterType => _monsterType;

        private readonly CompositeDisposable _disposables = new();

        public void InitializeMonster(
            MonsterData monsterData,
            IReadOnlyList<MonsterBehaviorData> behaviorData,
            Dictionary<int, IReadOnlyList<MonsterActionData>> actionGroups,
            float hpScale = 1f,
            float atkScale = 1f)
        {
            _disposables.Clear();

            _behaviorData = behaviorData;
            _actionGroups = actionGroups;
            _baseDamage = monsterData.monsterDamage * atkScale;
            _monsterType = monsterData.monsterType;
            _currentMonsterPhaseIndex = 0;
            _actionIndex = 0;

            var layerMaxHp = new float[behaviorData.Count];
            for (int i = 0; i < behaviorData.Count; i++)
                layerMaxHp[i] = behaviorData[i].phaseHealth * hpScale;

            _monsterHealthBar.Initialize(layerMaxHp, _phaseColorGradient);
            ApplyMonsterSprite(monsterData.monsterId);
            RefreshPhase(_currentMonsterPhaseIndex);

            _monsterSprite.OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    if (!MonsterSystem.TrySelectTarget(this)) return;
                    StartTargetIconTimer();
                })
                .AddTo(_disposables);

            MonsterSystem.CurrentTarget.Subscribe(target =>
            {
                if (target != this)
                {
                    CancelTargetIconTimer();
                    _targetIcon.gameObject.SetActive(false);
                }
            }).AddTo(_disposables);
        }

        private void CancelTargetIconTimer()
        {
            _targetIconTimerCts?.Cancel();
            _targetIconTimerCts?.Dispose();
            _targetIconTimerCts = null;
        }

        private void StartTargetIconTimer()
        {
            CancelTargetIconTimer();
            _targetIcon.gameObject.SetActive(true);

            _targetIconTimerCts = new CancellationTokenSource();
            HideTargetAfterDelay(_targetIconTimerCts.Token).Forget();
        }

        private async UniTaskVoid HideTargetAfterDelay(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_targetIconDuration), cancellationToken: token);
                _targetIcon.gameObject.SetActive(false);
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>데미지를 적용한다. 반환값은 이 호출로 전환된 레이어 수(0이면 레이어 전환 없음).</summary>
        public int TakeDamage(float damage)
        {
            if (_monsterHealthBar == null || _monsterHealthBar.IsDefeated)
                return 0;

            int phaseDelta = _monsterHealthBar.ApplyDamage(damage);
            if (phaseDelta > 0 && !_monsterHealthBar.IsDefeated)
                RefreshPhase(_monsterHealthBar.CurrentLayerIndex);

            return phaseDelta;
        }

        /// <summary>
        /// 타격 1회당 피격 연출 훅. BattleSystem이 패널 1개당 1회 호출한다. 좌우로 살짝 진동한다.
        /// 연속 타격으로 겹쳐 호출되면 이전 흔들림을 취소하고 고정된 기준 위치에서 재시작한다
        /// (재시작 방식). 그래야 여러 흔들림이 동시에 rectTransform을 건드려도 위치가 튀지 않는다.
        /// </summary>
        public void PlayHitReaction(CancellationToken token)
        {
            if (_monsterSprite == null) return;

            var rect = _monsterSprite.rectTransform;

            if (!_hitReactionActive)
            {
                _hitReactionBasePos = rect.anchoredPosition;
                _hitReactionActive = true;
            }

            _hitReactionCts?.Cancel();
            _hitReactionCts?.Dispose();
            _hitReactionCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            RunHitShakeAsync(rect, _hitReactionCts.Token).Forget();
        }

        private async UniTaskVoid RunHitShakeAsync(RectTransform rect, CancellationToken token)
        {
            try
            {
                await LMotion.Create(0f, 1f, HitShakeDuration)
                    .Bind(t =>
                    {
                        float damp = 1f - t;
                        float offset = Mathf.Sin(t * Mathf.PI * 2f * HitShakeCycles) * HitShakeAmplitude * damp;
                        rect.anchoredPosition = new Vector2(_hitReactionBasePos.x + offset, _hitReactionBasePos.y);
                    })
                    .ToUniTask(token);

                rect.anchoredPosition = _hitReactionBasePos;
                _hitReactionActive = false;
            }
            catch (OperationCanceledException)
            {
                // 다음 히트가 이어받아 재생 중. 기준 위치 복원은 마지막 히트가 담당한다.
            }
        }

        private void ApplyMonsterSprite(int monsterId)
        {
            if (_monsterSprite == null) return;

            string address = AddressableKeys.InGame.Get($"Monster_{monsterId}");
            if (string.IsNullOrEmpty(address)) return;

            var sprite = ResourceManager.LoadResource<Sprite>(address);
            if (sprite != null)
                _monsterSprite.sprite = sprite;
        }

        private void SetMonsterActionCount(int actionCount)
        {
            _monsterActionCountText.text = actionCount.ToString();
        }

        public async UniTask AdvanceTurnAsync(CancellationToken token)
        {
            //몬스터의 공격 방식 설정.
            if(_actionData == null || _actionData.Count == 0)
                return;

            _currentCountDown--;
            SetMonsterActionCount(_currentCountDown);

            if(_currentCountDown <= 0){
                var action = _actionData[_actionIndex];

                await MonsterAttackSystem.ExecuteAsync(this, action, token);

                _actionIndex = (_actionIndex + 1) % _actionData.Count;
                _currentCountDown = _actionData[_actionIndex].turnDelay;
            }

            SetMonsterActionCount(_currentCountDown);
        }

        private void RefreshPhase(int phaseIndex)
        {
            if (_behaviorData == null || phaseIndex >= _behaviorData.Count)
                return;

            _currentMonsterPhaseIndex = phaseIndex;
            _finalDamage = _baseDamage * _behaviorData[phaseIndex].phaseDamage;

            var actionGroupId = _behaviorData[_currentMonsterPhaseIndex].actionGroupId;

            if(_actionGroups != null && _actionGroups.TryGetValue(actionGroupId, out var actions)){
                _actionData = actions;
            }else{
                _actionData = null;
            }

            _actionIndex = 0;
            _currentCountDown = (_actionData != null && _actionData.Count > 0) ? _actionData[_actionIndex].turnDelay : int.MaxValue;

            SetMonsterActionCount(_currentCountDown);
        }

        private void OnDisable()
        {
            CancelTargetIconTimer();

            _hitReactionCts?.Cancel();
            _hitReactionCts?.Dispose();
            _hitReactionCts = null;
            _hitReactionActive = false;

            _disposables.Clear();
        }
    }
}