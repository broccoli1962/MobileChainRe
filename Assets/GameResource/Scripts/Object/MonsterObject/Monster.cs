using System;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.GameSystems;
using Backend.Object.UI;
using Backend.Util;
using Cysharp.Threading.Tasks;
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

        private IReadOnlyList<MonsterBehaviorData> _behaviorData;
        private IReadOnlyList<MonsterActionData> _actionData;
        private Dictionary<int, IReadOnlyList<MonsterActionData>> _actionGroups;
        private float _baseDamage;
        private float _finalDamage;
        private int _currentMonsterPhaseIndex = 0;
        private int _actionIndex = 0;
        private int _currentCountDown = 0;

        public bool IsDefeated => _monsterHealthBar != null && _monsterHealthBar.IsDefeated;

        private readonly CompositeDisposable _disposables = new();

        public void InitializeMonster(MonsterData monsterData, IReadOnlyList<MonsterBehaviorData> behaviorData, Dictionary<int, IReadOnlyList<MonsterActionData>> actionGroups)
        {
            _disposables.Clear();

            _behaviorData = behaviorData;
            _actionGroups = actionGroups;
            _baseDamage = monsterData.monsterDamage;

            var layerMaxHp = new float[behaviorData.Count];
            for (int i = 0; i < behaviorData.Count; i++)
                layerMaxHp[i] = behaviorData[i].phaseHealth;

            _monsterHealthBar.Initialize(layerMaxHp, _phaseColorGradient);
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

        public void TakeDamage(float damage)
        {
            if (_monsterHealthBar == null || _monsterHealthBar.IsDefeated)
                return;

            int phaseDelta = _monsterHealthBar.ApplyDamage(damage);
            if (phaseDelta > 0 && !_monsterHealthBar.IsDefeated)
                RefreshPhase(_monsterHealthBar.CurrentLayerIndex);
        }

        private void SetMonsterActionCount(int actionCount)
        {
            _monsterActionCountText.text = actionCount.ToString();
        }

        public UniTask AdvanceTurnAsync(CancellationToken token)
        {
            //몬스터의 공격 방식 설정.
            if(_actionData == null || _actionData.Count == 0)
                return UniTask.CompletedTask;

            _currentCountDown--;
            SetMonsterActionCount(_currentCountDown);

            if(_currentCountDown <= 0){
                var action = _actionData[_actionIndex];

                //ACTION Type에 따라 처리 실제 행동 실행해야함.
                Debug.Log($"Monster Action: {action.actionType}");

                _actionIndex = (_actionIndex + 1) % _actionData.Count;
                _currentCountDown = _actionData[_actionIndex].turnDelay;
            }

            SetMonsterActionCount(_currentCountDown);
            return UniTask.CompletedTask;
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
            _disposables.Clear();
        }
    }
}